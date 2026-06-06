<script setup>
import { ref, onMounted } from 'vue'
import { majalisApi } from '@/api'

const majalis = ref([])
const namazTimings = ref([])
const foodSchedule = ref([])
const loading = ref(true)
const activeTab = ref('majalis')

async function load() {
  loading.value = true
  const today = new Date().toISOString().split('T')[0]
  try {
    const [majRes, namazRes, foodRes] = await Promise.all([
      majalisApi.getAll(),
      majalisApi.getNamazTimings(today),
      majalisApi.getFoodSchedule(today),
    ])
    majalis.value = majRes.data.data || []
    namazTimings.value = namazRes.data.data || []
    foodSchedule.value = foodRes.data.data || []
  } finally {
    loading.value = false
  }
}

onMounted(load)
</script>

<template>
  <div class="space-y-6">
    <h1 class="text-2xl font-bold text-white">📿 Majalis & Schedule</h1>

    <!-- Tabs -->
    <div class="flex gap-2 border-b border-dark-700 pb-0">
      <button v-for="tab in ['majalis', 'namaz', 'food']" :key="tab"
        @click="activeTab = tab"
        :class="['px-4 py-2 text-sm font-medium rounded-t-lg transition-colors capitalize', activeTab === tab ? 'bg-dark-800 text-primary-400 border border-b-0 border-dark-700' : 'text-slate-400 hover:text-white']">
        {{ tab === 'namaz' ? '🕌 Namaz Timings' : tab === 'food' ? '🍽️ Food Schedule' : '📿 Majalis' }}
      </button>
    </div>

    <div v-if="loading" class="text-slate-400 text-center py-8">Loading...</div>

    <!-- Majalis -->
    <div v-else-if="activeTab === 'majalis'" class="space-y-3">
      <div v-if="!majalis.length" class="text-slate-400 text-center py-8">No majalis scheduled.</div>
      <div v-for="m in majalis" :key="m.id" class="card border-l-4 border-l-primary-600">
        <div class="flex items-start justify-between">
          <div>
            <p class="text-white font-semibold">{{ m.title }}</p>
            <p class="text-slate-400 text-sm mt-1">{{ m.venue }}</p>
            <div class="flex gap-4 mt-2 text-xs text-slate-400">
              <span v-if="m.molanaName">🎓 {{ m.molanaName }}</span>
              <span v-if="m.nohaKhuwanName">🎵 {{ m.nohaKhuwanName }}</span>
            </div>
          </div>
          <div class="text-right">
            <span class="badge-gold text-xs">{{ m.language }}</span>
            <p class="text-slate-400 text-xs mt-2">{{ new Date(m.startTime).toLocaleTimeString([], {hour:'2-digit', minute:'2-digit'}) }}</p>
            <p class="text-slate-500 text-xs">— {{ new Date(m.endTime).toLocaleTimeString([], {hour:'2-digit', minute:'2-digit'}) }}</p>
          </div>
        </div>
      </div>
    </div>

    <!-- Namaz Timings -->
    <div v-else-if="activeTab === 'namaz'" class="space-y-2">
      <div v-if="!namazTimings.length" class="text-slate-400 text-center py-8">No timings for today.</div>
      <div v-for="t in namazTimings" :key="t.id" class="card flex items-center justify-between py-3">
        <div class="flex items-center gap-3">
          <span class="text-2xl">🕌</span>
          <p class="text-white font-medium">{{ t.prayerName }}</p>
        </div>
        <div class="text-right">
          <p class="text-gold-400 font-semibold">{{ t.time }}</p>
          <p class="text-slate-500 text-xs">{{ t.venue || 'Main Masjid' }}</p>
        </div>
      </div>
    </div>

    <!-- Food Schedule -->
    <div v-else-if="activeTab === 'food'" class="space-y-3">
      <div v-if="!foodSchedule.length" class="text-slate-400 text-center py-8">No food schedule for today.</div>
      <div v-for="f in foodSchedule" :key="f.id" class="card flex items-center justify-between">
        <div class="flex items-center gap-3">
          <span class="text-2xl">🍽️</span>
          <div>
            <p class="text-white font-medium">{{ f.mealType }}</p>
            <p class="text-slate-400 text-xs">{{ f.location }}</p>
            <p v-if="f.description" class="text-slate-500 text-xs">{{ f.description }}</p>
          </div>
        </div>
        <div class="text-right">
          <p class="text-primary-400 font-semibold">{{ new Date(f.servedAt).toLocaleTimeString([], {hour:'2-digit', minute:'2-digit'}) }}</p>
          <p v-if="f.estimatedServings" class="text-slate-500 text-xs">~{{ f.estimatedServings }} servings</p>
        </div>
      </div>
    </div>

  </div>
</template>
