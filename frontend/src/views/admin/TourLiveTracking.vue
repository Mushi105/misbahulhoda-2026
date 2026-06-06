<template>
  <div class="p-6 space-y-6">

    <!-- Header -->
    <div class="flex items-center justify-between flex-wrap gap-3">
      <div>
        <h1 class="text-2xl font-bold text-emerald-300">🛰️ Tour Live Monitor</h1>
        <p class="text-emerald-600 text-sm mt-1">Track pilgrims through the Arbaeen 2026 journey day-by-day</p>
      </div>
      <div class="flex gap-2 items-center flex-wrap">
        <select v-model="selectedTourId" @change="load"
          class="bg-black/40 border border-emerald-800 rounded-lg px-3 py-2 text-emerald-200 text-sm">
          <option v-for="t in allTours" :key="t.id" :value="t.id">{{ t.tourName }}</option>
        </select>
        <button @click="openScheduleSetup" v-if="selectedTourId"
          class="px-3 py-2 rounded-lg bg-amber-900/60 border border-amber-700 text-amber-200 text-sm hover:bg-amber-800/60 transition">
          📅 Setup Schedule
        </button>
        <button @click="sendReminder" :disabled="reminderSending" v-if="selectedTourId"
          class="px-3 py-2 rounded-lg bg-blue-900/60 border border-blue-700 text-blue-200 text-sm hover:bg-blue-800/60 transition disabled:opacity-50">
          {{ reminderSending ? '...' : '📢 Send Reminder' }}
        </button>
        <button @click="load" :disabled="loading"
          class="px-3 py-2 rounded-lg bg-emerald-900/60 border border-emerald-700 text-emerald-200 text-sm hover:bg-emerald-800/60 transition disabled:opacity-50">
          {{ loading ? '⏳' : '🔄' }}
        </button>
      </div>
    </div>

    <!-- Alerts -->
    <div v-if="error" class="rounded-xl border border-red-800 bg-red-900/20 p-4 text-red-300 text-sm flex justify-between">
      {{ error }}<button @click="error=''" class="text-red-500 ml-3">✕</button>
    </div>
    <div v-if="successMsg" class="rounded-xl border border-emerald-700 bg-emerald-900/20 p-4 text-emerald-300 text-sm flex justify-between">
      ✅ {{ successMsg }}<button @click="successMsg=''" class="text-emerald-500 ml-3">✕</button>
    </div>

    <!-- No schedule warning -->
    <div v-if="data && !data.schedule?.length"
         class="rounded-xl border border-amber-800/50 p-4 text-amber-300 text-sm flex items-center gap-3"
         style="background:rgba(180,83,9,0.1);">
      <span class="text-2xl">⚠️</span>
      <div class="flex-1">
        <p class="font-semibold">Day Schedule not set</p>
        <p class="text-amber-400/70 text-xs mt-0.5">Live monitor will work once the 15-day schedule is set up.</p>
      </div>
      <button @click="openScheduleSetup"
        class="px-4 py-2 rounded-lg bg-amber-800/60 border border-amber-700 text-amber-200 text-sm hover:bg-amber-700/60 transition shrink-0">
        📅 Setup
      </button>
    </div>

    <!-- Today's Status Card -->
    <div v-if="data" class="grid grid-cols-2 md:grid-cols-4 gap-4">
      <div class="glass-card p-4 col-span-2 md:col-span-2">
        <p class="text-emerald-600 text-xs uppercase tracking-widest mb-1">Today — {{ fmtDate(data.today) }}</p>
        <div class="flex items-center gap-3">
          <span class="text-4xl">{{ data.todayIcon || '📍' }}</span>
          <div>
            <p class="text-white text-xl font-bold">{{ data.todayCity }}</p>
            <p class="text-emerald-400 text-sm">{{ data.todayActivity || 'Day ' + (data.todayDayNumber ?? '—') }}</p>
          </div>
        </div>
      </div>
      <div class="glass-card p-4 text-center">
        <p class="text-emerald-600 text-xs uppercase tracking-widest mb-1">Total Pilgrims</p>
        <p class="text-3xl font-bold text-emerald-300">{{ data.totalPilgrims }}</p>
      </div>
      <div class="glass-card p-4 text-center">
        <p class="text-emerald-600 text-xs uppercase tracking-widest mb-1">Currently in Iraq</p>
        <p class="text-3xl font-bold text-amber-300">{{ data.activePilgrims }}</p>
      </div>
    </div>

    <!-- 15-day Schedule Timeline -->
    <div v-if="data && data.schedule?.length" class="glass-card p-5">
      <h2 class="text-emerald-300 font-semibold mb-4">📅 15-Day Journey Timeline</h2>
      <div class="grid grid-cols-3 sm:grid-cols-5 md:grid-cols-8 lg:grid-cols-15 gap-2">
        <div v-for="day in data.schedule" :key="day.dayNumber"
          :class="['rounded-lg p-2 text-center border text-xs cursor-default transition',
            isToday(day.date)
              ? 'border-amber-500 bg-amber-900/30 ring-1 ring-amber-400'
              : day.isKeyDay
                ? 'border-emerald-600 bg-emerald-900/30'
                : 'border-emerald-900/60 bg-black/20']">
          <div class="text-lg">{{ day.icon || '📍' }}</div>
          <div class="font-bold" :class="isToday(day.date) ? 'text-amber-300' : 'text-emerald-300'">Day {{ day.dayNumber }}</div>
          <div :class="isToday(day.date) ? 'text-amber-200' : 'text-emerald-400'" class="truncate">{{ day.city }}</div>
          <div class="text-emerald-700">{{ fmtShortDate(day.date) }}</div>
          <div v-if="isToday(day.date)" class="mt-1 text-amber-400 font-bold text-xs">TODAY</div>
          <div v-else-if="day.isKeyDay" class="mt-1 text-emerald-500 text-xs">KEY</div>
        </div>
      </div>
    </div>

    <!-- Pilgrims Table -->
    <div v-if="data" class="glass-card overflow-hidden">
      <div class="px-5 py-4 border-b border-emerald-900/60 flex items-center justify-between flex-wrap gap-3">
        <h2 class="text-emerald-300 font-semibold">👥 Pilgrim Status</h2>
        <div class="flex gap-2 items-center">
          <input v-model="search" placeholder="Search pilgrim…"
            class="bg-black/30 border border-emerald-800 rounded-lg px-3 py-1.5 text-emerald-200 placeholder-emerald-700 text-sm w-48" />
          <select v-model="filterStatus"
            class="bg-black/30 border border-emerald-800 rounded-lg px-3 py-1.5 text-emerald-200 text-sm">
            <option value="">All</option>
            <option value="active">In Iraq</option>
            <option value="pending">Not Arrived</option>
            <option value="departed">Departed</option>
          </select>
        </div>
      </div>

      <div class="overflow-x-auto">
        <table class="w-full text-sm">
          <thead>
            <tr class="text-emerald-600 text-xs uppercase tracking-wider border-b border-emerald-900/40">
              <th class="px-4 py-3 text-left">#</th>
              <th class="px-4 py-3 text-left">Pilgrim</th>
              <th class="px-4 py-3 text-left">Country</th>
              <th class="px-4 py-3 text-left">Arrival</th>
              <th class="px-4 py-3 text-left">Departure</th>
              <th class="px-4 py-3 text-left">Current Location</th>
              <th class="px-4 py-3 text-left">Status</th>
              <th class="px-4 py-3 text-left">WhatsApp</th>
            </tr>
          </thead>
          <tbody>
            <tr v-if="!filteredPilgrims.length">
              <td colspan="8" class="px-4 py-8 text-center text-emerald-700">No pilgrims found</td>
            </tr>
            <tr v-for="(p, i) in filteredPilgrims" :key="p.id"
              class="border-b border-emerald-900/30 hover:bg-emerald-900/10 transition">
              <td class="px-4 py-3 text-emerald-700">{{ i + 1 }}</td>
              <td class="px-4 py-3 text-white font-medium">{{ p.fullName }}</td>
              <td class="px-4 py-3 text-emerald-400">{{ p.country || '—' }}</td>
              <td class="px-4 py-3 text-emerald-300">{{ fmtDate(p.arrivalDate) }}</td>
              <td class="px-4 py-3 text-emerald-300">{{ fmtDate(p.departureDate) }}</td>
              <td class="px-4 py-3">
                <span :class="['px-2 py-0.5 rounded-full text-xs font-medium',
                  p.isCurrentlyInIraq
                    ? 'bg-emerald-900/50 text-emerald-300 border border-emerald-700'
                    : p.todayCity === 'Not arrived yet'
                      ? 'bg-amber-900/40 text-amber-300 border border-amber-800'
                      : 'bg-slate-800/50 text-slate-400 border border-slate-700']">
                  {{ p.isCurrentlyInIraq ? '📍 ' + p.todayCity : p.todayCity }}
                </span>
              </td>
              <td class="px-4 py-3">
                <span :class="['px-2 py-0.5 rounded-full text-xs',
                  p.isCurrentlyInIraq
                    ? 'bg-green-900/40 text-green-400'
                    : p.todayCity === 'Not arrived yet'
                      ? 'bg-amber-900/30 text-amber-400'
                      : 'bg-slate-800/30 text-slate-500']">
                  {{ p.isCurrentlyInIraq ? '✅ Active' : p.todayCity === 'Not arrived yet' ? '⏳ Pending' : '✈️ Departed' }}
                </span>
              </td>
              <td class="px-4 py-3">
                <a v-if="p.whatsApp"
                  :href="`https://wa.me/${p.whatsApp.replace(/\D/g, '')}`" target="_blank"
                  class="text-green-400 hover:text-green-300 text-xs transition">📱 Message</a>
                <span v-else class="text-emerald-900 text-xs">—</span>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>

    <!-- Loading Skeleton -->
    <div v-if="loading && !data" class="space-y-4">
      <div v-for="n in 3" :key="n" class="glass-card h-20 animate-pulse" />
    </div>

  </div>

  <!-- ── Day Schedule Modal ─────────────────────────────────────────────── -->
  <Teleport to="body">
    <div v-if="showScheduleModal"
         class="fixed inset-0 z-50 flex items-start justify-center p-4 overflow-y-auto"
         style="background:rgba(0,0,0,0.85);">
      <div class="w-full max-w-3xl rounded-2xl overflow-hidden shadow-2xl my-4"
           style="background:#0d1f14; border:1px solid rgba(16,185,129,0.2);">

        <div class="flex items-center justify-between px-5 py-4 border-b border-emerald-900/40">
          <div>
            <h3 class="text-white font-bold text-lg">📅 15-Day Journey Schedule</h3>
            <p class="text-slate-400 text-xs mt-0.5">Set the city and programme for each day</p>
          </div>
          <button @click="showScheduleModal=false" class="text-slate-400 hover:text-white text-2xl">✕</button>
        </div>

        <!-- City presets -->
        <div class="px-5 py-3 border-b border-emerald-900/40">
          <p class="text-xs text-slate-500 mb-2">Quick presets (click any day to apply):</p>
          <div class="flex flex-wrap gap-2">
            <span v-for="p in CITY_PRESETS" :key="p.city+p.activity"
                  class="text-xs px-2 py-1 rounded-lg border border-emerald-900/40 text-emerald-400 cursor-help"
                  :title="p.activity">
              {{ p.icon }} {{ p.city }}
            </span>
          </div>
        </div>

        <!-- Days list -->
        <div class="p-5 space-y-2 max-h-[60vh] overflow-y-auto">
          <div v-for="day in scheduleForm" :key="day.dayNumber"
               :class="['rounded-xl border p-3', day.isKeyDay ? 'border-amber-700/50 bg-amber-950/20' : 'border-emerald-900/30 bg-black/20']">
            <div class="flex items-center gap-2 mb-2">
              <span class="text-emerald-600 text-xs font-bold w-12 shrink-0">Day {{ day.dayNumber }}</span>
              <input v-model="day.date" type="date"
                class="bg-black/30 border border-emerald-900/40 rounded-lg px-2 py-1 text-emerald-200 text-xs w-36 shrink-0" />
              <input v-model="day.icon" type="text"
                class="bg-black/30 border border-emerald-900/40 rounded-lg px-2 py-1 text-white text-sm w-12 text-center" placeholder="📍" />
              <label class="flex items-center gap-1 text-xs text-amber-400 cursor-pointer ml-auto shrink-0">
                <input v-model="day.isKeyDay" type="checkbox" class="accent-amber-500" />
                Key Day
              </label>
            </div>
            <div class="grid grid-cols-2 gap-2">
              <div class="flex gap-1">
                <input v-model="day.city" class="bg-black/30 border border-emerald-900/40 rounded-lg px-2 py-1.5 text-white text-xs flex-1" placeholder="City (e.g. Karbala)" />
                <!-- Quick preset buttons -->
                <div class="relative group">
                  <button class="text-emerald-700 hover:text-emerald-400 text-xs px-1.5 py-1.5 rounded border border-emerald-900/30 hover:border-emerald-700/50">▼</button>
                  <div class="absolute right-0 top-full mt-1 z-20 bg-dark-900 border border-emerald-800/40 rounded-xl shadow-xl hidden group-focus-within:block group-hover:block w-48">
                    <button v-for="p in CITY_PRESETS" :key="p.city+p.icon"
                      @click="applyPreset(day, p)"
                      class="w-full text-left text-xs px-3 py-2 text-emerald-300 hover:bg-emerald-900/30 transition">
                      {{ p.icon }} {{ p.city }} — {{ p.activity.substring(0,20) }}
                    </button>
                  </div>
                </div>
              </div>
              <input v-model="day.activity" class="bg-black/30 border border-emerald-900/40 rounded-lg px-2 py-1.5 text-white text-xs" placeholder="Activity / Programme" />
            </div>
          </div>
        </div>

        <div class="px-5 py-4 border-t border-emerald-900/40 flex gap-3">
          <button @click="saveSchedule" :disabled="savingSchedule"
            class="flex-1 py-2.5 rounded-xl font-semibold text-white disabled:opacity-50 text-sm transition"
            style="background:linear-gradient(135deg,#065f46,#047857);">
            {{ savingSchedule ? 'Saving...' : '✅ Save Schedule' }}
          </button>
          <button @click="showScheduleModal=false" class="px-5 py-2.5 rounded-xl border border-emerald-900/40 text-slate-400 hover:text-white text-sm">
            Cancel
          </button>
        </div>
      </div>
    </div>
  </Teleport>

