<script setup>
import { ref, onMounted, onUnmounted, computed } from 'vue'
import { karwanApi } from '@/api'
import { useAuthStore } from '@/stores/auth'
import { useNotificationStore } from '@/stores/notifications'
import api from '@/api/client'

const auth = useAuthStore()
const notifStore = useNotificationStore()

const buses        = ref([])
const selectedBus  = ref(null)
const passengers   = ref([])
const loading      = ref(true)
const gpsStatus    = ref('idle')   // idle | sending | sent | error
const gpsError     = ref('')
const connStatus   = ref('disconnected')
const lastGpsSent  = ref(null)
const currentPos   = ref(null)
let hub = null, watchId = null

// ── GPS ──────────────────────────────────────────────────────────────────────
function startWatchingGps() {
  if (!navigator.geolocation) return
  watchId = navigator.geolocation.watchPosition(
    pos => {
      currentPos.value = { lat: pos.coords.latitude, lng: pos.coords.longitude, accuracy: pos.coords.accuracy }
    },
    () => {},
    { enableHighAccuracy: true, maximumAge: 10000 }
  )
}

async function sendGps() {
  if (!selectedBus.value || !currentPos.value) return
  gpsStatus.value = 'sending'
  gpsError.value  = ''
  try {
    await karwanApi.updateGps({
      karwanId:  selectedBus.value.karwanId,
      busId:     selectedBus.value.id,
      latitude:  currentPos.value.lat,
      longitude: currentPos.value.lng,
    })
    gpsStatus.value = 'sent'
    lastGpsSent.value = new Date()
    setTimeout(() => { gpsStatus.value = 'idle' }, 3000)
  } catch {
    gpsStatus.value = 'error'
    gpsError.value  = 'GPS update failed. Check connection.'
  }
}

// ── SignalR ───────────────────────────────────────────────────────────────────
async function connectHub() {
  try {
    const { HubConnectionBuilder, HttpTransportType } = await import('@microsoft/signalr')
    hub = new HubConnectionBuilder()
      .withUrl('/hubs/notifications', {
        accessTokenFactory: () => localStorage.getItem('access_token') || '',
        transport: HttpTransportType.WebSockets | HttpTransportType.LongPolling,
      })
      .withAutomaticReconnect()
      .build()

    hub.on('NewNotification', n => notifStore.notifications.unshift(n))
    hub.onreconnecting(() => { connStatus.value = 'connecting' })
    hub.onreconnected(()  => { connStatus.value = 'connected'  })
    hub.onclose(()        => { connStatus.value = 'disconnected' })

    await hub.start()
    connStatus.value = 'connected'
  } catch {}
}

// ── Data ─────────────────────────────────────────────────────────────────────
async function fetchBuses() {
  loading.value = true
  try {
    const res = await api.get('/buses/my')
    buses.value = res.data.data || []
    if (buses.value.length === 1) await selectBus(buses.value[0])
  } catch {
    buses.value = []
  } finally {
    loading.value = false
  }
}

async function selectBus(bus) {
  selectedBus.value = bus
  passengers.value  = []
  try {
    const res = await api.get(`/buses/${bus.id}/passengers`)
    passengers.value = res.data.data || []
  } catch {}
}

// ── Lifecycle ─────────────────────────────────────────────────────────────────
onMounted(() => {
  fetchBuses()
  connectHub()
  startWatchingGps()
})
onUnmounted(() => {
  hub?.stop()
  if (watchId !== null) navigator.geolocation.clearWatch(watchId)
})

const gpsLabel = computed(() => {
  if (!currentPos.value) return 'Waiting for GPS...'
  return `${currentPos.value.lat.toFixed(5)}, ${currentPos.value.lng.toFixed(5)}`
})

const connDot = computed(() => ({
  connected:    'bg-emerald-500',
  connecting:   'bg-amber-400 animate-pulse',
  disconnected: 'bg-red-500',
}[connStatus.value] || 'bg-red-500'))

function timeAgo(d) {
  if (!d) return ''
  const m = Math.floor((Date.now() - new Date(d)) / 60000)
  if (m < 1) return 'just now'
  if (m < 60) return `${m}m ago`
  return `${Math.floor(m/60)}h ago`
}
</script>

