<script setup>
import { ref, onMounted } from 'vue'
import { majalisApi } from '@/api'

const majalis = ref([])
const namazTimings = ref([])
const foodSchedule = ref([])
const loading = ref(true)
const activeTab = ref('majalis')
const programs = ref({}) // keyed by majalisId

const PROGRAM_TYPE_ICONS = {
  Opening: '📿', Hadith: '📖', Marsiya: '🎙️',
  Salam: '🕊️', Noha: '🎵', Closing: '🕌', Break: '☕'
}
const PROGRAM_TYPE_COLORS = {
  Opening:  'text-purple-400',
  Hadith:   'text-amber-400',
  Marsiya:  'text-pink-400',
  Salam:    'text-blue-400',
  Noha:     'text-emerald-400',
  Closing:  'text-purple-300',
  Break:    'text-slate-400',
}

async function loadPrograms(majalisList) {
  const results = await Promise.allSettled(
    majalisList.map(m => majalisApi.getProgram(m.id))
  )
  majalisList.forEach((m, i) => {
    if (results[i].status === 'fulfilled')
      programs.value[m.id] = results[i].value.data.data || []
  })
}

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
    if (majalis.value.length) await loadPrograms(majalis.value)
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
    <div v-else-if="activeTab === 'majalis'" class="space-y-4">
      <div v-if="!majalis.length" class="text-slate-400 text-center py-8">No Majalis schedule found.</div>
      <div v-for="m in majalis" :key="m.id"
           class="rounded-xl overflow-hidden border border-dark-700"
           style="background:rgba(15,7,36,0.8);">

        <!-- Scholar Image -->
        <div v-if="m.imageUrl" class="relative">
          <img :src="m.imageUrl" :alt="m.molanaName || m.title"
               class="w-full object-cover"
               style="max-height:240px; object-fit:cover; object-position:top;" />
          <div class="absolute inset-0"
               style="background:linear-gradient(to bottom, transparent 40%, rgba(15,7,36,0.95) 100%)"></div>
          <!-- Time badge over image -->
          <div class="absolute top-3 right-3 rounded-lg px-3 py-1.5 text-xs font-bold"
               style="background:rgba(124,58,237,0.85);color:#e9d5ff;backdrop-filter:blur(4px);">
            {{ new Date(m.startTime).toLocaleTimeString([], {hour:'2-digit', minute:'2-digit'}) }}
            — {{ new Date(m.endTime).toLocaleTimeString([], {hour:'2-digit', minute:'2-digit'}) }}
          </div>
          <div class="absolute top-3 left-3">
            <span class="badge-gold text-xs px-2 py-1">{{ m.language }}</span>
          </div>
        </div>

        <div class="p-4">
          <!-- No image fallback header -->
          <div v-if="!m.imageUrl" class="flex items-center justify-between mb-2">
            <span class="badge-gold text-xs">{{ m.language }}</span>
            <span class="text-purple-300 text-sm font-semibold">
              {{ new Date(m.startTime).toLocaleTimeString([], {hour:'2-digit', minute:'2-digit'}) }}
              — {{ new Date(m.endTime).toLocaleTimeString([], {hour:'2-digit', minute:'2-digit'}) }}
            </span>
          </div>

          <p class="text-white font-bold text-lg leading-tight">{{ m.title }}</p>

          <!-- Scholar & Noha Khuwan -->
          <div class="mt-2 space-y-1">
            <div v-if="m.molanaName" class="flex items-center gap-2">
              <span class="text-purple-400 text-lg">🎓</span>
              <div>
                <p class="text-xs text-slate-400">Molana / Scholar</p>
                <p class="text-gold-300 font-semibold text-sm">{{ m.molanaName }}</p>
              </div>
            </div>
            <div v-if="m.nohaKhuwanName" class="flex items-center gap-2 mt-1">
              <span class="text-blue-400 text-lg">🎵</span>
              <div>
                <p class="text-xs text-slate-400">Noha Khuwaan</p>
                <p class="text-blue-300 font-semibold text-sm">{{ m.nohaKhuwanName }}</p>
              </div>
            </div>
          </div>

          <!-- Venue -->
          <div class="mt-3 flex items-center gap-2 text-sm text-slate-400">
            <span>📍</span>
            <span>{{ m.venue }}</span>
          </div>

          <!-- Description -->
          <p v-if="m.description" class="mt-2 text-slate-400 text-sm border-t border-dark-700 pt-2">
            {{ m.description }}
          </p>

          <!-- Program Timeline -->
          <div v-if="programs[m.id]?.length" class="mt-3 border-t border-purple-900/30 pt-3 space-y-2">
            <p class="text-purple-400 text-xs font-bold uppercase tracking-wider mb-3">📋 Today's Program</p>

            <div v-for="(item, idx) in programs[m.id]" :key="idx"
                 class="flex items-center gap-3">
              <!-- Time & connector line -->
              <div class="flex flex-col items-center shrink-0" style="width:48px;">
                <p class="text-white text-xs font-bold">{{ item.time || '—' }}</p>
                <div v-if="idx < programs[m.id].length - 1"
                     class="w-px flex-1 mt-1" style="min-height:20px; background:rgba(124,58,237,0.3);"></div>
              </div>

              <!-- Performer photo (if any) -->
              <div class="shrink-0">
                <img v-if="item.imageUrl" :src="item.imageUrl" :alt="item.performerName"
                     class="w-12 h-12 rounded-xl object-cover object-top border border-purple-900/40" />
                <div v-else
                     class="w-12 h-12 rounded-xl flex items-center justify-center text-xl"
                     style="background:rgba(124,58,237,0.1); border:1px solid rgba(124,58,237,0.2);">
                  {{ PROGRAM_TYPE_ICONS[item.type] || '📿' }}
                </div>
              </div>

              <!-- Info -->
              <div class="flex-1 min-w-0">
                <div class="flex items-center gap-2">
                  <span :class="['text-xs font-bold', PROGRAM_TYPE_COLORS[item.type] || 'text-purple-400']">
                    {{ PROGRAM_TYPE_ICONS[item.type] }} {{ item.type }}
                  </span>
                </div>
                <p v-if="item.performerName" class="text-white text-sm font-semibold leading-tight">
                  {{ item.performerName }}
                </p>
                <p v-if="item.description" class="text-slate-500 text-xs mt-0.5">{{ item.description }}</p>
              </div>
            </div>
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
