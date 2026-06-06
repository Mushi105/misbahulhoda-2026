<script setup>
import { ref, computed, onMounted, watch } from 'vue'
import { toursApi } from '@/api'

const tours = ref([])
const loading = ref(false)
const activeTab = ref('tours')
const selectedTour = ref(null)
const feedback = ref(null)
const feedbackLoading = ref(false)

// Modals
const showTourModal = ref(false)
const showPackageModal = ref(false)
const showHistoryModal = ref(false)
const editingTour = ref(null)
const editingPackage = ref(null)
const historyData = ref([])
const historyLoading = ref(false)
const tourFormError = ref('')

const tourForm = ref({
  tourName: '', tourType: 1, year: new Date().getFullYear(),
  startDate: '', endDate: '', status: 1, isOpen: true, description: ''
})

const packageForm = ref({
  packageName: '', description: '', price: 0, currency: 'USD',
  maxPilgrims: 100, durationDays: 14, includesAirportTransfer: false,
  includesBreakfast: false, includesLunch: false, includesDinner: false,
  hotelInfo: '', notes: '', isActive: true
})

const tourTypeLabels = { 1: 'Arbaeen', 2: 'Umrah', 3: 'Ashura', 4: 'Ziyarat', 5: 'Hajj', 6: 'Other' }
const tourStatusLabels = { 1: 'Upcoming', 2: 'Active', 3: 'Completed', 4: 'Cancelled' }
const tourStatusColors = {
  1: 'text-blue-400 bg-blue-900/30',
  2: 'text-green-400 bg-green-900/30',
  3: 'text-slate-400 bg-slate-800',
  4: 'text-red-400 bg-red-900/30'
}

const totalPilgrims = computed(() => tours.value.reduce((s, t) => s + (t.totalPilgrims || 0), 0))
const openTours = computed(() => tours.value.filter(t => t.isOpen).length)
const activeTours = computed(() => tours.value.filter(t => t.status === 2).length)

async function loadTours() {
  loading.value = true
  try {
    const res = await toursApi.getAll()
    tours.value = res.data.data || []
  } catch { tours.value = [] } finally { loading.value = false }
}

async function loadFeedback(tourId) {
  if (!tourId) return
  feedbackLoading.value = true
  try {
    const res = await toursApi.getFeedback(tourId)
    const d = res.data.data
    feedback.value = d ? {
      totalResponses: d.summary?.total ?? 0,
      averageOverall: d.summary?.averageOverall,
      averageHotel: d.summary?.averageHotel,
      averageTransport: d.summary?.averageTransport,
      recommendPercent: d.summary?.wouldRecommend && d.summary?.total
        ? (d.summary.wouldRecommend / d.summary.total) * 100 : 0,
      responses: d.feedbacks || [],
    } : null
  } catch { feedback.value = null } finally { feedbackLoading.value = false }
}

async function loadUserHistory(userId) {
  historyLoading.value = true
  try {
    const res = await toursApi.getUserHistory(userId)
    historyData.value = res.data.data || []
  } catch { historyData.value = [] } finally { historyLoading.value = false }
}

function openCreateTour() {
  editingTour.value = null
  tourFormError.value = ''
  tourForm.value = { tourName: '', tourType: 1, year: new Date().getFullYear(), startDate: '', endDate: '', status: 1, isOpen: true, description: '' }
  showTourModal.value = true
}

function openEditTour(tour) {
  editingTour.value = tour
  tourFormError.value = ''
  tourForm.value = {
    tourName:    tour.tourName,
    tourType:    Number(tour.tourType),
    year:        Number(tour.year),
    startDate:   tour.startDate?.slice(0, 10) || '',
    endDate:     tour.endDate?.slice(0, 10)   || '',
    status:      Number(tour.status),
    isOpen:      tour.isOpen,
    description: tour.description || '',
  }
  showTourModal.value = true
}

async function saveTour() {
  tourFormError.value = ''
  if (!tourForm.value.tourName?.trim()) { tourFormError.value = 'Tour name is required'; return }
  try {
    const payload = {
      ...tourForm.value,
      tourName: tourForm.value.tourName.trim(),
      tourType: Number(tourForm.value.tourType),
      status:   Number(tourForm.value.status),
      year:     Number(tourForm.value.year),
    }
    if (editingTour.value) {
      await toursApi.update(editingTour.value.id, payload)
    } else {
      await toursApi.create(payload)
    }
    showTourModal.value = false
    await loadTours()
  } catch (e) {
    tourFormError.value = e.response?.data?.message || 'Error saving tour'
  }
}

