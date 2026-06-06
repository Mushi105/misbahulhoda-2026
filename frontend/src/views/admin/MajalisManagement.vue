<script setup>
import { ref, onMounted } from 'vue'
import { majalisApi } from '@/api'
import ImageUploader from '@/components/ImageUploader.vue'

const tab       = ref('majalis')  // majalis | namaz | food
const majalis   = ref([])
const namaz     = ref([])
const food      = ref([])
const loading   = ref(false)
const msg       = ref({ type: '', text: '' })

// ── Forms ────────────────────────────────────────────────────────────────────
const showMajalisModal = ref(false)
const showNamazModal   = ref(false)
const showFoodModal    = ref(false)
const editingId        = ref(null)
const saving           = ref(false)

const majalisForm = ref({
  title: '', language: 'Urdu', molanaName: '', nohaKhuwanName: '',
  venue: '', startTime: '', endTime: '', description: '', imageUrl: ''
})

const PRAYERS = ['Fajr', 'Zuhr', 'Asr', 'Maghrib', 'Isha']
const namazDate  = ref(new Date().toISOString().split('T')[0])
const namazForm  = ref(PRAYERS.map(p => ({ prayerName: p, time: '', venue: '', date: namazDate.value })))

const foodForm = ref({
  mealType: 'Lunch', servedAt: '', location: '', description: '', estimatedServings: ''
})
const MEAL_TYPES = ['Breakfast', 'Lunch', 'Dinner', 'Sehri', 'Iftaar', 'Snack']

function notify(type, text) {
  msg.value = { type, text }
  setTimeout(() => msg.value = { type: '', text: '' }, 5000)
}

// ── Loaders ──────────────────────────────────────────────────────────────────
async function loadMajalis() {
  loading.value = true
  try { majalis.value = (await majalisApi.getAll()).data.data || [] }
  catch (e) { notify('error', 'Failed to load majalis.') }
  finally { loading.value = false }
}

async function loadNamaz() {
  loading.value = true
  try { namaz.value = (await majalisApi.getNamazTimings(namazDate.value)).data.data || [] }
  catch { namaz.value = [] }
  finally { loading.value = false }
}

async function loadFood() {
  loading.value = true
  try { food.value = (await majalisApi.getFoodSchedule()).data.data || [] }
  catch { food.value = [] }
  finally { loading.value = false }
}

async function load() {
  if (tab.value === 'majalis') await loadMajalis()
  else if (tab.value === 'namaz') await loadNamaz()
  else await loadFood()
}

// ── Majalis CRUD ─────────────────────────────────────────────────────────────
function openAddMajalis() {
  editingId.value = null
  majalisForm.value = { title: '', language: 'Urdu', molanaName: '', nohaKhuwanName: '', venue: '', startTime: '', endTime: '', description: '', imageUrl: '' }
  showMajalisModal.value = true
}

function openEditMajalis(m) {
  editingId.value = m.id
  majalisForm.value = {
    title: m.title, language: m.language || 'Urdu',
    molanaName: m.molanaName || '', nohaKhuwanName: m.nohaKhuwanName || '',
    venue: m.venue, description: m.description || '',
    startTime: m.startTime ? new Date(m.startTime).toISOString().slice(0,16) : '',
    endTime:   m.endTime   ? new Date(m.endTime).toISOString().slice(0,16)   : '',
    imageUrl:  m.imageUrl  || '',
  }
  showMajalisModal.value = true
}

async function saveMajalis() {
  if (!majalisForm.value.title || !majalisForm.value.startTime) return notify('error', 'Title and time are required.')
  saving.value = true
  try {
    const payload = {
      ...majalisForm.value,
      startTime: new Date(majalisForm.value.startTime).toISOString(),
      endTime:   majalisForm.value.endTime ? new Date(majalisForm.value.endTime).toISOString() : new Date(majalisForm.value.startTime).toISOString(),
    }
    if (editingId.value) await majalisApi.update(editingId.value, payload)
    else await majalisApi.create(payload)
    notify('success', editingId.value ? 'Majalis updated.' : 'Majalis created.')
    showMajalisModal.value = false
    await loadMajalis()
  } catch (e) { notify('error', e.response?.data?.message || 'Failed.') }
  finally { saving.value = false }
}

