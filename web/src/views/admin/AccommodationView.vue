<script setup>
import { ref, computed, onMounted } from 'vue'
import { accommodationApi, adminApi } from '@/api'
import api from '@/api/client'

// ── State ────────────────────────────────────────────────
const rooms         = ref([])
const hotelsData    = ref([])   // hotels with structure (for setup tab)
const occupancy     = ref(null)
const pilgrims      = ref([])
const loading       = ref(true)
const saving        = ref(false)
const msg           = ref({ type: '', text: '' })
const activeTab     = ref('rooms')   // 'rooms' | 'setup' | 'transit'

// Room view filters
const selectedHotelId = ref('')   // '' = all hotels
const selectedFloor   = ref(null)
const filterStatus    = ref('all')

// Room detail modal
const showModal  = ref(false)
const activeRoom = ref(null)

// Assign modal
const showAssign   = ref(false)
const assignSearch = ref('')
const assignRoom   = ref(null)

// Trail modal
const showTrail    = ref(false)
const trailData    = ref(null)
const trailLoading = ref(false)
const inTransit    = ref([])
const transitLoading = ref(false)

// ── Setup tab state ──────────────────────────────────────
const showAddHotel    = ref(false)
const addHotelForm    = ref({ name: '', city: '', address: '', phoneNumber: '' })
const savingHotel     = ref(false)

const showAddRooms    = ref(false)
const addRoomsHotelId = ref('')
const addRoomsHotelName = ref('')
const addRoomsForm    = ref({ floorNumber: 1, floorLabel: '', totalRooms: 20, bedCapacity: 4, isForFamily: false })
const savingRooms     = ref(false)

const showEditFloor   = ref(false)
const editFloorId     = ref('')
const editFloorForm   = ref({ floorNumber: 1, label: '' })
const savingFloor     = ref(false)

// ── Load data ────────────────────────────────────────────
async function load() {
  loading.value = true
  try {
    const [detailRes, occRes, pilgrimRes] = await Promise.all([
      api.get('/accommodation/rooms/detail'),
      accommodationApi.getOccupancy(),
      adminApi.getPilgrims({ pageSize: 200 }),
    ])
    rooms.value    = detailRes.data.data || []
    occupancy.value = occRes.data.data
    pilgrims.value  = (pilgrimRes.data.data?.items || []).filter(p => !p.roomId)
  } finally {
    loading.value = false
  }
}

async function loadHotelsSetup() {
  try {
    const res = await accommodationApi.getHotelsWithRooms()
    hotelsData.value = res.data.data || []
  } catch {
    hotelsData.value = []
  }
}

async function loadInTransit() {
  transitLoading.value = true
  try {
    const res = await api.get('/accommodation/in-transit')
    inTransit.value = res.data.data || []
  } finally {
    transitLoading.value = false
  }
}

async function openTrail(pilgrimId) {
  showTrail.value   = true
  trailData.value   = null
  trailLoading.value = true
  try {
    const res = await api.get(`/accommodation/pilgrim-trail/${pilgrimId}`)
    trailData.value = res.data.data
  } finally {
    trailLoading.value = false
  }
}

function switchTab(tab) {
  activeTab.value = tab
  if (tab === 'transit') loadInTransit()
  if (tab === 'setup') loadHotelsSetup()
}

onMounted(load)

// ── Computed ─────────────────────────────────────────────

// Unique hotels from room data
const hotelList = computed(() => {
  const map = {}
  rooms.value.forEach(r => {
    if (r.hotelId && !map[r.hotelId]) map[r.hotelId] = { id: r.hotelId, name: r.hotelName }
  })
  return Object.values(map)
})

// Floors available for selected hotel (or all)
const floors = computed(() => {
  const map = {}
  rooms.value
    .filter(r => !selectedHotelId.value || r.hotelId === selectedHotelId.value)
    .forEach(r => {
      if (!map[r.floorNumber]) map[r.floorNumber] = r.floorLabel || `Floor ${r.floorNumber}`
    })
  return Object.entries(map).sort((a, b) => +a[0] - +b[0])
})

const filteredRooms = computed(() => {
  let list = rooms.value
  if (selectedHotelId.value) list = list.filter(r => r.hotelId === selectedHotelId.value)
  if (selectedFloor.value !== null) list = list.filter(r => r.floorNumber === selectedFloor.value)
  if (filterStatus.value === 'available') list = list.filter(r => r.isAvailable)
  if (filterStatus.value === 'full')      list = list.filter(r => !r.isAvailable)
  return list
})

// Group filteredRooms by floor for display
const floorGroups = computed(() => {
  const map = {}
  filteredRooms.value.forEach(r => {
    const key = r.floorNumber
    if (!map[key]) map[key] = { num: key, label: r.floorLabel || `Floor ${r.floorNumber}`, rooms: [] }
    map[key].rooms.push(r)
  })
  return Object.values(map).sort((a, b) => a.num - b.num)
})

const assignablePilgrims = computed(() => {
  const q = assignSearch.value.toLowerCase().trim()
  if (!q) return pilgrims.value.slice(0, 20)
  return pilgrims.value.filter(p =>
    p.fullName?.toLowerCase().includes(q) ||
    p.email?.toLowerCase().includes(q) ||
    p.country?.toLowerCase().includes(q)
  ).slice(0, 20)
})

// ── Room helpers ─────────────────────────────────────────
function roomColor(r) {
  if (r.occupiedBeds === 0)                return 'border-green-700/50 bg-green-900/10'
  if (r.occupiedBeds >= r.bedCapacity)     return 'border-red-700/50 bg-red-900/10'
  return 'border-gold-700/50 bg-gold-900/10'
}
function roomDot(r) {
  if (r.occupiedBeds === 0)                return 'bg-green-400'
  if (r.occupiedBeds >= r.bedCapacity)     return 'bg-red-400'
  return 'bg-gold-400'
}

function openRoom(room) { activeRoom.value = room; showModal.value = true }

function openAssign(room) {
  assignRoom.value   = room
  assignSearch.value = ''
  showAssign.value   = true
  showModal.value    = false
}