async function deleteTour(id) {
  if (!confirm('Delete this tour?')) return
  await toursApi.delete(id)
  await loadTours()
}

async function completeTour(id) {
  if (!confirm('Mark this tour as completed? This will trigger feedback emails to all pilgrims.')) return
  await toursApi.complete(id)
  await loadTours()
}

async function sendFeedbackEmails(id) {
  if (!confirm('Send feedback request emails to all pilgrims of this tour?')) return
  await toursApi.sendFeedbackEmails(id)
  alert('Feedback emails queued successfully!')
}

const notifying = ref(null)
async function notifyCustomers(tour) {
  if (!confirm(`Send WhatsApp + Email to ALL existing customers about "${tour.tourName}"?\n\nThis will notify every pilgrim who has ever registered with us.`)) return
  notifying.value = tour.id
  try {
    const res = await toursApi.notifyCustomers(tour.id)
    const sent = res.data.data?.sent ?? 0
    alert(`Done! Notifications sent to ${sent} customers via Email + WhatsApp.`)
  } catch (e) {
    alert(e.response?.data?.message || 'Error sending notifications')
  } finally { notifying.value = null }
}

const DESTINATIONS = ['Iraq', 'Iran', 'Sham (Syria)', 'Saudi Arabia', 'Hajj', 'Other']

const defaultPkg = () => ({
  packageName: '', description: '', price: 0, currency: 'USD',
  maxPilgrims: 100, durationDays: 14,
  tripDuration: 1, destinations: '', routeDetail: '',
  includesAirportTransfer: false, includesBreakfast: false,
  includesLunch: false, includesDinner: false,
  hotelInfo: '', notes: '', isActive: true,
})

// helper: destinations string ↔ array
const selectedDests = computed({
  get: () => packageForm.value.destinations
    ? packageForm.value.destinations.split(',').map(d => d.trim()).filter(Boolean)
    : [],
  set: (arr) => { packageForm.value.destinations = arr.join(',') }
})
function toggleDest(d) {
  const arr = selectedDests.value
  const idx = arr.indexOf(d)
  if (idx === -1) selectedDests.value = [...arr, d]
  else            selectedDests.value = arr.filter(x => x !== d)
}

function openAddPackage(tour) {
  selectedTour.value = tour
  editingPackage.value = null
  packageForm.value = defaultPkg()
  showPackageModal.value = true
}

function openEditPackage(tour, pkg) {
  selectedTour.value = tour
  editingPackage.value = pkg
  packageForm.value = {
    ...pkg,
    tripDuration: Number(pkg.tripDuration ?? 1),
    destinations: pkg.destinations || '',
  }
  showPackageModal.value = true
}

async function savePackage() {
  try {
    const payload = {
      ...packageForm.value,
      tripDuration: Number(packageForm.value.tripDuration),
    }
    if (editingPackage.value) {
      await toursApi.updatePackage(editingPackage.value.id, payload)
    } else {
      await toursApi.addPackage(selectedTour.value.id, payload)
    }
    showPackageModal.value = false
    await loadTours()
  } catch (e) { alert(e.response?.data?.message || 'Error saving package') }
}

async function deletePackage(id) {
  if (!confirm('Delete this package?')) return
  await toursApi.deletePackage(id)
  await loadTours()
}

function selectTourForFeedback(tour) {
  selectedTour.value = tour
  feedback.value = null
  activeTab.value = 'feedback'
  loadFeedback(tour.id)
}

onMounted(loadTours)
</script>

