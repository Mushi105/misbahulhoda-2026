<script setup>
import { ref, onMounted } from 'vue'
import { itineraryApi } from '@/api'

const tab           = ref('today')
const loading       = ref(false)
const saving        = ref(false)
const sendingReminders = ref(false)
const error         = ref('')
const success       = ref('')
const today         = ref(null)
const arrivals      = ref([])
const departures    = ref([])
const upcoming      = ref(null)
const upcomingDays  = ref(7)
const arrivalDate   = ref(new Date().toISOString().split('T')[0])
const departureDate = ref(new Date().toISOString().split('T')[0])
const showAssign    = ref(false)
const assignRow     = ref(null)
const assignType    = ref(1)
const assignForm    = ref({
  driverName: '', driverPhone: '', vehicleNumber: '', vehicleType: 'Car',
  meetingPoint: '', scheduledTime: '', notes: ''
})
const updatingId = ref(null)

const vehicleTypes = ['Car', 'Van', 'Coaster', 'Bus', 'Ambulance', 'Other']

const statusColors = {
  Unassigned:     'text-slate-400 bg-slate-800',
  Pending:        'text-gold-400 bg-gold-900/50',
  DriverAssigned: 'text-blue-400 bg-blue-900/50',
  EnRoute:        'text-purple-400 bg-purple-900/50',
  Completed:      'text-green-400 bg-green-900/50',
  Cancelled:      'text-red-400 bg-red-900/40',
}
const statusIcons = {
  Unassigned:'⚪', Pending:'⏳', DriverAssigned:'🚗', EnRoute:'🛣️', Completed:'✅', Cancelled:'❌'
}
const statusOptions = [
  { value: 1, label: 'Pending' },
  { value: 2, label: 'Driver Assigned' },
  { value: 3, label: 'En Route' },
  { value: 4, label: 'Completed' },
  { value: 5, label: 'Cancelled' },
]

function fmtDate(d) {
  if (!d) return '—'
  return new Date(d).toLocaleDateString('en-GB', { day: '2-digit', month: 'short', year: 'numeric' })
}
function fmtTime(d) {
  if (!d) return '—'
  return new Date(d).toLocaleString('en-GB', { day: '2-digit', month: 'short', hour: '2-digit', minute: '2-digit' })
}
function wa(phone) {
  if (!phone) return null
  return 'https://wa.me/' + phone.replace(/\D/g, '')
}

// ── Loaders ───────────────────────────────────────────────────────────────────
async function loadToday() {
  loading.value = true; error.value = ''
  try {
    const res = await itineraryApi.getToday()
    today.value = res.data.data
  } catch (e) { error.value = e.response?.data?.message || 'Failed to load.' }
  finally { loading.value = false }
}

async function loadArrivals() {
  loading.value = true; error.value = ''
  try {
    const res = await itineraryApi.getArrivals(arrivalDate.value)
    arrivals.value = res.data.data
  } catch (e) { error.value = e.response?.data?.message || 'Failed to load.' }
  finally { loading.value = false }
}

async function loadDepartures() {
  loading.value = true; error.value = ''
  try {
    const res = await itineraryApi.getDepartures(departureDate.value)
    departures.value = res.data.data
  } catch (e) { error.value = e.response?.data?.message || 'Failed to load.' }
  finally { loading.value = false }
}

async function loadUpcoming() {
  loading.value = true; error.value = ''
  try {
    const res = await itineraryApi.getUpcoming(upcomingDays.value)
    upcoming.value = res.data.data
  } catch (e) { error.value = e.response?.data?.message || 'Failed to load.' }
  finally { loading.value = false }
}

async function triggerReminders() {
  sendingReminders.value = true
  try {
    await itineraryApi.sendReminders()
    success.value = 'Reminders are being sent via WhatsApp & Email to all pilgrims with upcoming travel.'
  } catch (e) { error.value = e.response?.data?.message || 'Failed.' }
  finally { sendingReminders.value = false }
}

async function switchTab(t) {
  tab.value = t
  if (t === 'today')      await loadToday()
  if (t === 'arrivals')   await loadArrivals()
  if (t === 'departures') await loadDepartures()
  if (t === 'upcoming')   await loadUpcoming()
}

async function refresh() {
  if (tab.value === 'today')      await loadToday()
  if (tab.value === 'arrivals')   await loadArrivals()
  if (tab.value === 'departures') await loadDepartures()
  if (tab.value === 'upcoming')   await loadUpcoming()
}

