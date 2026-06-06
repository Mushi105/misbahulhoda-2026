<script setup>
import { ref, computed, onMounted } from 'vue'
import { useRoute } from 'vue-router'
import { adminApi, accommodationApi } from '@/api'
import api from '@/api/client'
import AppPagination from '@/components/AppPagination.vue'

const route = useRoute()

function waLink(phone) {
  if (!phone) return null
  return 'https://wa.me/' + phone.replace(/[^\d]/g, '')
}

const pilgrims = ref([])
const loading = ref(true)
const search = ref('')
const statusFilter = ref('')
const totalCount = ref(0)
const page = ref(1)

// ── Bulk selection ────────────────────────────────────────
const selected = ref(new Set())
const allSelected = computed(() => pilgrims.value.length > 0 && pilgrims.value.every(p => selected.value.has(p.id)))
const someSelected = computed(() => selected.value.size > 0)

function toggleSelectAll() {
  if (allSelected.value) {
    selected.value = new Set()
  } else {
    selected.value = new Set(pilgrims.value.map(p => p.id))
  }
}
function toggleSelect(id) {
  const s = new Set(selected.value)
  s.has(id) ? s.delete(id) : s.add(id)
  selected.value = s
}

const bulkSaving = ref(false)

async function bulkApprove() {
  const ids = [...selected.value]
  const approvable = pilgrims.value.filter(p => ids.includes(p.id) && (p.status === 'Pending' || p.status === 'UnderReview'))
  if (!approvable.length) return notify('error', 'No pending pilgrims selected.')
  bulkSaving.value = true
  try {
    await Promise.all(approvable.map(p => adminApi.approvePilgrim(p.id)))
    notify('success', `${approvable.length} pilgrim(s) approved.`)
    selected.value = new Set()
    await load()
  } catch (e) {
    notify('error', e.response?.data?.message || 'Bulk approve failed.')
  } finally { bulkSaving.value = false }
}

function bulkReject() {
  const ids = [...selected.value]
  const rejectable = pilgrims.value.filter(p => ids.includes(p.id) && p.status !== 'Rejected')
  if (!rejectable.length) return notify('error', 'No valid pilgrims selected for rejection.')
  bulkRejectReason.value = ''
  showBulkRejectModal.value = true
}

async function submitBulkReject() {
  if (!bulkRejectReason.value.trim()) return notify('error', 'Please provide a rejection reason.')
  const ids = [...selected.value]
  const rejectable = pilgrims.value.filter(p => ids.includes(p.id) && p.status !== 'Rejected')
  bulkSaving.value = true
  try {
    await Promise.all(rejectable.map(p => adminApi.rejectPilgrim(p.id, bulkRejectReason.value)))
    notify('success', `${rejectable.length} pilgrim(s) rejected.`)
    showBulkRejectModal.value = false
    selected.value = new Set()
    await load()
  } catch (e) {
    notify('error', e.response?.data?.message || 'Bulk reject failed.')
  } finally { bulkSaving.value = false }
}

const rooms = ref([])
const buses = ref([])
const selectedHotel = ref('')
const selectedFloor = ref('')

const hotelGroups = computed(() => {
  const map = {}
  rooms.value.forEach(r => {
    const h = r.hotelName || 'Unknown Hotel'
    if (!map[h]) map[h] = { name: h, city: r.hotelCity || '', available: 0, rooms: [] }
    map[h].available++
    map[h].rooms.push(r)
  })
  return Object.values(map)
})

const floorGroups = computed(() => {
  const hotelRooms = rooms.value.filter(r => (r.hotelName || 'Unknown Hotel') === selectedHotel.value)
  const map = {}
  hotelRooms.forEach(r => {
    const f = r.floorLabel || `Floor ${r.floorNumber}` || 'Ground Floor'
    if (!map[f]) map[f] = []
    map[f].push(r)
  })
  return Object.entries(map).map(([label, rs]) => ({ label, rooms: rs }))
})

const filteredRooms = computed(() => {
  if (!selectedHotel.value) return []
  return rooms.value.filter(r => {
    const hotelMatch = (r.hotelName || 'Unknown Hotel') === selectedHotel.value
    const floorLabel = r.floorLabel || `Floor ${r.floorNumber}` || 'Ground Floor'
    const floorMatch = !selectedFloor.value || floorLabel === selectedFloor.value
    return hotelMatch && floorMatch
  })
})

const expandedId = ref(null)
const showRoomModal = ref(false)
const showBusModal = ref(false)
const showRejectModal = ref(false)
const showDetailModal = ref(false)
const showBulkRejectModal = ref(false)
const bulkRejectReason = ref('')
const selectedPilgrim = ref(null)
const pilgrimDetail = ref(null)
const detailLoading = ref(false)
const rejectReason = ref('')
const saving = ref(null)
const msg = ref({ type: '', text: '' })

const statusOptions = [
  { value: '', label: 'All Status' },
  { value: 'Pending', label: '⏳ Pending' },
  { value: 'UnderReview', label: '🔍 Under Review' },
  { value: 'Approved', label: '✅ Approved' },
  { value: 'Rejected', label: '❌ Rejected' },
]