<template>
  <div>
    <div class="flex items-center justify-between mb-6">
      <div>
        <h2 class="text-2xl font-bold text-white">Tour Management</h2>
        <p class="text-slate-400 text-sm mt-1">Manage tours, packages & pilgrim history</p>
      </div>
      <button @click="openCreateTour" class="btn-gold flex items-center gap-2">
        <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4"/>
        </svg>
        New Tour
      </button>
    </div>

    <!-- Stats -->
    <div class="grid grid-cols-2 md:grid-cols-4 gap-4 mb-6">
      <div class="card text-center">
        <p class="text-3xl font-bold text-gold-400">{{ tours.length }}</p>
        <p class="text-slate-400 text-sm">Total Tours</p>
      </div>
      <div class="card text-center">
        <p class="text-3xl font-bold text-green-400">{{ openTours }}</p>
        <p class="text-slate-400 text-sm">Open for Apply</p>
      </div>
      <div class="card text-center">
        <p class="text-3xl font-bold text-blue-400">{{ activeTours }}</p>
        <p class="text-slate-400 text-sm">Active Tours</p>
      </div>
      <div class="card text-center">
        <p class="text-3xl font-bold text-amber-400">{{ totalPilgrims }}</p>
        <p class="text-slate-400 text-sm">Total Pilgrims</p>
      </div>
    </div>

    <!-- Tabs -->
    <div class="flex gap-1 mb-6 bg-slate-900 p-1 rounded-xl w-fit">
      <button v-for="tab in ['tours','feedback']" :key="tab" @click="activeTab = tab"
        :class="['px-5 py-2 rounded-lg text-sm font-medium capitalize transition-colors',
          activeTab === tab ? 'bg-gold-700 text-white' : 'text-slate-400 hover:text-white']">
        {{ tab }}
      </button>
    </div>

    <!-- TOURS TAB -->
    <div v-if="activeTab === 'tours'">
      <div v-if="loading" class="text-center py-12 text-slate-400">Loading tours...</div>
      <div v-else-if="!tours.length" class="text-center py-12">
        <p class="text-slate-500 mb-4">No tours created yet</p>
        <button @click="openCreateTour" class="btn-gold">Create First Tour</button>
      </div>
      <div v-else class="space-y-6">
        <div v-for="tour in tours" :key="tour.id" class="card">
          <div class="flex flex-wrap items-start justify-between gap-4 mb-4">
            <div>
              <div class="flex items-center gap-3 flex-wrap">
                <h3 class="text-white font-bold text-lg">{{ tour.tourName }}</h3>
                <span :class="['px-2.5 py-0.5 rounded-full text-xs font-medium', tourStatusColors[tour.status]]">
                  {{ tourStatusLabels[tour.status] }}
                </span>
                <span v-if="tour.isOpen" class="px-2.5 py-0.5 rounded-full text-xs font-medium bg-green-900/30 text-green-400">
                  Open
                </span>
              </div>
              <div class="flex flex-wrap items-center gap-4 mt-2 text-sm text-slate-400">
                <span>{{ tourTypeLabels[tour.tourType] }} · {{ tour.year }}</span>
                <span v-if="tour.startDate">{{ tour.startDate?.slice(0,10) }} → {{ tour.endDate?.slice(0,10) }}</span>
                <span>{{ tour.packages?.length || 0 }} packages</span>
                <span>{{ tour.totalPilgrims || 0 }} pilgrims ({{ tour.approvedPilgrims || 0 }} approved)</span>
              </div>
              <p v-if="tour.description" class="text-slate-500 text-sm mt-1">{{ tour.description }}</p>
            </div>
            <div class="flex flex-wrap gap-2">
              <button @click="openEditTour(tour)" class="btn-ghost text-xs">Edit</button>
              <button @click="openAddPackage(tour)" class="btn-ghost text-xs text-blue-400">+ Package</button>
              <button @click="selectTourForFeedback(tour)" class="btn-ghost text-xs text-amber-400">Feedback</button>
              <button v-if="tour.status !== 3" @click="completeTour(tour.id)" class="btn-ghost text-xs text-green-400">Complete</button>
              <button v-if="tour.status === 3" @click="sendFeedbackEmails(tour.id)" class="btn-ghost text-xs text-purple-400">Send Emails</button>
              <button @click="notifyCustomers(tour)" :disabled="notifying === tour.id"
                class="btn-ghost text-xs text-cyan-400 disabled:opacity-50 flex items-center gap-1">
                <svg v-if="notifying === tour.id" class="w-3 h-3 animate-spin" fill="none" viewBox="0 0 24 24">
                  <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"/>
                  <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8v8z"/>
                </svg>
                {{ notifying === tour.id ? 'Sending...' : '📣 Notify Customers' }}
              </button>
              <button @click="deleteTour(tour.id)" class="btn-ghost text-xs text-red-400">Delete</button>
            </div>
          </div>

          <!-- Packages -->
          <div v-if="tour.packages?.length" class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-3">
            <div v-for="pkg in tour.packages" :key="pkg.id"
              class="rounded-lg p-4 border" style="background: rgba(255,255,255,0.03); border-color: rgba(180,83,9,0.15);">

              <!-- Header row -->
              <div class="flex items-start justify-between mb-3">
                <div>
                  <p class="text-white font-semibold text-sm">{{ pkg.packageName }}</p>
                  <span v-if="!pkg.isActive" class="text-xs text-slate-500 italic">inactive</span>
                </div>
                <div class="flex gap-1">
                  <button @click="openEditPackage(tour, pkg)" class="text-slate-500 hover:text-white p-1">
                    <svg class="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z"/>
                    </svg>
                  </button>
                  <button @click="deletePackage(pkg.id)" class="text-slate-500 hover:text-red-400 p-1">
                    <svg class="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16"/>
                    </svg>
                  </button>
                </div>
              </div>

              <!-- Pricing block -->
              <div class="rounded-lg p-3 mb-3" style="background: rgba(180,83,9,0.08); border: 1px solid rgba(180,83,9,0.2);">
                <div class="flex items-end justify-between">
                  <div>
                    <p class="text-xs text-slate-500 mb-0.5">Per person</p>
                    <p class="text-gold-400 font-bold text-xl leading-none">
                      {{ Number(pkg.price).toLocaleString() }}
                      <span class="text-sm font-normal text-gold-600">{{ pkg.currency }}</span>
                    </p>
                  </div>
                  <div class="text-right">
                    <p class="text-xs text-slate-500 mb-0.5">Per day</p>
                    <p class="text-slate-300 font-medium text-sm">
                      {{ pkg.durationDays > 0 ? Math.round(pkg.price / pkg.durationDays).toLocaleString() : '—' }}
                      <span class="text-xs text-slate-600">{{ pkg.currency }}/day</span>
                    </p>
                  </div>
                </div>
                <div class="border-t mt-2 pt-2" style="border-color: rgba(180,83,9,0.2);">
                  <div class="flex items-center justify-between text-xs">
                    <span class="text-slate-500">Total capacity revenue</span>
                    <span class="text-green-400 font-medium">
                      {{ (Number(pkg.price) * pkg.maxPilgrims).toLocaleString() }} {{ pkg.currency }}
                    </span>
                  </div>
                  <div class="flex items-center justify-between text-xs mt-1">
                    <span class="text-slate-500">Enrolled / Max</span>
                    <span class="text-slate-300 font-medium">
                      {{ pkg.pilgrimsCount || 0 }} / {{ pkg.maxPilgrims }}
                      <span v-if="pkg.maxPilgrims > 0" class="text-slate-600">
                        ({{ Math.round(((pkg.pilgrimsCount || 0) / pkg.maxPilgrims) * 100) }}%)
                      </span>
                    </span>
                  </div>
                  <div v-if="(pkg.pilgrimsCount || 0) > 0" class="mt-1.5">
                    <div class="h-1 rounded-full bg-slate-800 overflow-hidden">
                      <div class="h-full rounded-full bg-gold-600 transition-all"
                        :style="{ width: Math.min(((pkg.pilgrimsCount || 0) / pkg.maxPilgrims) * 100, 100) + '%' }" />
                    </div>
                  </div>
                </div>
              </div>

              <!-- Details -->
              <div class="text-xs text-slate-500 space-y-1.5">
                <div class="flex flex-wrap gap-1 mb-1">
                  <span :class="['px-2 py-0.5 rounded-full font-medium',
                    pkg.tripDuration == 1 ? 'bg-sky-900/30 text-sky-400' : 'bg-violet-900/30 text-violet-400']">
                    {{ pkg.tripDuration == 1 ? 'Short' : 'Long' }}
                  </span>
                  <template v-if="pkg.destinations">
                    <span v-for="d in pkg.destinations.split(',')" :key="d"
                      class="px-2 py-0.5 rounded-full bg-gold-900/20 text-gold-500 font-medium">
                      {{ d.trim() }}
                    </span>
                  </template>
                </div>
                <p v-if="pkg.routeDetail" class="text-slate-500 italic truncate" :title="pkg.routeDetail">
                  {{ pkg.routeDetail }}
                </p>
                <p>{{ pkg.durationDays }} days · {{ pkg.maxPilgrims }} seats total</p>
                <p v-if="pkg.hotelInfo" class="text-slate-600 truncate">{{ pkg.hotelInfo }}</p>
                <div class="flex flex-wrap gap-1 mt-1">
                  <span v-if="pkg.includesAirportTransfer" class="px-1.5 py-0.5 rounded bg-blue-900/30 text-blue-400">✈ Transfer</span>
                  <span v-if="pkg.includesBreakfast" class="px-1.5 py-0.5 rounded bg-amber-900/30 text-amber-400">🍳 B</span>
                  <span v-if="pkg.includesLunch" class="px-1.5 py-0.5 rounded bg-amber-900/30 text-amber-400">🍱 L</span>
                  <span v-if="pkg.includesDinner" class="px-1.5 py-0.5 rounded bg-amber-900/30 text-amber-400">🍽 D</span>
                </div>
              </div>
            </div>
          </div>
          <p v-else class="text-slate-600 text-sm italic">No packages yet — add one to open applications</p>
        </div>
      </div>
    </div>

    <!-- FEEDBACK TAB -->
    <div v-if="activeTab === 'feedback'">
      <div v-if="!selectedTour" class="text-center py-12">
        <p class="text-slate-500 mb-2">Select a tour from the Tours tab to view feedback</p>
        <button @click="activeTab = 'tours'" class="btn-ghost text-sm">Go to Tours</button>
      </div>
      <div v-else>
        <div class="flex items-center gap-3 mb-6">
          <button @click="activeTab = 'tours'" class="text-slate-400 hover:text-white">
            <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M10 19l-7-7m0 0l7-7m-7 7h18"/>
            </svg>
          </button>
          <h3 class="text-white font-bold">Feedback — {{ selectedTour.tourName }}</h3>
        </div>
        <div v-if="feedbackLoading" class="text-center py-12 text-slate-400">Loading feedback...</div>
        <div v-else-if="!feedback || !feedback.totalResponses" class="text-center py-12">
          <p class="text-slate-500">No feedback received for this tour yet</p>
          <button @click="sendFeedbackEmails(selectedTour.id)" class="btn-gold mt-4 text-sm">Send Feedback Request Emails</button>
        </div>
        <div v-else class="space-y-6">
          <div class="grid grid-cols-2 md:grid-cols-5 gap-4">
            <div class="card text-center">
              <p class="text-3xl font-bold text-gold-400">{{ feedback.totalResponses }}</p>
              <p class="text-slate-400 text-xs">Responses</p>
            </div>
            <div class="card text-center">
              <p class="text-3xl font-bold text-amber-400">{{ feedback.averageOverall?.toFixed(1) }}/5</p>
              <p class="text-slate-400 text-xs">Overall</p>
            </div>
            <div class="card text-center">
              <p class="text-3xl font-bold text-blue-400">{{ feedback.averageHotel?.toFixed(1) }}/5</p>
              <p class="text-slate-400 text-xs">Hotel</p>
            </div>
            <div class="card text-center">
              <p class="text-3xl font-bold text-green-400">{{ feedback.averageTransport?.toFixed(1) }}/5</p>
              <p class="text-slate-400 text-xs">Transport</p>
            </div>
            <div class="card text-center">
              <p class="text-3xl font-bold text-purple-400">{{ feedback.recommendPercent?.toFixed(0) }}%</p>
              <p class="text-slate-400 text-xs">Would Recommend</p>
            </div>
          </div>
          <div v-if="feedback.responses?.length" class="space-y-3">
            <h4 class="text-white font-medium">Individual Responses</h4>
            <div v-for="r in feedback.responses" :key="r.id"
              class="card text-sm">
              <div class="flex items-center justify-between mb-2">
                <div class="flex gap-3 text-slate-400">
                  <span>Overall: <span class="text-gold-400 font-bold">{{ r.overallRating }}/5</span></span>
                  <span v-if="r.hotelRating">Hotel: {{ r.hotelRating }}/5</span>
                  <span v-if="r.transportRating">Transport: {{ r.transportRating }}/5</span>
                </div>
                <div class="flex gap-2 text-xs">
                  <span v-if="r.wouldRecommend" class="text-green-400">✓ Recommends</span>
                  <span v-if="r.wouldReturn" class="text-blue-400">✓ Returns</span>
                </div>
              </div>
              <p v-if="r.comment" class="text-slate-300">"{{ r.comment }}"</p>
              <p v-if="r.suggestions" class="text-slate-500 mt-1">Suggestion: {{ r.suggestions }}</p>
              <p v-if="r.preferredPackageNextYear" class="text-amber-500 mt-1">Next year: {{ r.preferredPackageNextYear }}</p>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- TOUR MODAL -->
    <div v-if="showTourModal" class="fixed inset-0 z-50 flex items-center justify-center p-4"
      style="background: rgba(0,0,0,0.7);" @click.self="showTourModal = false">
      <div class="card w-full max-w-lg max-h-[90vh] overflow-y-auto">
        <h3 class="text-white font-bold text-lg mb-4">{{ editingTour ? 'Edit Tour' : 'Create New Tour' }}</h3>
        <div class="space-y-3">
          <div>
            <label class="label">Tour Name</label>
            <input v-model="tourForm.tourName" class="input" placeholder="e.g. Arbaeen 2026" />
          </div>
          <div class="grid grid-cols-2 gap-3">
            <div>
              <label class="label">Type</label>
              <select v-model="tourForm.tourType" class="input">
                <option :value="1">Arbaeen</option>
                <option :value="2">Umrah</option>
                <option :value="3">Ashura</option>
                <option :value="4">Ziyarat</option>
                <option :value="5">Hajj</option>
                <option :value="6">Other</option>
              </select>
            </div>
            <div>
              <label class="label">Year</label>
              <input v-model.number="tourForm.year" type="number" class="input" />
            </div>
          </div>
          <div class="grid grid-cols-2 gap-3">
            <div>
              <label class="label">Start Date</label>
              <input v-model="tourForm.startDate" type="date" class="input" />
            </div>
            <div>
              <label class="label">End Date</label>
              <input v-model="tourForm.endDate" type="date" class="input" />
            </div>
          </div>
          <div class="grid grid-cols-2 gap-3">
            <div>
              <label class="label">Status</label>
              <select v-model="tourForm.status" class="input">
                <option :value="1">Upcoming</option>
                <option :value="2">Active</option>
                <option :value="3">Completed</option>
                <option :value="4">Cancelled</option>
              </select>
            </div>
            <div class="flex items-center gap-3 pt-5">
              <input type="checkbox" id="isOpen" v-model="tourForm.isOpen" class="w-4 h-4" />
              <label for="isOpen" class="text-slate-300 text-sm">Open for Applications</label>
            </div>
          </div>
          <div>
            <label class="label">Description (optional)</label>
            <textarea v-model="tourForm.description" class="input" rows="2" />
          </div>
        </div>
        <div v-if="tourFormError" class="mt-4 p-3 rounded-lg bg-red-950/40 border border-red-800/40 text-red-400 text-sm">
          {{ tourFormError }}
        </div>
        <div class="flex gap-3 mt-4">
          <button @click="saveTour" class="btn-gold flex-1">{{ editingTour ? 'Update Tour' : 'Create Tour' }}</button>
          <button @click="showTourModal = false" class="btn-ghost flex-1">Cancel</button>
        </div>
      </div>
    </div>

    <!-- PACKAGE MODAL -->
    <div v-if="showPackageModal" class="fixed inset-0 z-50 flex items-center justify-center p-4"
      style="background: rgba(0,0,0,0.7);" @click.self="showPackageModal = false">
      <div class="card w-full max-w-lg max-h-[90vh] overflow-y-auto">
        <h3 class="text-white font-bold text-lg mb-1">{{ editingPackage ? 'Edit Package' : 'Add Package' }}</h3>
        <p class="text-slate-400 text-sm mb-4">{{ selectedTour?.tourName }}</p>
        <div class="space-y-3">
          <div>
            <label class="label">Package Name</label>
            <input v-model="packageForm.packageName" class="input" placeholder="e.g. Economy Package" />
          </div>
          <div class="grid grid-cols-3 gap-3">
            <div>
              <label class="label">Price</label>
              <input v-model.number="packageForm.price" type="number" class="input" />
            </div>
            <div>
              <label class="label">Currency</label>
              <select v-model="packageForm.currency" class="input">
                <option>USD</option><option>EUR</option><option>PKR</option><option>IRR</option><option>GBP</option>
              </select>
            </div>
            <div>
              <label class="label">Duration (days)</label>
              <input v-model.number="packageForm.durationDays" type="number" class="input" />
            </div>
          </div>
          <div class="grid grid-cols-2 gap-3">
            <div>
              <label class="label">Max Pilgrims</label>
              <input v-model.number="packageForm.maxPilgrims" type="number" class="input" />
            </div>
            <div>
              <label class="label">Hotel Info</label>
              <input v-model="packageForm.hotelInfo" class="input" placeholder="Hotel name / area" />
            </div>
          </div>

          <!-- Trip Type -->
          <div class="rounded-lg p-3 space-y-3" style="background:rgba(255,255,255,0.03);border:1px solid rgba(180,83,9,0.2)">
            <p class="text-slate-300 text-sm font-medium">Trip Type</p>

            <!-- Duration toggle -->
            <div>
              <label class="label">Duration</label>
              <div class="flex gap-2">
                <label :class="['flex-1 flex items-center justify-center py-2 rounded-lg border cursor-pointer text-sm font-medium transition-colors',
                  packageForm.tripDuration == 1 ? 'border-sky-500 bg-sky-900/30 text-sky-400' : 'border-slate-700 text-slate-500 hover:border-slate-500']">
                  <input type="radio" v-model.number="packageForm.tripDuration" :value="1" class="hidden" />
                  Short Trip
                </label>
                <label :class="['flex-1 flex items-center justify-center py-2 rounded-lg border cursor-pointer text-sm font-medium transition-colors',
                  packageForm.tripDuration == 2 ? 'border-violet-500 bg-violet-900/30 text-violet-400' : 'border-slate-700 text-slate-500 hover:border-slate-500']">
                  <input type="radio" v-model.number="packageForm.tripDuration" :value="2" class="hidden" />
                  Long Trip
                </label>
              </div>
            </div>

            <!-- Destination checkboxes (dynamic) -->
            <div>
              <label class="label">Destinations <span class="text-slate-600 font-normal">(select all that apply)</span></label>
              <div class="flex flex-wrap gap-2 mt-1">
                <button v-for="d in DESTINATIONS" :key="d" type="button"
                  @click="toggleDest(d)"
                  :class="['px-3 py-1.5 rounded-full border text-sm font-medium transition-colors',
                    selectedDests.includes(d)
                      ? 'border-gold-500 bg-gold-900/30 text-gold-400'
                      : 'border-slate-700 text-slate-500 hover:border-slate-500 hover:text-slate-300']">
                  {{ d }}
                </button>
              </div>
              <p v-if="!selectedDests.length" class="text-slate-600 text-xs mt-1">Select at least one destination</p>
            </div>

            <!-- Route detail -->
            <div>
              <label class="label">Route Detail <span class="text-slate-600 font-normal">(optional — flight path)</span></label>
              <input v-model="packageForm.routeDetail" class="input"
                placeholder="e.g. Islamabad → Mashhad → Najaf → Karbala → Islamabad" />
            </div>
          </div>

          <div>
            <label class="label">Includes</label>
            <div class="flex flex-wrap gap-4 mt-2">
              <label class="flex items-center gap-2 text-slate-300 text-sm cursor-pointer">
                <input type="checkbox" v-model="packageForm.includesAirportTransfer" class="w-4 h-4" />
                Airport Transfer
              </label>
              <label class="flex items-center gap-2 text-slate-300 text-sm cursor-pointer">
                <input type="checkbox" v-model="packageForm.includesBreakfast" class="w-4 h-4" />
                Breakfast
              </label>
              <label class="flex items-center gap-2 text-slate-300 text-sm cursor-pointer">
                <input type="checkbox" v-model="packageForm.includesLunch" class="w-4 h-4" />
                Lunch
              </label>
              <label class="flex items-center gap-2 text-slate-300 text-sm cursor-pointer">
                <input type="checkbox" v-model="packageForm.includesDinner" class="w-4 h-4" />
                Dinner
              </label>
            </div>
          </div>
          <div>
            <label class="label">Description</label>
            <textarea v-model="packageForm.description" class="input" rows="2" />
          </div>
          <div>
            <label class="label">Notes</label>
            <textarea v-model="packageForm.notes" class="input" rows="2" />
          </div>
          <label class="flex items-center gap-2 text-slate-300 text-sm cursor-pointer">
            <input type="checkbox" v-model="packageForm.isActive" class="w-4 h-4" />
            Active (visible to pilgrims)
          </label>
        </div>
        <div class="flex gap-3 mt-6">
          <button @click="savePackage" class="btn-gold flex-1">{{ editingPackage ? 'Update' : 'Add' }}</button>
          <button @click="showPackageModal = false" class="btn-ghost flex-1">Cancel</button>
        </div>
      </div>
    </div>
  </div>
</template>