// ── Assign driver ─────────────────────────────────────────────────────────────
function openAssign(row, type) {
  assignRow.value  = row
  assignType.value = type
  const t = row.transfer
  assignForm.value = {
    driverName:    t?.driverName    || '',
    driverPhone:   t?.driverPhone   || '',
    vehicleNumber: t?.vehicleNumber || '',
    vehicleType:   t?.vehicleType   || 'Car',
    meetingPoint:  t?.meetingPoint  || '',
    scheduledTime: t?.scheduledTime ? t.scheduledTime.slice(0, 16) : '',
    notes:         t?.notes         || '',
  }
  showAssign.value = true
}

async function submitAssign() {
  saving.value = true; error.value = ''
  try {
    await itineraryApi.assignDriver(assignRow.value.pilgrimId, {
      transferType:  assignType.value,
      driverName:    assignForm.value.driverName,
      driverPhone:   assignForm.value.driverPhone,
      vehicleNumber: assignForm.value.vehicleNumber,
      vehicleType:   assignForm.value.vehicleType,
      meetingPoint:  assignForm.value.meetingPoint,
      scheduledTime: assignForm.value.scheduledTime ? new Date(assignForm.value.scheduledTime).toISOString() : null,
      notes:         assignForm.value.notes,
    })
    success.value = 'Driver assigned!'
    showAssign.value = false
    await refresh()
  } catch (e) { error.value = e.response?.data?.message || 'Failed to assign.' }
  finally { saving.value = false }
}

async function updateStatus(transferId, status) {
  updatingId.value = transferId
  try {
    await itineraryApi.updateStatus(transferId, parseInt(status))
    success.value = 'Status updated!'
    await refresh()
  } catch (e) { error.value = e.response?.data?.message || 'Failed.' }
  finally { updatingId.value = null }
}

async function removeTransfer(transferId) {
  if (!confirm('Remove this driver assignment?')) return
  try {
    await itineraryApi.deleteTransfer(transferId)
    success.value = 'Assignment removed.'
    await refresh()
  } catch { error.value = 'Failed to remove.' }
}

onMounted(loadToday)
</script>

