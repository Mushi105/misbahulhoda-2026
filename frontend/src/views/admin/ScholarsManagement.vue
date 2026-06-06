<script setup>
import { ref, onMounted } from 'vue'
import api from '@/api/client'

const scholars = ref([])
const loading = ref(true)
const saving = ref(false)
const success = ref('')
const error = ref('')
const showForm = ref(false)
const editingId = ref(null)

const emptyForm = () => ({
  fullName: '', title: '', specialty: '', language: 'English',
  location: '', photoUrl: '', bio: '', quote: '', tags: '',
  youtubeSearch: '', youtubeUrl: '', website: '', isReciter: false, sortOrder: 0, isActive: true
})

const photoMode = ref('url')   // 'url' | 'upload'
const uploading = ref(false)
const photoFileInput = ref(null)

async function handlePhotoUpload(e) {
  const file = e.target.files?.[0]
  if (!file) return
  uploading.value = true
  try {
    const fd = new FormData()
    fd.append('file', file)
    const res = await api.post('/scholars/upload-photo', fd, { headers: { 'Content-Type': 'multipart/form-data' } })
    form.value.photoUrl = `http://localhost:5025${res.data.data.url}`
    error.value = ''
  } catch (e) {
    error.value = e.response?.data?.message || 'Upload failed.'
  } finally {
    uploading.value = false
  }
}

const form = ref(emptyForm())

const languages = ['English', 'Urdu', 'Arabic', 'English & Urdu']
const locations = ['United States', 'United Kingdom', 'Canada', 'Australia', 'Pakistan', 'India', 'Bahrain', 'Iraq', 'Iran', 'UAE', 'Other']

async function load() {
  loading.value = true
  try {
    const res = await api.get('/scholars')
    scholars.value = res.data.data || []
  } catch { error.value = 'Failed to load scholars.' }
  finally { loading.value = false }
}

function startAdd() {
  form.value = emptyForm()
  editingId.value = null
  showForm.value = true
  error.value = ''
}

function startEdit(s) {
  form.value = {
    fullName: s.fullName, title: s.title || '', specialty: s.specialty,
    language: s.language, location: s.location, photoUrl: s.photoUrl || '',
    bio: s.bio, quote: s.quote || '', tags: s.tags || '',
    youtubeSearch: s.youtubeSearch || '', youtubeUrl: s.youtubeUrl || '',
    website: s.website || '',
    isReciter: s.isReciter, sortOrder: s.sortOrder, isActive: s.isActive
  }
  photoMode.value = 'url'
  editingId.value = s.id
  showForm.value = true
  error.value = ''
  window.scrollTo({ top: 0, behavior: 'smooth' })
}

function cancelForm() {
  showForm.value = false
  editingId.value = null
  form.value = emptyForm()
}

async function save() {
  if (!form.value.fullName.trim()) { error.value = 'Full name is required.'; return }
  if (!form.value.specialty.trim()) { error.value = 'Specialty is required.'; return }
  if (!form.value.bio.trim()) { error.value = 'Bio is required.'; return }
  saving.value = true; error.value = ''
  try {
    const payload = { ...form.value, sortOrder: parseInt(form.value.sortOrder) || 0, youtubeUrl: form.value.youtubeUrl || null }
    if (editingId.value) {
      await api.put(`/scholars/${editingId.value}`, payload)
      success.value = `${form.value.fullName} updated successfully!`
    } else {
      await api.post('/scholars', payload)
      success.value = `${form.value.fullName} added successfully!`
    }
    cancelForm()
    await load()
  } catch (e) {
    error.value = e.response?.data?.message || 'Failed to save.'
  } finally { saving.value = false }
}

async function toggleActive(s) {
  try {
    await api.put(`/scholars/${s.id}`, { ...s, isActive: !s.isActive })
    await load()
  } catch {}
}

async function remove(s) {
  if (!confirm(`Remove ${s.fullName}? This cannot be undone.`)) return
  try {
    await api.delete(`/scholars/${s.id}`)
    success.value = `${s.fullName} removed.`
    await load()
  } catch { error.value = 'Failed to remove.' }
}

onMounted(load)
</script>