</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import { toursApi } from '@/api/index.js'

const loading       = ref(false)
const error         = ref('')
const data          = ref(null)
const allTours      = ref([])
const selectedTourId = ref('')
const search        = ref('')
const filterStatus  = ref('')

const ARBAEEN_TOUR_ID = '10000000-0000-0000-0000-000000000001'

// ── Day Schedule Setup ─────────────────────────────────────────────────────
const showScheduleModal = ref(false)
const scheduleForm = ref([])
const savingSchedule = ref(false)
const reminderSending = ref(false)
const successMsg = ref('')

const CITY_PRESETS = [
  { city: 'Lahore',   country: 'Pakistan', icon: '✈️',  activity: 'Departure from Pakistan' },
  { city: 'Dubai',    country: 'UAE',      icon: '🛫',  activity: 'Transit — Dubai Airport' },
  { city: 'Baghdad',  country: 'Iraq',     icon: '🛬',  activity: 'Arrival — Baghdad Airport' },
  { city: 'Karbala',  country: 'Iraq',     icon: '🕌',  activity: 'Ziyarat — Imam Hussain (AS)' },
  { city: 'Najaf',    country: 'Iraq',     icon: '🌹',  activity: 'Ziyarat — Imam Ali (AS)', isKeyDay: true },
  { city: 'Karbala',  country: 'Iraq',     icon: '🚶',  activity: 'Arbaeen Walk', isKeyDay: true },
  { city: 'Karbala',  country: 'Iraq',     icon: '🕌',  activity: 'Arbaeen Day — Ziyarat', isKeyDay: true },
  { city: 'Baghdad',  country: 'Iraq',     icon: '✈️',  activity: 'Departure from Baghdad' },
  { city: 'Lahore',   country: 'Pakistan', icon: '🏠',  activity: 'Return to Pakistan' },
]

