<script setup>
import { ref, onMounted, computed } from 'vue'
import { documentsApi } from '@/api'

const docs     = ref([])
const loading  = ref(true)
const msg      = ref({ type: '', text: '' })
const deleting = ref(null)

// Upload form
const showUpload   = ref(false)
const uploading    = ref(false)
const uploadFile   = ref(null)
const fileInput    = ref(null)
const uploadForm   = ref({ title: '', description: '', category: 'Tour Guide' })
const dragOver     = ref(false)

const categories = ['Tour Guide', 'Manual', 'Notice', 'Map', 'Schedule', 'General']

const categoryIcon = {
  'Tour Guide': '🗺️',
  'Manual':     '📖',
  'Notice':     '📢',
  'Map':        '🗾',
  'Schedule':   '📅',
  'General':    '📄',
}

function notify(type, text) {
  msg.value = { type, text }
  setTimeout(() => msg.value = { type: '', text: '' }, 5000)
}

async function load() {
  loading.value = true
  try {
    const res = await documentsApi.getAll()
    docs.value = res.data.data || []
  } finally { loading.value = false }
}

function onFileChange(e) {
  const f = e.target.files?.[0]
  if (f) {
    uploadFile.value = f
    if (!uploadForm.value.title) uploadForm.value.title = f.name.replace(/\.[^.]+$/, '')
  }
}

function onDrop(e) {
  dragOver.value = false
  const f = e.dataTransfer.files?.[0]
  if (f) {
    uploadFile.value = f
    if (!uploadForm.value.title) uploadForm.value.title = f.name.replace(/\.[^.]+$/, '')
  }
}

function openUpload() {
  uploadForm.value = { title: '', description: '', category: 'Tour Guide' }
  uploadFile.value = null
  showUpload.value = true
}

async function submitUpload() {
  if (!uploadFile.value) return notify('error', 'Please select a file.')
  if (!uploadForm.value.title.trim()) return notify('error', 'Title is required.')

  const fd = new FormData()
  fd.append('file', uploadFile.value)
  fd.append('title', uploadForm.value.title)
  fd.append('description', uploadForm.value.description || '')
  fd.append('category', uploadForm.value.category)

  uploading.value = true
  try {
    await documentsApi.upload(fd)
    notify('success', `"${uploadForm.value.title}" uploaded successfully.`)
    showUpload.value = false
    await load()
  } catch (e) {
    notify('error', e.response?.data?.message || 'Upload failed.')
  } finally { uploading.value = false }
}

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
    notify('error', 'Download failed.')
  }
}

async function viewInBrowser(doc) {
  try {
    const res = await documentsApi.download(doc.id)
    const url = URL.createObjectURL(res.data)
    window.open(url, '_blank')
  } catch {
    notify('error', 'Could not open document.')
  }
}

async function deleteDoc(doc) {
  if (!confirm(`Delete "${doc.title}"?`)) return
  deleting.value = doc.id
  try {
    await documentsApi.delete(doc.id)
    notify('success', `"${doc.title}" deleted.`)
    docs.value = docs.value.filter(d => d.id !== doc.id)
  } catch (e) {
    notify('error', e.response?.data?.message || 'Delete failed.')
  } finally { deleting.value = null }
}

const fileSizeLabel = (f) => {
  if (!f) return ''
  return f.size > 1_048_576 ? `${(f.size / 1_048_576).toFixed(1)} MB` : `${(f.size / 1024).toFixed(0)} KB`
}

const grouped = computed(() => {
  const map = {}
  docs.value.forEach(d => {
    const cat = d.category || 'General'
    if (!map[cat]) map[cat] = []
    map[cat].push(d)
  })
  return map
})

onMounted(load)
</script>

