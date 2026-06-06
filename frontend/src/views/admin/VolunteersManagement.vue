<script setup>
import { ref, computed, onMounted } from 'vue'
import { useRoute } from 'vue-router'
import { volunteerApi, zonesApi, adminApi } from '@/api'
import AppPagination from '@/components/AppPagination.vue'

const route = useRoute()

function waLink(phone) {
  if (!phone) return null
  return 'https://wa.me/' + phone.replace(/[^\d]/g, '')
}

// ─── DATA ────────────────────────────────────────────────────────────────────
const volunteers = ref([])
const zones = ref([])
const liveStatus = ref(null)
const loading = ref(true)
const activeTab = ref('list') // list | create | zones | emergency

// ─── MODALS ───────────────────────────────────────────────────────────────────
const showTaskModal = ref(false)
const showZoneModal = ref(false)
const showReportModal = ref(false)
const selectedVol = ref(null)
const report = ref(null)
const reportLoading = ref(false)

// ─── FORMS ───────────────────────────────────────────────────────────────────
const createForm = ref({ userId: '', skills: '', shiftStart: '', shiftEnd: '', assignedArea: '', emergencyPhone: '', zoneId: '' })
const taskForm = ref({ volunteerId: '', title: '', description: '', category: 'General', dueAt: '', isEmergency: false, notes: '' })
const zoneForm = ref({ name: '', zoneCode: '', description: '', location: '', maxVolunteers: 20 })
const emergencyForm = ref({ title: '', message: '', sendWhatsApp: false })

const allUsers = ref([])
const saving = ref(false)
const msg = ref({ type: '', text: '' })

const statusColors = {
  Available: 'bg-emerald-900/50 text-amber-700 border border-emerald-700',
  Busy: 'bg-yellow-900/50 text-yellow-400 border border-yellow-700',
  Offline: 'bg-slate-800 text-slate-400 border border-slate-600',
  EmergencyAssigned: 'bg-red-900/50 text-red-400 border border-red-700',
}

const taskCategories = ['AirportSupport', 'BusCoordination', 'CrowdManagement', 'FoodDistribution', 'HotelSupport', 'EmergencyResponse', 'General']

// ─── COMPUTED ─────────────────────────────────────────────────────────────────
const statusFilter = ref('')
const volSearch = ref('')
const volPage = ref(1)
const VOL_PAGE_SIZE = 20

const available = computed(() => volunteers.value.filter(v => v.status === 'Available').length)
const busy = computed(() => volunteers.value.filter(v => v.status === 'Busy').length)
const offline = computed(() => volunteers.value.filter(v => v.status === 'Offline').length)
const emergency = computed(() => volunteers.value.filter(v => v.status === 'EmergencyAssigned').length)

const filteredVolunteers = computed(() => {
  let list = volunteers.value
  if (statusFilter.value) list = list.filter(v => v.status === statusFilter.value)
  if (volSearch.value) {
    const q = volSearch.value.toLowerCase()
    list = list.filter(v => v.fullName?.toLowerCase().includes(q) || v.email?.toLowerCase().includes(q))
  }
  return list
})
const pagedVolunteers = computed(() => {
  const start = (volPage.value - 1) * VOL_PAGE_SIZE
  return filteredVolunteers.value.slice(start, start + VOL_PAGE_SIZE)
})

// ─── LOAD ─────────────────────────────────────────────────────────────────────
async function load() {
  loading.value = true
  try {
    const [vRes, zRes, uRes, lRes] = await Promise.all([
      volunteerApi.getAll(),
      zonesApi.getAll(),
      adminApi.getUsers(),
      adminApi.getVolunteerLiveStatus(),
    ])
    volunteers.value = vRes.data.data?.items || vRes.data.data || []
    zones.value = zRes.data.data?.items || zRes.data.data || []
    allUsers.value = (uRes.data.data?.items || uRes.data.data || []).filter(u => !volunteers.value.some(v => v.userId === u.id))
    liveStatus.value = lRes.data.data || null
  } finally {
    loading.value = false
  }
}