async function deleteMajalis(id) {
  if (!confirm('Delete this majalis?')) return
  try { await majalisApi.remove(id); notify('success', 'Deleted.'); await loadMajalis() }
  catch { notify('error', 'Delete failed.') }
}

// ── Program Items ─────────────────────────────────────────────────────────────
const showProgramModal = ref(false)
const programMajalisId = ref(null)
const programMajalisTitle = ref('')
const programItems = ref([])
const savingProgram = ref(false)

const PROGRAM_TYPES = [
  { label: 'Majlis Opening',  icon: '📿', value: 'Opening'  },
  { label: 'Hadith',          icon: '📖', value: 'Hadith'   },
  { label: 'Marsiya',         icon: '🎙️', value: 'Marsiya'  },
  { label: 'Salam',           icon: '🕊️', value: 'Salam'    },
  { label: 'Noha',            icon: '🎵', value: 'Noha'     },
  { label: 'Majlis Closing',  icon: '🕌', value: 'Closing'  },
  { label: 'Break',           icon: '☕', value: 'Break'    },
]

const PROGRAM_TYPE_ICONS = Object.fromEntries(PROGRAM_TYPES.map(t => [t.value, t.icon]))

async function openProgramModal(m) {
  programMajalisId.value = m.id
  programMajalisTitle.value = m.title
  showProgramModal.value = true
  savingProgram.value = false
  try {
    const res = await majalisApi.getProgram(m.id)
    programItems.value = (res.data.data || []).map(item => ({
      type: item.type,
      time: item.time,
      performerName: item.performerName || '',
      imageUrl: item.imageUrl || '',
      description: item.description || '',
    }))
    if (!programItems.value.length) addProgramItem()
  } catch { programItems.value = []; addProgramItem() }
}

function addProgramItem() {
  programItems.value.push({ type: 'Hadith', time: '', performerName: '', imageUrl: '', description: '' })
}

function removeProgramItem(idx) {
  programItems.value.splice(idx, 1)
}

function moveProgramItem(idx, dir) {
  const arr = programItems.value
  const swap = idx + dir
  if (swap < 0 || swap >= arr.length) return
  ;[arr[idx], arr[swap]] = [arr[swap], arr[idx]]
}

async function saveProgram() {
  savingProgram.value = true
  try {
    await majalisApi.saveProgram(programMajalisId.value, programItems.value.map(i => ({
      type: i.type, time: i.time,
      performerName: i.performerName || null,
      imageUrl: i.imageUrl || null,
      description: i.description || null,
    })))
    notify('success', 'Program saved successfully.')
    showProgramModal.value = false
  } catch { notify('error', 'Failed to save program.') }
  finally { savingProgram.value = false }
}

// ── Namaz Timings ─────────────────────────────────────────────────────────────
function openNamazModal() {
  namazForm.value = PRAYERS.map(p => {
    const existing = namaz.value.find(n => n.prayerName === p)
    return { prayerName: p, time: existing?.time ? existing.time.substring(0,5) : '', venue: existing?.venue || '', date: namazDate.value }
  })
  showNamazModal.value = true
}

async function saveNamaz() {
  const filled = namazForm.value.filter(n => n.time)
  if (!filled.length) return notify('error', 'Enter at least one namaz time.')
  saving.value = true
  try {
    await majalisApi.saveNamazTimings(filled.map(n => ({ ...n, date: namazDate.value })))
    notify('success', 'Namaz timings saved.')
    showNamazModal.value = false
    await loadNamaz()
  } catch { notify('error', 'Failed to save.') }
  finally { saving.value = false }
}

// ── Food Schedule ─────────────────────────────────────────────────────────────
function openFoodModal() {
  foodForm.value = { mealType: 'Lunch', servedAt: '', location: '', description: '', estimatedServings: '' }
  showFoodModal.value = true
}

async function saveFood() {
  if (!foodForm.value.servedAt || !foodForm.value.location) return notify('error', 'Date/time and location are required.')
  saving.value = true
  try {
    await majalisApi.createFoodSchedule({
      ...foodForm.value,
      servedAt: new Date(foodForm.value.servedAt).toISOString(),
      estimatedServings: foodForm.value.estimatedServings ? Number(foodForm.value.estimatedServings) : null
    })
    notify('success', 'Food schedule added.')
    showFoodModal.value = false
    await loadFood()
  } catch { notify('error', 'Failed.') }
  finally { saving.value = false }
}