function openScheduleSetup() {
  // pre-fill based on existing schedule or blank rows
  const existing = data.value?.schedule || []
  const days = []
  const tourStartDate = allTours.value.find(t => t.id === selectedTourId.value)
  const startDate = tourStartDate?.startDate ? new Date(tourStartDate.startDate) : new Date()

  for (let i = 1; i <= 15; i++) {
    const found = existing.find(e => e.dayNumber === i)
    const d = new Date(startDate)
    d.setDate(startDate.getDate() + i - 1)
    days.push({
      dayNumber: i,
      date: found?.date ? found.date.split('T')[0] : d.toISOString().split('T')[0],
      city: found?.city || '',
      country: found?.country || 'Iraq',
      hotel: found?.hotel || '',
      activity: found?.activity || '',
      activityUrdu: found?.activityUrdu || '',
      isKeyDay: found?.isKeyDay || false,
      icon: found?.icon || '📍',
    })
  }
  scheduleForm.value = days
  showScheduleModal.value = true
}

function applyPreset(day, preset) {
  day.city = preset.city
  day.country = preset.country
  day.icon = preset.icon
  day.activity = preset.activity
  if (preset.isKeyDay) day.isKeyDay = true
}

async function saveSchedule() {
  savingSchedule.value = true
  try {
    await toursApi.saveDaySchedules(selectedTourId.value, scheduleForm.value.map(d => ({
      dayNumber: d.dayNumber, date: d.date, city: d.city,
      country: d.country, hotel: d.hotel, activity: d.activity,
      activityUrdu: d.activityUrdu, notes: null, isKeyDay: d.isKeyDay, icon: d.icon
    })))
    successMsg.value = 'Day schedule saved!'
    showScheduleModal.value = false
    await load()
    setTimeout(() => successMsg.value = '', 4000)
  } catch (e) {
    error.value = e.response?.data?.message || 'Failed to save schedule.'
  } finally { savingSchedule.value = false }
}