onMounted(() => {
  if (route.query.status) statusFilter.value = route.query.status
  load()
})

// ─── ACTIONS ──────────────────────────────────────────────────────────────────
function notify(type, text) {
  msg.value = { type, text }
  setTimeout(() => (msg.value = { type: '', text: '' }), 4000)
}

async function createVolunteer() {
  if (!createForm.value.userId) return notify('error', 'Please select a user.')
  saving.value = true
  try {
    await volunteerApi.createByAdmin({
      userId: createForm.value.userId,
      skills: createForm.value.skills,
      shiftStart: createForm.value.shiftStart || null,
      shiftEnd: createForm.value.shiftEnd || null,
      assignedArea: createForm.value.assignedArea || null,
      emergencyPhone: createForm.value.emergencyPhone || null,
      zoneId: createForm.value.zoneId || null,
    })
    notify('success', 'Volunteer profile created successfully.')
    createForm.value = { userId: '', skills: '', shiftStart: '', shiftEnd: '', assignedArea: '', emergencyPhone: '', zoneId: '' }
    activeTab.value = 'list'
    await load()
  } catch (e) {
    notify('error', e.response?.data?.message || 'Failed to create volunteer.')
  } finally {
    saving.value = false
  }
}

function openTaskModal(vol) {
  selectedVol.value = vol
  taskForm.value = { volunteerId: vol.id, title: '', description: '', category: 'General', dueAt: '', isEmergency: false, notes: '' }
  showTaskModal.value = true
}

async function assignTask() {
  saving.value = true
  try {
    await volunteerApi.assignTask({
      volunteerId: taskForm.value.volunteerId,
      title: taskForm.value.title,
      description: taskForm.value.description || null,
      category: taskForm.value.category,
      dueAt: taskForm.value.dueAt || null,
      isEmergency: taskForm.value.isEmergency,
      notes: taskForm.value.notes || null,
    })
    notify('success', 'Task assigned.')
    showTaskModal.value = false
    await load()
  } catch (e) {
    notify('error', e.response?.data?.message || 'Failed to assign task.')
  } finally {
    saving.value = false
  }
}

function openZoneModal(vol) {
  selectedVol.value = vol
  showZoneModal.value = true
}

async function assignZone(zoneId) {
  if (!selectedVol.value) return
  saving.value = true
  try {
    await volunteerApi.assignZone(selectedVol.value.id, zoneId)
    notify('success', 'Zone assigned.')
    showZoneModal.value = false
    await load()
  } catch (e) {
    notify('error', e.response?.data?.message || 'Failed to assign zone.')
  } finally {
    saving.value = false
  }
}

async function openReport(vol) {
  selectedVol.value = vol
  showReportModal.value = true
  reportLoading.value = true
  report.value = null
  try {
    const res = await volunteerApi.getReport(vol.id)
    report.value = res.data.data
  } finally {
    reportLoading.value = false
  }
}

async function createZone() {
  saving.value = true
  try {
    await zonesApi.create({
      name: zoneForm.value.name,
      zoneCode: zoneForm.value.zoneCode,
      description: zoneForm.value.description || '',
      location: zoneForm.value.location || null,
      maxVolunteers: parseInt(zoneForm.value.maxVolunteers),
      latitude: null,
      longitude: null,
    })
    notify('success', 'Zone created.')
    zoneForm.value = { name: '', zoneCode: '', description: '', location: '', maxVolunteers: 20 }
    await load()
  } catch (e) {
    notify('error', e.response?.data?.message || 'Failed to create zone.')
  } finally {
    saving.value = false
  }
}

async function deleteZone(id) {
  if (!confirm('Delete this zone?')) return
  try {
    await zonesApi.delete(id)
    notify('success', 'Zone deleted.')
    await load()
  } catch (e) {
    notify('error', 'Failed to delete zone.')
  }
}