<template>
  <div class="space-y-6">

    <div class="flex items-start justify-between">
      <div>
        <h1 class="text-2xl font-bold text-gray-900">Scholars & Reciters</h1>
        <p class="text-gray-700 text-sm mt-1">Manage scholars and noha khuwan shown on the Tour Guide page</p>
      </div>
      <button @click="startAdd" class="btn-primary flex items-center gap-2">
        + Add Scholar
      </button>
    </div>

    <div v-if="success" class="bg-green-100 border border-green-300 text-green-700 rounded-lg px-4 py-3 flex justify-between text-sm">
      {{ success }}<button @click="success=''" class="text-green-500">✕</button>
    </div>
    <div v-if="error" class="bg-red-100 border border-red-300 text-red-600 rounded-lg px-4 py-3 flex justify-between text-sm">
      {{ error }}<button @click="error=''" class="text-red-500">✕</button>
    </div>

    <!-- Form -->
    <div v-if="showForm" class="card border border-primary-800">
      <div class="flex items-center justify-between mb-5">
        <h2 class="text-lg font-semibold text-gray-900">{{ editingId ? 'Edit Scholar' : 'Add New Scholar / Reciter' }}</h2>
        <button @click="cancelForm" class="text-gray-700 hover:text-gray-900 text-sm">✕ Cancel</button>
      </div>

      <div class="grid grid-cols-1 md:grid-cols-2 gap-4">

        <!-- Row 1 -->
        <div>
          <label class="label">Full Name <span class="text-red-400">*</span></label>
          <input v-model="form.fullName" class="input" placeholder="e.g. Sayyid Ali Zaidi" />
        </div>
        <div>
          <label class="label">Title</label>
          <input v-model="form.title" class="input" placeholder="e.g. Shaykh, Sayyid, Maulana" />
        </div>

        <!-- Row 2 -->
        <div>
          <label class="label">Specialty <span class="text-red-400">*</span></label>
          <input v-model="form.specialty" class="input" placeholder="e.g. History of Karbala" />
        </div>
        <div>
          <label class="label">Language</label>
          <select v-model="form.language" class="input">
            <option v-for="l in languages" :key="l" :value="l">{{ l }}</option>
          </select>
        </div>

        <!-- Row 3 -->
        <div>
          <label class="label">Location / Country</label>
          <select v-model="form.location" class="input">
            <option value="">Select...</option>
            <option v-for="l in locations" :key="l" :value="l">{{ l }}</option>
          </select>
        </div>
        <!-- Photo — toggle URL / Upload -->
        <div class="md:col-span-2">
          <label class="label">Photo</label>
          <div class="flex gap-1 mb-2">
            <button type="button" @click="photoMode = 'url'"
              :class="['px-3 py-1 rounded-lg text-xs font-medium transition-colors', photoMode === 'url' ? 'bg-gold-700 text-white' : 'bg-gray-100 text-gray-700 hover:text-gray-900']">
              🔗 Paste URL
            </button>
            <button type="button" @click="photoMode = 'upload'"
              :class="['px-3 py-1 rounded-lg text-xs font-medium transition-colors', photoMode === 'upload' ? 'bg-gold-700 text-white' : 'bg-gray-100 text-gray-700 hover:text-gray-900']">
              📁 Upload File
            </button>
          </div>

          <!-- URL paste -->
          <div v-if="photoMode === 'url'">
            <input v-model="form.photoUrl" class="input" placeholder="https://... right-click photo → Copy image address" />
            <p class="text-xs text-gray-700 mt-1">Google the scholar → Images → right-click → Copy image address</p>
          </div>

          <!-- File upload -->
          <div v-else class="space-y-2">
            <input ref="photoFileInput" type="file" accept=".jpg,.jpeg,.png,.webp" class="hidden" @change="handlePhotoUpload" />
            <button type="button" @click="photoFileInput.click()" :disabled="uploading"
              class="w-full border-2 border-dashed border-gray-300 hover:border-gold-500 rounded-lg py-4 text-gray-700 hover:text-gold-600 transition-colors text-sm disabled:opacity-50">
              <span v-if="uploading">⏳ Uploading...</span>
              <span v-else>📁 Click to select photo (JPG/PNG/WebP, max 5MB)</span>
            </button>
            <p v-if="form.photoUrl && photoMode === 'upload'" class="text-xs text-green-400">✓ Photo uploaded</p>
          </div>
        </div>

        <!-- Photo preview -->
        <div v-if="form.photoUrl" class="md:col-span-2 flex items-center gap-4 bg-gray-100 rounded-lg p-3">
          <img :src="form.photoUrl" alt="Photo" class="w-16 h-16 rounded-full object-cover border-2 border-gold-600" @error="e => { e.target.style.display='none'; e.target.nextSibling.style.display='flex' }" />
          <div class="w-16 h-16 rounded-full bg-gold-900/40 border-2 border-gold-700 items-center justify-center text-gold-400 font-bold text-lg hidden">?</div>
          <div class="flex-1">
            <p class="text-gray-700 text-sm">Photo preview — if broken, URL is invalid</p>
            <button type="button" @click="form.photoUrl = ''" class="text-xs text-red-400 hover:text-red-300 mt-1">✕ Remove photo</button>
          </div>
        </div>

        <!-- Bio -->
        <div class="md:col-span-2">
          <label class="label">Bio <span class="text-red-400">*</span> <span class="text-gray-600 text-xs">(detailed biography — shown publicly)</span></label>
          <textarea v-model="form.bio" class="input h-32 resize-none" placeholder="Write a detailed bio about this scholar. Include their background, where they studied, what they're known for, and why pilgrims should attend their sessions..."></textarea>
          <p class="text-xs text-gray-700 text-right mt-1">{{ form.bio.length }} chars</p>
        </div>

        <!-- Quote -->
        <div class="md:col-span-2">
          <label class="label">Inspirational Quote <span class="text-gray-600 text-xs">(optional — shown in a highlight box)</span></label>
          <input v-model="form.quote" class="input" placeholder='"A meaningful quote from this scholar about Hussain (AS), Karbala, or the spiritual journey..."' />
        </div>

        <!-- Tags -->
        <div>
          <label class="label">Tags <span class="text-gray-600 text-xs">(comma-separated)</span></label>
          <input v-model="form.tags" class="input" placeholder="YouTube Lectures, English Podcast, UK Based, Youth Speaker" />
        </div>
        <div>
          <label class="label">YouTube Search Hint <span class="text-gray-600 text-xs">(shown as "Search: ..." text)</span></label>
          <input v-model="form.youtubeSearch" class="input" placeholder="e.g. Sayyid Ali Zaidi Karbala" />
        </div>

        <!-- YouTube URL (direct link) -->
        <div class="md:col-span-2">
          <label class="label">YouTube Channel / Video Link <span class="text-gray-600 text-xs">(direct clickable link)</span></label>
          <input v-model="form.youtubeUrl" class="input" placeholder="https://www.youtube.com/@scholarname or https://youtu.be/..." />
          <p v-if="form.youtubeUrl" class="text-xs text-green-400 mt-1">✓ YouTube link set — will show as a button on the pilgrim Tour Guide</p>
        </div>

        <!-- Website + Sort + Reciter -->
        <div>
          <label class="label">Website</label>
          <input v-model="form.website" class="input" placeholder="https://..." />
        </div>
        <div>
          <label class="label">Sort Order <span class="text-gray-600 text-xs">(lower = appears first)</span></label>
          <input v-model.number="form.sortOrder" type="number" min="0" max="999" class="input" />
        </div>

        <!-- Toggles -->
        <div class="flex items-center gap-6">
          <label class="flex items-center gap-3 cursor-pointer">
            <div @click="form.isReciter = !form.isReciter"
              :class="['w-11 h-6 rounded-full transition-colors relative cursor-pointer', form.isReciter ? 'bg-gold-600' : 'bg-gray-200']">
              <span :class="['absolute top-0.5 w-5 h-5 bg-white rounded-full transition-transform shadow', form.isReciter ? 'translate-x-5' : 'translate-x-0.5']"></span>
            </div>
            <span class="text-gray-600 text-sm">{{ form.isReciter ? '🎤 Noha Khuwan / Reciter' : '📚 Scholar / Zakir' }}</span>
          </label>
        </div>
        <div class="flex items-center gap-6">
          <label class="flex items-center gap-3 cursor-pointer">
            <div @click="form.isActive = !form.isActive"
              :class="['w-11 h-6 rounded-full transition-colors relative cursor-pointer', form.isActive ? 'bg-primary-600' : 'bg-gray-200']">
              <span :class="['absolute top-0.5 w-5 h-5 bg-white rounded-full transition-transform shadow', form.isActive ? 'translate-x-5' : 'translate-x-0.5']"></span>
            </div>
            <span class="text-gray-600 text-sm">{{ form.isActive ? 'Active (visible publicly)' : 'Hidden' }}</span>
          </label>
        </div>
      </div>

      <div class="mt-6 flex gap-3">
        <button @click="save" :disabled="saving" class="btn-primary px-8">
          {{ saving ? 'Saving...' : (editingId ? '✓ Update Scholar' : '+ Add Scholar') }}
        </button>
        <button @click="cancelForm" class="btn-outline">Cancel</button>
      </div>
    </div>

    <!-- List -->
    <div v-if="loading" class="text-center py-12 text-gray-700">Loading...</div>

    <div v-else-if="scholars.length === 0 && !showForm" class="card text-center py-12">
      <div class="text-5xl mb-3">📚</div>
      <p class="text-gray-900 font-medium">No scholars added yet</p>
      <p class="text-gray-700 text-sm mt-1">Click "+ Add Scholar" to add the first scholar.</p>
      <p class="text-gray-600 text-xs mt-3">The Tour Guide page will show scholars from the database once added here.</p>
    </div>

    <div v-else class="space-y-4">
      <div class="flex gap-3 text-xs text-gray-600 px-1">
        <span>📚 Scholars: {{ scholars.filter(s => !s.isReciter).length }}</span>
        <span>🎤 Reciters: {{ scholars.filter(s => s.isReciter).length }}</span>
        <span>✓ Active: {{ scholars.filter(s => s.isActive).length }}</span>
      </div>

      <div v-for="s in scholars" :key="s.id" :class="['card transition-all', !s.isActive ? 'opacity-50' : '']">
        <div class="flex items-start gap-4">
          <!-- Avatar / Photo -->
          <div class="flex-shrink-0">
            <img v-if="s.photoUrl" :src="s.photoUrl" :alt="s.fullName"
              class="w-16 h-16 rounded-xl object-cover border-2 border-primary-800"
              @error="e => e.target.style.display='none'" />
            <div v-else
              class="w-16 h-16 rounded-xl bg-gradient-to-br from-primary-700 to-primary-900 flex items-center justify-center text-xl font-bold text-gray-900">
              {{ s.fullName.split(' ').map(n=>n[0]).slice(0,2).join('') }}
            </div>
          </div>

          <div class="flex-1 min-w-0">
            <div class="flex items-start justify-between gap-2">
              <div>
                <div class="flex items-center gap-2">
                  <p class="font-bold text-gray-900">{{ s.fullName }}</p>
                  <span class="text-xs bg-gray-100 text-gray-700 px-1.5 py-0.5 rounded">{{ s.isReciter ? '🎤 Reciter' : '📚 Scholar' }}</span>
                  <span :class="['text-xs px-1.5 py-0.5 rounded', s.language === 'English' || s.language === 'English & Urdu' ? 'bg-blue-100 text-blue-700' : 'bg-green-100 text-green-700']">{{ s.language }}</span>
                  <span v-if="!s.isActive" class="text-xs bg-gray-100 text-gray-700 px-1.5 py-0.5 rounded">Hidden</span>
                </div>
                <p class="text-amber-700 text-xs mt-0.5">{{ s.specialty }}</p>
                <p class="text-gray-600 text-xs">📍 {{ s.location }}</p>
              </div>
              <div class="flex items-center gap-2 flex-shrink-0">
                <button @click="toggleActive(s)" :class="['text-xs px-2 py-1 rounded border', s.isActive ? 'border-gray-300 text-gray-700 hover:text-red-500' : 'border-green-300 text-green-700 hover:text-green-600']">
                  {{ s.isActive ? 'Hide' : 'Show' }}
                </button>
                <button @click="startEdit(s)" class="text-xs px-3 py-1 rounded bg-gray-100 text-gray-600 hover:bg-gray-200 border border-gray-300">Edit</button>
                <button @click="remove(s)" class="text-xs px-2 py-1 rounded text-red-600 hover:text-red-700 border border-red-300 hover:bg-red-50">Del</button>
              </div>
            </div>
            <p class="text-gray-700 text-sm mt-2 line-clamp-2">{{ s.bio }}</p>
            <div class="flex flex-wrap gap-1.5 mt-2">
              <a v-if="s.youtubeUrl" :href="s.youtubeUrl" target="_blank" rel="noopener"
                class="text-xs bg-red-100 text-red-600 border border-red-300 px-2 py-0.5 rounded-full hover:bg-red-200 transition-colors">
                ▶ YouTube
              </a>
              <a v-if="s.website" :href="s.website" target="_blank" rel="noopener"
                class="text-xs bg-gray-100 text-gray-700 border border-gray-300 px-2 py-0.5 rounded-full hover:bg-gray-200 transition-colors">
                🌐 Website
              </a>
              <span v-for="tag in (s.tags || '').split(',').map(t=>t.trim()).filter(Boolean)" :key="tag"
                class="text-xs bg-gray-100 text-gray-700 px-2 py-0.5 rounded-full">{{ tag }}</span>
            </div>
          </div>
        </div>
      </div>
    </div>

  </div>
</template>
