<script setup>
import { ref, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { toursApi } from '@/api'

const route = useRoute()
const router = useRouter()
const tourId = route.query.tourId
const submitting = ref(false)
const submitted = ref(false)

const form = ref({
  overallRating: 0,
  hotelRating: null,
  transportRating: null,
  organizationRating: null,
  foodRating: null,
  comment: '',
  suggestions: '',
  wouldRecommend: false,
  wouldReturn: false,
  preferredPackageNextYear: '',
})

const hoverRatings = ref({
  overall: 0, hotel: 0, transport: 0, organization: 0, food: 0
})

function setRating(field, val) { form.value[field] = val }
function setHover(field, val) { hoverRatings.value[field] = val }
function clearHover(field) { hoverRatings.value[field] = 0 }
function starClass(field, i, ratingKey) {
  const active = hoverRatings.value[ratingKey] || form.value[field]
  return i <= active ? 'text-gold-400' : 'text-slate-700'
}

async function submit() {
  if (!form.value.overallRating) { alert('Please give an overall rating'); return }
  submitting.value = true
  try {
    await toursApi.submitFeedback(tourId, form.value)
    submitted.value = true
  } catch (e) {
    alert(e.response?.data?.message || 'Failed to submit feedback')
  } finally { submitting.value = false }
}

if (!tourId) router.replace('/pilgrim/history')
</script>

<template>
  <div class="max-w-2xl mx-auto px-4 py-6">

    <div v-if="submitted" class="text-center py-16">
      <div class="text-6xl mb-4">🌹</div>
      <h2 class="text-2xl font-bold text-white mb-2">JazakAllah Khair!</h2>
      <p class="text-slate-400 mb-2">Thank you for your valuable feedback.</p>
      <p class="text-slate-500 text-sm mb-8">Your input helps us serve pilgrims better in future tours.</p>
      <div class="flex justify-center gap-4">
        <button @click="router.push('/pilgrim/history')" class="btn-gold">View My History</button>
        <button @click="router.push('/pilgrim/portal')" class="btn-ghost">Back to Portal</button>
      </div>
    </div>

    <div v-else>
      <div class="mb-6">
        <button @click="router.push('/pilgrim/history')" class="flex items-center gap-2 text-slate-400 hover:text-white mb-4">
          <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M10 19l-7-7m0 0l7-7m-7 7h18"/>
          </svg>
          Back
        </button>
        <h2 class="text-2xl font-bold text-white">Tour Feedback</h2>
        <p class="text-slate-400 text-sm mt-1">Your honest feedback helps us improve</p>
      </div>

      <div class="space-y-5">

        <!-- Overall Rating (required) -->
        <div class="card">
          <h3 class="text-white font-medium mb-3">Overall Experience <span class="text-red-400">*</span></h3>
          <div class="flex gap-2">
            <button v-for="i in 5" :key="i"
              @click="setRating('overallRating', i)"
              @mouseenter="setHover('overall', i)" @mouseleave="clearHover('overall')"
              :class="['text-3xl transition-colors', starClass('overallRating', i, 'overall')]">
              ★
            </button>
          </div>
          <p class="text-slate-500 text-xs mt-2">
            {{ ['', 'Very Poor', 'Poor', 'Average', 'Good', 'Excellent'][form.overallRating] || 'Click to rate' }}
          </p>
        </div>

        <!-- Sub-ratings -->
        <div class="card">
          <h3 class="text-white font-medium mb-4">Detailed Ratings <span class="text-slate-500 text-xs font-normal">(optional)</span></h3>
          <div class="space-y-4">
            <div v-for="({label, field, hover}) in [
              {label: 'Hotel & Accommodation', field: 'hotelRating', hover: 'hotel'},
              {label: 'Transportation', field: 'transportRating', hover: 'transport'},
              {label: 'Organization & Management', field: 'organizationRating', hover: 'organization'},
              {label: 'Food Quality', field: 'foodRating', hover: 'food'},
            ]" :key="field">
              <div class="flex items-center justify-between">
                <span class="text-slate-300 text-sm">{{ label }}</span>
                <div class="flex gap-1.5">
                  <button v-for="i in 5" :key="i"
                    @click="setRating(field, i)"
                    @mouseenter="setHover(hover, i)" @mouseleave="clearHover(hover)"
                    :class="['text-xl transition-colors', starClass(field, i, hover)]">
                    ★
                  </button>
                </div>
              </div>
            </div>
          </div>
        </div>

        <!-- Comment -->
        <div class="card">
          <h3 class="text-white font-medium mb-3">Your Comments</h3>
          <div class="space-y-3">
            <div>
              <label class="label">What did you enjoy most?</label>
              <textarea v-model="form.comment" class="input" rows="3"
                placeholder="Share what you liked about the tour..." />
            </div>
            <div>
              <label class="label">Suggestions for improvement</label>
              <textarea v-model="form.suggestions" class="input" rows="3"
                placeholder="How could we make the next tour better?" />
            </div>
          </div>
        </div>

        <!-- Recommend & Return -->
        <div class="card">
          <h3 class="text-white font-medium mb-4">Would you...</h3>
          <div class="space-y-3">
            <label class="flex items-center gap-3 cursor-pointer">
              <input type="checkbox" v-model="form.wouldRecommend" class="w-5 h-5 rounded" />
              <span class="text-slate-300">Recommend this tour to family & friends?</span>
            </label>
            <label class="flex items-center gap-3 cursor-pointer">
              <input type="checkbox" v-model="form.wouldReturn" class="w-5 h-5 rounded" />
              <span class="text-slate-300">Join us again for a future tour?</span>
            </label>
          </div>
        </div>

        <!-- Next year preference -->
        <div class="card">
          <h3 class="text-white font-medium mb-2">Next Year</h3>
          <label class="label">Which package would you prefer next year?</label>
          <input v-model="form.preferredPackageNextYear" class="input"
            placeholder="e.g. VIP Package, Economy, Premium..." />
        </div>

        <button @click="submit" :disabled="submitting || !form.overallRating"
          class="btn-gold w-full py-3 text-base font-semibold disabled:opacity-50">
          {{ submitting ? 'Submitting...' : 'Submit Feedback' }}
        </button>
      </div>
    </div>
  </div>
</template>
