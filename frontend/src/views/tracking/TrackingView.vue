<script setup>
import { ref, onMounted, onUnmounted, nextTick } from 'vue'
import { karwanApi } from '@/api'
import { useAuthStore } from '@/stores/auth'

const auth = useAuthStore()
const karwans  = ref([])
const selected  = ref(null)
const lastLoc   = ref(null)
const loading   = ref(true)
const mapError  = ref('')
const connStatus = ref('disconnected')

let map = null, marker = null, hub = null, pollInterval = null, L = null

// ── Leaflet — lazy load so a missing dep never crashes the page ─────────────
async function loadLeaflet() {
  if (L) return true
  try {
    const mod = await import('leaflet')
    L = mod.default ?? mod
    await import('leaflet/dist/leaflet.css')
    // fix Vite marker icon paths
    delete L.Icon.Default.prototype._getIconUrl
    L.Icon.Default.mergeOptions({
      iconRetinaUrl: new URL('/node_modules/leaflet/dist/images/marker-icon-2x.png', import.meta.url).href,
      iconUrl:       new URL('/node_modules/leaflet/dist/images/marker-icon.png',    import.meta.url).href,
      shadowUrl:     new URL('/node_modules/leaflet/dist/images/marker-shadow.png',  import.meta.url).href,
    })
    return true
  } catch {
    mapError.value = 'Map library failed to load.'
    return false
  }
}

function initMap() {
  if (map || !L) return
  const el = document.getElementById('tracking-map')
  if (!el) return
  try {
    map = L.map(el).setView([32.6, 44.0], 7)
    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
      attribution: '© OpenStreetMap',
      maxZoom: 19,
    }).addTo(map)
  } catch (e) {
    mapError.value = 'Map init failed: ' + e.message
  }
}

function placeMarker(lat, lng, label) {
  if (!map || !L) return
  map.invalidateSize()
  const pos = [lat, lng]
  if (marker) marker.setLatLng(pos)
  else marker = L.marker(pos).addTo(map)
  marker.bindPopup(`<b>${selected.value?.name}</b>${label ? '<br>' + label : ''}`).openPopup()
  map.flyTo(pos, 14, { duration: 1 })
  lastLoc.value = { lat, lng, label, time: new Date() }
}

function resetMap() {
  if (marker) { marker.remove(); marker = null }
  lastLoc.value = null
  map?.setView([32.6, 44.0], 7)
}

// ── SignalR — lazy load ─────────────────────────────────────────────────────
async function connectHub() {
  try {
    const { HubConnectionBuilder, HttpTransportType, LogLevel, HubConnectionState } = await import('@microsoft/signalr')
    const base = (import.meta.env.VITE_API_URL || 'http://localhost:5025/api/v1').replace('/api/v1', '')

    hub = new HubConnectionBuilder()
      .withUrl(`${base}/hubs/tracking`, {
        accessTokenFactory: () => auth.token || '',
        transport: HttpTransportType.WebSockets | HttpTransportType.ServerSentEvents | HttpTransportType.LongPolling,
      })
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Warning)
      .build()

    hub.on('LocationUpdated', (data) => {
      if (data?.latitude && data?.longitude)
        placeMarker(data.latitude, data.longitude, data.address || '')
    })
    hub.onreconnecting(() => { connStatus.value = 'connecting' })
    hub.onreconnected(()  => { connStatus.value = 'connected' })
    hub.onclose(()        => { connStatus.value = 'disconnected' })

    connStatus.value = 'connecting'
    await hub.start()
    connStatus.value = 'connected'
  } catch {
    connStatus.value = 'disconnected'
  }
}

async function joinGroup(id)  {
  try { if (hub?.state === 'Connected') await hub.invoke('JoinKarwanGroup',  String(id)) } catch {}
}
async function leaveGroup(id) {
  try { if (hub?.state === 'Connected') await hub.invoke('LeaveKarwanGroup', String(id)) } catch {}
}

// ── Karwan selection ────────────────────────────────────────────────────────
async function selectKarwan(k) {
  if (selected.value?.id === k.id) return
  if (selected.value) await leaveGroup(selected.value.id)
  selected.value = k
  resetMap()
  await nextTick()
  map?.invalidateSize()

  try {
    const res = await karwanApi.getLocation(k.id)
    const loc  = res.data.data
    if (loc?.latitude && loc?.longitude) placeMarker(loc.latitude, loc.longitude, loc.address || '')
  } catch {}

  await joinGroup(k.id)
}

async function load() {
  try {
    const res   = await karwanApi.getAll()
    karwans.value = res.data.data || []
  } finally { loading.value = false }
}

onMounted(async () => {
  await load()
  const ok = await loadLeaflet()
  if (ok) { await nextTick(); initMap() }
  connectHub()   // don't await — runs in background
  pollInterval = setInterval(load, 30000)
})

onUnmounted(async () => {
  clearInterval(pollInterval)
  if (selected.value) await leaveGroup(selected.value.id)
  try { await hub?.stop() } catch {}
  if (map) { map.remove(); map = null; L = null }
})

const dotColor = { connected: 'bg-emerald-400', connecting: 'bg-yellow-400 animate-pulse', disconnected: 'bg-slate-500' }
const dotLabel = { connected: 'Live', connecting: 'Connecting…', disconnected: 'Offline' }
function fmtTime(d) { return d ? new Date(d).toLocaleTimeString('en-GB', { hour:'2-digit', minute:'2-digit', second:'2-digit' }) : '—' }
</script>