<template>
  <div class="p-6 space-y-6">

    <!-- Header -->
    <div>
      <h1 class="text-2xl font-bold text-white">✈️ Itinerary & Airport Transfers</h1>
      <p class="text-slate-400 text-sm mt-1">Manage pilgrim arrivals, departures and airport pickup/drop assignments</p>
    </div>

    <div v-if="success" class="bg-green-900/50 border border-green-700 text-green-300 rounded-lg px-4 py-3 flex justify-between">
      {{ success }}<button @click="success=''" class="text-green-500">✕</button>
    </div>
    <div v-if="error" class="bg-red-900/50 border border-red-700 text-red-300 rounded-lg px-4 py-3 flex justify-between">
      {{ error }}<button @click="error=''" class="text-red-500">✕</button>
    </div>

    <!-- Tabs + Send Reminders -->
    <div class="flex items-center justify-between flex-wrap gap-3">
      <div class="flex gap-1 bg-dark-800 rounded-xl p-1 border border-dark-600">
        <button v-for="[t,label] in [['today','📅 Today'],['upcoming','📆 Upcoming'],['arrivals','🛬 Arrivals'],['departures','🛫 Departures']]"
          :key="t" @click="switchTab(t)"
          :class="['px-4 py-2 rounded-lg text-sm font-medium transition-all', tab === t ? 'bg-primary-700 text-white' : 'text-slate-400 hover:text-white']">
          {{ label }}
        </button>
      </div>
      <button @click="triggerReminders" :disabled="sendingReminders"
        class="flex items-center gap-2 bg-gold-800 hover:bg-gold-700 disabled:opacity-50 text-white text-sm font-semibold px-4 py-2 rounded-xl transition-colors">
        <span v-if="sendingReminders" class="animate-spin">⏳</span>
        <span v-else>📲</span>
        {{ sendingReminders ? 'Sending...' : 'Send Reminders Now' }}
      </button>
    </div>

    <div v-if="loading" class="text-center py-20 text-slate-400">Loading...</div>

    <!-- ── TODAY ─────────────────────────────────────────────────────────────── -->
    <template v-else-if="tab === 'today' && today">
      <div class="grid grid-cols-2 md:grid-cols-4 gap-4">
        <div class="card text-center py-4">
          <div class="text-3xl font-bold text-green-400">{{ today.arrivingCount }}</div>
          <div class="text-slate-400 text-xs mt-1">🛬 Arriving Today</div>
        </div>
        <div class="card text-center py-4">
          <div class="text-3xl font-bold text-blue-400">{{ today.departingCount }}</div>
          <div class="text-slate-400 text-xs mt-1">🛫 Departing Today</div>
        </div>
        <div class="card text-center py-4">
          <div class="text-3xl font-bold text-red-400">{{ today.pendingPickups }}</div>
          <div class="text-slate-400 text-xs mt-1">⚠️ Pickups Unassigned</div>
        </div>
        <div class="card text-center py-4">
          <div class="text-3xl font-bold text-orange-400">{{ today.pendingDropoffs }}</div>
          <div class="text-slate-400 text-xs mt-1">⚠️ Dropoffs Unassigned</div>
        </div>
      </div>

      <div class="card">
        <h3 class="text-white font-semibold mb-4">🛬 Today's Arrivals
          <span class="text-slate-500 font-normal text-sm">({{ today.arrivals?.length || 0 }})</span>
        </h3>
        <div v-if="today.arrivals?.length">
          <PilgrimTransferRows :rows="today.arrivals" :type="1"
            :statusColors="statusColors" :statusIcons="statusIcons" :statusOptions="statusOptions"
            :updatingId="updatingId" :wa="wa" :fmtDate="fmtDate" :fmtTime="fmtTime"
            @assign="openAssign" @status="updateStatus" @remove="removeTransfer" />
        </div>
        <div v-else class="text-center py-8 text-slate-500 text-sm">No arrivals today.</div>
      </div>

      <div class="card">
        <h3 class="text-white font-semibold mb-4">🛫 Today's Departures
          <span class="text-slate-500 font-normal text-sm">({{ today.departures?.length || 0 }})</span>
        </h3>
        <div v-if="today.departures?.length">
          <PilgrimTransferRows :rows="today.departures" :type="2"
            :statusColors="statusColors" :statusIcons="statusIcons" :statusOptions="statusOptions"
            :updatingId="updatingId" :wa="wa" :fmtDate="fmtDate" :fmtTime="fmtTime"
            @assign="openAssign" @status="updateStatus" @remove="removeTransfer" />
        </div>
        <div v-else class="text-center py-8 text-slate-500 text-sm">No departures today.</div>
      </div>
    </template>

    <!-- ── UPCOMING ──────────────────────────────────────────────────────────── -->
    <template v-else-if="tab === 'upcoming'">

      <div class="flex items-center gap-4 flex-wrap">
        <div class="flex items-center gap-2">
          <label class="text-slate-400 text-sm">Show next</label>
          <select v-model.number="upcomingDays" @change="loadUpcoming" class="input w-auto text-sm">
            <option :value="3">3 days</option>
            <option :value="7">7 days</option>
            <option :value="14">14 days</option>
            <option :value="30">30 days</option>
          </select>
        </div>
        <div class="flex flex-col gap-1">
          <p class="text-slate-500 text-sm">📅 <strong class="text-slate-400">8:00 AM UTC</strong> — 2-day advance reminders sent to all travellers.</p>
          <p class="text-slate-500 text-sm">⏱️ <strong class="text-slate-400">Every 30 min</strong> — 5-hour-before reminder fires when flight is 5h away.</p>
          <p class="text-slate-500 text-sm">🌙 <strong class="text-red-400">8:00 PM UTC</strong> — If driver not assigned for tomorrow: pilgrim notified, admin alerted. <em class="text-slate-600">Auto safety net.</em></p>
        </div>
      </div>

      <div v-if="!upcoming?.dates?.length" class="card text-center py-16 text-slate-500">
        <div class="text-5xl mb-3">📆</div>
        <p>No upcoming travel in the next {{ upcomingDays }} days.</p>
      </div>

      <div v-for="day in upcoming?.dates" :key="day.date" class="card space-y-4">
        <div class="flex items-center justify-between">
          <h3 class="text-white font-bold text-base">📅 {{ day.dayLabel }}</h3>
          <div class="flex gap-3 text-xs">
            <span v-if="day.arrivingCount" class="text-green-400 bg-green-900/40 px-2 py-0.5 rounded-full">
              🛬 {{ day.arrivingCount }} arriving
            </span>
            <span v-if="day.departingCount" class="text-blue-400 bg-blue-900/40 px-2 py-0.5 rounded-full">
              🛫 {{ day.departingCount }} departing
            </span>
          </div>
        </div>

        <div v-if="day.arrivals?.length">
          <p class="text-green-400 text-xs font-semibold uppercase tracking-wider mb-2">🛬 Arrivals</p>
          <UpcomingMiniTable :rows="day.arrivals" :statusColors="statusColors" :statusIcons="statusIcons"
            :wa="wa" @assign="openAssign($event, 1)" />
        </div>

        <div v-if="day.departures?.length">
          <p class="text-blue-400 text-xs font-semibold uppercase tracking-wider mb-2">🛫 Departures</p>
          <UpcomingMiniTable :rows="day.departures" :statusColors="statusColors" :statusIcons="statusIcons"
            :wa="wa" @assign="openAssign($event, 2)" />
        </div>
      </div>

    </template>

    <!-- ── ARRIVALS ───────────────────────────────────────────────────────────── -->
    <template v-else-if="tab === 'arrivals'">
      <div class="flex items-center gap-3">
        <label class="text-slate-400 text-sm">Date:</label>
        <input v-model="arrivalDate" type="date" class="input w-auto" @change="loadArrivals" />
      </div>
      <div class="card">
        <h3 class="text-white font-semibold mb-4">🛬 Arrivals on {{ fmtDate(arrivalDate) }}
          <span class="text-slate-500 font-normal text-sm">({{ arrivals.length }})</span>
        </h3>
        <div v-if="arrivals.length">
          <PilgrimTransferRows :rows="arrivals" :type="1"
            :statusColors="statusColors" :statusIcons="statusIcons" :statusOptions="statusOptions"
            :updatingId="updatingId" :wa="wa" :fmtDate="fmtDate" :fmtTime="fmtTime"
            @assign="openAssign" @status="updateStatus" @remove="removeTransfer" />
        </div>
        <div v-else class="text-center py-12 text-slate-500">
          <div class="text-5xl mb-3">🛬</div><p>No arrivals on this date.</p>
        </div>
      </div>
    </template>

    <!-- ── DEPARTURES ─────────────────────────────────────────────────────────── -->
    <template v-else-if="tab === 'departures'">
      <div class="flex items-center gap-3">
        <label class="text-slate-400 text-sm">Date:</label>
        <input v-model="departureDate" type="date" class="input w-auto" @change="loadDepartures" />
      </div>
      <div class="card">
        <h3 class="text-white font-semibold mb-4">🛫 Departures on {{ fmtDate(departureDate) }}
          <span class="text-slate-500 font-normal text-sm">({{ departures.length }})</span>
        </h3>
        <div v-if="departures.length">
          <PilgrimTransferRows :rows="departures" :type="2"
            :statusColors="statusColors" :statusIcons="statusIcons" :statusOptions="statusOptions"
            :updatingId="updatingId" :wa="wa" :fmtDate="fmtDate" :fmtTime="fmtTime"
            @assign="openAssign" @status="updateStatus" @remove="removeTransfer" />
        </div>
        <div v-else class="text-center py-12 text-slate-500">
          <div class="text-5xl mb-3">🛫</div><p>No departures on this date.</p>
        </div>
      </div>
    </template>

    <!-- ── ASSIGN DRIVER MODAL ────────────────────────────────────────────────── -->
    <Teleport to="body">
      <div v-if="showAssign" class="fixed inset-0 bg-black/70 z-50 flex items-center justify-center p-4">
        <div class="bg-dark-900 border border-dark-600 rounded-2xl p-6 w-full max-w-lg shadow-2xl max-h-[90vh] overflow-y-auto">
          <div class="flex items-center justify-between mb-5">
            <div>
              <h3 class="text-white font-bold text-lg">
                {{ assignType === 1 ? '🛬 Assign Pickup Driver' : '🛫 Assign Dropoff Driver' }}
              </h3>
              <p class="text-slate-400 text-sm mt-0.5">
                {{ assignRow?.fullName }} — {{ assignRow?.flightNumber || 'No flight set' }}
                <span v-if="assignRow?.airport"> · {{ assignRow.airport }}</span>
              </p>
            </div>
            <button @click="showAssign = false" class="text-slate-500 hover:text-white text-xl">✕</button>
          </div>

          <div class="space-y-3">
            <div class="grid grid-cols-2 gap-3">
              <div>
                <label class="label">Driver Name *</label>
                <input v-model="assignForm.driverName" type="text" class="input" placeholder="Full name" />
              </div>
              <div>
                <label class="label">Driver Phone</label>
                <input v-model="assignForm.driverPhone" type="text" class="input" placeholder="+964..." />
              </div>
            </div>
            <div class="grid grid-cols-2 gap-3">
              <div>
                <label class="label">Vehicle Number</label>
                <input v-model="assignForm.vehicleNumber" type="text" class="input" placeholder="BAS-1234" />
              </div>
              <div>
                <label class="label">Vehicle Type</label>
                <select v-model="assignForm.vehicleType" class="input">
                  <option v-for="v in vehicleTypes" :key="v" :value="v">{{ v }}</option>
                </select>
              </div>
            </div>
            <div>
              <label class="label">{{ assignType === 1 ? 'Meeting Point at Airport' : 'Pickup Location' }}</label>
              <input v-model="assignForm.meetingPoint" type="text" class="input"
                :placeholder="assignType === 1 ? 'Arrivals Hall Gate 2' : 'Hotel lobby / room number'" />
            </div>
            <div>
              <label class="label">Scheduled Time</label>
              <input v-model="assignForm.scheduledTime" type="datetime-local" class="input" />
            </div>
            <div>
              <label class="label">Notes</label>
              <textarea v-model="assignForm.notes" class="input h-20 resize-none" placeholder="Special instructions..."></textarea>
            </div>
          </div>

          <div class="flex gap-3 mt-5">
            <button @click="submitAssign" :disabled="saving || !assignForm.driverName"
              class="btn-primary flex-1 disabled:opacity-50">
              {{ saving ? 'Saving...' : assignRow?.transfer ? 'Update Assignment' : 'Assign Driver' }}
            </button>
            <button @click="showAssign = false" class="px-4 py-2 text-slate-400 hover:text-white text-sm border border-dark-600 rounded-xl">Cancel</button>
          </div>
        </div>
      </div>
    </Teleport>

  </div>
