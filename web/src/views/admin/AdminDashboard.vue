<script setup>
import { ref, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { adminApi } from '@/api'
import NoticeBoard from '@/components/NoticeBoard.vue'

const router = useRouter()
const stats = ref(null)
const loading = ref(true)

onMounted(async () => {
  try {
    const res = await adminApi.getDashboard()
    stats.value = res.data.data
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
      <h1 class="text-2xl font-bold text-white">Admin Dashboard</h1>
      <p class="text-slate-400 text-sm mt-1">Arbaeen 2026 — Real-time overview</p>
    </div>

    <div v-if="loading" class="text-slate-400 text-center py-12">Loading dashboard...</div>

    <template v-else-if="stats">

      <!-- Pilgrims -->
      <div>
        <div class="flex items-center justify-between mb-3">
          <h2 class="text-sm font-semibold text-slate-400 uppercase tracking-wider">Pilgrims</h2>
          <button @click="goToPilgrims()" class="text-xs text-primary-400 hover:text-primary-300">View All →</button>
        </div>
        <div class="grid grid-cols-2 lg:grid-cols-4 gap-4">
          <button @click="goToPilgrims()" class="stat-card text-left hover:border-primary-700 hover:bg-dark-700 transition-all cursor-pointer">
            <div class="stat-icon bg-primary-900">🕌</div>
            <div>
              <p class="text-2xl font-bold text-white">{{ stats.pilgrims.total }}</p>
              <p class="text-slate-400 text-xs">Total Pilgrims</p>
            </div>
          </button>
          <button @click="goToPilgrims('Pending')" class="stat-card text-left hover:border-gold-700 hover:bg-dark-700 transition-all cursor-pointer">
            <div class="stat-icon bg-gold-900">⏳</div>
            <div>
              <p class="text-2xl font-bold text-gold-400">{{ stats.pilgrims.pending }}</p>
              <p class="text-slate-400 text-xs">Pending</p>
            </div>
          </button>
          <button @click="goToPilgrims('Approved')" class="stat-card text-left hover:border-green-700 hover:bg-dark-700 transition-all cursor-pointer">
            <div class="stat-icon bg-green-900">✅</div>
            <div>
              <p class="text-2xl font-bold text-green-400">{{ stats.pilgrims.approved }}</p>
              <p class="text-slate-400 text-xs">Approved</p>
            </div>
          </button>
          <button @click="goToPilgrims('Rejected')" class="stat-card text-left hover:border-red-700 hover:bg-dark-700 transition-all cursor-pointer">
            <div class="stat-icon bg-red-900">❌</div>
            <div>
              <p class="text-2xl font-bold text-red-400">{{ stats.pilgrims.rejected }}</p>
              <p class="text-slate-400 text-xs">Rejected</p>
            </div>
          </button>
        </div>
      </div>

      <!-- Volunteers -->
      <div>
        <div class="flex items-center justify-between mb-3">
          <h2 class="text-sm font-semibold text-slate-400 uppercase tracking-wider">Volunteers</h2>
          <button @click="goToVolunteers()" class="text-xs text-primary-400 hover:text-primary-300">View All →</button>
        </div>
        <div class="grid grid-cols-2 lg:grid-cols-4 gap-4">
          <button @click="goToVolunteers()" class="stat-card text-left hover:border-blue-700 hover:bg-dark-700 transition-all cursor-pointer">
            <div class="stat-icon bg-blue-900">🤝</div>
            <div>
              <p class="text-2xl font-bold text-white">{{ stats.volunteers.total }}</p>
              <p class="text-slate-400 text-xs">Total</p>
            </div>
          </button>
          <button @click="goToVolunteers('Available')" class="stat-card text-left hover:border-green-700 hover:bg-dark-700 transition-all cursor-pointer">
            <div class="stat-icon bg-primary-900">🟢</div>
            <div>
              <p class="text-2xl font-bold text-primary-400">{{ stats.volunteers.available }}</p>
              <p class="text-slate-400 text-xs">Available</p>
            </div>
          </button>
          <button @click="goToVolunteers('Busy')" class="stat-card text-left hover:border-yellow-700 hover:bg-dark-700 transition-all cursor-pointer">
            <div class="stat-icon bg-yellow-900">🔵</div>
            <div>
              <p class="text-2xl font-bold text-yellow-400">{{ stats.volunteers.busy }}</p>
              <p class="text-slate-400 text-xs">Busy</p>
            </div>
          </button>
          <button @click="goToVolunteers('Offline')" class="stat-card text-left hover:border-slate-600 hover:bg-dark-700 transition-all cursor-pointer">
            <div class="stat-icon bg-slate-800">⚫</div>
            <div>
              <p class="text-2xl font-bold text-slate-400">{{ stats.volunteers.offline }}</p>
              <p class="text-slate-400 text-xs">Offline</p>
            </div>
          </button>
        </div>
      </div>

      <!-- Accommodation & Transport -->
      <div class="grid grid-cols-1 lg:grid-cols-2 gap-6">
        <div class="card">
          <div class="flex items-center justify-between mb-4">
            <h2 class="text-white font-semibold">🏨 Accommodation</h2>
            <router-link to="/admin/accommodation" class="text-xs text-primary-400 hover:text-primary-300">View →</router-link>
          </div>
          <div class="space-y-3">
            <div class="flex justify-between text-sm">
              <span class="text-slate-400">Total Rooms</span>
              <span class="text-white font-medium">{{ stats.accommodation.totalRooms }}</span>
            </div>
            <div class="flex justify-between text-sm">
              <span class="text-slate-400">Available</span>
              <span class="text-primary-400 font-medium">{{ stats.accommodation.availableRooms }}</span>
            </div>
            <div class="flex justify-between text-sm">
              <span class="text-slate-400">Occupied</span>
              <span class="text-gold-400 font-medium">{{ stats.accommodation.occupiedRooms }}</span>
            </div>
            <div class="mt-4">
              <div class="flex justify-between text-xs text-slate-400 mb-1">
                <span>Occupancy</span>
                <span>{{ stats.accommodation.occupancyRate }}%</span>
              </div>
              <div class="w-full bg-dark-700 rounded-full h-2">
                <div class="bg-primary-500 h-2 rounded-full transition-all"
                     :style="`width: ${stats.accommodation.occupancyRate}%`"></div>
              </div>
            </div>
          </div>
        </div>

        <div class="card">
          <h2 class="text-white font-semibold mb-4">🚌 Transport</h2>
          <div class="space-y-3">
            <div class="flex justify-between text-sm">
              <span class="text-slate-400">Total Karwans</span>
              <span class="text-white font-medium">{{ stats.transport.totalKarwans }}</span>
            </div>
            <div class="flex justify-between text-sm">
              <span class="text-slate-400">Active Karwans</span>
              <span class="text-primary-400 font-medium">{{ stats.transport.activeKarwans }}</span>
            </div>
            <div class="flex justify-between text-sm">
              <span class="text-slate-400">Total Buses</span>
              <span class="text-white font-medium">{{ stats.transport.totalBuses }}</span>
            </div>
            <div class="flex justify-between text-sm">
              <span class="text-slate-400">Active Buses</span>
              <span class="text-primary-400 font-medium">{{ stats.transport.activeBuses }}</span>
            </div>
          </div>
        </div>
      </div>

      <!-- Notice Board -->
      <div class="card">
        <div class="flex items-center justify-between mb-4">
          <h2 class="text-white font-semibold">📋 Notice Board</h2>
          <router-link to="/admin/noticeboard" class="text-primary-400 hover:text-primary-300 text-sm">Manage →</router-link>
        </div>
        <NoticeBoard />
      </div>

    </template>
  </div>
</template>