function fmtDate(d) {
  if (!d) return '—'
  return new Date(d).toLocaleDateString('en-GB', { day: '2-digit', month: 'short', year: 'numeric' })
}

function notify(type, text) {
  msg.value = { type, text }
  setTimeout(() => msg.value = { type: '', text: '' }, 4000)
}

// ── Assign pilgrim ────────────────────────────────────────
async function assign(pilgrim) {
  if (!assignRoom.value) return
  saving.value = true
  try {
    await adminApi.allocateRoom(pilgrim.id, assignRoom.value.id)
    notify('success', `${pilgrim.fullName} assigned to Room ${assignRoom.value.roomNumber}`)
    showAssign.value = false
    await load()
  } catch (e) {
    notify('error', e.response?.data?.message || 'Assignment failed.')
  } finally { saving.value = false }
}

async function deallocate(room, occupant) {
  if (!confirm(`Remove ${occupant.fullName} from Room ${room.roomNumber}?`)) return
  saving.value = true
  try {
    await api.delete(`/accommodation/rooms/${room.id}/deallocate/${occupant.pilgrimId}`)
    notify('success', `${occupant.fullName} removed from Room ${room.roomNumber}`)
    showModal.value = false
    await load()
  } catch (e) {
    notify('error', e.response?.data?.message || 'Failed to deallocate.')
  } finally { saving.value = false }
}

// ── Hotel Setup actions ──────────────────────────────────
function openAddHotel() { addHotelForm.value = { name: '', city: '', address: '', phoneNumber: '' }; showAddHotel.value = true }

async function createHotel() {
  if (!addHotelForm.value.name || !addHotelForm.value.city) return notify('error', 'Hotel name and city are required.')
  savingHotel.value = true
  try {
    await accommodationApi.createHotel(addHotelForm.value)
    showAddHotel.value = false
    notify('success', `Hotel "${addHotelForm.value.name}" created.`)
  } catch (e) {
    notify('error', e.response?.data?.message || 'Failed to create hotel.')
    savingHotel.value = false
    return
  }
  savingHotel.value = false
  await loadHotelsSetup()
  await load()
}

async function deleteHotel(hotel) {
  if (!confirm(`Delete "${hotel.name}"? This will also remove all its rooms and data.`)) return
  try {
    await accommodationApi.deleteHotel(hotel.id)
    notify('success', `${hotel.name} deleted.`)
    await loadHotelsSetup()
    await load()
  } catch (e) {
    notify('error', e.response?.data?.message || 'Delete failed.')
  }
}

function openAddRooms(hotel) {
  addRoomsHotelId.value   = hotel.id
  addRoomsHotelName.value = hotel.name
  addRoomsForm.value = { floorNumber: 1, floorLabel: 'Floor 1', totalRooms: 20, bedCapacity: 4, isForFamily: false }
  showAddRooms.value = true
}

// Preview: floor 5, room 1 → "501", room 20 → "520"
const roomPreview = computed(() => {
  const f = addRoomsForm.value
  const first = `${f.floorNumber}${String(1).padStart(2, '0')}`
  const last  = `${f.floorNumber}${String(f.totalRooms).padStart(2, '0')}`
  return { first, last }
})

async function submitAddRooms() {
  const f = addRoomsForm.value
  if (!f.floorLabel || f.totalRooms < 1) return notify('error', 'Please fill all fields correctly.')
  savingRooms.value = true
  try {
    const res = await accommodationApi.addRoomsToHotel(addRoomsHotelId.value, f)
    notify('success', res.data?.message || 'Rooms added.')
    showAddRooms.value = false
    await loadHotelsSetup()
    await load()
  } catch (e) {
    notify('error', e.response?.data?.message || 'Failed to add rooms.')
  } finally { savingRooms.value = false }
}

function selectHotel(id) {
  selectedHotelId.value = id
  selectedFloor.value   = null
  activeTab.value = 'rooms'
}

function openEditFloor(floor) {
  editFloorId.value   = floor.id
  editFloorForm.value = { floorNumber: floor.floorNumber, label: floor.label }
  showEditFloor.value = true
}

async function submitEditFloor() {
  if (!editFloorForm.value.label) return notify('error', 'Floor label is required.')
  savingFloor.value = true
  try {
    await accommodationApi.updateFloor(editFloorId.value, editFloorForm.value)
    notify('success', 'Floor updated. Room numbers renamed.')
    showEditFloor.value = false
    await loadHotelsSetup()
    await load()
  } catch (e) {
    notify('error', e.response?.data?.message || 'Failed to update floor.')
  } finally { savingFloor.value = false }
}

async function deleteFloor(floor) {
  if (!confirm(`Delete "${floor.label}" and all its rooms?`)) return
  try {
    await accommodationApi.deleteFloor(floor.id)
    notify('success', `${floor.label} deleted.`)
    await loadHotelsSetup()
    await load()
  } catch (e) {
    notify('error', e.response?.data?.message || 'Failed to delete floor.')
  }
}
</script>

