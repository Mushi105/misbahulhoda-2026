<script setup>
import { ref, onMounted, onUnmounted } from 'vue'
import { karwanApi } from '@/api'

const karwans = ref([])
const selected = ref(null)
const location = ref(null)
const loading = ref(true)
let pollInterval = null

async function load() {
  try {
    const res = await karwanApi.getAll()
    karwans.value = res.data.data || []
  } finally {
    loading.value = false
  }
}

async function selectKarwan(k) {
  selected.value = k
  location.value = null
  try {
    const res = await karwanApi.getLocation(k.id)
    location.value = res.data.data
  } catch {}
}

onMounted(() => {
  load()
  pollInterval = setInterval(load, 30000)
})

onUnmounted(() => clearInterval(pollInterval))
</script>

<template>
  <div class="space-y-6">
    <h1 class="text-2xl font-bold text-white">📍 Live Tracking</h1>

    <div class="grid grid-cols-1 lg:grid-cols-3 gap-6">
      <!-- Karwan List -->
      <div class="space-y-3">
        <h2 class="text-sm font-semibold text-slate-400 uppercase">Karwans</h2>
        <div v-if="loading" class="text-slate-400 text-sm">Loading...</div>
        <div v-else-if="!karwans.length" class="text-slate-400 text-sm">No karwans found.</div>
        <div v-for="k in karwans" :key="k.id"
             @click="selectKarwan(k)"
             :class="['card cursor-pointer transition-all border', selected?.id === k.id ? 'border-primary-500 bg-primary-900/20' : 'border-dark-700 hover:border-primary-700']">
          <div class="flex items-center justify-between mb-2">
            <p class="text-white font-semibold">{{ k.name }}</p>
            <span :class="k.isActive ? 'badge-green' : 'badge-gray'">{{ k.isActive ? 'Active' : 'Inactive' }}</span>
          </div>
          <p class="text-slate-400 text-xs">Pole: {{ k.poleNumber }}</p>
          <p class="text-slate-400 text-xs">Buses: {{ k.totalBuses }} | Pilgrims: {{ k.totalPilgrims }}</p>
          <p v-if="k.currentLocation" class="text-primary-400 text-xs mt-1 truncate">📍 {{ k.currentLocation }}</p>
        </div>
      </div>

      <!-- Map / Location Info -->
      <div class="lg:col-span-2 card">
        <div v-if="!selected" class="flex items-center justify-center h-64 text-slate-500">
          <div class="text-center">
            <div class="text-4xl mb-2">🗺️</div>
            <p>Select a Karwan to view location</p>
          </div>
        </div>
        <div v-else>
          <h2 class="text-white font-semibold mb-4">{{ selected.name }}</h2>
          <div v-if="location" class="space-y-3">
            <div class="bg-dark-900 rounded-lg p-4 border border-dark-700">
              <p class="text-slate-400 text-xs uppercase mb-3">Last Known Location</p>
              <div class="grid grid-cols-2 gap-3 text-sm">
                <div><p class="text-slate-400">Latitude</p><p class="text-white font-mono">{{ location.latitude }}</p></div>
                <div><p class="text-slate-400">Longitude</p><p class="text-white font-mono">{{ location.longitude }}</p></div>
                <div><p class="text-slate-400">Address</p><p class="text-white">{{ location.address || 'N/A' }}</p></div>
                <div><p class="text-slate-400">Speed</p><p class="text-white">{{ location.speed ? location.speed + ' km/h' : 'N/A' }}</p></div>
                <div class="col-span-2"><p class="text-slate-400">Updated</p><p class="text-white">{{ new Date(location.recordedAt).toLocaleString() }}</p></div>
              </div>
            </div>

            <!-- Google Maps Link -->
            <a :href="`https://www.google.com/maps?q=${location.latitude},${location.longitude}`"
               target="_blank" rel="noopener"
               class="btn-primary inline-flex items-center gap-2 text-sm">
              🗺️ View on Google Maps
            </a>
          </div>
          <div v-else class="text-slate-400 text-sm py-4">No GPS data available for this Karwan yet.</div>
        </div>
      </div>
    </div>
  </div>
</template>
