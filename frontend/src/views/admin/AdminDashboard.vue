<script setup>
import { ref, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { adminApi, itineraryApi } from '@/api'
import NoticeBoard from '@/components/NoticeBoard.vue'

const router = useRouter()
const stats = ref(null)
const loading = ref(true)
const todayTravel = ref(null)
const upcomingDays = ref(null)

onMounted(async () => {
  try {
    const [dashRes, todayRes, upcomingRes] = await Promise.all([
      adminApi.getDashboard(),
      itineraryApi.getToday().catch(() => null),
      itineraryApi.getUpcoming(3).catch(() => null),
    ])
    stats.value = dashRes.data.data
    todayTravel.value = todayRes?.data?.data || null
    upcomingDays.value = upcomingRes?.data?.data || null
  } catch (e) {
    console.error(e)
  } finally {
    loading.value = false
  }
})

function goToPilgrims(status = '') {
  router.push({ path: '/admin/pilgrims', query: status ? { status } : {} })
}

function goToVolunteers(status = '') {
  router.push({ path: '/admin/volunteers', query: status ? { status } : {} })
}
</script>

<template>
  <div class="space-y-6">

    <div>
      <h1 class="text-2xl font-bold text-gray-900">Admin Dashboard</h1>
      <p class="text-gray-700 text-sm mt-1">Arbaeen 2026 — Real-time overview</p>
    </div>

    <div v-if="loading" class="text-slate-400 text-center py-12">Loading dashboard...</div>

    <template v-else-if="stats">

      <!-- Pilgrims -->
      <div>
        <div class="flex items-center justify-between mb-3">
          <h2 class="text-sm font-semibold text-slate-400 uppercase tracking-wider">Pilgrims</h2>
          <button @click="goToPilgrims()" class="text-xs text-amber-700 hover:text-amber-600">View All →</button>
        </div>
        <div class="grid grid-cols-2 lg:grid-cols-4 gap-4">
          <button @click="goToPilgrims()" class="stat-card text-left hover:border-primary-700  transition-all cursor-pointer">
            <div class="stat-icon bg-amber-100">🕌</div>
            <div>
              <p class="text-2xl font-bold text-gray-900">{{ stats.pilgrims.total }}</p>
              <p class="text-gray-600 text-xs">Total Pilgrims</p>
            </div>
          </button>
          <button @click="goToPilgrims('Pending')" class="stat-card text-left hover:border-gold-700  transition-all cursor-pointer">
            <div class="stat-icon bg-gold-900">⏳</div>
            <div>
              <p class="text-2xl font-bold text-amber-700">{{ stats.pilgrims.pending }}</p>
              <p class="text-gray-600 text-xs">Pending</p>
            </div>
          </button>
          <button @click="goToPilgrims('Approved')" class="stat-card text-left hover:border-green-700  transition-all cursor-pointer">
            <div class="stat-icon bg-green-100">✅</div>
            <div>
              <p class="text-2xl font-bold text-green-700">{{ stats.pilgrims.approved }}</p>
              <p class="text-gray-600 text-xs">Approved</p>
            </div>
          </button>
          <button @click="goToPilgrims('Rejected')" class="stat-card text-left hover:border-red-700  transition-all cursor-pointer">
            <div class="stat-icon bg-red-100">❌</div>
            <div>
              <p class="text-2xl font-bold text-red-600">{{ stats.pilgrims.rejected }}</p>
              <p class="text-gray-600 text-xs">Rejected</p>
            </div>
          </button>
        </div>
      </div>

      <!-- Volunteers -->
      <div>
        <div class="flex items-center justify-between mb-3">
          <h2 class="text-sm font-semibold text-slate-400 uppercase tracking-wider">Volunteers</h2>
          <button @click="goToVolunteers()" class="text-xs text-amber-700 hover:text-amber-600">View All →</button>
        </div>
        <div class="grid grid-cols-2 lg:grid-cols-4 gap-4">
          <button @click="goToVolunteers()" class="stat-card text-left hover:border-blue-700  transition-all cursor-pointer">
            <div class="stat-icon bg-blue-100">🤝</div>
            <div>
              <p class="text-2xl font-bold text-gray-900">{{ stats.volunteers.total }}</p>
              <p class="text-gray-600 text-xs">Total</p>
            </div>
          </button>
          <button @click="goToVolunteers('Available')" class="stat-card text-left hover:border-green-700  transition-all cursor-pointer">
            <div class="stat-icon bg-amber-100">🟢</div>
            <div>
              <p class="text-2xl font-bold text-amber-700">{{ stats.volunteers.available }}</p>
              <p class="text-gray-600 text-xs">Available</p>
            </div>
          </button>
          <button @click="goToVolunteers('Busy')" class="stat-card text-left hover:border-yellow-700  transition-all cursor-pointer">
            <div class="stat-icon bg-yellow-100">🔵</div>
            <div>
              <p class="text-2xl font-bold text-yellow-400">{{ stats.volunteers.busy }}</p>
              <p class="text-gray-600 text-xs">Busy</p>
            </div>
          </button>
          <button @click="goToVolunteers('Offline')" class="stat-card text-left hover:border-slate-600  transition-all cursor-pointer">
            <div class="stat-icon bg-slate-800">⚫</div>
            <div>
              <p class="text-2xl font-bold text-slate-400">{{ stats.volunteers.offline }}</p>
              <p class="text-gray-600 text-xs">Offline</p>
            </div>
          </button>
        </div>
      </div>

      <!-- Accommodation & Transport -->
      <div class="grid grid-cols-1 lg:grid-cols-2 gap-6">
        <div class="card">
          <div class="flex items-center justify-between mb-4">
            <h2 class="text-gray-900 font-semibold">🏨 Accommodation</h2>
            <router-link to="/admin/accommodation" class="text-xs text-amber-700 hover:text-amber-600">View →</router-link>
          </div>
          <div class="space-y-3">
            <div class="flex justify-between text-sm">
              <span class="text-gray-700">Total Rooms</span>
              <span class="text-gray-900 font-medium">{{ stats.accommodation.totalRooms }}</span>
            </div>
            <div class="flex justify-between text-sm">
              <span class="text-gray-700">Available</span>
              <span class="text-amber-700 font-medium">{{ stats.accommodation.availableRooms }}</span>
            </div>
            <div class="flex justify-between text-sm">
              <span class="text-gray-700">Occupied</span>
              <span class="text-amber-700 font-medium">{{ stats.accommodation.occupiedRooms }}</span>
            </div>
            <div class="mt-4">
              <div class="flex justify-between text-xs text-gray-600 mb-1">
                <span>Occupancy</span>
                <span>{{ stats.accommodation.occupancyRate }}%</span>
              </div>
              <div class="w-full bg-gray-100 rounded-full h-2">
                <div class="bg-primary-500 h-2 rounded-full transition-all"
                     :style="`width: ${stats.accommodation.occupancyRate}%`"></div>
              </div>
            </div>
          </div>
        </div>

        <div class="card">
          <h2 class="text-gray-900 font-semibold mb-4">🚌 Transport</h2>
          <div class="space-y-3">
            <div class="flex justify-between text-sm">
              <span class="text-gray-700">Total Karwans</span>
              <span class="text-gray-900 font-medium">{{ stats.transport.totalKarwans }}</span>
            </div>
            <div class="flex justify-between text-sm">
              <span class="text-gray-700">Active Karwans</span>
              <span class="text-amber-700 font-medium">{{ stats.transport.activeKarwans }}</span>
            </div>
            <div class="flex justify-between text-sm">
              <span class="text-gray-700">Total Buses</span>
              <span class="text-gray-900 font-medium">{{ stats.transport.totalBuses }}</span>
            </div>
            <div class="flex justify-between text-sm">
              <span class="text-gray-700">Active Buses</span>
              <span class="text-amber-700 font-medium">{{ stats.transport.activeBuses }}</span>
            </div>
          </div>
        </div>
      </div>

      <!-- Today's Arrivals & Departures -->
      <div v-if="todayTravel" class="rounded-xl border overflow-hidden"
           style="background:rgba(10,10,10,0.5); border-color:rgba(212,168,0,0.2);">
        <div class="px-5 py-3 flex items-center justify-between border-b"
             style="border-color:rgba(212,168,0,0.12); background:rgba(212,168,0,0.05);">
          <div class="flex items-center gap-2">
            <span class="text-lg">✈️</span>
            <h2 class="text-sm font-semibold text-gray-900">Today's Travel — {{ new Date().toLocaleDateString('en-GB', {day:'2-digit', month:'short'}) }}</h2>
          </div>
          <router-link to="/admin/itinerary" class="text-xs text-amber-700 hover:text-amber-600">Full View →</router-link>
        </div>
        <div class="grid grid-cols-2 md:grid-cols-4 gap-0 divide-x divide-emerald-900/20">
          <div class="p-4 text-center">
            <p class="text-2xl font-bold text-amber-700">{{ todayTravel.arrivingCount || 0 }}</p>
            <p class="text-gray-600 text-xs mt-1">🛬 Arriving</p>
          </div>
          <div class="p-4 text-center">
            <p class="text-2xl font-bold text-blue-400">{{ todayTravel.departingCount || 0 }}</p>
            <p class="text-gray-600 text-xs mt-1">🛫 Departing</p>
          </div>
          <div class="p-4 text-center">
            <p class="text-2xl font-bold" :class="(todayTravel.pendingPickups||0) > 0 ? 'text-red-600' : 'text-slate-500'">
              {{ todayTravel.pendingPickups || 0 }}
            </p>
            <p class="text-gray-600 text-xs mt-1">🚗 Pickup Pending</p>
          </div>
          <div class="p-4 text-center">
            <p class="text-2xl font-bold" :class="(todayTravel.pendingDropoffs||0) > 0 ? 'text-orange-400' : 'text-slate-500'">
              {{ todayTravel.pendingDropoffs || 0 }}
            </p>
            <p class="text-gray-600 text-xs mt-1">🚗 Dropoff Pending</p>
          </div>
        </div>

        <!-- Arriving today list -->
        <div v-if="todayTravel.arrivals?.length" class="px-5 pb-4">
          <p class="text-amber-700 text-xs font-bold uppercase tracking-wider mb-2">🛬 Today's Arrivals</p>
          <div class="space-y-1">
            <div v-for="p in todayTravel.arrivals.slice(0,5)" :key="p.id"
                 class="flex items-center gap-3 rounded-lg px-3 py-2 text-sm"
                 style="background:rgba(212,168,0,0.05); border:1px solid rgba(212,168,0,0.1);">
              <div class="w-7 h-7 rounded-full flex items-center justify-center text-white text-xs font-bold shrink-0"
                   style="background:linear-gradient(135deg,#b88a00,#D4A800);">
                {{ p.fullName?.[0] }}
              </div>
              <div class="flex-1 min-w-0">
                <p class="text-white text-xs font-medium truncate">{{ p.fullName }}</p>
                <p class="text-gray-600 text-xs">{{ p.arrivalFlight || '—' }} · {{ p.arrivalTime || 'Time not set' }}</p>
              </div>
              <span :class="['text-xs px-2 py-0.5 rounded-full', p.transferStatus === 'Completed' ? 'bg-emerald-900/50 text-amber-700' : 'bg-red-100/40 text-red-600']">
                {{ p.transferStatus === 'Completed' ? '✅ Ready' : '⚠️ No Driver' }}
              </span>
            </div>
            <p v-if="todayTravel.arrivals.length > 5" class="text-xs text-gray-600 text-center pt-1">
              +{{ todayTravel.arrivals.length - 5 }} more →
            </p>
          </div>
        </div>
      </div>

      <!-- Upcoming 3 days -->
      <div v-if="upcomingDays?.dates?.length" class="rounded-xl border overflow-hidden"
           style="background:rgba(10,10,10,0.5); border-color:rgba(212,168,0,0.2);">
        <div class="px-5 py-3 flex items-center justify-between border-b"
             style="border-color:rgba(212,168,0,0.12); background:rgba(212,168,0,0.05);">
          <div class="flex items-center gap-2">
            <span class="text-lg">📆</span>
            <h2 class="text-sm font-semibold text-gray-900">Next 3-Day Schedule</h2>
          </div>
          <router-link to="/admin/itinerary" class="text-xs text-amber-700 hover:text-amber-600">Full View →</router-link>
        </div>
        <div class="p-4 space-y-2">
          <div v-for="day in upcomingDays.dates" :key="day.date"
               class="flex items-center gap-3 rounded-lg px-3 py-2.5"
               style="background:rgba(212,168,0,0.04); border:1px solid rgba(212,168,0,0.1);">
            <div class="text-center shrink-0 w-12">
              <p class="text-gray-900 font-bold text-sm">{{ new Date(day.date).toLocaleDateString('en-GB',{day:'2-digit'}) }}</p>
              <p class="text-gray-600 text-xs">{{ new Date(day.date).toLocaleDateString('en-GB',{month:'short'}) }}</p>
            </div>
            <div class="flex-1 flex gap-4 text-xs">
              <span v-if="day.arrivingCount" class="text-amber-700">🛬 {{ day.arrivingCount }} arriving</span>
              <span v-if="day.departingCount" class="text-blue-400">🛫 {{ day.departingCount }} departing</span>
              <span v-if="!day.arrivingCount && !day.departingCount" class="text-slate-600">No travel</span>
            </div>
          </div>
        </div>
      </div>

      <!-- Notice Board -->
      <div class="card">
        <div class="flex items-center justify-between mb-4">
          <h2 class="text-gray-900 font-semibold">📋 Notice Board</h2>
          <router-link to="/admin/noticeboard" class="text-amber-700 hover:text-amber-600 text-sm">Manage →</router-link>
        </div>
        <NoticeBoard />
      </div>

    </template>
  </div>
</template>
