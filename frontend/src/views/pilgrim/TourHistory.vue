<script setup>
import { ref, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { toursApi } from '@/api'

const router = useRouter()
const history = ref([])
const loading = ref(false)

const statusLabels = {
  0: 'Pending', 1: 'Pending', 2: 'Approved', 3: 'Rejected',
  Pending: 'Pending', Approved: 'Approved', Rejected: 'Rejected'
}
const statusColors = {
  0: 'text-amber-700 bg-amber-100',
  1: 'text-amber-700 bg-amber-100',
  2: 'text-green-700 bg-green-100',
  3: 'text-red-600 bg-red-100',
  Pending: 'text-amber-700 bg-amber-100',
  Approved: 'text-green-700 bg-green-100',
  Rejected: 'text-red-600 bg-red-100',
}
const tourTypeLabels = { 1: 'Arbaeen', 2: 'Umrah', 3: 'Ashura', 4: 'Ziyarat', 5: 'Other' }

async function load() {
  loading.value = true
  try {
    const res = await toursApi.getMyHistory()
    history.value = res.data.data || []
  } catch { history.value = [] } finally { loading.value = false }
}

function submitFeedback(tourId) {
  router.push(`/pilgrim/feedback?tourId=${tourId}`)
}

function applyAgain() {
  router.push('/pilgrim/tours')
}

onMounted(load)
</script>

<template>
  <div class="max-w-3xl mx-auto px-4 py-6">
    <div class="flex items-center justify-between mb-6">
      <div>
        <h2 class="text-2xl font-bold text-gray-900">My Tour History</h2>
        <p class="text-gray-700 text-sm mt-1">All your applications across years</p>
      </div>
      <button @click="applyAgain" class="btn-gold text-sm">Apply for New Tour</button>
    </div>

    <div v-if="loading" class="text-center py-12 text-gray-700">Loading history...</div>

    <div v-else-if="!history.length" class="text-center py-16">
      <div class="text-5xl mb-4">🕌</div>
      <p class="text-gray-700 mb-2">No applications yet</p>
      <p class="text-gray-600 text-sm mb-6">Apply for an available tour to start your journey</p>
      <button @click="applyAgain" class="btn-gold">Browse Tours</button>
    </div>

    <div v-else class="space-y-4">
      <div v-for="app in history" :key="app.id"
        class="card border border-transparent hover:border-gold-800/40 transition-colors">
        <div class="flex flex-wrap items-start justify-between gap-3 mb-3">
          <div>
            <div class="flex flex-wrap items-center gap-2 mb-1">
              <h3 class="text-gray-900 font-bold">{{ app.tour?.tourName || 'Tour Application' }}</h3>
              <span class="px-2 py-0.5 rounded-full text-xs bg-amber-100 text-amber-700">
                {{ tourTypeLabels[app.tour?.tourType] || 'Tour' }} · {{ app.tour?.year || '' }}
              </span>
              <span :class="['px-2.5 py-0.5 rounded-full text-xs font-medium', statusColors[app.status] || 'text-gray-700 bg-gray-100']">
                {{ statusLabels[app.status] || app.status }}
              </span>
            </div>
            <p v-if="app.package?.packageName" class="text-amber-700 text-sm">{{ app.package.packageName }}</p>
          </div>
          <div v-if="app.status === 'Approved'" class="text-xs text-green-700 mt-1">✓ Approved</div>
        </div>

        <div class="grid grid-cols-2 md:grid-cols-4 gap-3 text-sm text-gray-700 mb-3">
          <div v-if="app.arrivalDate">
            <p class="text-xs text-gray-600 mb-0.5">Arrival</p>
            <p class="text-gray-700">{{ app.arrivalDate?.slice(0,10) }}</p>
          </div>
          <div v-if="app.departureDate">
            <p class="text-xs text-gray-600 mb-0.5">Departure</p>
            <p class="text-gray-700">{{ app.departureDate?.slice(0,10) }}</p>
          </div>
          <div v-if="app.country">
            <p class="text-xs text-gray-600 mb-0.5">Country</p>
            <p class="text-gray-700">{{ app.country }}</p>
          </div>
          <div v-if="app.familyMemberCount">
            <p class="text-xs text-gray-600 mb-0.5">Family</p>
            <p class="text-gray-700">{{ app.familyMemberCount }} member{{ app.familyMemberCount > 1 ? 's' : '' }}</p>
          </div>
        </div>

        <div v-if="app.rejectionReason" class="mb-3 p-2 rounded bg-red-100 border border-red-300">
          <p class="text-red-600 text-xs"><span class="font-medium">Rejection reason:</span> {{ app.rejectionReason }}</p>
        </div>

        <!-- Package details -->
        <div v-if="app.package" class="flex flex-wrap gap-2 text-xs mt-2">
          <span class="px-2 py-0.5 rounded bg-amber-100 text-amber-700">
            {{ app.package.price?.toLocaleString() }} {{ app.package.currency }}
          </span>
          <span v-if="app.package.hotelInfo" class="px-2 py-0.5 rounded bg-gray-100 text-gray-700">
            {{ app.package.hotelInfo }}
          </span>
        </div>

        <!-- Feedback CTA (pending) -->
        <div v-if="app.feedbackPending" class="mt-4 p-3 rounded-lg bg-purple-100 border border-purple-300">
          <p class="text-purple-700 text-sm font-medium mb-2">We'd love your feedback on this tour!</p>
          <button @click="submitFeedback(app.tour?.id)" class="btn-gold text-xs py-1.5 px-4">
            Submit Feedback
          </button>
        </div>

        <!-- Feedback display (submitted) -->
        <div v-if="app.feedback" class="mt-4 rounded-lg border border-gray-200 bg-gray-50 p-4">
          <div class="flex items-center justify-between mb-3">
            <p class="text-gray-700 text-sm font-medium">Your Feedback</p>
            <p class="text-gray-600 text-xs">{{ app.feedback.submittedAt?.slice(0,10) }}</p>
          </div>

          <!-- Stars -->
          <div class="flex items-center gap-1 mb-3">
            <span v-for="i in 5" :key="i"
              :class="['text-xl', i <= app.feedback.overallRating ? 'text-amber-500' : 'text-gray-300']">★</span>
            <span class="text-gray-700 text-sm ml-2">{{ app.feedback.overallRating }}/5</span>
            <span class="text-gray-600 text-xs ml-1">overall</span>
          </div>

          <!-- Sub-ratings row -->
          <div v-if="app.feedback.hotelRating || app.feedback.transportRating || app.feedback.organizationRating || app.feedback.foodRating"
            class="flex flex-wrap gap-3 mb-3 text-xs text-gray-600">
            <span v-if="app.feedback.hotelRating">Hotel: <span class="text-amber-700">{{ app.feedback.hotelRating }}/5</span></span>
            <span v-if="app.feedback.transportRating">Transport: <span class="text-amber-700">{{ app.feedback.transportRating }}/5</span></span>
            <span v-if="app.feedback.organizationRating">Organization: <span class="text-amber-700">{{ app.feedback.organizationRating }}/5</span></span>
            <span v-if="app.feedback.foodRating">Food: <span class="text-amber-700">{{ app.feedback.foodRating }}/5</span></span>
          </div>

          <!-- Comment -->
          <p v-if="app.feedback.comment" class="text-gray-600 text-sm italic mb-2">"{{ app.feedback.comment }}"</p>
          <p v-if="app.feedback.suggestions" class="text-gray-600 text-xs mb-2">Suggestion: {{ app.feedback.suggestions }}</p>

          <!-- Recommend / Return badges -->
          <div class="flex flex-wrap gap-2 mt-2">
            <span v-if="app.feedback.wouldRecommend"
              class="px-2 py-0.5 rounded-full text-xs bg-green-100 text-green-700">✓ Would recommend</span>
            <span v-if="app.feedback.wouldReturn"
              class="px-2 py-0.5 rounded-full text-xs bg-blue-100 text-blue-700">✓ Would return</span>
            <span v-if="app.feedback.preferredPackageNextYear"
              class="px-2 py-0.5 rounded-full text-xs bg-amber-100 text-amber-700">
              Next year: {{ app.feedback.preferredPackageNextYear }}
            </span>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>
