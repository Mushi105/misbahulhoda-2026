<script setup>
import { ref, onMounted, computed } from 'vue'
import { documentsApi } from '@/api'

const docs    = ref([])
const loading = ref(true)
const error   = ref('')
const search  = ref('')
const activeCategory = ref('All')

const categoryIcon = {
  'Tour Guide': '🗺️',
  'Manual':     '📖',
  'Notice':     '📢',
  'Map':        '🗾',
  'Schedule':   '📅',
  'General':    '📄',
}

async function load() {
  try {
    const res = await documentsApi.getAll()
    docs.value = res.data.data || []
  } catch {
    error.value = 'Failed to load documents.'
  } finally { loading.value = false }
}

const categories = computed(() => ['All', ...new Set(docs.value.map(d => d.category))])

const filtered = computed(() => {
  let list = docs.value
  if (activeCategory.value !== 'All') list = list.filter(d => d.category === activeCategory.value)
  if (search.value.trim()) {
    const q = search.value.toLowerCase()
    list = list.filter(d => d.title.toLowerCase().includes(q) || d.description?.toLowerCase().includes(q))
  }
  return list
})

async function download(doc) {
  try {
    const res = await documentsApi.download(doc.id)
    const url = URL.createObjectURL(res.data)
    const a = document.createElement('a')
    a.href = url
    a.download = doc.fileName
    a.click()
    URL.revokeObjectURL(url)
  } catch {
    error.value = 'Download failed. Please try again.'
  }
}

async function viewPdf(doc) {
  try {
    const res = await documentsApi.download(doc.id)
    const url = URL.createObjectURL(res.data)
    window.open(url, '_blank')
  } catch {
    error.value = 'Could not open document.'
  }
}

onMounted(load)
</script>

<template>
  <div class="space-y-6">

    <!-- Header -->
    <div>
      <h1 class="text-2xl font-bold text-gray-900">Documents & Guides</h1>
      <p class="text-gray-700 text-sm mt-1">Tour guides, manuals and notices from Arbaeen 2026 team</p>
    </div>

    <div v-if="error" class="bg-red-100 border border-red-300 text-red-600 rounded-lg px-4 py-3 text-sm">
      {{ error }}
    </div>

    <div v-if="loading" class="text-center py-16 text-gray-700">Loading documents...</div>

    <template v-else-if="!docs.length">
      <div class="card text-center py-16">
        <div class="text-5xl mb-4">📂</div>
        <p class="text-gray-900 font-semibold">No documents available yet</p>
        <p class="text-gray-700 text-sm mt-2">Tour guides and manuals will appear here once uploaded by the team.</p>
      </div>
    </template>

    <template v-else>
      <!-- Search + Category filter -->
      <div class="space-y-3">
        <input v-model="search" type="text" class="input w-full sm:w-80"
               placeholder="Search documents..." />
        <div class="flex flex-wrap gap-2">
          <button v-for="cat in categories" :key="cat"
                  @click="activeCategory = cat"
                  :class="['px-3 py-1.5 rounded-lg text-xs font-semibold border transition-all',
                    activeCategory === cat
                      ? 'bg-amber-600 text-white border-amber-500'
                      : 'bg-white text-gray-700 border-gray-300 hover:border-amber-500 hover:text-gray-900']">
            {{ cat === 'All' ? '📋 All' : `${categoryIcon[cat] || '📄'} ${cat}` }}
          </button>
        </div>
      </div>

      <!-- Document list -->
      <div v-if="!filtered.length" class="card text-center py-10">
        <p class="text-gray-600">No documents found for this filter.</p>
      </div>

      <div v-else class="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
        <div v-for="doc in filtered" :key="doc.id"
             class="card border border-gray-200 hover:border-amber-400 transition-all">

          <!-- Icon + Info -->
          <div class="flex items-start gap-3">
            <div class="w-14 h-14 rounded-xl flex items-center justify-center text-3xl flex-shrink-0"
                 :style="doc.contentType === 'application/pdf'
                   ? 'background:rgba(239,68,68,0.12);border:1px solid rgba(239,68,68,0.25)'
                   : 'background:rgba(59,130,246,0.12);border:1px solid rgba(59,130,246,0.25)'">
              {{ doc.contentType === 'application/pdf' ? '📕' : '🖼️' }}
            </div>
            <div class="flex-1 min-w-0">
              <span class="text-xs px-2 py-0.5 rounded-full bg-amber-100 text-amber-700 border border-amber-300 font-medium">
                {{ categoryIcon[doc.category] || '📄' }} {{ doc.category }}
              </span>
              <p class="text-gray-900 font-semibold text-sm mt-1.5 leading-snug">{{ doc.title }}</p>
              <p v-if="doc.description" class="text-gray-600 text-xs mt-0.5 line-clamp-2">{{ doc.description }}</p>
              <div class="flex items-center gap-2 mt-2 text-xs text-gray-600">
                <span>{{ doc.sizeLabel }}</span>
                <span>·</span>
                <span>{{ new Date(doc.createdAt).toLocaleDateString('en-GB', { day:'2-digit', month:'short', year:'numeric' }) }}</span>
              </div>
            </div>
          </div>

          <!-- Action buttons -->
          <div class="flex gap-2 mt-4">
            <button @click="viewPdf(doc)"
                    class="flex-1 flex items-center justify-center gap-1.5 py-2.5 rounded-xl text-sm font-semibold border border-blue-300 text-blue-600 hover:bg-blue-50 transition-colors">
              <span>👁</span> View
            </button>
            <button @click="download(doc)"
                    class="flex-1 flex items-center justify-center gap-1.5 py-2.5 rounded-xl text-sm font-semibold text-gray-900 transition-colors"
                    style="background: rgba(180,83,9,0.6); border: 1px solid rgba(180,83,9,0.4);">
              <span>⬇</span> Download
            </button>
          </div>
        </div>
      </div>
    </template>

  </div>
</template>
