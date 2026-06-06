<script setup>
import { ref, onMounted } from 'vue'
import { noticeApi } from '@/api/notices'
import NoticeBoard from '@/components/NoticeBoard.vue'

const notices = ref([])
const loading = ref(true)
const saving = ref(false)
const showForm = ref(false)
const editingId = ref(null)
const success = ref('')
const error = ref('')

const form = ref({
  title: '', content: '', category: 0, priority: 0,
  isPinned: false, scheduledAt: '', expiresAt: '',
  arabicContent: '', urduContent: '',
  notifyUsers: true, notifyRole: '', sendWhatsApp: false, sendEmail: false
})

const categories = [
  { value: 0, label: '📢 General' },
  { value: 1, label: '🍽️ Food' },
  { value: 2, label: '🚌 Transport' },
  { value: 3, label: '🕌 Prayer' },
  { value: 4, label: '🌙 Ziyarat' },
  { value: 5, label: '🚨 Emergency' },
  { value: 6, label: '🏨 Accommodation' },
  { value: 7, label: '🏥 Medical' }
]

const priorities = [
  { value: 0, label: 'Normal' },
  { value: 1, label: '⭐ Important' },
  { value: 2, label: '🔥 Urgent' }
]

const quickTemplates = [
  {
    title: 'Breakfast Time',
    content: 'Breakfast is served from 8:00 AM to 10:00 AM at the hotel dining hall. Please arrive on time.',
    urduContent: 'ناشتہ صبح 8:00 بجے سے 10:00 بجے تک ہوگا۔ براہ کرم وقت پر آئیں۔',
    category: 1, priority: 0
  },
  {
    title: 'Lunch Time',
    content: 'Lunch will be served from 1:00 PM to 3:00 PM. Please come to the dining area on time.',
    urduContent: 'دوپہر کا کھانا 1:00 بجے سے 3:00 بجے تک ہوگا۔',
    category: 1, priority: 0
  },
  {
    title: 'Dinner Time',
    content: 'Dinner is served from 7:00 PM to 9:00 PM at the hotel dining hall.',
    urduContent: 'رات کا کھانا شام 7:00 بجے سے 9:00 بجے تک ہوگا۔',
    category: 1, priority: 0
  },
  {
    title: 'Ziyarat Bus Departure',
    content: 'Ziyarat bus to Masjid Kufa departs at 5:00 PM. Please be at the hotel entrance by 4:45 PM. Do NOT be late.',
    urduContent: 'مسجد کوفہ کی زیارت کے لیے بس شام 5 بجے روانہ ہوگی۔ براہ کرم 4:45 بجے تک ہوٹل کے داخلی دروازے پر آ جائیں۔ دیر مت کریں۔',
    arabicContent: 'الحافلة ستتوجه إلى مسجد الكوفة في الساعة 5 مساءً. يرجى التواجد عند مدخل الفندق في الساعة 4:45 مساءً.',
    category: 4, priority: 1
  },
  {
    title: 'Bus Departure — Masjid Kufa',
    content: 'Arrive at Masjid Kufa bus stops on time. Bus departs at scheduled time without waiting.',
    urduContent: 'مسجد کوفہ کی بسوں کے لیے وقت پر پہنچیں۔ بس مقررہ وقت پر روانہ ہوگی۔',
    category: 2, priority: 1
  },
  {
    title: 'Emergency Alert',
    content: 'IMPORTANT: Please stay with your group at all times. If separated, contact your group leader immediately.',
    urduContent: 'اہم: براہ کرم ہر وقت اپنے گروپ کے ساتھ رہیں۔ الگ ہونے کی صورت میں فوری طور پر اپنے گروپ لیڈر سے رابطہ کریں۔',
    category: 5, priority: 2
  }
]

function applyTemplate(t) {
  form.value.title = t.title
  form.value.content = t.content
  form.value.urduContent = t.urduContent || ''
  form.value.arabicContent = t.arabicContent || ''
  form.value.category = t.category
  form.value.priority = t.priority
  showForm.value = true
}