async function sendEmergency() {
  if (!emergencyForm.value.title || !emergencyForm.value.message) return notify('error', 'Fill in title and message.')
  saving.value = true
  try {
    const res = await adminApi.emergencyBroadcast(emergencyForm.value)
    notify('success', res.data.message || 'Emergency alert sent.')
    emergencyForm.value = { title: '', message: '', sendWhatsApp: false }
  } catch (e) {
    notify('error', e.response?.data?.message || 'Failed to send alert.')
  } finally {
    saving.value = false
  }
}

function zoneName(zoneId) {
  return zones.value.find(z => z.id === zoneId)?.name || '—'
}

function formatHour(h) {
  return h ? new Date(h).toLocaleString() : '—'
}
</script>

<template>
  <div class="space-y-6">
    <!-- Header -->
    <div class="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
      <div>
        <h1 class="text-2xl font-bold text-gray-900">Volunteers Management</h1>
        <p class="text-gray-700 text-sm mt-1">Assign zones, tasks, track status and performance</p>
      </div>
      <button @click="activeTab = 'emergency'" class="btn-danger text-sm">
        🚨 Emergency Broadcast
      </button>
    </div>

    <!-- Alert -->
    <div v-if="msg.text" :class="msg.type === 'success' ? 'bg-green-100 border-green-300 text-green-700' : 'bg-red-100 border-red-300 text-red-600'"
      class="border rounded-lg px-4 py-3 text-sm">
      {{ msg.text }}
    </div>

    <!-- Stats -->
    <div class="grid grid-cols-2 sm:grid-cols-4 gap-4">
      <div class="card text-center">
        <div class="text-3xl font-bold text-amber-700">{{ available }}</div>
        <div class="text-gray-700 text-sm mt-1">Available</div>
      </div>
      <div class="card text-center">
        <div class="text-3xl font-bold text-yellow-400">{{ busy }}</div>
        <div class="text-gray-700 text-sm mt-1">Busy</div>
      </div>
      <div class="card text-center">
        <div class="text-3xl font-bold text-red-400">{{ emergency }}</div>
        <div class="text-gray-700 text-sm mt-1">Emergency</div>
      </div>
      <div class="card text-center">
        <div class="text-3xl font-bold text-slate-400">{{ offline }}</div>
        <div class="text-gray-700 text-sm mt-1">Offline</div>
      </div>
    </div>

    <!-- Tabs -->
    <div class="flex gap-2 flex-wrap">
      <button v-for="t in ['list', 'create', 'zones', 'emergency']" :key="t"
        @click="activeTab = t"
        :class="activeTab === t ? 'bg-amber-600 text-white' : 'bg-gray-100 text-gray-600 hover:text-gray-900'"
        class="px-4 py-2 rounded-lg text-sm font-medium capitalize transition-colors">
        {{ t === 'list' ? '👥 Volunteers' : t === 'create' ? '➕ Add Volunteer' : t === 'zones' ? '🗺 Zones' : '🚨 Emergency' }}
      </button>
    </div>

    <!-- TAB: Volunteer List -->
    <div v-if="activeTab === 'list'">
      <div v-if="loading" class="card text-center text-gray-700 py-12">Loading...</div>
      <div v-else-if="!volunteers.length" class="card text-center text-gray-700 py-12">
        No volunteers yet. Add volunteers from the "Add Volunteer" tab.
      </div>
      <div v-else class="space-y-3">
        <!-- Filters row -->
        <div class="flex flex-wrap items-center gap-3">
          <input v-model="volSearch" @input="volPage = 1" type="text" class="form-input w-56"
            placeholder="Search by name or email..." />
          <div class="flex items-center gap-2 flex-wrap">
            <button v-for="s in ['', 'Available', 'Busy', 'Offline', 'EmergencyAssigned']" :key="s"
              @click="statusFilter = s; volPage = 1"
              :class="statusFilter === s ? 'bg-amber-600 text-white border-primary-600' : 'bg-gray-100 text-gray-700 border-gray-200 hover:border-primary-400 hover:text-gray-900'"
              class="px-3 py-1.5 rounded-lg text-xs font-medium border transition-colors">
              {{ s === '' ? 'All' : s === 'EmergencyAssigned' ? '🚨 Emergency' : s === 'Available' ? '🟢 ' + s : s === 'Busy' ? '🔵 ' + s : '⚫ ' + s }}
              <span class="ml-1 opacity-70">
                {{ s === '' ? volunteers.length : volunteers.filter(v => v.status === s).length }}
              </span>
            </button>
          </div>
          <span class="text-gray-700 text-sm ml-auto">{{ filteredVolunteers.length }} volunteers</span>
        </div>

        <div v-for="vol in pagedVolunteers" :key="vol.id"
          class="card flex flex-col sm:flex-row sm:items-center gap-4">
          <!-- Info -->
          <div class="flex-1">
            <div class="flex items-center gap-3 flex-wrap">
              <div class="w-10 h-10 rounded-full bg-amber-700 flex items-center justify-center text-gray-900 font-bold text-sm shrink-0">
                {{ vol.fullName?.charAt(0) }}
              </div>
              <div>
                <p class="text-gray-900 font-semibold">{{ vol.fullName }}</p>
                <div class="flex items-center gap-2 flex-wrap mt-0.5">
                  <span class="text-gray-600 text-xs">{{ vol.email }}</span>
                  <a v-if="vol.whatsApp" :href="waLink(vol.whatsApp)" target="_blank" rel="noopener"
                     class="inline-flex items-center gap-1 text-xs px-2 py-0.5 rounded-full bg-emerald-900/40 border border-emerald-700/50 text-amber-700 hover:bg-emerald-800/50 transition-colors">
                    💬 WhatsApp
                  </a>
                  <a v-else-if="vol.phone" :href="waLink(vol.phone)" target="_blank" rel="noopener"
                     class="inline-flex items-center gap-1 text-xs px-2 py-0.5 rounded-full bg-slate-800 border border-slate-700 text-slate-400 hover:border-emerald-700 hover:text-amber-700 transition-colors">
                    📞 {{ vol.phone }}
                  </a>
                </div>
              </div>
              <span :class="statusColors[vol.status] || 'bg-slate-800 text-slate-400'" class="px-2 py-0.5 rounded-full text-xs font-medium">
                {{ vol.status }}
              </span>
              <span v-if="vol.isCheckedIn" class="bg-emerald-900/40 text-amber-700 border border-emerald-700 px-2 py-0.5 rounded-full text-xs">
                ✓ Checked In
              </span>
            </div>
            <div class="mt-2 flex flex-wrap gap-4 text-xs text-gray-600">
              <span>🗺 Zone: <span class="text-gray-600">{{ vol.zoneName || '—' }}</span></span>
              <span>🔧 Skills: <span class="text-gray-600">{{ vol.skills || '—' }}</span></span>
              <span>📋 Tasks Done: <span class="text-gray-600">{{ vol.totalTasksCompleted }}</span></span>
              <span>📍 Area: <span class="text-gray-600">{{ vol.assignedArea || '—' }}</span></span>
            </div>
          </div>
          <!-- Actions -->
          <div class="flex flex-wrap gap-2 shrink-0">
            <button @click="openZoneModal(vol)" class="btn-sm-outline">🗺 Zone</button>
            <button @click="openTaskModal(vol)" class="btn-sm-outline">📋 Task</button>
            <button @click="openReport(vol)" class="btn-sm-outline">📊 Report</button>
          </div>
        </div>

        <AppPagination :page="volPage" :total="filteredVolunteers.length" :pageSize="VOL_PAGE_SIZE"
          @update:page="p => { volPage = p }" />
      </div>
    </div>

    <!-- TAB: Create Volunteer -->
    <div v-if="activeTab === 'create'">
      <div class="card max-w-2xl">
        <h2 class="text-lg font-semibold text-gray-900 mb-6">Create Volunteer Profile</h2>
        <p class="text-gray-700 text-sm mb-4">Select an existing registered user and assign them the volunteer role.</p>
        <div class="space-y-4">
          <div>
            <label class="form-label">Select User *</label>
            <select v-model="createForm.userId" class="form-input">
              <option value="">— Choose a user —</option>
              <option v-for="u in allUsers" :key="u.id" :value="u.id">{{ u.fullName }} ({{ u.email }})</option>
            </select>
          </div>
          <div class="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <div>
              <label class="form-label">Skills / Expertise</label>
              <input v-model="createForm.skills" class="form-input" placeholder="e.g. First Aid, Crowd Management" />
            </div>
            <div>
              <label class="form-label">Emergency Phone</label>
              <input v-model="createForm.emergencyPhone" class="form-input" placeholder="+92..." />
            </div>
            <div>
              <label class="form-label">Shift Start</label>
              <input v-model="createForm.shiftStart" class="form-input" placeholder="e.g. 06:00" />
            </div>
            <div>
              <label class="form-label">Shift End</label>
              <input v-model="createForm.shiftEnd" class="form-input" placeholder="e.g. 14:00" />
            </div>
            <div>
              <label class="form-label">Assigned Area</label>
              <input v-model="createForm.assignedArea" class="form-input" placeholder="e.g. Gate 3 / Hotel Lobby" />
            </div>
            <div>
              <label class="form-label">Zone</label>
              <select v-model="createForm.zoneId" class="form-input">
                <option value="">— No Zone —</option>
                <option v-for="z in zones" :key="z.id" :value="z.id">{{ z.name }} ({{ z.zoneCode }})</option>
              </select>
            </div>
          </div>
          <div class="flex gap-3 pt-2">
            <button @click="createVolunteer" :disabled="saving" class="btn-primary">
              {{ saving ? 'Creating...' : 'Create Volunteer Profile' }}
            </button>
            <button @click="activeTab = 'list'" class="btn-outline">Cancel</button>
          </div>
        </div>
      </div>
    </div>

    <!-- TAB: Zones Management -->
    <div v-if="activeTab === 'zones'">
      <div class="grid grid-cols-1 lg:grid-cols-2 gap-6">
        <!-- Create Zone -->
        <div class="card">
          <h2 class="text-lg font-semibold text-gray-900 mb-4">Create New Zone</h2>
          <div class="space-y-3">
            <div class="grid grid-cols-2 gap-3">
              <div>
                <label class="form-label">Zone Name *</label>
                <input v-model="zoneForm.name" class="form-input" placeholder="e.g. Zone A - Main Gate" />
              </div>
              <div>
                <label class="form-label">Zone Code *</label>
                <input v-model="zoneForm.zoneCode" class="form-input" placeholder="e.g. ZA-01" />
              </div>
            </div>
            <div>
              <label class="form-label">Description</label>
              <input v-model="zoneForm.description" class="form-input" placeholder="Brief description of this zone" />
            </div>
            <div class="grid grid-cols-2 gap-3">
              <div>
                <label class="form-label">Location</label>
                <input v-model="zoneForm.location" class="form-input" placeholder="e.g. Near Gate 3" />
              </div>
              <div>
                <label class="form-label">Max Volunteers</label>
                <input v-model.number="zoneForm.maxVolunteers" type="number" class="form-input" min="1" />
              </div>
            </div>
            <button @click="createZone" :disabled="saving" class="btn-primary w-full">
              {{ saving ? 'Creating...' : 'Create Zone' }}
            </button>
          </div>
        </div>

        <!-- Zones List -->
        <div class="card">
          <h2 class="text-lg font-semibold text-gray-900 mb-4">All Zones ({{ zones.length }})</h2>
          <div v-if="!zones.length" class="text-gray-700 text-sm text-center py-8">No zones yet.</div>
          <div v-else class="space-y-2 max-h-96 overflow-y-auto pr-1">
            <div v-for="z in zones" :key="z.id" class="flex items-center justify-between bg-gray-100 rounded-lg px-4 py-3">
              <div>
                <p class="text-gray-900 font-medium">{{ z.name }}</p>
                <p class="text-gray-600 text-xs">{{ z.zoneCode }} · Max: {{ z.maxVolunteers }} · Assigned: {{ z.assignedVolunteers }}</p>
              </div>
              <button @click="deleteZone(z.id)" class="text-red-400 hover:text-red-300 text-sm px-2 py-1">🗑</button>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- TAB: Emergency Broadcast -->
    <div v-if="activeTab === 'emergency'">
      <div class="card max-w-2xl border-red-800/50">
        <div class="flex items-center gap-3 mb-6">
          <div class="w-12 h-12 rounded-full bg-red-900/50 flex items-center justify-center text-2xl">🚨</div>
          <div>
            <h2 class="text-lg font-semibold text-gray-900">Emergency Broadcast</h2>
            <p class="text-red-400 text-sm">This will notify ALL active users immediately</p>
          </div>
        </div>

        <!-- Quick Templates -->
        <div class="mb-4">
          <p class="text-gray-600 text-xs mb-2 uppercase tracking-wide">Quick Templates</p>
          <div class="flex flex-wrap gap-2">
            <button @click="emergencyForm = { title: 'Medical Emergency', message: 'A medical emergency is in progress. All medical volunteers please report to the first aid center immediately.', sendWhatsApp: false }"
              class="btn-sm-outline border-red-700 text-red-400">🏥 Medical</button>
            <button @click="emergencyForm = { title: 'Bus Departure Alert', message: 'All pilgrims please report to the bus parking area immediately. Buses departing in 15 minutes.', sendWhatsApp: false }"
              class="btn-sm-outline border-yellow-700 text-yellow-400">🚌 Bus Departure</button>
            <button @click="emergencyForm = { title: 'Crowd Management Alert', message: 'High crowd density detected. All volunteers please report to assigned crowd management zones immediately.', sendWhatsApp: false }"
              class="btn-sm-outline border-orange-700 text-orange-400">👥 Crowd Alert</button>
            <button @click="emergencyForm = { title: 'Lost Pilgrim Found', message: 'A lost pilgrim has been found at the main gate. If you are missing a family member, please contact the information desk.', sendWhatsApp: false }"
              class="btn-sm-outline">🔍 Lost Pilgrim</button>
          </div>
        </div>

        <div class="space-y-4">
          <div>
            <label class="form-label">Alert Title *</label>
            <input v-model="emergencyForm.title" class="form-input" placeholder="e.g. Medical Emergency at Gate 2" />
          </div>
          <div>
            <label class="form-label">Message *</label>
            <textarea v-model="emergencyForm.message" class="form-input" rows="4"
              placeholder="Describe the emergency situation and what actions are required..."></textarea>
          </div>
          <div class="flex items-center gap-3 p-3 bg-gray-100 rounded-lg">
            <input type="checkbox" v-model="emergencyForm.sendWhatsApp" id="sendWA" class="w-4 h-4 accent-primary-500" />
            <label for="sendWA" class="text-gray-600 text-sm cursor-pointer">
              Also send WhatsApp notification to all users with WhatsApp number
              <span class="text-slate-500">(requires Twilio credentials)</span>
            </label>
          </div>
          <button @click="sendEmergency" :disabled="saving"
            class="w-full py-3 bg-red-700 hover:bg-red-600 text-gray-900 font-semibold rounded-lg transition-colors disabled:opacity-50">
            {{ saving ? 'Sending...' : '🚨 Send Emergency Alert to All Users' }}
          </button>
        </div>
      </div>
    </div>

    <!-- MODAL: Assign Task -->
    <Teleport to="body">
      <div v-if="showTaskModal" class="fixed inset-0 z-50 flex items-center justify-center bg-black/70 p-4">
        <div class="bg-white border border-gray-200 rounded-xl w-full max-w-md p-6">
          <div class="flex items-center justify-between mb-5">
            <h3 class="text-gray-900 font-semibold text-lg">Assign Task</h3>
            <button @click="showTaskModal = false" class="text-gray-600 hover:text-gray-900 text-xl leading-none">×</button>
          </div>
          <p class="text-gray-700 text-sm mb-4">Volunteer: <span class="text-gray-900 font-medium">{{ selectedVol?.fullName }}</span></p>
          <div class="space-y-3">
            <div>
              <label class="form-label">Task Title *</label>
              <input v-model="taskForm.title" class="form-input" placeholder="e.g. Guide pilgrims at Gate 3" />
            </div>
            <div>
              <label class="form-label">Category</label>
              <select v-model="taskForm.category" class="form-input">
                <option v-for="c in taskCategories" :key="c" :value="c">{{ c }}</option>
              </select>
            </div>
            <div>
              <label class="form-label">Description</label>
              <textarea v-model="taskForm.description" class="form-input" rows="2" placeholder="Optional details..."></textarea>
            </div>
            <div>
              <label class="form-label">Due Date/Time</label>
              <input v-model="taskForm.dueAt" type="datetime-local" class="form-input" />
            </div>
            <div>
              <label class="form-label">Notes</label>
              <input v-model="taskForm.notes" class="form-input" placeholder="Internal notes..." />
            </div>
            <div class="flex items-center gap-2">
              <input type="checkbox" v-model="taskForm.isEmergency" id="isEmerg" class="w-4 h-4 accent-red-500" />
              <label for="isEmerg" class="text-sm text-red-400 cursor-pointer font-medium">Emergency Task (volunteer status → Emergency Assigned)</label>
            </div>
          </div>
          <div class="flex gap-3 mt-5">
            <button @click="assignTask" :disabled="saving" class="btn-primary flex-1">
              {{ saving ? 'Assigning...' : 'Assign Task' }}
            </button>
            <button @click="showTaskModal = false" class="btn-outline flex-1">Cancel</button>
          </div>
        </div>
      </div>
    </Teleport>

    <!-- MODAL: Assign Zone -->
    <Teleport to="body">
      <div v-if="showZoneModal" class="fixed inset-0 z-50 flex items-center justify-center bg-black/70 p-4">
        <div class="bg-white border border-gray-200 rounded-xl w-full max-w-sm p-6">
          <div class="flex items-center justify-between mb-5">
            <h3 class="text-gray-900 font-semibold text-lg">Assign Zone</h3>
            <button @click="showZoneModal = false" class="text-gray-600 hover:text-gray-900 text-xl leading-none">×</button>
          </div>
          <p class="text-gray-700 text-sm mb-4">Volunteer: <span class="text-gray-900 font-medium">{{ selectedVol?.fullName }}</span></p>
          <div v-if="!zones.length" class="text-gray-700 text-sm text-center py-6">
            No zones available. Create zones first from the Zones tab.
          </div>
          <div v-else class="space-y-2">
            <button v-for="z in zones" :key="z.id" @click="assignZone(z.id)"
              :disabled="saving"
              class="w-full text-left flex items-center justify-between p-3 bg-gray-100 hover:bg-gray-200 rounded-lg transition-colors disabled:opacity-50">
              <div>
                <p class="text-gray-900 font-medium">{{ z.name }}</p>
                <p class="text-gray-600 text-xs">{{ z.zoneCode }} · Capacity: {{ z.assignedVolunteers }}/{{ z.maxVolunteers }}</p>
              </div>
              <span v-if="selectedVol?.zoneId === z.id" class="text-amber-700 text-xs">✓ Current</span>
            </button>
          </div>
          <button @click="showZoneModal = false" class="btn-outline w-full mt-4">Cancel</button>
        </div>
      </div>
    </Teleport>

    <!-- MODAL: Performance Report -->
    <Teleport to="body">
      <div v-if="showReportModal" class="fixed inset-0 z-50 flex items-center justify-center bg-black/70 p-4">
        <div class="bg-white border border-gray-200 rounded-xl w-full max-w-md p-6">
          <div class="flex items-center justify-between mb-5">
            <h3 class="text-gray-900 font-semibold text-lg">Performance Report</h3>
            <button @click="showReportModal = false" class="text-gray-600 hover:text-gray-900 text-xl leading-none">×</button>
          </div>
          <div v-if="reportLoading" class="text-center text-gray-700 py-8">Loading report...</div>
          <div v-else-if="report" class="space-y-3">
            <div class="text-center mb-4">
              <div class="w-14 h-14 rounded-full bg-amber-700 flex items-center justify-center text-gray-900 font-bold text-xl mx-auto mb-2">
                {{ report.volunteerName?.charAt(0) }}
              </div>
              <p class="text-gray-900 font-semibold">{{ report.volunteerName }}</p>
              <span :class="statusColors[report.status] || 'bg-slate-800 text-slate-400'" class="px-3 py-0.5 rounded-full text-xs font-medium">
                {{ report.status }}
              </span>
            </div>
            <div class="grid grid-cols-2 gap-3">
              <div class="bg-gray-100 rounded-lg p-3 text-center">
                <div class="text-2xl font-bold text-gray-900">{{ report.totalTasks }}</div>
                <div class="text-gray-600 text-xs mt-1">Total Tasks</div>
              </div>
              <div class="bg-gray-100 rounded-lg p-3 text-center">
                <div class="text-2xl font-bold text-emerald-600">{{ report.completedTasks }}</div>
                <div class="text-gray-600 text-xs mt-1">Completed</div>
              </div>
              <div class="bg-gray-100 rounded-lg p-3 text-center">
                <div class="text-2xl font-bold text-yellow-600">{{ report.pendingTasks }}</div>
                <div class="text-gray-600 text-xs mt-1">Pending</div>
              </div>
              <div class="bg-gray-100 rounded-lg p-3 text-center">
                <div class="text-2xl font-bold text-red-500">{{ report.emergencyTasksHandled }}</div>
                <div class="text-gray-600 text-xs mt-1">Emergency</div>
              </div>
            </div>
            <div class="bg-gray-100 rounded-lg p-3 flex justify-between">
              <span class="text-gray-700 text-sm">Attendance Days</span>
              <span class="text-gray-900 font-medium">{{ report.totalAttendanceDays }}</span>
            </div>
            <div class="bg-gray-100 rounded-lg p-3 flex justify-between">
              <span class="text-gray-700 text-sm">Total Hours Worked</span>
              <span class="text-gray-900 font-medium">{{ report.totalHoursWorked }}h</span>
            </div>
            <div class="bg-gray-100 rounded-lg p-3 flex justify-between">
              <span class="text-gray-700 text-sm">Area</span>
              <span class="text-gray-900 font-medium">{{ report.assignedArea || '—' }}</span>
            </div>
            <div class="bg-gray-100 rounded-lg p-3 flex justify-between">
              <span class="text-gray-700 text-sm">Last Check-In</span>
              <span class="text-gray-900 font-medium text-xs">{{ report.lastCheckIn ? new Date(report.lastCheckIn).toLocaleString() : '—' }}</span>
            </div>
          </div>
          <a v-if="report?.phone || report?.whatsApp"
             :href="waLink(report.whatsApp || report.phone)" target="_blank" rel="noopener"
             class="flex items-center justify-center gap-2 w-full mt-3 py-2.5 rounded-xl text-sm font-semibold text-gray-900 transition-colors"
             style="background: rgba(37,211,102,0.2); border: 1px solid rgba(37,211,102,0.4);">
            💬 Contact on WhatsApp
          </a>
          <button @click="showReportModal = false" class="btn-outline w-full mt-2">Close</button>
        </div>
      </div>
    </Teleport>
  </div>
</template>