async function deleteFood(id) {
  if (!confirm('Delete?')) return
  try { await majalisApi.deleteFoodSchedule(id); notify('success', 'Deleted.'); await loadFood() }
  catch { notify('error', 'Delete failed.') }
}

function fmtDT(d) {
  if (!d) return '—'
  return new Date(d).toLocaleString('en-GB', { day:'2-digit', month:'short', hour:'2-digit', minute:'2-digit' })
}

onMounted(load)
</script>

<template>
  <div class="space-y-5">

    <!-- Header -->
    <div class="flex items-center justify-between flex-wrap gap-3">
      <div>
        <h1 class="text-2xl font-bold text-gray-900">📿 Majalis Management</h1>
        <p class="text-gray-700 text-sm mt-0.5">Manage Majalis, Namaz Timings and Food Schedule</p>
      </div>
      <div class="flex gap-2">
        <button v-if="tab==='majalis'" @click="openAddMajalis"
          class="px-4 py-2 rounded-xl text-sm font-semibold text-gray-900"
          style="background:linear-gradient(135deg,#d97706,#b45309);">
          + Add Majalis
        </button>
        <button v-if="tab==='namaz'" @click="openNamazModal"
          class="px-4 py-2 rounded-xl text-sm font-semibold text-gray-900"
          style="background:linear-gradient(135deg,#b88a00,#D4A800);">
          🕌 Set Namaz Timings
        </button>
        <button v-if="tab==='food'" @click="openFoodModal"
          class="px-4 py-2 rounded-xl text-sm font-semibold text-gray-900"
          style="background:linear-gradient(135deg,#1d4ed8,#1e40af);">
          + Add Food Schedule
        </button>
      </div>
    </div>

    <!-- Alert -->
    <div v-if="msg.text"
         :class="msg.type==='success' ? 'bg-emerald-900/40 border-emerald-700 text-amber-600' : 'bg-red-900/40 border-red-700 text-red-300'"
         class="border rounded-lg px-4 py-3 text-sm flex justify-between items-center">
      {{ msg.text }}<button @click="msg.text=''" class="opacity-60 hover:opacity-100 ml-4">✕</button>
    </div>

    <!-- Tabs -->
    <div class="flex gap-1 border-b border-emerald-900/30 pb-0">
      <button v-for="[t, label] in [['majalis','📿 Majalis'],['namaz','🕌 Namaz Timings'],['food','🍽️ Food Schedule']]"
        :key="t" @click="tab=t; load()"
        :class="['px-4 py-2 text-sm font-medium rounded-t-lg transition-colors',
          tab===t ? 'bg-gray-100 text-amber-700 border border-b-0 border-gray-200' : 'text-gray-700 hover:text-gray-900']">
        {{ label }}
      </button>
    </div>

    <div v-if="loading" class="text-center py-12 text-gray-700">Loading...</div>

    <!-- ── Majalis List ─────────────────────────────────────────────────────── -->
    <template v-else-if="tab==='majalis'">
      <div v-if="!majalis.length"
           class="card text-center py-12 text-slate-500">
        <div class="text-5xl mb-3">📿</div>
        <p class="font-medium text-slate-400">No Majalis scheduled yet</p>
        <p class="text-sm mt-1">Click the "+ Add Majalis" button above to add the first one</p>
      </div>
      <div v-for="m in majalis" :key="m.id"
           class="rounded-xl border p-4 transition-all"
           style="background:#ffffff; border-color:rgba(180,83,9,0.35);">
        <div class="flex items-start gap-3">
          <!-- Scholar thumbnail -->
          <div v-if="m.imageUrl" class="shrink-0 w-16 h-16 rounded-xl overflow-hidden border border-amber-900/30">
            <img :src="m.imageUrl" alt="Scholar" class="w-full h-full object-cover object-top" />
          </div>
          <div v-else class="shrink-0 w-16 h-16 rounded-xl flex items-center justify-center text-3xl"
               style="background:rgba(180,83,9,0.1); border:1px solid rgba(180,83,9,0.2);">
            📿
          </div>
          <div class="flex-1 min-w-0">
            <div class="flex items-center gap-2 flex-wrap">
              <p class="text-gray-900 font-bold">{{ m.title }}</p>
              <span class="text-xs px-2 py-0.5 rounded-full border"
                    :class="m.language==='Urdu' ? 'bg-emerald-900/40 border-emerald-700 text-amber-700' : 'bg-blue-900/40 border-blue-700 text-blue-400'">
                {{ m.language }}
              </span>
            </div>
            <p class="text-gray-700 text-sm mt-1">📍 {{ m.venue }}</p>
            <div class="flex gap-4 mt-1 text-xs text-gray-600">
              <span>🕐 {{ fmtDT(m.startTime) }}</span>
              <span v-if="m.endTime">— {{ fmtDT(m.endTime) }}</span>
            </div>
            <div class="flex gap-4 mt-1 text-xs text-gray-600">
              <span v-if="m.molanaName">🎓 {{ m.molanaName }}</span>
              <span v-if="m.nohaKhuwanName">🎵 {{ m.nohaKhuwanName }}</span>
            </div>
            <p v-if="m.description" class="text-gray-600 text-xs mt-1">{{ m.description }}</p>
          </div>
          <div class="flex flex-col gap-2 shrink-0">
            <button @click="openProgramModal(m)"
              class="text-xs px-3 py-1.5 rounded-lg border border-purple-800/50 text-purple-300 hover:bg-purple-900/30 transition"
              style="min-height:36px; touch-action:manipulation;">
              📋 Program
            </button>
            <button @click="openEditMajalis(m)"
              class="text-xs px-3 py-1.5 rounded-lg border border-amber-800/50 text-amber-400 hover:bg-amber-900/30 transition"
              style="min-height:36px; touch-action:manipulation;">
              ✏️ Edit
            </button>
            <button @click="deleteMajalis(m.id)"
              class="text-xs px-3 py-1.5 rounded-lg border border-red-900/50 text-red-400 hover:bg-red-950/30 transition"
              style="min-height:36px; touch-action:manipulation;">
              🗑️
            </button>
          </div>
        </div>
      </div>
    </template>

    <!-- ── Namaz Timings ────────────────────────────────────────────────────── -->
    <template v-else-if="tab==='namaz'">
      <div class="flex items-center gap-3">
        <label class="text-gray-700 text-sm">Date:</label>
        <input v-model="namazDate" @change="loadNamaz" type="date"
          class="input w-auto text-sm" style="min-height:40px;" />
      </div>
      <div v-if="!namaz.length" class="card text-center py-12 text-slate-500">
        <div class="text-4xl mb-3">🕌</div>
        <p class="font-medium text-slate-400">No namaz timings set for this date</p>
        <p class="text-sm mt-1">Click "Set Namaz Timings" button above to add</p>
      </div>
      <div v-else class="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-5 gap-3">
        <div v-for="n in namaz" :key="n.id"
             class="rounded-xl border p-4 text-center"
             style="background:#ffffff; border-color:rgba(212,168,0,0.35);">
          <p class="text-amber-700 font-bold text-sm">{{ n.prayerName }}</p>
          <p class="text-gray-900 text-xl font-bold mt-1">{{ n.time?.substring(0,5) || '—' }}</p>
          <p v-if="n.venue" class="text-gray-600 text-xs mt-1">{{ n.venue }}</p>
        </div>
      </div>
    </template>

    <!-- ── Food Schedule ────────────────────────────────────────────────────── -->
    <template v-else-if="tab==='food'">
      <div v-if="!food.length" class="card text-center py-12 text-slate-500">
        <div class="text-4xl mb-3">🍽️</div>
        <p class="font-medium text-slate-400">No food schedule added yet</p>
        <p class="text-sm mt-1">Click "+ Add Food Schedule" button above to add</p>
      </div>
      <div v-for="f in food" :key="f.id"
           class="rounded-xl border p-4 flex items-center gap-4"
           style="background:#ffffff; border-color:rgba(212,168,0,0.35);">
        <div class="w-12 h-12 rounded-xl flex items-center justify-center text-2xl shrink-0"
             style="background:rgba(212,168,0,0.1); border:1px solid rgba(212,168,0,0.2);">
          {{ f.mealType==='Breakfast'?'🌅':f.mealType==='Lunch'?'☀️':f.mealType==='Dinner'?'🌙':f.mealType==='Sehri'?'⭐':f.mealType==='Iftaar'?'🌙':'🍽️' }}
        </div>
        <div class="flex-1 min-w-0">
          <p class="text-gray-900 font-semibold">{{ f.mealType }}</p>
          <p class="text-amber-700 text-sm">🕐 {{ fmtDT(f.servedAt) }}</p>
          <p class="text-gray-600 text-xs">📍 {{ f.location }}</p>
          <p v-if="f.description" class="text-gray-600 text-xs mt-0.5">{{ f.description }}</p>
          <p v-if="f.estimatedServings" class="text-gray-600 text-xs">👥 ~{{ f.estimatedServings }} servings</p>
        </div>
        <button @click="deleteFood(f.id)"
          class="text-xs px-3 py-1.5 rounded-lg border border-red-900/50 text-red-400 hover:bg-red-950/30 transition shrink-0"
          style="min-height:36px; touch-action:manipulation;">🗑️</button>
      </div>
    </template>

  </div>

  <!-- ── Majalis Modal ─────────────────────────────────────────────────────── -->
  <Teleport to="body">
    <div v-if="showMajalisModal"
         class="fixed inset-0 z-50 flex items-center justify-center p-4"
         style="background:rgba(0,0,0,0.85);" @click.self="showMajalisModal=false">
      <div class="w-full max-w-lg rounded-2xl overflow-hidden shadow-2xl"
           style="background:#0d1f14; border:1px solid rgba(180,83,9,0.3);">
        <div class="h-0.5 w-full" style="background:linear-gradient(90deg,transparent,#d97706,transparent);"></div>
        <div class="flex items-center justify-between px-5 py-4 border-b border-amber-900/30">
          <h3 class="text-gray-900 font-bold text-lg">{{ editingId ? '✏️ Edit Majalis' : '+ New Majalis' }}</h3>
          <button @click="showMajalisModal=false" class="text-slate-400 hover:text-white text-xl">✕</button>
        </div>
        <div class="p-5 space-y-4 max-h-[75vh] overflow-y-auto">
          <div>
            <label class="form-label">Majalis Title *</label>
            <input v-model="majalisForm.title" class="input w-full" placeholder="e.g. Majlis-e-Aza — Shab-e-Ashura" />
          </div>
          <div class="grid grid-cols-2 gap-3">
            <div>
              <label class="form-label">Language</label>
              <select v-model="majalisForm.language" class="input w-full">
                <option>Urdu</option><option>English</option><option>Arabic</option><option>Punjabi</option>
              </select>
            </div>
            <div>
              <label class="form-label">Venue</label>
              <input v-model="majalisForm.venue" class="input w-full" placeholder="Hall / Imambargah" />
            </div>
          </div>
          <div class="grid grid-cols-2 gap-3">
            <div>
              <label class="form-label">Start Time *</label>
              <input v-model="majalisForm.startTime" type="datetime-local" class="input w-full" />
            </div>
            <div>
              <label class="form-label">End Time</label>
              <input v-model="majalisForm.endTime" type="datetime-local" class="input w-full" />
            </div>
          </div>
          <div>
            <label class="form-label">Zakir / Molana</label>
            <input v-model="majalisForm.molanaName" class="input w-full" placeholder="Agha xyz" />
          </div>
          <div>
            <label class="form-label">Noha Khuwan</label>
            <input v-model="majalisForm.nohaKhuwanName" class="input w-full" placeholder="Ustad xyz" />
          </div>
          <ImageUploader
            v-model="majalisForm.imageUrl"
            label="Scholar / Molana Photo (URL or Upload)"
            placeholder="https://... or use the Upload button above"
            previewClass="w-full max-h-44 object-cover object-top rounded-xl"
          />
          <div>
            <label class="form-label">Description (optional)</label>
            <textarea v-model="majalisForm.description" class="input w-full" rows="2" placeholder="Any additional notes..."></textarea>
          </div>
          <div class="flex gap-3 pt-2">
            <button @click="saveMajalis" :disabled="saving"
              class="flex-1 py-2.5 rounded-xl text-gray-900 font-semibold text-sm disabled:opacity-50"
              style="background:linear-gradient(135deg,#d97706,#b45309);">
              {{ saving ? 'Saving...' : editingId ? '✅ Update' : '✅ Create' }}
            </button>
            <button @click="showMajalisModal=false" class="btn-outline px-5">Cancel</button>
          </div>
        </div>
      </div>
    </div>

    <!-- ── Namaz Modal ─────────────────────────────────────────────────────── -->
    <div v-if="showNamazModal"
         class="fixed inset-0 z-50 flex items-center justify-center p-4"
         style="background:rgba(0,0,0,0.85);" @click.self="showNamazModal=false">
      <div class="w-full max-w-md rounded-2xl overflow-hidden shadow-2xl"
           style="background:#0d1f14; border:1px solid rgba(212,168,0,0.25);">
        <div class="flex items-center justify-between px-5 py-4 border-b border-emerald-900/30">
          <h3 class="text-gray-900 font-bold text-lg">🕌 Namaz Timings — {{ namazDate }}</h3>
          <button @click="showNamazModal=false" class="text-slate-400 hover:text-white text-xl">✕</button>
        </div>
        <div class="p-5 space-y-3">
          <div v-for="n in namazForm" :key="n.prayerName"
               class="flex items-center gap-3">
            <span class="text-amber-700 font-semibold text-sm w-20 shrink-0">{{ n.prayerName }}</span>
            <input v-model="n.time" type="time" class="input flex-1" style="min-height:40px;" />
            <input v-model="n.venue" class="input flex-1" placeholder="Venue" style="min-height:40px;" />
          </div>
          <div class="flex gap-3 pt-2">
            <button @click="saveNamaz" :disabled="saving"
              class="flex-1 py-2.5 rounded-xl text-gray-900 font-semibold text-sm disabled:opacity-50"
              style="background:linear-gradient(135deg,#b88a00,#D4A800);">
              {{ saving ? 'Saving...' : '✅ Save' }}
            </button>
            <button @click="showNamazModal=false" class="btn-outline px-5">Cancel</button>
          </div>
        </div>
      </div>
    </div>

    <!-- ── Food Modal ──────────────────────────────────────────────────────── -->
    <div v-if="showFoodModal"
         class="fixed inset-0 z-50 flex items-center justify-center p-4"
         style="background:rgba(0,0,0,0.85);" @click.self="showFoodModal=false">
      <div class="w-full max-w-md rounded-2xl overflow-hidden shadow-2xl"
           style="background:#0d1f14; border:1px solid rgba(29,78,216,0.3);">
        <div class="flex items-center justify-between px-5 py-4 border-b border-blue-900/30">
          <h3 class="text-gray-900 font-bold text-lg">🍽️ Add Food Schedule</h3>
          <button @click="showFoodModal=false" class="text-slate-400 hover:text-white text-xl">✕</button>
        </div>
        <div class="p-5 space-y-4">
          <div>
            <label class="form-label">Meal Type *</label>
            <select v-model="foodForm.mealType" class="input w-full">
              <option v-for="t in MEAL_TYPES" :key="t">{{ t }}</option>
            </select>
          </div>
          <div>
            <label class="form-label">Date & Time *</label>
            <input v-model="foodForm.servedAt" type="datetime-local" class="input w-full" />
          </div>
          <div>
            <label class="form-label">Location *</label>
            <input v-model="foodForm.location" class="input w-full" placeholder="Hotel dining hall / Tent area" />
          </div>
          <div>
            <label class="form-label">Description</label>
            <input v-model="foodForm.description" class="input w-full" placeholder="e.g. Biryani + Daal" />
          </div>
          <div>
            <label class="form-label">Estimated Servings</label>
            <input v-model="foodForm.estimatedServings" type="number" class="input w-full" placeholder="e.g. 200" />
          </div>
          <div class="flex gap-3 pt-1">
            <button @click="saveFood" :disabled="saving"
              class="flex-1 py-2.5 rounded-xl text-gray-900 font-semibold text-sm disabled:opacity-50"
              style="background:linear-gradient(135deg,#1d4ed8,#1e40af);">
              {{ saving ? 'Saving...' : '✅ Add' }}
            </button>
            <button @click="showFoodModal=false" class="btn-outline px-5">Cancel</button>
          </div>
        </div>
      </div>
    </div>

    <!-- ── Program Modal ─────────────────────────────────────────────────── -->
    <div v-if="showProgramModal"
         class="fixed inset-0 z-50 flex items-start justify-center p-4 overflow-y-auto"
         style="background:rgba(0,0,0,0.88);" @click.self="showProgramModal=false">
      <div class="w-full max-w-2xl rounded-2xl overflow-hidden shadow-2xl my-4"
           style="background:#0d1120; border:1px solid rgba(124,58,237,0.35);">
        <div class="h-0.5 w-full" style="background:linear-gradient(90deg,transparent,#7c3aed,transparent);"></div>
        <div class="flex items-center justify-between px-5 py-4 border-b border-purple-900/30">
          <div>
            <h3 class="text-gray-900 font-bold text-lg">📋 Majalis Program Setup</h3>
            <p class="text-purple-300 text-xs mt-0.5">{{ programMajalisTitle }}</p>
          </div>
          <button @click="showProgramModal=false" class="text-slate-400 hover:text-white text-xl">✕</button>
        </div>

        <div class="p-5 space-y-3 max-h-[78vh] overflow-y-auto">

          <!-- Program items list -->
          <div v-for="(item, idx) in programItems" :key="idx"
               class="rounded-xl border p-4 space-y-3"
               style="background:rgba(124,58,237,0.05); border-color:rgba(124,58,237,0.2);">

            <!-- Row 1: order controls + type -->
            <div class="flex items-center gap-2">
              <div class="flex flex-col gap-0.5 shrink-0">
                <button @click="moveProgramItem(idx,-1)" :disabled="idx===0"
                  class="w-6 h-6 rounded text-xs text-gray-600 hover:text-white disabled:opacity-20"
                  style="background:rgba(255,255,255,0.05);">▲</button>
                <button @click="moveProgramItem(idx,1)" :disabled="idx===programItems.length-1"
                  class="w-6 h-6 rounded text-xs text-gray-600 hover:text-white disabled:opacity-20"
                  style="background:rgba(255,255,255,0.05);">▼</button>
              </div>
              <div class="w-8 h-8 rounded-lg flex items-center justify-center text-lg shrink-0"
                   style="background:rgba(124,58,237,0.15);">
                {{ PROGRAM_TYPE_ICONS[item.type] || '📿' }}
              </div>
              <select v-model="item.type" class="input flex-1 text-sm" style="min-height:38px;">
                <option v-for="t in PROGRAM_TYPES" :key="t.value" :value="t.value">
                  {{ t.icon }} {{ t.label }}
                </option>
              </select>
              <input v-model="item.time" type="time" class="input text-sm" style="width:110px; min-height:38px;" />
              <button @click="removeProgramItem(idx)"
                class="text-red-400 hover:text-red-300 text-lg shrink-0 px-1">✕</button>
            </div>

            <!-- Row 2: performer name -->
            <input v-model="item.performerName" class="input w-full text-sm" placeholder="Performer / Speaker name"
                   style="min-height:38px;" />

            <!-- Row 3: photo upload/url -->
            <ImageUploader
              v-model="item.imageUrl"
              label=""
              placeholder="Photo URL or Upload"
              previewClass="w-full max-h-28 object-cover object-top rounded-lg"
            />

            <!-- Row 3: description (optional) -->
            <input v-model="item.description" class="input w-full text-sm" placeholder="Note (optional)"
                   style="min-height:36px;" />
          </div>

          <!-- Add item button -->
          <button @click="addProgramItem"
            class="w-full py-2.5 rounded-xl border-2 border-dashed border-purple-800/40 text-purple-400 hover:border-purple-600 hover:text-purple-300 text-sm font-medium transition">
            + Add Item (Hadith / Marsiya / Salam / Noha...)
          </button>

          <!-- Save button -->
          <div class="flex gap-3 pt-1">
            <button @click="saveProgram" :disabled="savingProgram"
              class="flex-1 py-3 rounded-xl text-gray-900 font-bold text-sm disabled:opacity-50"
              style="background:linear-gradient(135deg,#6d28d9,#5b21b6);">
              {{ savingProgram ? 'Saving...' : '✅ Save Program' }}
            </button>
            <button @click="showProgramModal=false" class="btn-outline px-6">Cancel</button>
          </div>
        </div>
      </div>
    </div>

  </Teleport>
</template>