<template>
  <div class="space-y-6">

    <!-- Header -->
    <div class="flex items-center justify-between">
      <div>
        <h1 class="text-2xl font-bold text-white">Documents</h1>
        <p class="text-slate-400 text-sm mt-0.5">Upload tour guides, manuals and notices for pilgrims</p>
      </div>
      <button @click="openUpload"
              class="flex items-center gap-2 px-4 py-2.5 rounded-xl text-sm font-semibold text-white"
              style="background: rgba(180,83,9,0.7); border: 1px solid rgba(180,83,9,0.5);">
        + Upload Document
      </button>
    </div>

    <!-- Alert -->
    <div v-if="msg.text"
         :class="msg.type === 'success' ? 'bg-emerald-900/50 border-emerald-700 text-emerald-300' : 'bg-red-900/50 border-red-700 text-red-300'"
         class="border rounded-lg px-4 py-3 text-sm flex justify-between">
      {{ msg.text }}
      <button @click="msg.text = ''" class="opacity-60 hover:opacity-100">✕</button>
    </div>

    <!-- Loading -->
    <div v-if="loading" class="card text-center py-12 text-slate-400">Loading documents...</div>

    <!-- Empty -->
    <div v-else-if="!docs.length" class="card text-center py-16">
      <div class="text-5xl mb-4">📂</div>
      <p class="text-white font-bold text-lg">No documents uploaded yet</p>
      <p class="text-slate-400 text-sm mt-2 mb-6">Upload tour guides, manuals, and notices for pilgrims.</p>
      <button @click="openUpload"
              class="px-6 py-2.5 rounded-xl text-sm font-semibold text-white"
              style="background: rgba(180,83,9,0.7);">
        + Upload First Document
      </button>
    </div>

    <!-- Grouped by category -->
    <template v-else>
      <div v-for="(catDocs, cat) in grouped" :key="cat" class="space-y-3">
        <div class="flex items-center gap-3">
          <div class="h-px flex-1 bg-dark-700"></div>
          <span class="text-xs font-semibold text-gold-500 uppercase tracking-wider">
            {{ categoryIcon[cat] || '📄' }} {{ cat }} ({{ catDocs.length }})
          </span>
          <div class="h-px flex-1 bg-dark-700"></div>
        </div>

        <div class="grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
          <div v-for="doc in catDocs" :key="doc.id"
               class="card border border-dark-600 hover:border-gold-800/50 transition-all">
            <!-- File type icon + title -->
            <div class="flex items-start gap-3">
              <div class="w-12 h-12 rounded-xl flex items-center justify-center text-2xl flex-shrink-0"
                   :style="doc.contentType === 'application/pdf' ? 'background:rgba(239,68,68,0.15);border:1px solid rgba(239,68,68,0.3)' : 'background:rgba(59,130,246,0.15);border:1px solid rgba(59,130,246,0.3)'">
                {{ doc.contentType === 'application/pdf' ? '📕' : '🖼️' }}
              </div>
              <div class="flex-1 min-w-0">
                <p class="text-white font-semibold text-sm leading-tight">{{ doc.title }}</p>
                <p v-if="doc.description" class="text-slate-400 text-xs mt-0.5 line-clamp-2">{{ doc.description }}</p>
                <div class="flex items-center gap-2 mt-1.5 flex-wrap">
                  <span class="text-xs text-slate-500">{{ doc.sizeLabel }}</span>
                  <span class="text-slate-600">·</span>
                  <span class="text-xs text-slate-500">{{ new Date(doc.createdAt).toLocaleDateString('en-GB', { day:'2-digit', month:'short', year:'numeric' }) }}</span>
                  <span class="text-slate-600">·</span>
                  <span class="text-xs text-slate-500">{{ doc.uploadedBy }}</span>
                </div>
              </div>
            </div>

            <!-- Actions -->
            <div class="flex items-center gap-2 mt-3 pt-3 border-t border-dark-700">
              <button @click="viewInBrowser(doc)"
                      class="flex-1 text-xs px-3 py-1.5 rounded-lg border border-blue-800/60 text-blue-400 hover:bg-blue-950/40 transition-colors font-medium">
                👁 View
              </button>
              <button @click="download(doc)"
                      class="flex-1 text-xs px-3 py-1.5 rounded-lg border border-emerald-800/60 text-emerald-400 hover:bg-emerald-950/40 transition-colors font-medium">
                ⬇ Download
              </button>
              <button @click="deleteDoc(doc)" :disabled="deleting === doc.id"
                      class="text-xs px-3 py-1.5 rounded-lg border border-red-800/60 text-red-400 hover:bg-red-950/40 transition-colors font-medium disabled:opacity-50">
                🗑
              </button>
            </div>
          </div>
        </div>
      </div>
    </template>

    <!-- ─── Upload Modal ──────────────────────────────────── -->
    <Teleport to="body">
      <div v-if="showUpload"
           class="fixed inset-0 z-50 flex items-center justify-center p-4"
           style="background: rgba(0,0,0,0.85);"
           @click.self="showUpload = false">
        <div class="w-full max-w-lg rounded-2xl overflow-hidden shadow-2xl"
             style="background: #0f172a; border: 1px solid rgba(180,83,9,0.35);">
          <div class="h-0.5 w-full" style="background: linear-gradient(90deg, transparent, #d97706, transparent);"></div>

          <div class="px-6 py-4 border-b border-dark-700 flex items-center justify-between">
            <h3 class="text-white font-bold text-lg">Upload Document</h3>
            <button @click="showUpload = false" class="text-slate-400 hover:text-white text-2xl">✕</button>
          </div>

          <div class="p-6 space-y-4">

            <!-- Drop zone -->
            <div @dragover.prevent="dragOver = true"
                 @dragleave="dragOver = false"
                 @drop.prevent="onDrop"
                 @click="fileInput.click()"
                 :class="['border-2 border-dashed rounded-xl p-8 text-center cursor-pointer transition-all',
                   dragOver ? 'border-gold-500 bg-gold-900/20' : uploadFile ? 'border-emerald-700 bg-emerald-900/10' : 'border-dark-600 bg-dark-800 hover:border-gold-700']">
              <input ref="fileInput" type="file" accept=".pdf,.jpg,.jpeg,.png" class="hidden" @change="onFileChange" />
              <div v-if="uploadFile">
                <div class="text-4xl mb-2">{{ uploadFile.type === 'application/pdf' ? '📕' : '🖼️' }}</div>
                <p class="text-white font-semibold text-sm">{{ uploadFile.name }}</p>
                <p class="text-slate-400 text-xs mt-1">{{ fileSizeLabel(uploadFile) }}</p>
                <p class="text-gold-500 text-xs mt-2">Click to change file</p>
              </div>
              <div v-else>
                <div class="text-4xl mb-3">📂</div>
                <p class="text-white font-medium">Drop file here or click to browse</p>
                <p class="text-slate-500 text-xs mt-1">PDF, JPG, PNG — max 20 MB</p>
              </div>
            </div>

            <!-- Title -->
            <div>
              <label class="form-label">Title *</label>
              <input v-model="uploadForm.title" class="input w-full" placeholder="e.g. Arbaeen 2026 Tour Guide" />
            </div>

            <!-- Category -->
            <div>
              <label class="form-label">Category</label>
              <select v-model="uploadForm.category" class="input w-full">
                <option v-for="c in categories" :key="c" :value="c">{{ categoryIcon[c] }} {{ c }}</option>
              </select>
            </div>

            <!-- Description -->
            <div>
              <label class="form-label">Description (optional)</label>
              <textarea v-model="uploadForm.description" class="input w-full" rows="2"
                        placeholder="Brief description of this document..."></textarea>
            </div>

            <div class="flex gap-3 pt-2">
              <button @click="submitUpload" :disabled="uploading || !uploadFile"
                      class="flex-1 py-2.5 rounded-xl text-sm font-semibold text-white disabled:opacity-40"
                      style="background: rgba(180,83,9,0.7);">
                {{ uploading ? 'Uploading...' : '⬆ Upload Document' }}
              </button>
              <button @click="showUpload = false" class="btn-outline px-5">Cancel</button>
            </div>
          </div>
        </div>
      </div>
    </Teleport>

  </div>
</template>
