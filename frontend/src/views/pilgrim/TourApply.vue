<script setup>
import { ref, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { toursApi } from '@/api'

const router = useRouter()
const tours = ref([])
const loading = ref(false)
const selectedTour = ref(null)
const selectedPackage = ref(null)
const applyStep = ref('list') // list | packages | form | success
const submitting = ref(false)
const tourSchedule = ref([])
const scheduleLoading = ref(false)

const form = ref({
  packageId: '',
  arrivalDate: '',
  departureDate: '',
  country: '',
  passportNumber: '',
  passportExpiry: '',
  visaNumber: '',
  familyMemberCount: 1,
  arrivalFlight: '',
  departureFlight: '',
  arrivalAirport: '',
  departureAirport: '',
  arrivalTime: '',
  departureTime: '',
  emergencyContactName: '',
  emergencyContactPhone: '',
})

const tourTypeLabels = { 1: 'Arbaeen', 2: 'Umrah', 3: 'Ashura', 4: 'Ziyarat', 5: 'Hajj', 6: 'Other' }

async function loadTours() {
  loading.value = true
  try {
    const res = await toursApi.getOpenTours()
    tours.value = res.data.data || []
  } catch { tours.value = [] } finally { loading.value = false }
}

async function selectTour(tour) {
  selectedTour.value = tour
  applyStep.value = 'packages'
  tourSchedule.value = []
  scheduleLoading.value = true
  try {
    const res = await toursApi.getSchedule(tour.id)
    tourSchedule.value = res.data?.data ?? []
  } catch { /* schedule is optional */ } finally {
    scheduleLoading.value = false
  }
}

function selectPackage(pkg) {
  selectedPackage.value = pkg
  form.value.packageId = pkg.id
  applyStep.value = 'form'
}

async function submitApplication() {
  if (!form.value.country || !form.value.arrivalDate || !form.value.departureDate) {
    alert('Please fill in required fields: Country, Arrival Date, Departure Date')
    return
  }
  submitting.value = true
  try {
    await toursApi.apply(selectedTour.value.id, form.value)
    applyStep.value = 'success'
  } catch (e) {
    alert(e.response?.data?.message || 'Application failed. Please try again.')
  } finally { submitting.value = false }
}

function goToHistory() { router.push('/pilgrim/history') }
function goToPortal() { router.push('/pilgrim/portal') }
function goBack() {
  if (applyStep.value === 'packages') applyStep.value = 'list'
  else if (applyStep.value === 'form') applyStep.value = 'packages'
}

onMounted(loadTours)
</script>

<template>
  <div class="max-w-4xl mx-auto px-4 py-6">

    <!-- Success Screen -->
    <div v-if="applyStep === 'success'" class="text-center py-16">
      <div class="text-6xl mb-4">🌟</div>
      <h2 class="text-2xl font-bold text-gray-900 mb-2">Application Submitted!</h2>
      <p class="text-gray-700 mb-2">Your application for <span class="text-amber-700 font-medium">{{ selectedTour?.tourName }}</span> has been received.</p>
      <p class="text-gray-600 text-sm mb-8">The admin team will review your application and notify you of the outcome via WhatsApp and email.</p>
      <div class="flex justify-center gap-4">
        <button @click="goToPortal" class="btn-gold">Go to My Portal</button>
        <button @click="goToHistory" class="btn-ghost">View My History</button>
      </div>
    </div>

    <!-- Tour List -->
    <div v-else-if="applyStep === 'list'">
      <div class="mb-6">
        <h2 class="text-2xl font-bold text-gray-900">Open Tours</h2>
        <p class="text-gray-700 text-sm mt-1">Select a tour to apply for</p>
      </div>

      <div v-if="loading" class="text-center py-12 text-gray-700">Loading available tours...</div>
      <div v-else-if="!tours.length" class="text-center py-12">
        <div class="text-5xl mb-3">🕌</div>
        <p class="text-gray-700">No tours are currently open for applications.</p>
        <p class="text-gray-600 text-sm mt-1">Check back later or contact us for more information.</p>
      </div>
      <div v-else class="grid gap-4">
        <div v-for="tour in tours" :key="tour.id"
          class="card cursor-pointer hover:border-gold-600 transition-all border border-transparent"
          @click="selectTour(tour)">
          <div class="flex items-start justify-between">
            <div>
              <div class="flex items-center gap-2 flex-wrap mb-1">
                <h3 class="text-gray-900 font-bold text-lg">{{ tour.tourName }}</h3>
                <span class="px-2 py-0.5 rounded-full text-xs bg-green-100 text-green-700">Open</span>
                <span class="px-2 py-0.5 rounded-full text-xs bg-amber-100 text-amber-700">
                  {{ tourTypeLabels[tour.tourType] }}
                </span>
              </div>
              <div class="text-gray-700 text-sm space-y-1">
                <p v-if="tour.startDate">
                  {{ tour.startDate?.slice(0,10) }} → {{ tour.endDate?.slice(0,10) }}
                </p>
                <p>{{ tour.packages?.filter(p => p.isActive).length || 0 }} packages available</p>
                <p v-if="tour.description" class="text-gray-700">{{ tour.description }}</p>
              </div>
            </div>
            <svg class="w-6 h-6 text-amber-700 mt-1 flex-shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5l7 7-7 7"/>
            </svg>
          </div>
        </div>
      </div>

      <div class="mt-6 text-center">
        <button @click="goToHistory" class="text-gray-700 hover:text-amber-700 text-sm underline">
          View my application history
        </button>
      </div>
    </div>

    <!-- Package Selection -->
    <div v-else-if="applyStep === 'packages'">
      <button @click="goBack" class="flex items-center gap-2 text-gray-700 hover:text-gray-900 mb-6">
        <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M10 19l-7-7m0 0l7-7m-7 7h18"/>
        </svg>
        Back to Tours
      </button>
      <h2 class="text-xl font-bold text-gray-900 mb-1">{{ selectedTour?.tourName }}</h2>
      <p class="text-gray-700 text-sm mb-4">Select your preferred package</p>

      <!-- 15-Day Itinerary Preview -->
      <div v-if="scheduleLoading" class="mb-6 rounded-xl border border-gray-200 bg-gray-50 p-4 text-center text-gray-700 text-sm animate-pulse">
        Loading itinerary…
      </div>
      <div v-else-if="tourSchedule.length" class="mb-6 rounded-xl border border-gray-200 overflow-hidden bg-white">
        <div class="px-4 py-3 border-b border-gray-200 flex items-center gap-2 bg-gray-50">
          <span class="text-lg">🗺️</span>
          <span class="text-gray-900 font-semibold text-sm">15-Day Journey — {{ selectedTour?.tourName }}</span>
          <span class="ml-auto text-gray-700 text-xs">{{ selectedTour?.startDate?.slice(0,10) }} → {{ selectedTour?.endDate?.slice(0,10) }}</span>
        </div>
        <div class="p-4 grid grid-cols-3 sm:grid-cols-5 gap-2">
          <div v-for="day in tourSchedule" :key="day.dayNumber"
            :class="['rounded-lg p-2 text-center border text-xs',
              day.isKeyDay
                ? 'border-amber-300 bg-amber-50'
                : 'border-gray-200 bg-gray-50']">
            <div class="text-xl">{{ day.icon || '📍' }}</div>
            <div class="text-gray-700 font-bold">Day {{ day.dayNumber }}</div>
            <div class="text-gray-600 font-medium truncate">{{ day.city }}</div>
            <div class="text-gray-600 text-xs">{{ day.date?.slice(5,10) }}</div>
            <div v-if="day.isKeyDay" class="mt-0.5 text-amber-700 text-xs font-bold">★ Key</div>
            <div v-if="day.activityUrdu" class="text-gray-700 text-xs mt-0.5 truncate" dir="rtl">{{ day.activityUrdu }}</div>
          </div>
        </div>
        <div class="px-4 pb-3 flex flex-wrap gap-3 text-xs text-gray-700">
          <span>🕌 Kadhimiyyah (Days 1-2)</span>
          <span>🌹 Najaf (Days 3-5)</span>
          <span>🕯️ Karbala (Days 6-13)</span>
          <span>🛫 Return (Days 14-15)</span>
        </div>
      </div>

      <div class="grid gap-4">
        <div v-for="pkg in selectedTour?.packages?.filter(p => p.isActive)" :key="pkg.id"
          class="card cursor-pointer hover:border-gold-600 transition-all border border-transparent"
          @click="selectPackage(pkg)">
          <div class="flex items-start justify-between gap-4">
            <div class="flex-1">
              <h3 class="text-gray-900 font-bold mb-1">{{ pkg.packageName }}</h3>
              <p class="text-amber-700 font-bold text-xl mb-2">{{ pkg.price.toLocaleString() }} {{ pkg.currency }}</p>
              <div class="text-gray-700 text-sm space-y-1">
                <div class="flex flex-wrap gap-1.5 mb-2">
                  <span :class="['px-2 py-0.5 rounded-full text-xs font-medium',
                    pkg.tripDuration == 1 ? 'bg-sky-100 text-sky-700' : 'bg-violet-100 text-violet-700']">
                    {{ pkg.tripDuration == 1 ? 'Short Trip' : 'Long Trip' }}
                  </span>
                  <span :class="['px-2 py-0.5 rounded-full text-xs font-medium',
                    pkg.tripRoute == 3 ? 'bg-emerald-100 text-emerald-700' :
                    pkg.tripRoute == 2 ? 'bg-rose-100 text-rose-700' : 'bg-amber-100 text-amber-700']">
                    {{ {1:'Iraq Only', 2:'Iran Only', 3:'Iran + Iraq Combined', 4:'Other'}[pkg.tripRoute] || 'Iraq Only' }}
                  </span>
                </div>
                <p v-if="pkg.routeDetail" class="text-gray-700 italic text-xs">{{ pkg.routeDetail }}</p>
                <p>{{ pkg.durationDays }} days · Max {{ pkg.maxPilgrims }} pilgrims</p>
                <p v-if="pkg.hotelInfo">Hotel: {{ pkg.hotelInfo }}</p>
                <div class="flex flex-wrap gap-1.5 mt-2">
                  <template v-if="pkg.destinations">
                    <span v-for="d in pkg.destinations.split(',')" :key="d"
                      class="px-2 py-0.5 rounded-full text-xs bg-amber-100 text-amber-700 font-medium">
                      {{ d.trim() }}
                    </span>
                  </template>
                  <span v-if="pkg.includesAirportTransfer" class="px-2 py-0.5 rounded-full text-xs bg-blue-100 text-blue-700">✈ Airport Transfer</span>
                  <span v-if="pkg.includesBreakfast" class="px-2 py-0.5 rounded-full text-xs bg-amber-100 text-amber-700">🍳 Breakfast</span>
                  <span v-if="pkg.includesLunch" class="px-2 py-0.5 rounded-full text-xs bg-amber-100 text-amber-700">🍱 Lunch</span>
                  <span v-if="pkg.includesDinner" class="px-2 py-0.5 rounded-full text-xs bg-amber-100 text-amber-700">🍽 Dinner</span>
                </div>
                <p v-if="pkg.description" class="text-gray-700 mt-1">{{ pkg.description }}</p>
              </div>
            </div>
            <svg class="w-6 h-6 text-amber-700 mt-1 flex-shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5l7 7-7 7"/>
            </svg>
          </div>
        </div>
      </div>
    </div>

    <!-- Application Form -->
    <div v-else-if="applyStep === 'form'">
      <button @click="goBack" class="flex items-center gap-2 text-gray-700 hover:text-gray-900 mb-6">
        <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M10 19l-7-7m0 0l7-7m-7 7h18"/>
        </svg>
        Back to Packages
      </button>

      <div class="mb-6">
        <h2 class="text-xl font-bold text-gray-900">Application Form</h2>
        <p class="text-gray-700 text-sm">{{ selectedTour?.tourName }} · <span class="text-amber-700">{{ selectedPackage?.packageName }}</span></p>
      </div>

      <div class="space-y-6">
        <!-- Travel Dates -->
        <div class="card">
          <h3 class="text-gray-900 font-medium mb-4">Travel Dates <span class="text-red-400">*</span></h3>
          <div class="grid grid-cols-2 gap-4">
            <div>
              <label class="label">Arrival Date <span class="text-red-400">*</span></label>
              <input v-model="form.arrivalDate" type="date" class="input" required />
            </div>
            <div>
              <label class="label">Departure Date <span class="text-red-400">*</span></label>
              <input v-model="form.departureDate" type="date" class="input" required />
            </div>
          </div>
        </div>

        <!-- Personal Info -->
        <div class="card">
          <h3 class="text-gray-900 font-medium mb-4">Personal & Passport</h3>
          <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div>
              <label class="label">Country <span class="text-red-400">*</span></label>
              <input v-model="form.country" class="input" placeholder="e.g. Pakistan" required />
            </div>
            <div>
              <label class="label">Family Members (including you)</label>
              <input v-model.number="form.familyMemberCount" type="number" min="1" class="input" />
            </div>
            <div>
              <label class="label">Passport Number</label>
              <input v-model="form.passportNumber" class="input" placeholder="e.g. AB1234567" />
            </div>
            <div>
              <label class="label">Passport Expiry</label>
              <input v-model="form.passportExpiry" type="date" class="input" />
            </div>
            <div>
              <label class="label">Visa Number</label>
              <input v-model="form.visaNumber" class="input" placeholder="Optional" />
            </div>
          </div>
        </div>

        <!-- Flight Info -->
        <div class="card">
          <h3 class="text-gray-900 font-medium mb-4">Flight Details (Optional)</h3>
          <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div>
              <label class="label">Arrival Flight</label>
              <input v-model="form.arrivalFlight" class="input" placeholder="e.g. PK301" />
            </div>
            <div>
              <label class="label">Arrival Airport</label>
              <input v-model="form.arrivalAirport" class="input" placeholder="e.g. NJF, Baghdad" />
            </div>
            <div>
              <label class="label">Arrival Time</label>
              <input v-model="form.arrivalTime" type="time" class="input" />
            </div>
            <div></div>
            <div>
              <label class="label">Departure Flight</label>
              <input v-model="form.departureFlight" class="input" placeholder="e.g. PK302" />
            </div>
            <div>
              <label class="label">Departure Airport</label>
              <input v-model="form.departureAirport" class="input" placeholder="e.g. NJF, Najaf" />
            </div>
            <div>
              <label class="label">Departure Time</label>
              <input v-model="form.departureTime" type="time" class="input" />
            </div>
          </div>
        </div>

        <!-- Emergency Contact -->
        <div class="card">
          <h3 class="text-gray-900 font-medium mb-4">Emergency Contact</h3>
          <div class="grid grid-cols-2 gap-4">
            <div>
              <label class="label">Name</label>
              <input v-model="form.emergencyContactName" class="input" placeholder="Full name" />
            </div>
            <div>
              <label class="label">Phone (WhatsApp)</label>
              <input v-model="form.emergencyContactPhone" class="input" placeholder="+92..." />
            </div>
          </div>
        </div>

        <button @click="submitApplication" :disabled="submitting"
          class="btn-gold w-full py-3 text-base font-semibold disabled:opacity-50">
          {{ submitting ? 'Submitting...' : 'Submit Application' }}
        </button>
      </div>
    </div>

  </div>
</template>