function resetForm() {
  form.value = { title: '', content: '', category: 0, priority: 0, isPinned: false, scheduledAt: '', expiresAt: '', arabicContent: '', urduContent: '' }
  editingId.value = null
}

function startEdit(notice) {
  editingId.value = notice.id
  form.value = {
    title: notice.title,
    content: notice.content,
    category: notice.category,
    priority: notice.priority,
    isPinned: notice.isPinned,
    scheduledAt: notice.scheduledAt ? notice.scheduledAt.slice(0, 16) : '',
    expiresAt: notice.expiresAt ? notice.expiresAt.slice(0, 16) : '',
    arabicContent: notice.arabicContent || '',
    urduContent: notice.urduContent || ''
  }
  showForm.value = true
}

async function load() {
  loading.value = true
  try {
    const res = await noticeApi.getAll()
    notices.value = res.data.data || []
  } finally { loading.value = false }
}

async function save() {
  if (!form.value.title || !form.value.content) { error.value = 'Title and content are required.'; return }
  saving.value = true; error.value = ''
  try {
    const payload = {
      ...form.value,
      category: parseInt(form.value.category),
      priority: parseInt(form.value.priority),
      scheduledAt: form.value.scheduledAt || null,
      expiresAt: form.value.expiresAt || null,
      arabicContent: form.value.arabicContent || null,
      urduContent: form.value.urduContent || null
    }
    if (editingId.value) {
      await noticeApi.update(editingId.value, payload)
      success.value = 'Notice updated!'
    } else {
      await noticeApi.create(payload)
      const channels = ['In-App']
      if (payload.notifyUsers && payload.sendWhatsApp) channels.push('WhatsApp')
      if (payload.notifyUsers && payload.sendEmail) channels.push('Email')
      success.value = `Notice posted! Notifications sent via: ${channels.join(', ')}`
    }
    showForm.value = false
    resetForm()
    await load()
  } catch (e) {
    error.value = e.response?.data?.message || 'Failed to save.'
  } finally { saving.value = false }
}

async function deleteNotice(id) {
  if (!confirm('Delete this notice?')) return
  try {
    await noticeApi.delete(id)
    success.value = 'Notice deleted.'
    await load()
  } catch { error.value = 'Failed to delete.' }
}

async function toggleNotice(id) {
  try {
    await noticeApi.toggle(id)
    await load()
  } catch {}
}

onMounted(load)
</script>

