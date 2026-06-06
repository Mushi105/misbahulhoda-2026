<template>
  <div class="p-6 space-y-6">

    <!-- Header -->
    <div class="flex items-center justify-between flex-wrap gap-3">
      <div>
        <h1 class="text-2xl font-bold text-emerald-300">🛰️ Tour Live Monitor</h1>
        <p class="text-emerald-600 text-sm mt-1">Track pilgrims through the Arbaeen 2026 journey day-by-day</p>
      </div>
      <div class="flex gap-2 items-center">
        <button @click="load" :disabled="loading"
          class="px-4 py-2 rounded-lg bg-emerald-900/60 border border-emerald-700 text-emerald-200 text-sm hover:bg-emerald-800/60 transition disabled:opacity-50">
          {{ loading ? '⏳ Loading…' : '🔄 Refresh' }}
        </button>
        <select v-model="selectedTourId" @change="load"
          class="bg-black/40 border border-emerald-800 rounded-lg px-3 py-2 text-emerald-200 text-sm">
          <option v-for="t in allTours" :key="t.id" :value="t.id">{{ t.tourName }}</option>
        </select>
      </div>
    </div>

    <!-- Error -->
    <div v-if="error" class="rounded-xl border border-red-800 bg-red-900/20 p-4 text-red-300 text-sm">{{ error }}</div>

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