</template>

<!-- ── Inline sub-component (table rows) ─────────────────────────────────────── -->
<script>
export default {
  components: {
    UpcomingMiniTable: {
      props: { rows: Array, statusColors: Object, statusIcons: Object, wa: Function },
      emits: ['assign'],
      template: `
        <div class="overflow-x-auto">
          <table class="w-full text-xs">
            <thead>
              <tr class="text-slate-600 border-b border-dark-700 uppercase">
                <th class="text-left pb-1.5 font-medium pr-3">Pilgrim</th>
                <th class="text-left pb-1.5 font-medium pr-3">Flight / Airport</th>
                <th class="text-left pb-1.5 font-medium pr-3">Time</th>
                <th class="text-left pb-1.5 font-medium pr-3">Family</th>
                <th class="text-left pb-1.5 font-medium pr-3">Pickup Status</th>
                <th class="pb-1.5"></th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="row in rows" :key="row.pilgrimId" class="border-b border-dark-800 hover:bg-dark-800/30">
                <td class="py-2 pr-3">
                  <p class="text-white font-semibold">{{ row.fullName }}</p>
                  <p class="text-slate-500">{{ row.country }}</p>
                  <a v-if="wa(row.whatsApp || row.phone)" :href="wa(row.whatsApp || row.phone)"
                    target="_blank" class="text-green-400 hover:underline">💬 WA</a>
                </td>
                <td class="py-2 pr-3">
                  <p class="text-white">{{ row.flightNumber || '—' }}</p>
                  <p class="text-slate-500">{{ row.airport || 'Airport not set' }}</p>
                </td>
                <td class="py-2 pr-3 text-white">{{ row.time || '—' }}</td>
                <td class="py-2 pr-3">
                  <span class="text-gold-400 font-semibold">{{ row.familyCount }}</span>
                </td>
                <td class="py-2 pr-3">
                  <span :class="['px-2 py-0.5 rounded-full font-medium', statusColors[row.transferStatus] || 'text-slate-400 bg-slate-800']">
                    {{ statusIcons[row.transferStatus] }} {{ row.transferStatus }}
                  </span>
                  <p v-if="row.transfer?.driverName" class="text-slate-400 mt-0.5">
                    🚗 {{ row.transfer.driverName }}
                  </p>
                </td>
                <td class="py-2">
                  <button @click="$emit('assign', row)"
                    class="px-2 py-1 rounded bg-primary-800 hover:bg-primary-700 text-white whitespace-nowrap">
                    {{ row.transfer ? '✏️ Edit' : '+ Assign' }}
                  </button>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      `
    },
    PilgrimTransferRows: {
      props: {
        rows: Array, type: Number,
        statusColors: Object, statusIcons: Object, statusOptions: Array,
        updatingId: String, wa: Function, fmtDate: Function, fmtTime: Function,
      },
      emits: ['assign', 'status', 'remove'],
      template: `
        <div class="overflow-x-auto">
          <table class="w-full text-sm">
            <thead>
              <tr class="text-slate-500 border-b border-dark-600 text-xs uppercase">
                <th class="text-left pb-2 font-medium pr-3">Pilgrim</th>
                <th class="text-left pb-2 font-medium pr-3">Flight / Airport</th>
                <th class="text-left pb-2 font-medium pr-3">Date & Time</th>
                <th class="text-left pb-2 font-medium pr-3">Family</th>
                <th class="text-left pb-2 font-medium pr-3">Transfer Status</th>
                <th class="text-left pb-2 font-medium pr-3">Driver / Vehicle</th>
                <th class="pb-2"></th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="row in rows" :key="row.pilgrimId" class="border-b border-dark-700 hover:bg-dark-800/40">
                <td class="py-3 pr-3">
                  <p class="text-white font-semibold">{{ row.fullName }}</p>
                  <p class="text-slate-500 text-xs">{{ row.country }}</p>
                  <a v-if="wa(row.whatsApp || row.phone)" :href="wa(row.whatsApp || row.phone)"
                    target="_blank" class="text-xs text-green-400 hover:underline">💬 WhatsApp</a>
                </td>
                <td class="py-3 pr-3">
                  <p class="text-white">{{ row.flightNumber || '—' }}</p>
                  <p class="text-slate-400 text-xs">{{ row.airport || 'Airport not set' }}</p>
                </td>
                <td class="py-3 pr-3">
                  <p class="text-white">{{ fmtDate(row.date) }}</p>
                  <p class="text-slate-400 text-xs">{{ row.time || 'Time not set' }}</p>
                </td>
                <td class="py-3 pr-3">
                  <span class="text-gold-400 font-semibold">{{ row.familyCount }}</span>
                  <span class="text-slate-500 text-xs"> member{{ row.familyCount !== 1 ? 's' : '' }}</span>
                </td>
                <td class="py-3 pr-3">
                  <div class="flex items-center gap-2">
                    <span :class="['text-xs px-2 py-0.5 rounded-full font-medium', statusColors[row.transferStatus] || 'text-slate-400 bg-slate-800']">
                      {{ statusIcons[row.transferStatus] }} {{ row.transferStatus }}
                    </span>
                    <select v-if="row.transferId"
                      :disabled="updatingId === row.transferId"
                      @change="$emit('status', row.transferId, $event.target.value)"
                      class="text-xs bg-dark-700 border border-dark-600 text-slate-300 rounded px-1 py-0.5">
                      <option v-for="s in statusOptions" :key="s.value" :value="s.value">{{ s.label }}</option>
                    </select>
                  </div>
                  <div v-if="row.transfer?.scheduledTime" class="text-xs text-slate-500 mt-1">
                    🕐 {{ fmtTime(row.transfer.scheduledTime) }}
                  </div>
                </td>
                <td class="py-3 pr-3">
                  <div v-if="row.transfer?.driverName">
                    <p class="text-white text-xs font-semibold">{{ row.transfer.driverName }}</p>
                    <p class="text-slate-400 text-xs">{{ row.transfer.vehicleType }} · {{ row.transfer.vehicleNumber || '—' }}</p>
                    <a v-if="wa(row.transfer.driverPhone)" :href="wa(row.transfer.driverPhone)"
                      target="_blank" class="text-xs text-green-400 hover:underline">📞 {{ row.transfer.driverPhone }}</a>
                    <p v-if="row.transfer.meetingPoint" class="text-slate-500 text-xs mt-0.5">📍 {{ row.transfer.meetingPoint }}</p>
                  </div>
                  <span v-else class="text-slate-600 text-xs italic">Not assigned</span>
                </td>
                <td class="py-3">
                  <div class="flex flex-col gap-1">
                    <button @click="$emit('assign', row, type)"
                      class="text-xs px-2 py-1 rounded bg-primary-800 hover:bg-primary-700 text-white whitespace-nowrap">
                      {{ row.transfer ? '✏️ Edit' : '+ Assign' }}
                    </button>
                    <button v-if="row.transferId" @click="$emit('remove', row.transferId)"
                      class="text-xs px-2 py-1 rounded bg-red-900/50 hover:bg-red-900 text-red-400 whitespace-nowrap">
                      Remove
                    </button>
                  </div>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      `
    }
  }
}
</script>