<template>
  <div class="space-y-4">

    <!-- Header -->
    <div class="flex items-center justify-between flex-wrap gap-2">
      <div>
        <h1 class="text-2xl font-bold text-white">📍 Live Karwan Tracking</h1>
        <p class="text-slate-400 text-sm mt-0.5">Real-time GPS location of all Karwans</p>
      </div>
      <div class="flex items-center gap-2 text-xs rounded-lg px-3 py-1.5 border border-slate-700 bg-slate-900/50">
        <div :class="['w-2 h-2 rounded-full flex-shrink-0', dotColor[connStatus]]"></div>
        <span class="text-slate-300">{{ dotLabel[connStatus] }}</span>
      </div>
    </div>

    <!-- Map load error -->
    <div v-if="mapError" class="rounded-lg border border-red-800 bg-red-950/30 px-4 py-3 text-red-400 text-sm">
      ⚠️ {{ mapError }}
    </div>

    <div class="grid grid-cols-1 lg:grid-cols-3 gap-4">

      <!-- Karwan list -->
      <div class="space-y-2">
        <p class="text-xs font-bold text-slate-500 uppercase tracking-wider px-1">Select Karwan</p>

        <div v-if="loading" class="text-center py-6 text-slate-500 text-sm">Loading…</div>

        <div v-else-if="!karwans.length"
             class="rounded-xl border border-emerald-900/20 p-5 text-center text-slate-500 text-sm"
             style="background:rgba(2,20,10,0.45);">
          <div class="text-3xl mb-2">🚌</div>
          No Karwan found.<br>
          <span class="text-xs text-slate-600">Create one from Admin → Tours.</span>
        </div>

        <div v-for="k in karwans" :key="k.id"
             @click="selectKarwan(k)"
             :class="['rounded-xl border p-3 cursor-pointer transition-all select-none',
               selected?.id === k.id
                 ? 'border-emerald-600 bg-emerald-900/20'
                 : 'border-emerald-900/20 hover:border-emerald-700/40']"
             style="background:rgba(2,20,10,0.45); touch-action:manipulation; min-height:52px;">

          <div class="flex items-center justify-between mb-1">
            <p class="text-white font-semibold text-sm">{{ k.name }}</p>
            <span :class="['text-xs px-2 py-0.5 rounded-full font-medium border',
              k.isActive ? 'bg-emerald-900/50 text-emerald-400 border-emerald-700'
                         : 'bg-slate-800 text-slate-500 border-slate-600']">
              {{ k.isActive ? '● Active' : '○ Inactive' }}
            </span>
          </div>
          <div class="text-xs text-slate-500 space-y-0.5">
            <p>🏛️ Pole: <span class="text-slate-300">{{ k.poleNumber || '—' }}</span></p>
            <p>🚌 {{ k.totalBuses || 0 }} buses · 🕌 {{ k.totalPilgrims || 0 }} pilgrims</p>
          </div>
        </div>
      </div>

      <!-- Map -->
      <div class="lg:col-span-2 flex flex-col gap-3">
        <div class="relative rounded-xl overflow-hidden border border-emerald-700/30" style="height:420px;">

          <!-- Leaflet renders here — always in DOM -->
          <div id="tracking-map" style="height:100%; width:100%; z-index:0;"></div>

          <!-- Overlay: no karwan selected -->
          <div v-if="!selected"
               class="absolute inset-0 z-10 flex items-center justify-center"
               style="background:rgba(1,12,6,0.88); backdrop-filter:blur(4px);">
            <div class="text-center">
              <div class="text-5xl mb-3">🗺️</div>
              <p class="text-slate-300 font-medium">Select a Karwan</p>
              <p class="text-slate-500 text-sm mt-1">Choose a Karwan from the list on the left</p>
            </div>
          </div>

          <!-- No GPS badge -->
          <div v-else-if="!lastLoc"
               class="absolute bottom-3 left-1/2 -translate-x-1/2 z-10
                      rounded-lg px-3 py-1.5 text-xs text-yellow-400 border border-yellow-800/50"
               style="background:rgba(1,12,6,0.9);">
            ⏳ GPS data not yet available — waiting for driver update
          </div>
        </div>

        <!-- Info strip -->
        <div v-if="selected"
             class="rounded-xl border border-emerald-900/20 p-4"
             style="background:rgba(2,20,10,0.45);">
          <div class="grid grid-cols-2 sm:grid-cols-4 gap-3 text-sm">
            <div><p class="text-slate-500 text-xs mb-0.5">KARWAN</p><p class="text-white font-semibold">{{ selected.name }}</p></div>
            <div><p class="text-slate-500 text-xs mb-0.5">POLE</p><p class="text-white">{{ selected.poleNumber || '—' }}</p></div>
            <div><p class="text-slate-500 text-xs mb-0.5">LAST UPDATE</p><p class="text-white">{{ fmtTime(lastLoc?.time) }}</p></div>
            <div><p class="text-slate-500 text-xs mb-0.5">LOCATION</p><p class="text-emerald-400 text-xs truncate">{{ lastLoc?.label || '—' }}</p></div>
          </div>
          <a v-if="lastLoc"
             :href="`https://www.google.com/maps?q=${lastLoc.lat},${lastLoc.lng}`"
             target="_blank" rel="noopener"
             class="mt-3 inline-flex items-center gap-1.5 text-xs text-blue-400 hover:text-blue-300">
            🗺️ Open in Google Maps
          </a>
        </div>
      </div>
    </div>
  </div>
</template>