const statusStyle = {
  Pending: 'bg-yellow-900/50 text-yellow-400 border border-yellow-700',
  UnderReview: 'bg-blue-900/50 text-blue-400 border border-blue-700',
  Approved: 'bg-emerald-900/50 text-emerald-400 border border-emerald-700',
  Rejected: 'bg-red-900/50 text-red-400 border border-red-700',
  Cancelled: 'bg-slate-800 text-slate-400 border border-slate-600',
}

let searchTimer = null

function notify(type, text) {
  msg.value = { type, text }
  setTimeout(() => (msg.value = { type: '', text: '' }), 5000)
}

async function load() {
  loading.value = true
  selected.value = new Set()
  try {
    const res = await adminApi.getPilgrims({ search: search.value || undefined, status: statusFilter.value || undefined, page: page.value, pageSize: 50 })
    const d = res.data.data
    pilgrims.value = d.items || []
    totalCount.value = d.totalCount || 0
  } finally {
    loading.value = false
  }
}

function onSearchInput() {
  clearTimeout(searchTimer)
  searchTimer = setTimeout(load, 400)
}

async function approve(p) {
  saving.value = p.id + '-approve'
  try {
    await adminApi.approvePilgrim(p.id)
    notify('success', `${p.fullName} approved. Notification sent.`)
    await load()
  } catch (e) {
    notify('error', e.response?.data?.message || 'Approval failed.')
  } finally {
    saving.value = null
  }
}

async function markUnderReview(p) {
  saving.value = p.id + '-review'
  try {
    await adminApi.markUnderReview(p.id)
    notify('success', `${p.fullName} marked as Under Review.`)
    await load()
  } catch (e) {
    notify('error', e.response?.data?.message || 'Failed to update status.')
  } finally {
    saving.value = null
  }
}

async function resetToPending(p) {
  saving.value = p.id + '-reset'
  try {
    await adminApi.resetToPending(p.id)
    notify('success', `${p.fullName} reset to Pending.`)
    await load()
  } catch (e) {
    notify('error', e.response?.data?.message || 'Reset failed.')
  } finally {
    saving.value = null
  }
}

function openReject(p) {
  selectedPilgrim.value = p
  rejectReason.value = ''
  showRejectModal.value = true
}

async function submitReject() {
  if (!rejectReason.value.trim()) return notify('error', 'Please provide a rejection reason.')
  saving.value = 'reject'
  try {
    await adminApi.rejectPilgrim(selectedPilgrim.value.id, rejectReason.value)
    notify('success', `${selectedPilgrim.value.fullName} rejected.`)
    showRejectModal.value = false
    await load()
  } catch (e) {
    notify('error', e.response?.data?.message || 'Rejection failed.')
  } finally {
    saving.value = null
  }
}

async function openDetail(p) {
  selectedPilgrim.value = p
  pilgrimDetail.value = null
  showDetailModal.value = true
  detailLoading.value = true
  try {
    const res = await api.get(`/admin/pilgrims/${p.id}`)
    pilgrimDetail.value = res.data.data
  } catch {
    notify('error', 'Failed to load pilgrim details.')
  } finally {
    detailLoading.value = false
  }
}

async function openRoomModal(p) {
  selectedPilgrim.value = p
  selectedHotel.value = ''
  selectedFloor.value = ''
  showRoomModal.value = true
  const res = await accommodationApi.getRooms({ pageSize: 500 })
  rooms.value = (res.data.data?.items || res.data.data || []).filter(r => r.occupiedBeds < r.bedCapacity)
}

async function allocateRoom(roomId) {
  saving.value = 'room'
  try {
    await adminApi.allocateRoom(selectedPilgrim.value.id, roomId)
    notify('success', 'Room allocated. Pilgrim notified.')
    showRoomModal.value = false
    await load()
  } catch (e) {
    notify('error', e.response?.data?.message || 'Room allocation failed.')
  } finally {
    saving.value = null
  }
}

async function openBusModal(p) {
  selectedPilgrim.value = p
  showBusModal.value = true
  if (!buses.value.length) {
    try {
      const res = await api.get('/buses', { params: { pageSize: 100 } })
      buses.value = (res.data.data?.items || res.data.data || []).filter(b => b.currentPassengers < b.capacity)
    } catch {
      buses.value = []
    }
  }
}

async function allocateBus(busId) {
  saving.value = 'bus'
  try {
    await adminApi.allocateBus(selectedPilgrim.value.id, busId)
    notify('success', 'Bus allocated. Pilgrim notified.')
    showBusModal.value = false
    await load()
  } catch (e) {
    notify('error', e.response?.data?.message || 'Bus allocation failed.')
  } finally {
    saving.value = null
  }
}

const genderLabel = (g) => g === 1 ? '♂ Male' : '♀ Female'
const relLabel = (r) => (['Self','Spouse','Son','Daughter','Father','Mother','Brother','Sister','','','','','','','','','','','','','','','','','','','','','','','','','','','','','','','','','','','','','','','','','','','','','','','','','','','','','','','','','','','','','','','','','','','','','','','','','','','','','','','','','','','','','','','','','','','','','Other'])[r] || r

function fmtDateTime(d) {
  if (!d) return '—'
  const dt = new Date(d)
  return dt.toLocaleDateString('en-GB', { day: '2-digit', month: 'short', year: 'numeric' }) + ' ' +
         dt.toLocaleTimeString('en-GB', { hour: '2-digit', minute: '2-digit' })
}

onMounted(() => {
  if (route.query.status) statusFilter.value = route.query.status
  load()
})
</script>