<template>
  <div class="space-y-5 max-w-2xl mx-auto">

    <!-- Header -->
    <div class="flex items-center justify-between">
      <div>
        <h1 class="page-title">Driver Dashboard</h1>
        <p class="page-subtitle">Salam, {{ auth.user?.fullName }}</p>
      </div>
      <div class="flex items-center gap-2 text-xs font-medium px-3 py-1.5 rounded-full"
           style="background:rgba(212,168,0,0.08); border:1px solid rgba(212,168,0,0.2); color:#b88a00;">
        <span :class="['w-2 h-2 rounded-full', connDot]"></span>
        {{ connStatus }}
      </div>
    </div>

    <!-- GPS Card -->
    <div class="card" style="border-left:3px solid #D4A800;">
      <div class="flex items-start justify-between gap-4">
        <div>
          <h2 class="font-semibold text-gray-800 mb-1">Live GPS Location</h2>
          <p class="text-sm text-gray-500 flex items-center gap-1.5">
            <span class="text-base">📍</span> {{ gpsLabel }}
          </p>
          <p v-if="lastGpsSent" class="text-xs text-gray-400 mt-1">
            Last sent: {{ timeAgo(lastGpsSent) }}
          </p>
        </div>
        <button @click="sendGps"
          :disabled="!selectedBus || !currentPos || gpsStatus === 'sending'"
          :class="['btn flex items-center gap-2 text-sm transition-all',
            gpsStatus === 'sent'    ? 'bg-emerald-600 text-white' :
            gpsStatus === 'error'   ? 'bg-red-600 text-white'     :
            gpsStatus === 'sending' ? 'opacity-60 cursor-wait'    : 'btn-primary']">
          <span v-if="gpsStatus === 'sending'">⏳</span>
          <span v-else-if="gpsStatus === 'sent'">✅</span>
          <span v-else-if="gpsStatus === 'error'">❌</span>
          <span v-else>📡</span>
          {{ gpsStatus === 'sending' ? 'Sending...' : gpsStatus === 'sent' ? 'Sent!' : 'Send GPS' }}
        </button>
      </div>
      <p v-if="gpsError" class="mt-2 text-xs text-red-600">{{ gpsError }}</p>
      <p v-if="!currentPos" class="mt-2 text-xs text-amber-600">
        Allow location access in your browser to enable GPS updates.
      </p>
    </div>

    <!-- Bus Selection -->
    <div class="card">
      <h2 class="font-semibold text-gray-800 mb-3">My Bus Assignment</h2>
      <div v-if="loading" class="text-center py-6 text-gray-400">Loading...</div>
      <div v-else-if="buses.length === 0" class="text-center py-6">
        <div class="text-4xl mb-2">🚌</div>
        <p class="text-gray-500 text-sm">No bus assigned yet. Contact admin.</p>
      </div>
      <div v-else class="space-y-2">
        <button v-for="bus in buses" :key="bus.id"
          @click="selectBus(bus)"
          :class="['w-full flex items-center gap-3 p-3 rounded-xl border text-left transition-all',
            selectedBus?.id === bus.id
              ? 'border-amber-400 bg-amber-50'
              : 'border-gray-200 hover:border-amber-300 hover:bg-amber-50/50']">
          <span class="text-2xl">🚌</span>
          <div class="flex-1 min-w-0">
            <p class="font-medium text-gray-800">{{ bus.plateNumber || bus.name }}</p>
            <p class="text-xs text-gray-500">{{ bus.karwanName || 'Karwan' }} · Capacity: {{ bus.capacity }}</p>
          </div>
          <span v-if="selectedBus?.id === bus.id" class="text-amber-500 text-sm font-semibold">Active</span>
        </button>
      </div>
    </div>

    <!-- Passenger List -->
    <div v-if="selectedBus" class="card">
      <div class="flex items-center justify-between mb-3">
        <h2 class="font-semibold text-gray-800">Passengers</h2>
        <span class="text-xs text-gray-500">{{ passengers.length }} total</span>
      </div>
      <div v-if="passengers.length === 0" class="text-center py-4 text-gray-400 text-sm">
        No passengers assigned yet.
      </div>
      <div v-else class="space-y-2">
        <div v-for="(p, i) in passengers" :key="p.id"
          class="flex items-center gap-3 p-2.5 rounded-lg bg-gray-50 border border-gray-100">
          <div class="w-8 h-8 rounded-full flex items-center justify-center text-xs font-bold text-white flex-shrink-0"
               style="background:linear-gradient(135deg,#b88a00,#D4A800);">
            {{ i + 1 }}
          </div>
          <div class="flex-1 min-w-0">
            <p class="text-sm font-medium text-gray-800 truncate">{{ p.fullName }}</p>
            <p class="text-xs text-gray-500">{{ p.country }} · {{ p.seatNumber ? 'Seat ' + p.seatNumber : 'No seat' }}</p>
          </div>
          <span :class="['text-xs px-2 py-0.5 rounded-full font-medium',
            p.isBoarded ? 'bg-emerald-100 text-emerald-700' : 'bg-gray-100 text-gray-500']">
            {{ p.isBoarded ? 'Boarded' : 'Pending' }}
          </span>
        </div>
      </div>
    </div>

    <!-- Notifications -->
    <div class="card">
      <div class="flex items-center justify-between mb-3">
        <h2 class="font-semibold text-gray-800">Recent Notifications</h2>
        <button @click="notifStore.markAllRead()" v-if="notifStore.unreadCount > 0"
          class="text-xs" style="color:#b88a00;">Mark all read</button>
      </div>
      <div v-if="notifStore.notifications.length === 0" class="text-center py-4 text-gray-400 text-sm">
        No notifications
      </div>
      <div v-else class="space-y-2">
        <div v-for="n in notifStore.notifications.slice(0,5)" :key="n.id"
          @click="notifStore.markRead(n.id)"
          :class="['flex gap-3 p-3 rounded-xl cursor-pointer transition-colors border',
            !n.isRead ? 'border-amber-200 bg-amber-50' : 'border-gray-100 bg-gray-50 hover:bg-gray-100']">
          <span class="text-lg flex-shrink-0">
            {{ n.event === 'Emergency' ? '🚨' : n.event === 'BusDeparture' ? '🚌' : '📢' }}
          </span>
          <div class="flex-1 min-w-0">
            <p :class="['text-sm font-medium', !n.isRead ? 'text-amber-900' : 'text-gray-700']">{{ n.title }}</p>
            <p class="text-xs text-gray-500 mt-0.5 line-clamp-2">{{ n.message }}</p>
          </div>
        </div>
      </div>
    </div>

  </div>
</template>