<template>
  <div class="space-y-6">

    <div class="flex items-center justify-between">
      <div>
        <h1 class="text-2xl font-bold text-gray-900">Notice Board</h1>
        <p class="text-gray-700 text-sm">Post announcements visible to all users</p>
      </div>
      <button @click="showForm = !showForm; resetForm()" class="btn-primary">
        {{ showForm ? 'Cancel' : '+ New Notice' }}
      </button>
    </div>

    <div v-if="success" class="bg-green-900/50 border border-green-700 text-green-300 rounded-lg px-4 py-3 flex justify-between text-sm">
      {{ success }}<button @click="success=''" class="text-green-500">✕</button>
    </div>
    <div v-if="error" class="bg-red-900/50 border border-red-700 text-red-300 rounded-lg px-4 py-3 flex justify-between text-sm">
      {{ error }}<button @click="error=''" class="text-red-500">✕</button>
    </div>

    <div class="grid grid-cols-1 xl:grid-cols-3 gap-6">

      <!-- Left: Form + Templates -->
      <div class="xl:col-span-1 space-y-4">

        <!-- Quick Templates -->
        <div class="card">
          <h2 class="text-base font-semibold text-gray-900 mb-3">Quick Templates</h2>
          <div class="space-y-2">
            <button v-for="t in quickTemplates" :key="t.title"
              @click="applyTemplate(t)"
              class="w-full text-left p-3 rounded-lg border border-gray-200 hover:border-primary-500 hover:bg-gray-50 transition-colors group">
              <p class="text-gray-900 text-xs font-medium">
                {{ categories.find(c => c.value === t.category)?.label }} — {{ t.title }}
              </p>
              <p class="text-gray-600 text-xs mt-0.5 line-clamp-1">{{ t.content }}</p>
            </button>
          </div>
        </div>

        <!-- Form -->
        <div v-if="showForm" class="card space-y-4">
          <h2 class="text-base font-semibold text-gray-900">{{ editingId ? 'Edit Notice' : 'New Notice' }}</h2>

          <div>
            <label class="label">Title *</label>
            <input v-model="form.title" type="text" class="input" placeholder="e.g. Breakfast Time" />
          </div>

          <div class="grid grid-cols-2 gap-3">
            <div>
              <label class="label">Category</label>
              <select v-model.number="form.category" class="input">
                <option v-for="c in categories" :key="c.value" :value="c.value">{{ c.label }}</option>
              </select>
            </div>
            <div>
              <label class="label">Priority</label>
              <select v-model.number="form.priority" class="input">
                <option v-for="p in priorities" :key="p.value" :value="p.value">{{ p.label }}</option>
              </select>
            </div>
          </div>

          <div>
            <label class="label">Content (English) *</label>
            <textarea v-model="form.content" class="input h-24 resize-none" placeholder="Write the announcement..."></textarea>
          </div>

          <div>
            <label class="label">Urdu Content (اردو)</label>
            <textarea v-model="form.urduContent" class="input h-20 resize-none text-right font-arabic" placeholder="اردو میں لکھیں..."></textarea>
          </div>

          <div>
            <label class="label">Arabic Content (عربي)</label>
            <textarea v-model="form.arabicContent" class="input h-16 resize-none text-right font-arabic" placeholder="اكتب بالعربية..."></textarea>
          </div>

          <div class="grid grid-cols-2 gap-3">
            <div>
              <label class="label">Scheduled At</label>
              <input v-model="form.scheduledAt" type="datetime-local" class="input" />
            </div>
            <div>
              <label class="label">Expires At</label>
              <input v-model="form.expiresAt" type="datetime-local" class="input" />
            </div>
          </div>

          <label class="flex items-center gap-2 cursor-pointer">
            <input v-model="form.isPinned" type="checkbox" class="w-4 h-4" />
            <span class="text-gray-600 text-sm">📌 Pin to top</span>
          </label>

          <!-- Notification delivery options (only for new notices) -->
          <div v-if="!editingId" class="rounded-xl border border-gray-200 bg-gray-50 p-4 space-y-3">
            <label class="flex items-center gap-2 cursor-pointer">
              <input v-model="form.notifyUsers" type="checkbox" class="w-4 h-4 accent-primary-500" />
              <span class="text-gray-900 text-sm font-medium">🔔 Notify users when posted</span>
            </label>
            <template v-if="form.notifyUsers">
              <div>
                <label class="text-xs text-gray-600 block mb-1">Send to</label>
                <select v-model="form.notifyRole" class="input text-sm py-1.5">
                  <option value="">All Users</option>
                  <option value="Pilgrim">Pilgrims only</option>
                  <option value="Volunteer">Volunteers only</option>
                  <option value="VolunteerManager">Volunteer Managers only</option>
                </select>
              </div>
              <div class="flex gap-3 flex-wrap">
                <label class="flex items-center gap-2 cursor-pointer bg-gray-50 border rounded-lg px-3 py-2 text-sm transition-colors"
                       :class="form.sendWhatsApp ? 'border-emerald-500 bg-emerald-50' : 'border-gray-200'">
                  <input type="checkbox" v-model="form.sendWhatsApp" class="w-4 h-4 accent-emerald-500" />
                  💬 Also send WhatsApp
                </label>
                <label class="flex items-center gap-2 cursor-pointer bg-gray-50 border rounded-lg px-3 py-2 text-sm transition-colors"
                       :class="form.sendEmail ? 'border-blue-500 bg-blue-50' : 'border-gray-200'">
                  <input type="checkbox" v-model="form.sendEmail" class="w-4 h-4 accent-blue-500" />
                  📧 Also send Email
                </label>
              </div>
            </template>
          </div>

          <div class="flex gap-2 pt-2">
            <button @click="save" :disabled="saving" class="btn-primary flex-1">
              {{ saving ? 'Saving...' : (editingId ? 'Update Notice' : 'Post Notice') }}
            </button>
            <button @click="showForm = false; resetForm()" class="px-4 text-gray-700 hover:text-gray-900 text-sm">Cancel</button>
          </div>
        </div>
      </div>

      <!-- Right: Active Notices + Management Table -->
      <div class="xl:col-span-2 space-y-6">

        <!-- Live Preview -->
        <div class="card">
          <h2 class="text-base font-semibold text-gray-900 mb-4">Live Notice Board</h2>
          <NoticeBoard />
        </div>

        <!-- Management Table -->
        <div class="card overflow-hidden p-0">
          <div class="px-4 py-3 border-b border-gray-200">
            <h2 class="text-base font-semibold text-gray-900">All Notices</h2>
          </div>
          <div v-if="loading" class="text-center py-8 text-gray-700 text-sm">Loading...</div>
          <table v-else class="w-full text-sm">
            <thead class="bg-gray-50 text-xs text-gray-600 uppercase">
              <tr>
                <th class="text-left px-4 py-2 font-medium">Title</th>
                <th class="text-left px-4 py-2 font-medium hidden md:table-cell">Category</th>
                <th class="text-left px-4 py-2 font-medium hidden lg:table-cell">Priority</th>
                <th class="text-left px-4 py-2 font-medium">Status</th>
                <th class="px-4 py-2"></th>
              </tr>
            </thead>
            <tbody class="divide-y divide-gray-200">
              <tr v-for="n in notices" :key="n.id" class="hover:bg-gray-50">
                <td class="px-4 py-2.5">
                  <div class="flex items-center gap-2">
                    <span v-if="n.isPinned" class="text-gold-500 text-xs">📌</span>
                    <span class="text-gray-900 text-sm">{{ n.title }}</span>
                  </div>
                </td>
                <td class="px-4 py-2.5 text-gray-700 hidden md:table-cell">{{ n.category }}</td>
                <td class="px-4 py-2.5 hidden lg:table-cell">
                  <span :class="n.priority === 'Urgent' ? 'text-red-400' : n.priority === 'Important' ? 'text-gold-400' : 'text-slate-500'" class="text-xs">
                    {{ n.priority }}
                  </span>
                </td>
                <td class="px-4 py-2.5">
                  <span :class="n.isActive ? 'text-green-700 bg-green-100' : 'text-gray-700 bg-gray-100'" class="text-xs px-2 py-0.5 rounded">
                    {{ n.isActive ? 'Active' : 'Hidden' }}
                  </span>
                </td>
                <td class="px-4 py-2.5">
                  <div class="flex items-center gap-1 justify-end">
                    <button @click="startEdit(n)" class="text-xs text-blue-400 hover:text-blue-300 px-2 py-1 rounded hover:bg-blue-950">Edit</button>
                    <button @click="toggleNotice(n.id)" class="text-xs text-gray-700 hover:text-gray-900 px-2 py-1 rounded hover:bg-gray-100">
                      {{ n.isActive ? 'Hide' : 'Show' }}
                    </button>
                    <button @click="deleteNotice(n.id)" class="text-xs text-red-500 hover:text-red-400 px-2 py-1 rounded hover:bg-red-950">Delete</button>
                  </div>
                </td>
              </tr>
            </tbody>
          </table>
          <div v-if="!loading && !notices.length" class="text-center py-8 text-slate-500 text-sm">No notices yet. Post your first notice!</div>
        </div>
      </div>
    </div>
  </div>
</template>