<template>
  <div class="space-y-5">

    <!-- Header -->
    <div class="flex items-center justify-between">
      <div>
        <h1 class="text-2xl font-bold text-white">Accommodation</h1>
        <p class="text-slate-400 text-sm mt-0.5">Hotel setup · room management · pilgrim assignment</p>
      </div>
      <button @click="activeTab === 'rooms' ? load() : (activeTab === 'setup' ? loadHotelsSetup() : loadInTransit())"
              class="text-slate-400 hover:text-gold-400 text-sm">↻ Refresh</button>
    </div>

    <!-- Tabs -->
    <div class="flex gap-1 bg-dark-800 rounded-xl p-1 w-fit">
      <button @click="switchTab('rooms')"
              :class="['px-4 py-2 rounded-lg text-sm font-medium transition-colors', activeTab === 'rooms' ? 'bg-gold-700 text-white' : 'text-slate-400 hover:text-white']">
        🛏 Rooms
      </button>
      <button @click="switchTab('setup')"
              :class="['px-4 py-2 rounded-lg text-sm font-medium transition-colors', activeTab === 'setup' ? 'bg-gold-700 text-white' : 'text-slate-400 hover:text-white']">
        🏨 Hotel Setup
      </button>
      <button @click="switchTab('transit')"
              :class="['px-4 py-2 rounded-lg text-sm font-medium transition-colors flex items-center gap-2', activeTab === 'transit' ? 'bg-orange-700 text-white' : 'text-slate-400 hover:text-white']">
        🚌 In Transit
        <span v-if="inTransit.length" class="bg-orange-500 text-white text-xs rounded-full px-1.5 py-0.5 leading-none">{{ inTransit.length }}</span>
      </button>
    </div>

    <!-- Toast -->
    <div v-if="msg.text"
         :class="['rounded-lg px-4 py-3 text-sm flex justify-between', msg.type === 'success' ? 'bg-green-900/50 border border-green-700 text-green-300' : 'bg-red-900/50 border border-red-700 text-red-300']">
      {{ msg.text }}<button @click="msg.text=''" class="opacity-60 hover:opacity-100">✕</button>
    </div>

    <!-- ═══════════════ ROOMS TAB ═══════════════ -->
    <template v-if="activeTab === 'rooms'">

      <!-- Stats -->
      <div v-if="occupancy" class="grid grid-cols-2 lg:grid-cols-4 gap-3">
        <div class="card flex items-center gap-3 py-3">
          <div class="w-10 h-10 rounded-xl bg-slate-800 flex items-center justify-center text-xl">🏨</div>
          <div><p class="text-xl font-bold text-white">{{ occupancy.totalRooms }}</p><p class="text-xs text-slate-400">Total Rooms</p></div>
        </div>
        <div class="card flex items-center gap-3 py-3">
          <div class="w-10 h-10 rounded-xl bg-green-900/40 flex items-center justify-center text-xl">✅</div>
          <div><p class="text-xl font-bold text-green-400">{{ occupancy.availableRooms }}</p><p class="text-xs text-slate-400">Available</p></div>
        </div>
        <div class="card flex items-center gap-3 py-3">
          <div class="w-10 h-10 rounded-xl bg-red-900/40 flex items-center justify-center text-xl">🔒</div>
          <div><p class="text-xl font-bold text-red-400">{{ occupancy.totalRooms - occupancy.availableRooms }}</p><p class="text-xs text-slate-400">Occupied</p></div>
        </div>
        <div class="card flex items-center gap-3 py-3">
          <div class="w-10 h-10 rounded-xl bg-gold-900/40 flex items-center justify-center text-xl">📊</div>
          <div><p class="text-xl font-bold text-gold-400">{{ occupancy.occupancyRate }}%</p><p class="text-xs text-slate-400">Occupancy</p></div>
        </div>
      </div>

      <!-- Legend -->
      <div class="flex items-center gap-5 text-xs text-slate-400">
        <span class="flex items-center gap-1.5"><span class="w-3 h-3 rounded-full bg-green-400"></span> Empty</span>
        <span class="flex items-center gap-1.5"><span class="w-3 h-3 rounded-full bg-gold-400"></span> Partially occupied</span>
        <span class="flex items-center gap-1.5"><span class="w-3 h-3 rounded-full bg-red-400"></span> Full</span>
      </div>

      <!-- Hotel filter tabs -->
      <div v-if="hotelList.length > 1" class="flex flex-wrap gap-2">
        <button @click="selectedHotelId = ''; selectedFloor = null"
                :class="['px-3 py-1.5 rounded-lg text-xs font-semibold border transition-all',
                  selectedHotelId === '' ? 'bg-gold-700 text-white border-gold-600' : 'bg-dark-800 text-slate-400 border-dark-600 hover:border-gold-700 hover:text-white']">
          🏘 All Hotels
        </button>
        <button v-for="h in hotelList" :key="h.id"
                @click="selectedHotelId = h.id; selectedFloor = null"
                :class="['px-3 py-1.5 rounded-lg text-xs font-semibold border transition-all',
                  selectedHotelId === h.id ? 'bg-gold-700 text-white border-gold-600' : 'bg-dark-800 text-slate-400 border-dark-600 hover:border-gold-700 hover:text-white']">
          🏨 {{ h.name }}
        </button>
      </div>

      <!-- Floor filter + status filter -->
      <div class="flex flex-wrap items-center gap-3">
        <div class="flex flex-wrap gap-1.5">
          <button @click="selectedFloor = null"
                  :class="['px-3 py-1.5 rounded-lg text-xs font-medium transition-colors', selectedFloor === null ? 'bg-gold-700 text-white' : 'bg-dark-700 text-slate-400 hover:text-white']">
            All Floors
          </button>
          <button v-for="[num, label] in floors" :key="num"
                  @click="selectedFloor = +num"
                  :class="['px-3 py-1.5 rounded-lg text-xs font-medium transition-colors', selectedFloor === +num ? 'bg-gold-700 text-white' : 'bg-dark-700 text-slate-400 hover:text-white']">
            {{ label }}
          </button>
        </div>
        <div class="flex gap-1.5 ml-auto">
          <button v-for="opt in [{v:'all',l:'All'},{v:'available',l:'Available'},{v:'full',l:'Full'}]" :key="opt.v"
                  @click="filterStatus = opt.v"
                  :class="['px-3 py-1.5 rounded-lg text-xs font-medium transition-colors', filterStatus === opt.v ? 'bg-slate-600 text-white' : 'bg-dark-700 text-slate-400 hover:text-white']">
            {{ opt.l }}
          </button>
        </div>
      </div>

      <!-- Loading -->
      <div v-if="loading" class="text-center py-16 text-slate-400">Loading rooms...</div>

      <!-- Rooms Grid grouped by floor -->
      <template v-else>
        <div v-if="!rooms.length" class="card text-center py-12">
          <p class="text-2xl mb-2">🏨</p>
          <p class="text-white font-semibold">No rooms yet</p>
          <p class="text-slate-400 text-sm mt-1">Go to <button @click="switchTab('setup')" class="text-gold-400 hover:underline">Hotel Setup</button> to create hotels and add rooms.</p>
        </div>

        <template v-else>
          <div v-for="group in floorGroups" :key="group.num" class="space-y-3">
            <!-- Floor header -->
            <div class="flex items-center gap-3">
              <div class="h-px flex-1 bg-dark-700"></div>
              <span class="text-xs font-semibold text-gold-500 uppercase tracking-wider px-2">{{ group.label }}</span>
              <div class="h-px flex-1 bg-dark-700"></div>
            </div>
            <!-- Room cards -->
            <div class="grid grid-cols-3 sm:grid-cols-4 md:grid-cols-6 lg:grid-cols-8 xl:grid-cols-10 gap-2">
              <div v-for="room in group.rooms" :key="room.id"
                   @click="openRoom(room)"
                   :class="['relative rounded-xl border cursor-pointer transition-all hover:-translate-y-0.5 hover:shadow-lg p-3', roomColor(room)]">
                <div :class="['absolute top-2 right-2 w-2 h-2 rounded-full', roomDot(room)]"></div>
                <p class="text-white font-bold text-sm leading-none mb-1">{{ room.roomNumber }}</p>
                <div class="w-full bg-dark-800 rounded-full h-1 mb-1.5">
                  <div :style="`width:${room.bedCapacity > 0 ? (room.occupiedBeds/room.bedCapacity)*100 : 0}%`"
                       :class="['h-1 rounded-full transition-all', room.occupiedBeds >= room.bedCapacity ? 'bg-red-400' : room.occupiedBeds > 0 ? 'bg-gold-400' : 'bg-green-400']">
                  </div>
                </div>
                <p class="text-slate-400 text-xs">{{ room.occupiedBeds }}/{{ room.bedCapacity }}</p>
                <p v-if="room.isForFamily" class="text-gold-500 text-xs">Family</p>
              </div>
            </div>
          </div>

          <div v-if="filteredRooms.length === 0" class="card text-center py-12">
            <p class="text-white">No rooms found</p>
            <p class="text-slate-400 text-sm mt-1">Try changing the filters.</p>
          </div>
        </template>
      </template>

    </template>
    <!-- ═══════════════ END ROOMS TAB ═══════════════ -->

    <!-- ═══════════════ HOTEL SETUP TAB ═══════════════ -->
    <template v-if="activeTab === 'setup'">

      <div class="flex items-center justify-between">
        <p class="text-slate-400 text-sm">{{ hotelsData.length }} hotel(s) configured</p>
        <button @click="openAddHotel"
                class="flex items-center gap-2 text-sm px-4 py-2 rounded-lg font-semibold text-white transition-colors"
                style="background: rgba(180,83,9,0.7); border: 1px solid rgba(180,83,9,0.5);">
          + Add Hotel
        </button>
      </div>

      <!-- No hotels state -->
      <div v-if="!hotelsData.length" class="card text-center py-16">
        <div class="text-5xl mb-4">🏨</div>
        <p class="text-white font-bold text-lg">No hotels yet</p>
        <p class="text-slate-400 text-sm mt-2 mb-6">Add your first hotel to start managing accommodation.</p>
        <button @click="openAddHotel"
                class="px-6 py-2.5 rounded-xl text-sm font-semibold text-white"
                style="background: rgba(180,83,9,0.7);">
          + Add First Hotel
        </button>
      </div>

      <!-- Hotel cards -->
      <div v-else class="space-y-4">
        <div v-for="hotel in hotelsData" :key="hotel.id"
             class="card border border-dark-600">

          <!-- Hotel header -->
          <div class="flex items-start justify-between gap-4">
            <div class="flex items-center gap-3">
              <div class="w-12 h-12 rounded-xl bg-gold-900/40 border border-gold-700/50 flex items-center justify-center text-2xl flex-shrink-0">🏨</div>
              <div>
                <p class="text-white font-bold text-lg">{{ hotel.name }}</p>
                <p class="text-slate-400 text-sm">📍 {{ hotel.city }}<span v-if="hotel.address"> · {{ hotel.address }}</span></p>
              </div>
            </div>
            <div class="flex items-center gap-2 flex-shrink-0">
              <button @click="selectHotel(hotel.id)"
                      class="text-xs px-3 py-1.5 rounded-lg border border-gold-800/60 text-gold-400 hover:bg-gold-900/30 transition-colors font-medium">
                View Rooms →
              </button>
              <button @click="openAddRooms(hotel)"
                      class="text-xs px-3 py-1.5 rounded-lg bg-emerald-800 hover:bg-emerald-700 text-white transition-colors font-medium">
                + Add Rooms
              </button>
              <button @click="deleteHotel(hotel)"
                      class="text-xs px-3 py-1.5 rounded-lg border border-red-800/60 text-red-400 hover:bg-red-950/40 transition-colors font-medium">
                Delete
              </button>
            </div>
          </div>

          <!-- Stats row -->
          <div class="mt-4 grid grid-cols-4 gap-3">
            <div class="bg-dark-900 rounded-lg p-3 text-center">
              <p class="text-lg font-bold text-white">{{ hotel.totalRooms }}</p>
              <p class="text-xs text-slate-500">Rooms</p>
            </div>
            <div class="bg-dark-900 rounded-lg p-3 text-center">
              <p class="text-lg font-bold text-green-400">{{ hotel.availableRooms }}</p>
              <p class="text-xs text-slate-500">Available</p>
            </div>
            <div class="bg-dark-900 rounded-lg p-3 text-center">
              <p class="text-lg font-bold text-red-400">{{ hotel.totalRooms - hotel.availableRooms }}</p>
              <p class="text-xs text-slate-500">Occupied</p>
            </div>
            <div class="bg-dark-900 rounded-lg p-3 text-center">
              <p class="text-lg font-bold text-gold-400">{{ hotel.totalBeds }}</p>
              <p class="text-xs text-slate-500">Total Beds</p>
            </div>
          </div>

          <!-- Floor breakdown -->
          <div v-if="hotel.floors?.length" class="mt-4">
            <p class="text-xs font-semibold text-slate-500 uppercase tracking-wider mb-2">Floors</p>
            <div class="space-y-2">
              <div v-for="floor in hotel.floors" :key="floor.id"
                   class="bg-dark-900 border border-dark-700 rounded-lg px-3 py-2 flex items-center justify-between gap-3">
                <div class="text-xs">
                  <p class="text-white font-medium">{{ floor.label }}</p>
                  <p class="text-slate-500 mt-0.5">
                    <span class="text-green-400">{{ floor.availableRooms }}</span>/{{ floor.totalRooms }} rooms ·
                    <span class="text-gold-400">{{ floor.occupiedBeds }}</span>/{{ floor.totalBeds }} beds
                    <span class="text-slate-600 ml-1">(Floor #{{ floor.floorNumber }})</span>
                  </p>
                </div>
                <div class="flex gap-1.5 flex-shrink-0">
                  <button @click="openEditFloor(floor)"
                          class="text-xs px-2.5 py-1 rounded-lg border border-gold-800/60 text-gold-400 hover:bg-gold-900/30 transition-colors">
                    ✏️ Edit
                  </button>
                  <button @click="deleteFloor(floor)"
                          class="text-xs px-2.5 py-1 rounded-lg border border-red-800/60 text-red-400 hover:bg-red-950/40 transition-colors">
                    🗑
                  </button>
                </div>
              </div>
            </div>
          </div>

          <div v-else class="mt-3 text-center py-4 bg-dark-900/50 rounded-lg border border-dashed border-dark-600">
            <p class="text-slate-500 text-xs">No rooms added yet. Click <strong class="text-gold-500">+ Add Rooms</strong> to get started.</p>
          </div>
        </div>
      </div>

    </template>
    <!-- ═══════════════ END SETUP TAB ═══════════════ -->

    <!-- ═══════════════ IN TRANSIT TAB ═══════════════ -->
    <template v-if="activeTab === 'transit'">
      <div class="card">
        <div class="px-5 py-3 border-b border-dark-700 flex items-center justify-between">
          <div>
            <h3 class="text-white font-semibold">Pilgrims In Transit</h3>
            <p class="text-slate-400 text-xs mt-0.5">Checked out but not yet assigned a new room</p>
          </div>
          <span v-if="inTransit.length" class="text-sm text-orange-400 font-medium">{{ inTransit.length }} waiting</span>
        </div>
        <div v-if="transitLoading" class="p-8 text-center text-slate-400">Loading...</div>
        <div v-else-if="!inTransit.length" class="p-10 text-center">
          <div class="text-4xl mb-3">✅</div>
          <p class="text-white font-medium">No pilgrims in transit</p>
          <p class="text-slate-400 text-sm mt-1">All pilgrims are currently assigned to a room.</p>
        </div>
        <div v-else class="divide-y divide-dark-700">
          <div v-for="p in inTransit" :key="p.pilgrimId"
               class="px-5 py-4 flex items-center gap-4 hover:bg-dark-800/50 transition-colors">
            <div class="w-10 h-10 rounded-full bg-orange-900/40 border border-orange-700/50 flex items-center justify-center text-sm font-bold text-orange-400 flex-shrink-0">
              {{ p.fullName?.split(' ').map(n=>n[0]).slice(0,2).join('') || '?' }}
            </div>
            <div class="flex-1 min-w-0">
              <p class="text-white font-semibold text-sm">{{ p.fullName }}</p>
              <p class="text-slate-500 text-xs">{{ p.country }}</p>
              <div class="flex flex-wrap gap-2 mt-1">
                <span class="text-xs text-slate-400">Last: <span class="text-orange-300">{{ p.lastHotel }}</span>, {{ p.lastCity }}</span>
                <span class="text-xs text-slate-500">Room {{ p.lastRoom }}</span>
                <span class="text-xs text-slate-500">Checked out {{ fmtDate(p.checkedOutAt) }}</span>
              </div>
            </div>
            <div class="flex items-center gap-2 flex-shrink-0">
              <span class="text-xs text-slate-500">{{ p.stayCount }} stay(s)</span>
              <button @click="openTrail(p.pilgrimId)"
                      class="text-xs px-3 py-1.5 rounded-lg font-medium text-primary-400 border border-primary-800 hover:bg-primary-900/30 transition-colors">
                View Trail
              </button>
            </div>
          </div>
        </div>
      </div>
    </template>
    <!-- ═══════════════ END IN TRANSIT ═══════════════ -->


    <!-- ─── MODAL: Add Hotel ──────────────────────────── -->
    <Teleport to="body">
      <div v-if="showAddHotel"
           class="fixed inset-0 z-50 flex items-center justify-center p-4"
           style="background: rgba(0,0,0,0.8);"
           @click.self="showAddHotel = false">
        <div class="w-full max-w-md rounded-2xl overflow-hidden shadow-2xl"
             style="background: #0f172a; border: 1px solid rgba(180,83,9,0.35);">
          <div class="h-0.5 w-full" style="background: linear-gradient(90deg, transparent, #d97706, transparent);"></div>
          <div class="px-6 py-4 border-b border-dark-700 flex items-center justify-between">
            <h3 class="text-white font-bold text-lg">Add Hotel</h3>
            <button @click="showAddHotel = false" class="text-slate-400 hover:text-white text-2xl">✕</button>
          </div>
          <div class="p-6 space-y-4">
            <div>
              <label class="form-label">Hotel Name *</label>
              <input v-model="addHotelForm.name" class="input w-full" placeholder="e.g. Kazimain Hotel 1" />
            </div>
            <div>
              <label class="form-label">City *</label>
              <input v-model="addHotelForm.city" class="input w-full" placeholder="e.g. Kazimain, Karbala, Najaf" />
            </div>
            <div>
              <label class="form-label">Address (optional)</label>
              <input v-model="addHotelForm.address" class="input w-full" placeholder="Street address..." />
            </div>
            <div>
              <label class="form-label">Phone (optional)</label>
              <input v-model="addHotelForm.phoneNumber" class="input w-full" placeholder="+964 ..." />
            </div>
            <div class="flex gap-3 pt-2">
              <button @click="createHotel" :disabled="savingHotel"
                      class="flex-1 py-2.5 rounded-xl text-sm font-semibold text-white disabled:opacity-50"
                      style="background: rgba(180,83,9,0.7);">
                {{ savingHotel ? 'Creating...' : '✓ Create Hotel' }}
              </button>
              <button @click="showAddHotel = false" class="btn-outline px-5">Cancel</button>
            </div>
          </div>
        </div>
      </div>
    </Teleport>

    <!-- ─── MODAL: Add Rooms to Hotel ─────────────────── -->
    <Teleport to="body">
      <div v-if="showAddRooms"
           class="fixed inset-0 z-50 flex items-center justify-center p-4"
           style="background: rgba(0,0,0,0.8);"
           @click.self="showAddRooms = false">
        <div class="w-full max-w-md rounded-2xl overflow-hidden shadow-2xl"
             style="background: #0f172a; border: 1px solid rgba(180,83,9,0.35);">
          <div class="h-0.5 w-full" style="background: linear-gradient(90deg, transparent, #d97706, transparent);"></div>
          <div class="px-6 py-4 border-b border-dark-700 flex items-center justify-between">
            <div>
              <h3 class="text-white font-bold text-lg">Add Rooms</h3>
              <p class="text-slate-400 text-xs mt-0.5">🏨 {{ addRoomsHotelName }}</p>
            </div>
            <button @click="showAddRooms = false" class="text-slate-400 hover:text-white text-2xl">✕</button>
          </div>
          <div class="p-6 space-y-4">

            <div class="grid grid-cols-2 gap-3">
              <div>
                <label class="form-label">Floor Number *</label>
                <input v-model.number="addRoomsForm.floorNumber" type="number" min="0" class="input w-full" placeholder="e.g. 1, 2, 5" />
              </div>
              <div>
                <label class="form-label">Floor Label *</label>
                <input v-model="addRoomsForm.floorLabel" class="input w-full" placeholder="e.g. Floor 1, Ground" />
              </div>
            </div>

            <div>
              <label class="form-label">Total Rooms on this Floor *</label>
              <input v-model.number="addRoomsForm.totalRooms" type="number" min="1" max="500" class="input w-full" placeholder="e.g. 20, 40, 50" />
            </div>

            <!-- Room number preview -->
            <div class="bg-dark-900 border border-dark-700 rounded-xl px-4 py-3">
              <p class="text-xs text-slate-500 uppercase font-semibold mb-2">Room Numbers Preview</p>
              <div class="flex items-center gap-3 flex-wrap">
                <span class="text-white font-bold text-lg">{{ roomPreview.first }}</span>
                <span class="text-slate-500">→</span>
                <span class="text-white font-bold text-lg">{{ roomPreview.last }}</span>
                <span class="text-slate-500 text-xs ml-1">({{ addRoomsForm.totalRooms }} rooms)</span>
              </div>
              <p class="text-xs text-slate-600 mt-1">Floor {{ addRoomsForm.floorNumber }}, Room 1 = <span class="text-gold-400">{{ roomPreview.first }}</span> · Floor {{ addRoomsForm.floorNumber }}, Room {{ addRoomsForm.totalRooms }} = <span class="text-gold-400">{{ roomPreview.last }}</span></p>
            </div>

            <div class="grid grid-cols-2 gap-3">
              <div>
                <label class="form-label">Beds per Room *</label>
                <input v-model.number="addRoomsForm.bedCapacity" type="number" min="1" max="20" class="input w-full" />
              </div>
              <div class="flex items-end pb-1">
                <label class="flex items-center gap-2 cursor-pointer">
                  <input v-model="addRoomsForm.isForFamily" type="checkbox" class="w-4 h-4 accent-gold-500" />
                  <span class="text-slate-300 text-sm">Family Rooms</span>
                </label>
              </div>
            </div>

            <div class="flex gap-3 pt-2">
              <button @click="submitAddRooms" :disabled="savingRooms"
                      class="flex-1 py-2.5 rounded-xl text-sm font-semibold text-white disabled:opacity-50"
                      style="background: rgba(180,83,9,0.7);">
                {{ savingRooms ? 'Adding...' : `✓ Add ${addRoomsForm.totalRooms} Rooms` }}
              </button>
              <button @click="showAddRooms = false" class="btn-outline px-5">Cancel</button>
            </div>
          </div>
        </div>
      </div>
    </Teleport>

    <!-- ─── MODAL: Edit Floor ─────────────────────────── -->
    <Teleport to="body">
      <div v-if="showEditFloor"
           class="fixed inset-0 z-50 flex items-center justify-center p-4"
           style="background: rgba(0,0,0,0.8);"
           @click.self="showEditFloor = false">
        <div class="w-full max-w-sm rounded-2xl overflow-hidden shadow-2xl"
             style="background: #0f172a; border: 1px solid rgba(180,83,9,0.35);">
          <div class="h-0.5 w-full" style="background: linear-gradient(90deg, transparent, #d97706, transparent);"></div>
          <div class="px-6 py-4 border-b border-dark-700 flex items-center justify-between">
            <h3 class="text-white font-bold text-lg">Edit Floor</h3>
            <button @click="showEditFloor = false" class="text-slate-400 hover:text-white text-2xl">✕</button>
          </div>
          <div class="p-6 space-y-4">

            <div>
              <label class="form-label">Floor Number *</label>
              <input v-model.number="editFloorForm.floorNumber" type="number" min="0" class="input w-full"
                     placeholder="e.g. 2" />
              <p class="text-xs text-slate-500 mt-1">
                Changing floor number will also rename all rooms.
                Floor <span class="text-gold-400">{{ editFloorForm.floorNumber }}</span> →
                rooms <span class="text-gold-400">{{ editFloorForm.floorNumber }}01</span>,
                <span class="text-gold-400">{{ editFloorForm.floorNumber }}02</span>...
              </p>
            </div>

            <div>
              <label class="form-label">Floor Label *</label>
              <input v-model="editFloorForm.label" class="input w-full" placeholder="e.g. Floor 2, Ground Floor" />
            </div>

            <div class="flex gap-3 pt-2">
              <button @click="submitEditFloor" :disabled="savingFloor"
                      class="flex-1 py-2.5 rounded-xl text-sm font-semibold text-white disabled:opacity-50"
                      style="background: rgba(180,83,9,0.7);">
                {{ savingFloor ? 'Saving...' : '✓ Save Changes' }}
              </button>
              <button @click="showEditFloor = false" class="btn-outline px-5">Cancel</button>
            </div>
          </div>
        </div>
      </div>
    </Teleport>

    <!-- ─── Room Detail Modal ──────────────────────────── -->
    <div v-if="showModal && activeRoom"
         class="fixed inset-0 z-50 flex items-center justify-center p-4"
         style="background: rgba(0,0,0,0.75); backdrop-filter: blur(4px);"
         @click.self="showModal = false">
      <div class="w-full max-w-lg rounded-2xl overflow-hidden shadow-2xl"
           style="background: #0a1208; border: 1px solid rgba(180,83,9,0.3);">
        <div class="px-5 py-4 flex items-center justify-between border-b border-dark-700">
          <div class="flex items-center gap-3">
            <div :class="['w-10 h-10 rounded-xl flex items-center justify-center text-lg border', roomColor(activeRoom)]">🛏️</div>
            <div>
              <h3 class="text-white font-bold text-base">Room {{ activeRoom.roomNumber }}</h3>
              <p class="text-slate-400 text-xs">{{ activeRoom.floorLabel }} · {{ activeRoom.hotelName }} · {{ activeRoom.occupiedBeds }}/{{ activeRoom.bedCapacity }} beds</p>
            </div>
          </div>
          <button @click="showModal = false" class="text-slate-400 hover:text-white text-xl leading-none">✕</button>
        </div>
        <div class="p-5 space-y-4 max-h-[70vh] overflow-y-auto">
          <div class="grid grid-cols-3 gap-3 text-center">
            <div class="bg-dark-800 rounded-lg p-3"><p class="text-xl font-bold text-white">{{ activeRoom.bedCapacity }}</p><p class="text-xs text-slate-500">Total Beds</p></div>
            <div class="bg-dark-800 rounded-lg p-3"><p class="text-xl font-bold text-gold-400">{{ activeRoom.occupiedBeds }}</p><p class="text-xs text-slate-500">Occupied</p></div>
            <div class="bg-dark-800 rounded-lg p-3"><p class="text-xl font-bold text-green-400">{{ activeRoom.bedCapacity - activeRoom.occupiedBeds }}</p><p class="text-xs text-slate-500">Free</p></div>
          </div>
          <div>
            <div class="flex items-center justify-between mb-3">
              <h4 class="text-white font-semibold text-sm">Occupants ({{ activeRoom.occupants?.length || 0 }})</h4>
              <button v-if="activeRoom.isAvailable" @click="openAssign(activeRoom)"
                      class="text-xs px-3 py-1.5 rounded-lg font-medium text-white transition-colors"
                      style="background: rgba(180,83,9,0.7);">+ Assign Pilgrim</button>
            </div>
            <div v-if="!activeRoom.occupants?.length" class="bg-dark-800 rounded-lg p-4 text-center text-slate-500 text-sm">No pilgrims assigned yet</div>
            <div v-else class="space-y-2">
              <div v-for="occ in activeRoom.occupants" :key="occ.pilgrimId"
                   class="bg-dark-800 rounded-xl p-3 flex items-start gap-3">
                <div class="w-10 h-10 rounded-full bg-gold-800/40 border border-gold-700/50 flex items-center justify-center text-sm font-bold text-gold-400 flex-shrink-0">
                  {{ occ.fullName?.split(' ').map(n=>n[0]).slice(0,2).join('') || '?' }}
                </div>
                <div class="flex-1 min-w-0">
                  <p class="text-white font-semibold text-sm">{{ occ.fullName }}</p>
                  <p class="text-slate-500 text-xs">{{ occ.country }} · {{ occ.familyMemberCount }} member(s)</p>
                  <div class="flex items-center gap-3 mt-1.5 flex-wrap">
                    <span class="flex items-center gap-1 text-xs text-green-400"><span>✈️ In:</span><span>{{ fmtDate(occ.checkIn) }}</span></span>
                    <span class="flex items-center gap-1 text-xs text-red-400"><span>✈️ Out:</span><span>{{ fmtDate(occ.checkOut) }}</span></span>
                  </div>
                </div>
                <div class="flex flex-col gap-1 flex-shrink-0 mt-1">
                  <button @click="openTrail(occ.pilgrimId)" class="text-primary-400 hover:text-primary-300 text-xs" title="View hotel trail">🗺️</button>
                  <button @click="deallocate(activeRoom, occ)" class="text-red-500 hover:text-red-300 text-xs" title="Remove from room">✕</button>
                </div>
              </div>
            </div>
          </div>
          <button v-if="activeRoom.isAvailable" @click="openAssign(activeRoom)"
                  class="w-full py-2.5 rounded-xl text-sm font-semibold text-white transition-colors"
                  style="background: linear-gradient(135deg, rgba(180,83,9,0.6), rgba(180,83,9,0.4)); border: 1px solid rgba(180,83,9,0.4);">
            + Assign Pilgrim to This Room
          </button>
        </div>
      </div>
    </div>

    <!-- ─── Assign Pilgrim Modal ───────────────────────── -->
    <div v-if="showAssign && assignRoom"
         class="fixed inset-0 z-50 flex items-center justify-center p-4"
         style="background: rgba(0,0,0,0.8); backdrop-filter: blur(4px);"
         @click.self="showAssign = false">
      <div class="w-full max-w-md rounded-2xl overflow-hidden shadow-2xl"
           style="background: #0a1208; border: 1px solid rgba(180,83,9,0.3);">
        <div class="px-5 py-4 border-b border-dark-700 flex items-center justify-between">
          <div>
            <h3 class="text-white font-bold">Assign Pilgrim</h3>
            <p class="text-slate-400 text-xs">Room {{ assignRoom.roomNumber }} · {{ assignRoom.floorLabel }} · {{ assignRoom.hotelName }}</p>
          </div>
          <button @click="showAssign = false" class="text-slate-400 hover:text-white text-xl">✕</button>
        </div>
        <div class="p-5 space-y-4">
          <input v-model="assignSearch" class="input w-full" placeholder="Search pilgrim by name, email or country..." autofocus />
          <div class="max-h-72 overflow-y-auto space-y-2">
            <div v-if="!assignablePilgrims.length" class="text-center text-slate-500 text-sm py-6">No unassigned pilgrims found</div>
            <div v-for="p in assignablePilgrims" :key="p.id"
                 @click="assign(p)"
                 class="flex items-center gap-3 bg-dark-800 hover:bg-dark-700 rounded-xl px-3 py-2.5 cursor-pointer transition-colors group">
              <div class="w-9 h-9 rounded-full bg-primary-900/50 border border-primary-800/50 flex items-center justify-center text-xs font-bold text-primary-400 flex-shrink-0">
                {{ p.fullName?.split(' ').map(n=>n[0]).slice(0,2).join('') }}
              </div>
              <div class="flex-1 min-w-0">
                <p class="text-white text-sm font-medium">{{ p.fullName }}</p>
                <p class="text-slate-500 text-xs">{{ p.country }} · {{ p.familyMemberCount }} member(s)</p>
                <div class="flex gap-3 mt-0.5 text-xs">
                  <span class="text-green-400">In: {{ fmtDate(p.arrivalDate) }}</span>
                  <span class="text-red-400">Out: {{ fmtDate(p.departureDate) }}</span>
                </div>
              </div>
              <span class="text-gold-400 text-sm opacity-0 group-hover:opacity-100 transition-opacity">→</span>
            </div>
          </div>
          <p class="text-xs text-slate-600 text-center">Only unassigned pilgrims shown · {{ pilgrims.length }} available</p>
        </div>
      </div>
    </div>

    <!-- ─── Pilgrim Trail Modal ──────────────────────────── -->
    <div v-if="showTrail"
         class="fixed inset-0 z-50 flex items-center justify-center p-4"
         style="background: rgba(0,0,0,0.8); backdrop-filter: blur(4px);"
         @click.self="showTrail = false">
      <div class="w-full max-w-lg rounded-2xl overflow-hidden shadow-2xl"
           style="background: #0a1208; border: 1px solid rgba(59,130,246,0.3);">
        <div class="px-5 py-4 border-b border-dark-700 flex items-center justify-between"
             style="background: rgba(59,130,246,0.08);">
          <div>
            <h3 class="text-white font-bold">Hotel Trail</h3>
            <p v-if="trailData" class="text-slate-400 text-xs mt-0.5">{{ trailData.pilgrimName }} · {{ trailData.country }} · {{ trailData.trail?.length || 0 }} stay(s)</p>
          </div>
          <button @click="showTrail = false" class="text-slate-400 hover:text-white text-xl leading-none">✕</button>
        </div>
        <div class="p-5 max-h-[70vh] overflow-y-auto">
          <div v-if="trailLoading" class="text-center py-10 text-slate-400">Loading trail...</div>
          <div v-else-if="!trailData?.trail?.length" class="text-center py-10">
            <div class="text-3xl mb-2">📋</div>
            <p class="text-white">No hotel history yet</p>
            <p class="text-slate-400 text-sm mt-1">This pilgrim has not been assigned any room yet.</p>
          </div>
          <div v-else class="relative">
            <div class="absolute left-5 top-0 bottom-0 w-0.5 bg-dark-700"></div>
            <div class="space-y-1">
              <div v-for="(stay, i) in trailData.trail" :key="stay.id" class="relative flex gap-4 pb-6">
                <div :class="['relative z-10 w-10 h-10 rounded-full flex items-center justify-center text-sm flex-shrink-0 border-2',
                  stay.isCurrentStay ? 'bg-green-900 border-green-500 text-green-300' : 'bg-dark-800 border-dark-600 text-slate-400']">{{ i + 1 }}</div>
                <div :class="['flex-1 rounded-xl p-4 border', stay.isCurrentStay ? 'border-green-700/50 bg-green-900/10' : 'border-dark-700 bg-dark-800/60']">
                  <div class="flex items-start justify-between gap-2">
                    <div>
                      <p class="text-white font-semibold text-sm">{{ stay.hotelName }}</p>
                      <p class="text-slate-400 text-xs">{{ stay.hotelCity }}</p>
                    </div>
                    <span :class="['text-xs px-2 py-0.5 rounded-full flex-shrink-0', stay.isCurrentStay ? 'bg-green-900/60 text-green-400 border border-green-700/50' : 'bg-dark-700 text-slate-500']">
                      {{ stay.isCurrentStay ? 'Current' : 'Completed' }}
                    </span>
                  </div>
                  <div class="mt-2 flex flex-wrap gap-x-4 gap-y-1 text-xs">
                    <span class="text-slate-400">Room <span class="text-white font-medium">{{ stay.roomNumber }}</span></span>
                    <span class="text-slate-400">{{ stay.floorLabel }}</span>
                  </div>
                  <div class="mt-2 flex flex-wrap gap-x-4 gap-y-1 text-xs">
                    <span class="text-green-400">✈️ In: {{ fmtDate(stay.checkedInAt) }}</span>
                    <span v-if="stay.checkedOutAt" class="text-red-400">🚪 Out: {{ fmtDate(stay.checkedOutAt) }} <span class="text-slate-500">({{ stay.checkedOutBy }})</span></span>
                    <span v-else class="text-green-400">🟢 Still here</span>
                  </div>
                </div>
              </div>
            </div>
            <div :class="['rounded-xl p-3 text-center text-sm mt-2 border',
              trailData.currentRoomId ? 'bg-green-900/20 border-green-700/40 text-green-400' : 'bg-orange-900/20 border-orange-700/40 text-orange-400']">
              <span v-if="trailData.currentRoomId">✅ Currently assigned to a room</span>
              <span v-else>🚌 In Transit — awaiting next room assignment</span>
            </div>
          </div>
        </div>
      </div>
    </div>

  </div>
</template>