<template>
  <div class="space-y-6">
    <!-- Header -->
    <div>
      <h1 class="text-2xl font-bold text-white">Pilgrims Management</h1>
      <p class="text-slate-400 text-sm mt-1">Review applications, approve/reject, allocate rooms and buses</p>
    </div>

    <!-- Alert -->
    <div v-if="msg.text" :class="msg.type === 'success' ? 'bg-emerald-900/50 border-emerald-700 text-emerald-300' : 'bg-red-900/50 border-red-700 text-red-300'"
      class="border rounded-lg px-4 py-3 text-sm">
      {{ msg.text }}
    </div>

    <!-- Filters -->
    <div class="flex flex-wrap gap-3">
      <input v-model="search" @input="onSearchInput" type="text" class="form-input w-64" placeholder="Search by name, email, passport..." />
      <select v-model="statusFilter" @change="load" class="form-input w-48">
        <option v-for="s in statusOptions" :key="s.value" :value="s.value">{{ s.label }}</option>
      </select>
      <span class="text-slate-400 text-sm self-center">{{ totalCount }} total</span>
    </div>

    <!-- Bulk Action Bar -->
    <div v-if="someSelected"
         class="flex items-center gap-3 flex-wrap bg-dark-800 border border-gold-800/50 rounded-xl px-4 py-3">
      <span class="text-gold-400 text-sm font-semibold">{{ selected.size }} selected</span>
      <button @click="bulkApprove" :disabled="bulkSaving"
        class="text-xs px-4 py-2 rounded-lg bg-emerald-800 hover:bg-emerald-700 text-emerald-200 font-medium disabled:opacity-50 transition-colors">
        {{ bulkSaving ? '...' : '✅ Approve Selected' }}
      </button>
      <button @click="bulkReject" :disabled="bulkSaving"
        class="text-xs px-4 py-2 rounded-lg bg-red-900/60 hover:bg-red-800 text-red-300 font-medium disabled:opacity-50 transition-colors">
        {{ bulkSaving ? '...' : '❌ Reject Selected' }}
      </button>
      <button @click="selected = new Set()" class="text-xs text-slate-400 hover:text-white ml-auto">✕ Clear</button>
    </div>

    <!-- Table -->
    <div v-if="loading" class="card text-center text-slate-400 py-12">Loading pilgrims...</div>
    <div v-else-if="!pilgrims.length" class="card text-center text-slate-400 py-12">No pilgrims found.</div>
    <div v-else class="space-y-2">
      <!-- Select All row -->
      <div class="flex items-center gap-3 px-1 pb-1">
        <input type="checkbox" :checked="allSelected" @change="toggleSelectAll"
          class="w-4 h-4 accent-gold-500 cursor-pointer" />
        <span class="text-slate-400 text-xs">
          {{ allSelected ? 'Deselect all' : 'Select all' }}
          <span class="text-slate-500">({{ pilgrims.length }} shown)</span>
        </span>
      </div>

      <div v-for="p in pilgrims" :key="p.id"
           :class="['rounded-xl border transition-all overflow-hidden',
             selected.has(p.id) ? 'border-gold-700 bg-gold-950/20' : 'border-emerald-900/20 bg-dark-900/50',
             expandedId === p.id ? 'border-emerald-700/40' : '']"
           style="background:rgba(2,20,10,0.45);">

        <!-- ── Compact row (always visible) ── -->
        <div class="flex items-center gap-3 px-3 py-2.5 cursor-pointer select-none"
             style="touch-action:manipulation; min-height:52px;"
             @click="expandedId = expandedId === p.id ? null : p.id">
          <input type="checkbox" :checked="selected.has(p.id)"
            @click.stop @change="toggleSelect(p.id)"
            class="w-4 h-4 accent-gold-500 cursor-pointer shrink-0" />

          <!-- Avatar -->
          <div class="w-9 h-9 rounded-full flex items-center justify-center text-white font-bold text-sm shrink-0"
               style="background:linear-gradient(135deg,#92400e,#451a03);">
            {{ p.fullName?.charAt(0) || '?' }}
          </div>

          <!-- Name + status -->
          <div class="flex-1 min-w-0">
            <p class="text-white text-sm font-semibold truncate">{{ p.fullName }}</p>
            <p class="text-slate-500 text-xs truncate">{{ p.country || '—' }} · {{ p.passportNumber || 'No Passport' }}</p>
          </div>

          <!-- Status badge + expand arrow -->
          <div class="flex items-center gap-2 shrink-0">
            <span :class="statusStyle[p.status] || 'bg-slate-800 text-slate-400'"
                  class="px-2 py-0.5 rounded-full text-xs font-medium hidden sm:inline">
              {{ p.status }}
            </span>
            <div :class="['w-2 h-2 rounded-full shrink-0',
              p.status==='Approved' ? 'bg-emerald-400' :
              p.status==='Pending' ? 'bg-yellow-400' :
              p.status==='UnderReview' ? 'bg-blue-400' : 'bg-red-400']"></div>
            <svg :class="['w-4 h-4 text-slate-500 transition-transform', expandedId===p.id ? 'rotate-180' : '']"
                 fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 9l-7 7-7-7"/>
            </svg>
          </div>
        </div>

        <!-- ── Expanded section ── -->
        <div v-if="expandedId === p.id"
             class="px-3 pb-3 border-t space-y-3" style="border-color:rgba(16,185,129,0.12);">

          <!-- Contact row -->
          <div class="flex items-center gap-2 flex-wrap pt-2">
            <span class="text-slate-400 text-xs">{{ p.email }}</span>
            <a v-if="p.whatsApp" :href="waLink(p.whatsApp)" target="_blank" rel="noopener"
               class="inline-flex items-center gap-1 text-xs px-2 py-0.5 rounded-full bg-emerald-900/40 border border-emerald-700/50 text-emerald-400">
              📱 WhatsApp
            </a>
            <a v-else-if="p.phone" :href="waLink(p.phone)" target="_blank" rel="noopener"
               class="inline-flex items-center gap-1 text-xs px-2 py-0.5 rounded-full bg-slate-800 border border-slate-700 text-slate-400">
              📞 {{ p.phone }}
            </a>
          </div>

          <!-- Details grid -->
          <div class="grid grid-cols-2 sm:grid-cols-3 gap-2 text-xs">
            <div class="bg-black/20 rounded-lg px-2 py-1.5">
              <p class="text-slate-500">Arrival</p>
              <p class="text-slate-200">{{ p.arrivalDate ? new Date(p.arrivalDate).toLocaleDateString('en-GB') : '—' }}</p>
            </div>
            <div class="bg-black/20 rounded-lg px-2 py-1.5">
              <p class="text-slate-500">Family</p>
              <p class="text-slate-200">{{ p.familyMemberCount || 0 }} members</p>
            </div>
            <div class="bg-black/20 rounded-lg px-2 py-1.5">
              <p class="text-slate-500">Status</p>
              <p :class="p.status==='Approved'?'text-emerald-400':p.status==='Pending'?'text-yellow-400':p.status==='UnderReview'?'text-blue-400':'text-red-400'">
                {{ p.status }}
              </p>
            </div>
            <div class="bg-black/20 rounded-lg px-2 py-1.5">
              <p class="text-slate-500">🛏 Room</p>
              <p :class="p.roomNumber ? 'text-emerald-400' : 'text-yellow-400'">
                {{ p.roomNumber ? p.roomNumber + (p.hotelName ? ' · ' + p.hotelName : '') : 'Not Assigned' }}
              </p>
            </div>
            <div class="bg-black/20 rounded-lg px-2 py-1.5">
              <p class="text-slate-500">🚌 Bus</p>
              <p :class="p.busNumber ? 'text-emerald-400' : 'text-yellow-400'">
                {{ p.busNumber || 'Not Assigned' }}
              </p>
            </div>
            <div v-if="p.rejectionReason" class="bg-red-950/30 rounded-lg px-2 py-1.5 col-span-2 sm:col-span-1">
              <p class="text-slate-500">Reason</p>
              <p class="text-red-400 text-xs">{{ p.rejectionReason }}</p>
            </div>
          </div>

          <!-- Action buttons -->
          <div class="flex flex-wrap gap-2">
            <button @click="openDetail(p)"
              class="text-xs px-3 py-1.5 rounded-lg bg-slate-800 hover:bg-slate-700 text-slate-300 border border-slate-700 font-medium transition-colors">
              📋 Details
            </button>
            <button v-if="p.status === 'Pending'" @click="markUnderReview(p)" :disabled="saving === p.id+'-review'"
              class="text-xs px-3 py-1.5 rounded-lg bg-blue-900/60 hover:bg-blue-800 text-blue-300 font-medium disabled:opacity-50 transition-colors">
              {{ saving === p.id+'-review' ? '...' : '🔍 Review' }}
            </button>
            <button v-if="p.status === 'Pending' || p.status === 'UnderReview'" @click="approve(p)" :disabled="saving === p.id+'-approve'"
              class="text-xs px-3 py-1.5 rounded-lg bg-emerald-800 hover:bg-emerald-700 text-emerald-200 font-medium disabled:opacity-50 transition-colors">
              {{ saving === p.id+'-approve' ? '...' : '✅ Approve' }}
            </button>
            <button v-if="p.status !== 'Rejected'" @click="openReject(p)"
              class="text-xs px-3 py-1.5 rounded-lg bg-red-900/60 hover:bg-red-800 text-red-300 font-medium transition-colors">
              ❌ Reject
            </button>
            <button v-if="p.status === 'Rejected'" @click="resetToPending(p)" :disabled="saving === p.id+'-reset'"
              class="text-xs px-3 py-1.5 rounded-lg bg-slate-700 hover:bg-slate-600 text-slate-300 font-medium disabled:opacity-50 transition-colors">
              {{ saving === p.id+'-reset' ? '...' : '↩ Reset' }}
            </button>
            <button v-if="p.status === 'Approved'" @click="openRoomModal(p)"
              :class="p.roomId ? 'bg-emerald-900/60 text-emerald-300' : 'bg-yellow-900/60 text-yellow-300'"
              class="text-xs px-3 py-1.5 rounded-lg hover:opacity-80 font-medium transition-colors">
              🛏 {{ p.roomId ? 'Room' : 'Assign Room' }}
            </button>
            <button v-if="p.status === 'Approved'" @click="openBusModal(p)"
              :class="p.busId ? 'bg-emerald-900/60 text-emerald-300' : 'bg-yellow-900/60 text-yellow-300'"
              class="text-xs px-3 py-1.5 rounded-lg hover:opacity-80 font-medium transition-colors">
              🚌 {{ p.busId ? 'Bus' : 'Assign Bus' }}
            </button>
          </div>
        </div>
      </div>
    </div>

    <!-- MODAL: Reject Pilgrim -->
    <Teleport to="body">
      <div v-if="showRejectModal" class="fixed inset-0 z-50 flex items-center justify-center bg-black/70 p-4">
        <div class="rounded-xl w-full max-w-md p-6"
             style="background:rgba(15,5,5,0.96); border:1px solid rgba(239,68,68,0.3); backdrop-filter:blur(12px);">
          <div class="flex items-center gap-3 mb-4">
            <div class="w-10 h-10 rounded-full bg-red-900/60 flex items-center justify-center text-xl shrink-0">❌</div>
            <div>
              <h3 class="text-white font-semibold">Reject Application</h3>
              <p class="text-red-400 text-xs mt-0.5">{{ selectedPilgrim?.fullName }}</p>
            </div>
          </div>
          <div class="mb-4">
            <label class="form-label">Rejection Reason *</label>
            <textarea v-model="rejectReason" class="input" rows="3"
              placeholder="Please provide a clear reason for rejection..."></textarea>
          </div>
          <p class="text-slate-500 text-xs mb-4">The pilgrim will receive an email and WhatsApp notification with this reason.</p>
          <div class="flex gap-3">
            <button @click="submitReject" :disabled="saving === 'reject'"
              class="btn-danger flex-1">{{ saving === 'reject' ? 'Rejecting...' : 'Confirm Reject' }}</button>
            <button @click="showRejectModal = false" class="btn-outline flex-1">Cancel</button>
          </div>
        </div>
      </div>
    </Teleport>

    <!-- MODAL: Bulk Reject -->
    <Teleport to="body">
      <div v-if="showBulkRejectModal" class="fixed inset-0 z-50 flex items-center justify-center bg-black/70 p-4">
        <div class="rounded-xl w-full max-w-md p-6"
             style="background:rgba(15,5,5,0.96); border:1px solid rgba(239,68,68,0.3); backdrop-filter:blur(12px);">
          <div class="flex items-center gap-3 mb-4">
            <div class="w-10 h-10 rounded-full bg-red-900/60 flex items-center justify-center text-xl shrink-0">❌</div>
            <div>
              <h3 class="text-white font-semibold">Bulk Reject</h3>
              <p class="text-red-400 text-xs mt-0.5">{{ selected.size }} pilgrim(s) selected</p>
            </div>
          </div>
          <div class="mb-4">
            <label class="form-label">Rejection Reason for All *</label>
            <textarea v-model="bulkRejectReason" class="input" rows="3"
              placeholder="Reason applied to all selected pilgrims..."></textarea>
          </div>
          <p class="text-slate-500 text-xs mb-4">Each pilgrim will receive an email and WhatsApp notification with this reason.</p>
          <div class="flex gap-3">
            <button @click="submitBulkReject" :disabled="bulkSaving"
              class="btn-danger flex-1">{{ bulkSaving ? 'Rejecting...' : 'Confirm Reject All' }}</button>
            <button @click="showBulkRejectModal = false" class="btn-outline flex-1">Cancel</button>
          </div>
        </div>
      </div>
    </Teleport>

    <!-- MODAL: Assign Room -->
    <Teleport to="body">
      <div v-if="showRoomModal" class="fixed inset-0 z-50 flex items-center justify-center bg-black/70 p-4">
        <div class="bg-dark-800 border border-dark-600 rounded-xl w-full max-w-lg shadow-2xl">

          <!-- Header -->
          <div class="flex items-center justify-between px-6 py-4 border-b border-dark-600">
            <div>
              <h3 class="text-white font-semibold text-lg">Assign Room</h3>
              <p class="text-slate-400 text-xs mt-0.5">{{ selectedPilgrim?.fullName }}</p>
            </div>
            <button @click="showRoomModal = false" class="text-slate-400 hover:text-white text-xl">×</button>
          </div>

          <div class="p-6 space-y-4">

            <!-- No rooms -->
            <div v-if="!rooms.length" class="text-slate-400 text-sm text-center py-6">
              No available rooms. Please add rooms in Accommodation management.
            </div>

            <template v-else>

              <!-- STEP 1: Select Hotel -->
              <div>
                <p class="text-xs text-slate-500 uppercase tracking-wider mb-2 font-semibold">Step 1 — Select Hotel</p>
                <div class="space-y-2">
                  <button v-for="h in hotelGroups" :key="h.name"
                    @click="selectedHotel = h.name; selectedFloor = ''"
                    :class="['w-full text-left px-4 py-3 rounded-lg border transition-all',
                      selectedHotel === h.name
                        ? 'border-gold-600 bg-gold-900/30 text-white'
                        : 'border-dark-600 bg-dark-700 text-slate-300 hover:border-gold-800 hover:bg-dark-600']">
                    <div class="flex items-center justify-between">
                      <div>
                        <p class="font-medium">🏨 {{ h.name }}</p>
                        <p v-if="h.city" class="text-xs text-slate-500 mt-0.5">{{ h.city }}</p>
                      </div>
                      <span class="text-emerald-400 text-sm font-semibold">{{ h.available }} rooms free</span>
                    </div>
                  </button>
                </div>
              </div>

              <!-- STEP 2: Select Floor (only if hotel chosen & has multiple floors) -->
              <div v-if="selectedHotel && floorGroups.length > 1">
                <p class="text-xs text-slate-500 uppercase tracking-wider mb-2 font-semibold">Step 2 — Select Floor</p>
                <div class="flex flex-wrap gap-2">
                  <button
                    @click="selectedFloor = ''"
                    :class="['px-3 py-1.5 rounded-lg text-xs border transition-all',
                      selectedFloor === '' ? 'border-gold-600 bg-gold-900/30 text-gold-300' : 'border-dark-600 bg-dark-700 text-slate-400 hover:border-gold-800']">
                    All Floors ({{ floorGroups.reduce((s,f) => s + f.rooms.length, 0) }})
                  </button>
                  <button v-for="f in floorGroups" :key="f.label"
                    @click="selectedFloor = f.label"
                    :class="['px-3 py-1.5 rounded-lg text-xs border transition-all',
                      selectedFloor === f.label ? 'border-gold-600 bg-gold-900/30 text-gold-300' : 'border-dark-600 bg-dark-700 text-slate-400 hover:border-gold-800']">
                    {{ f.label }} ({{ f.rooms.length }})
                  </button>
                </div>
              </div>

              <!-- STEP 3: Select Room -->
              <div v-if="selectedHotel">
                <p class="text-xs text-slate-500 uppercase tracking-wider mb-2 font-semibold">
                  {{ floorGroups.length > 1 ? 'Step 3' : 'Step 2' }} — Select Room
                  <span class="text-slate-600 normal-case font-normal ml-1">({{ filteredRooms.length }} available)</span>
                </p>
                <div class="space-y-1.5 max-h-52 overflow-y-auto pr-1">
                  <button v-for="r in filteredRooms" :key="r.id"
                    @click="allocateRoom(r.id)"
                    :disabled="saving === 'room'"
                    class="w-full text-left flex items-center justify-between bg-dark-700 hover:bg-dark-600 border border-transparent hover:border-emerald-800/60 rounded-lg px-4 py-2.5 transition-colors disabled:opacity-50">
                    <div class="flex items-center gap-3">
                      <span class="text-white font-semibold">{{ r.roomNumber }}</span>
                      <span class="text-slate-500 text-xs">{{ r.floorLabel || `Floor ${r.floorNumber}` }}</span>
                      <span v-if="r.isForFamily" class="text-xs bg-primary-900 text-primary-400 px-1.5 py-0.5 rounded">Family</span>
                    </div>
                    <div class="text-right shrink-0">
                      <span class="text-emerald-400 text-xs font-medium">{{ r.bedCapacity - r.occupiedBeds }} beds free</span>
                      <span class="text-slate-600 text-xs ml-1">({{ r.bedCapacity }} total)</span>
                    </div>
                  </button>
                </div>
              </div>

            </template>

            <button @click="showRoomModal = false" class="btn-outline w-full">Cancel</button>
          </div>
        </div>
      </div>
    </Teleport>

    <!-- MODAL: Pilgrim Detail -->
    <Teleport to="body">
      <div v-if="showDetailModal" class="fixed inset-0 z-50 flex items-center justify-center bg-black/75 p-4" @click.self="showDetailModal = false">
        <div class="bg-dark-900 border border-dark-600 rounded-2xl w-full max-w-2xl max-h-[90vh] overflow-y-auto shadow-2xl">

          <!-- Header -->
          <div class="sticky top-0 bg-dark-900 border-b border-dark-700 px-6 py-4 flex items-center justify-between">
            <div class="flex items-center gap-3">
              <div class="w-10 h-10 rounded-full bg-gradient-to-br from-amber-700 to-amber-900 flex items-center justify-center text-white font-bold text-lg shrink-0">
                {{ pilgrimDetail?.user?.fullName?.charAt(0) || selectedPilgrim?.fullName?.charAt(0) || '?' }}
              </div>
              <div>
                <p class="text-white font-semibold">{{ pilgrimDetail?.user?.fullName || selectedPilgrim?.fullName }}</p>
                <span :class="statusStyle[pilgrimDetail?.status || selectedPilgrim?.status] || 'bg-slate-800 text-slate-400'" class="text-xs px-2 py-0.5 rounded-full">
                  {{ pilgrimDetail?.status || selectedPilgrim?.status }}
                </span>
              </div>
            </div>
            <button @click="showDetailModal = false" class="text-slate-400 hover:text-white text-2xl leading-none">✕</button>
          </div>

          <div v-if="detailLoading" class="py-16 text-center text-slate-400">Loading...</div>

          <div v-else-if="pilgrimDetail" class="p-6 space-y-6">

            <!-- Contact Info -->
            <section>
              <h3 class="text-xs font-semibold text-slate-500 uppercase tracking-wider mb-3">Contact</h3>
              <div class="grid grid-cols-2 gap-3 text-sm">
                <div><p class="text-slate-500 text-xs">Email</p><p class="text-white text-xs break-all">{{ pilgrimDetail.user?.email }}</p></div>
                <div>
                  <p class="text-slate-500 text-xs">Phone</p>
                  <a v-if="pilgrimDetail.user?.phoneNumber" :href="waLink(pilgrimDetail.user.phoneNumber)" target="_blank" rel="noopener"
                     class="inline-flex items-center gap-1 text-emerald-400 hover:text-emerald-300 text-xs font-medium transition-colors">
                    📞 {{ pilgrimDetail.user.phoneNumber }}
                  </a>
                  <p v-else class="text-white">—</p>
                </div>
                <div>
                  <p class="text-slate-500 text-xs mb-1">WhatsApp</p>
                  <a v-if="pilgrimDetail.user?.whatsAppNumber" :href="waLink(pilgrimDetail.user.whatsAppNumber)" target="_blank" rel="noopener"
                     class="inline-flex items-center gap-1.5 px-3 py-1.5 rounded-lg text-xs font-semibold text-white transition-colors"
                     style="background: rgba(37,211,102,0.2); border: 1px solid rgba(37,211,102,0.4);">
                    <span>💬</span> Chat on WhatsApp
                  </a>
                  <p v-else class="text-slate-500 text-xs">No WhatsApp number</p>
                </div>
                <div><p class="text-slate-500 text-xs">Country</p><p class="text-white">{{ pilgrimDetail.country || '—' }}</p></div>
              </div>
            </section>

            <!-- Travel Details -->
            <section>
              <h3 class="text-xs font-semibold text-slate-500 uppercase tracking-wider mb-3">Travel Details</h3>
              <div class="grid grid-cols-2 gap-3 text-sm">
                <div><p class="text-slate-500 text-xs">Passport</p><p class="text-white">{{ pilgrimDetail.passportNumber || '—' }}</p></div>
                <div><p class="text-slate-500 text-xs">Visa</p><p class="text-white">{{ pilgrimDetail.visaNumber || '—' }}</p></div>
                <div><p class="text-slate-500 text-xs">Arrival</p><p class="text-white">{{ new Date(pilgrimDetail.arrivalDate).toLocaleDateString('en-GB') }}</p></div>
                <div><p class="text-slate-500 text-xs">Departure</p><p class="text-white">{{ new Date(pilgrimDetail.departureDate).toLocaleDateString('en-GB') }}</p></div>
                <div><p class="text-slate-500 text-xs">Arrival Flight</p><p class="text-white">{{ pilgrimDetail.arrivalFlight || '—' }}</p></div>
                <div><p class="text-slate-500 text-xs">Departure Flight</p><p class="text-white">{{ pilgrimDetail.departureFlight || '—' }}</p></div>
              </div>
            </section>

            <!-- Emergency Contact -->
            <section v-if="pilgrimDetail.emergencyContactName">
              <h3 class="text-xs font-semibold text-slate-500 uppercase tracking-wider mb-3">Emergency Contact</h3>
              <div class="grid grid-cols-2 gap-3 text-sm">
                <div><p class="text-slate-500 text-xs">Name</p><p class="text-white">{{ pilgrimDetail.emergencyContactName }}</p></div>
                <div>
                  <p class="text-slate-500 text-xs">Phone</p>
                  <a v-if="pilgrimDetail.emergencyContactPhone" :href="waLink(pilgrimDetail.emergencyContactPhone)" target="_blank" rel="noopener"
                     class="text-emerald-400 hover:text-emerald-300 text-sm transition-colors">
                    📞 {{ pilgrimDetail.emergencyContactPhone }}
                  </a>
                  <p v-else class="text-white">—</p>
                </div>
              </div>
            </section>

            <!-- Current Allocation -->
            <section>
              <h3 class="text-xs font-semibold text-slate-500 uppercase tracking-wider mb-3">Current Allocation</h3>
              <div class="grid grid-cols-2 gap-3 text-sm">
                <div v-if="pilgrimDetail.currentRoom">
                  <p class="text-slate-500 text-xs">Room</p>
                  <p class="text-emerald-400 font-semibold">{{ pilgrimDetail.currentRoom.roomNumber }}</p>
                  <p class="text-slate-500 text-xs">{{ pilgrimDetail.currentRoom.hotelName }} · {{ pilgrimDetail.currentRoom.floorLabel }}</p>
                </div>
                <div v-else><p class="text-slate-500 text-xs">Room</p><p class="text-yellow-400 text-sm">Not assigned</p></div>

                <div v-if="pilgrimDetail.currentBus">
                  <p class="text-slate-500 text-xs">Bus</p>
                  <p class="text-emerald-400 font-semibold">Bus {{ pilgrimDetail.currentBus.busNumber }}</p>
                  <p class="text-slate-500 text-xs">{{ pilgrimDetail.currentBus.driverName || 'Driver TBA' }}</p>
                </div>
                <div v-else><p class="text-slate-500 text-xs">Bus</p><p class="text-yellow-400 text-sm">Not assigned</p></div>
              </div>
            </section>

            <!-- Hotel Trail -->
            <section v-if="pilgrimDetail.hotelTrail?.length">
              <h3 class="text-xs font-semibold text-slate-500 uppercase tracking-wider mb-3">Hotel Journey Trail</h3>
              <div class="space-y-2">
                <div v-for="(stay, i) in pilgrimDetail.hotelTrail" :key="i"
                     :class="['rounded-lg px-4 py-3 border text-sm flex items-center gap-4',
                       stay.isCurrentStay ? 'bg-green-900/20 border-green-700/40' : 'bg-dark-800 border-dark-700']">
                  <div :class="['w-6 h-6 rounded-full flex items-center justify-center text-xs font-bold shrink-0',
                    stay.isCurrentStay ? 'bg-green-700 text-white' : 'bg-dark-600 text-slate-400']">
                    {{ i + 1 }}
                  </div>
                  <div class="flex-1">
                    <p class="text-white font-medium">{{ stay.hotelName }} <span class="text-slate-500 font-normal">— {{ stay.hotelCity }}</span></p>
                    <p class="text-slate-400 text-xs">Room {{ stay.roomNumber }} · {{ stay.floorLabel }}</p>
                    <p class="text-xs mt-0.5">
                      <span class="text-green-400">In: {{ new Date(stay.checkedInAt).toLocaleDateString('en-GB') }}</span>
                      <span v-if="stay.checkedOutAt" class="text-red-400 ml-3">Out: {{ new Date(stay.checkedOutAt).toLocaleDateString('en-GB') }} ({{ stay.checkedOutBy }})</span>
                      <span v-else class="text-green-400 ml-3">🟢 Current stay</span>
                    </p>
                  </div>
                </div>
              </div>
            </section>

            <!-- Family Members -->
            <section>
              <h3 class="text-xs font-semibold text-slate-500 uppercase tracking-wider mb-3">
                Family Members ({{ pilgrimDetail.familyMembers?.length || 0 }})
              </h3>
              <div v-if="!pilgrimDetail.familyMembers?.length" class="text-slate-500 text-sm text-center py-4 bg-dark-800 rounded-lg">
                No family members added yet.
              </div>
              <div v-else class="space-y-2">
                <div v-for="m in pilgrimDetail.familyMembers" :key="m.id"
                     class="bg-dark-800 rounded-lg px-4 py-3 flex items-center gap-4 text-sm">
                  <div class="w-9 h-9 rounded-full flex items-center justify-center text-sm font-bold shrink-0"
                       :class="m.gender === 1 ? 'bg-blue-900 text-blue-300' : 'bg-pink-900 text-pink-300'">
                    {{ m.gender === 1 ? '♂' : '♀' }}
                  </div>
                  <div class="flex-1 min-w-0">
                    <p class="text-white font-medium">{{ m.fullName }}</p>
                    <p class="text-slate-500 text-xs">
                      {{ relLabel(m.relationship) }}
                      <span v-if="m.nationality"> · {{ m.nationality }}</span>
                      <span v-if="m.passportNumber"> · {{ m.passportNumber }}</span>
                    </p>
                  </div>
                  <div class="flex gap-1.5 shrink-0">
                    <span v-if="m.isMinor" class="text-xs bg-gold-900 text-gold-400 px-1.5 py-0.5 rounded">Minor</span>
                    <span v-if="m.requiresWheelchair" class="text-xs bg-slate-700 text-slate-300 px-1.5 py-0.5 rounded">♿</span>
                  </div>
                </div>
              </div>
            </section>

            <!-- Rejection reason -->
            <div v-if="pilgrimDetail.rejectionReason" class="bg-red-950 border border-red-800 rounded-lg px-4 py-3">
              <p class="text-red-400 text-sm font-medium">Rejection Reason</p>
              <p class="text-red-300 text-sm mt-1">{{ pilgrimDetail.rejectionReason }}</p>
            </div>

          </div>
        </div>
      </div>
    </Teleport>

    <!-- Pagination -->
    <AppPagination :page="page" :total="totalCount" :pageSize="20"
      @update:page="p => { page = p; load() }" />

    <!-- MODAL: Assign Bus -->
    <Teleport to="body">
      <div v-if="showBusModal" class="fixed inset-0 z-50 flex items-center justify-center bg-black/70 p-4">
        <div class="bg-dark-800 border border-dark-600 rounded-xl w-full max-w-lg p-6">
          <div class="flex items-center justify-between mb-4">
            <h3 class="text-white font-semibold text-lg">Assign Bus</h3>
            <button @click="showBusModal = false" class="text-slate-400 hover:text-white text-xl">×</button>
          </div>
          <p class="text-slate-400 text-sm mb-4">Pilgrim: <span class="text-white">{{ selectedPilgrim?.fullName }}</span></p>
          <div v-if="!buses.length" class="text-yellow-400 text-sm text-center py-6">
            No buses available or bus data cannot be loaded. Please check Bus management.
          </div>
          <div v-else class="space-y-2 max-h-80 overflow-y-auto pr-1">
            <button v-for="b in buses" :key="b.id" @click="allocateBus(b.id)"
              :disabled="saving === 'bus'"
              class="w-full text-left flex items-center justify-between bg-dark-700 hover:bg-dark-600 rounded-lg px-4 py-3 transition-colors disabled:opacity-50">
              <div>
                <p class="text-white font-medium">Bus {{ b.busNumber }}</p>
                <p class="text-slate-400 text-xs">{{ b.plateNumber }} · Driver: {{ b.driverName || 'TBA' }} · {{ b.currentPassengers }}/{{ b.capacity }}</p>
              </div>
              <span class="text-emerald-400 text-xs">{{ b.capacity - b.currentPassengers }} seats</span>
            </button>
          </div>
          <button @click="showBusModal = false" class="btn-outline w-full mt-4">Cancel</button>
        </div>
      </div>
    </Teleport>
  </div>
</template>