async function sendReminder() {
  if (!confirm('Send tour date reminder to all approved pilgrims?')) return
  reminderSending.value = true
  try {
    const res = await toursApi.sendTourReminder(selectedTourId.value)
    successMsg.value = res.data?.message || 'Reminder sent!'
    setTimeout(() => successMsg.value = '', 5000)
  } catch (e) {
    error.value = e.response?.data?.message || 'Failed to send reminder.'
  } finally { reminderSending.value = false }
}

async function fetchTours() {
  try {
    const res = await toursApi.getAll()
    allTours.value = res.data?.data ?? []
    if (!selectedTourId.value && allTours.value.length) {
      const arbaeen = allTours.value.find(t => t.id === ARBAEEN_TOUR_ID)
      selectedTourId.value = arbaeen?.id ?? allTours.value[0].id
    }
  } catch { /* ignore */ }
}

async function load() {
  if (!selectedTourId.value) return
  loading.value = true
  error.value   = ''
  try {
    const res  = await toursApi.getLiveTracking(selectedTourId.value)
    data.value = res.data?.data ?? null
  } catch (e) {
    error.value = e.response?.data?.message || 'Failed to load tracking data.'
  } finally {
    loading.value = false
  }
}

const filteredPilgrims = computed(() => {
  if (!data.value?.pilgrims) return []
  let list = data.value.pilgrims
  if (search.value.trim()) {
    const q = search.value.toLowerCase()
    list = list.filter(p =>
      p.fullName?.toLowerCase().includes(q) || p.country?.toLowerCase().includes(q)
    )
  }
  if (filterStatus.value === 'active')   list = list.filter(p => p.isCurrentlyInIraq)
  if (filterStatus.value === 'pending')  list = list.filter(p => !p.isCurrentlyInIraq && p.todayCity === 'Not arrived yet')
  if (filterStatus.value === 'departed') list = list.filter(p => !p.isCurrentlyInIraq && p.todayCity === 'Departed')
  return list
})

function isToday(dateStr) {
  const today = new Date()
  const d     = new Date(dateStr)
  return d.getUTCFullYear() === today.getUTCFullYear() &&
         d.getUTCMonth()    === today.getUTCMonth() &&
         d.getUTCDate()     === today.getUTCDate()
}

function fmtDate(d) {
  if (!d) return '—'
  return new Date(d).toLocaleDateString('en-GB', { day: '2-digit', month: 'short', year: 'numeric' })
}

function fmtShortDate(d) {
  if (!d) return ''
  return new Date(d).toLocaleDateString('en-GB', { day: '2-digit', month: 'short' })
}

onMounted(async () => {
  await fetchTours()
  await load()
})
</script>

<style scoped>
.glass-card {
  background: rgba(2, 20, 10, 0.5);
  backdrop-filter: blur(8px);
  border: 1px solid rgba(6, 78, 40, 0.4);
  border-radius: 0.75rem;
}
</style>
