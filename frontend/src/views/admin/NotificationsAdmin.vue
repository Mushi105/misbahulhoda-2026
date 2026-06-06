<script setup>
import { ref, onMounted, computed } from 'vue'
import { adminApi } from '@/api'
import api from '@/api/client'

const users = ref([])
const history = ref([])
const sending = ref(false)
const success = ref('')
const error = ref('')
const searchQuery = ref('')
const filterRole = ref('')

const form = ref({ userId: '', title: '', message: '', type: 1, event: 7, broadcast: false, broadcastRole: '', sendWhatsApp: false, sendEmail: false })

// Match backend enums exactly
const notificationTypes = [
  { value: 1, label: '📱 Push Notification' },
  { value: 2, label: '💬 WhatsApp' },
  { value: 3, label: '📧 Email' },
  { value: 4, label: '📱 SMS' },
]

const notificationEvents = [
  { value: 7, label: '📢 General Announcement' },
  { value: 1, label: '✅ Application Approved' },
  { value: 2, label: '🏠 Room Allocation' },
  { value: 3, label: '🚌 Bus Departure' },
  { value: 4, label: '📿 Majalis Reminder' },
  { value: 5, label: '🍽 Food Timing' },
  { value: 6, label: '🚨 Emergency Alert' },
]

const quickTemplates = [
  {
    icon: '✅', label: 'App Approved',
    title: 'Application Approved — Mubarak! 🌹',
    message: 'Assalamu Alaikum! Your Arbaeen 2026 application has been approved. Please log in to your portal to view your details. Labbayk Ya Hussain!',
    type: 1, event: 1
  },
  {
    icon: '🏠', label: 'Room Allocated',
    title: 'Room Allocated',
    message: 'Your room has been allocated. Please check your pilgrim portal for room number and hotel details.',
    type: 1, event: 2
  },
  {
    icon: '🚌', label: 'Bus Departure',
    title: 'Bus Departure — 30 Minutes',
    message: 'Your bus departs in 30 minutes. Please be at the designated pickup point with your luggage. Do not be late.',
    type: 1, event: 3
  },
  {
    icon: '📿', label: 'Majalis Tonight',
    title: 'Majalis Tonight at 8:00 PM',
    message: 'Join us for tonight\'s Majalis at 8:00 PM. Scholar: Sayyid Ali Zaidi. Venue: Hotel lobby hall.',
    type: 1, event: 4
  },
  {
    icon: '🍽', label: 'Dinner Ready',
    title: 'Dinner is Ready',
    message: 'Dinner is being served at the hotel dining hall. Please come within the next 45 minutes.',
    type: 1, event: 5
  },
  {
    icon: '🚨', label: 'Emergency',
    title: '⚠️ Emergency Alert',
    message: 'URGENT: Please stay at your current location and await further instructions from your group leader.',
    type: 1, event: 6
  },
  {
    icon: '📋', label: 'Docs Needed',
    title: 'Action Required — Documents',
    message: 'Please upload your passport and visa documents to complete your registration. Deadline: 48 hours.',
    type: 1, event: 7
  },
  {
    icon: '✈️', label: 'Flight Update',
    title: 'Flight Information Updated',
    message: 'Your flight details have been updated. Please check your pilgrim portal for the latest departure time and terminal information.',
    type: 1, event: 7
  },
]

const filteredUsers = computed(() => {
  let list = users.value
  if (filterRole.value) list = list.filter(u => u.role === filterRole.value)
  if (searchQuery.value.trim()) {
    const q = searchQuery.value.toLowerCase()
    list = list.filter(u => u.fullName?.toLowerCase().includes(q) || u.email?.toLowerCase().includes(q))
  }
  return list
})

const roleColors = {
  SuperAdmin: 'bg-purple-900 text-purple-300',
  Admin: 'bg-blue-900 text-blue-300',
  Pilgrim: 'bg-primary-900 text-primary-300',
  Volunteer: 'bg-gold-900 text-gold-300',
  Driver: 'bg-orange-900 text-orange-300',
}

function applyTemplate(t) {
  form.value.title = t.title
  form.value.message = t.message
  form.value.type = t.type
  form.value.event = t.event
}

function selectUser(u) {
  form.value.userId = u.id
  form.value.broadcast = false
}

async function send() {
  if (!form.value.title.trim() || !form.value.message.trim()) {
    error.value = 'Title and message are required.'
    return
  }
  if (!form.value.broadcast && !form.value.userId) {
    error.value = 'Select a recipient or enable broadcast.'
    return
  }
  sending.value = true; error.value = ''; success.value = ''
  try {
    if (form.value.broadcast) {
      const res = await api.post('/notifications/broadcast', {
        title: form.value.title,
        message: form.value.message,
        event: form.value.event,
        role: form.value.broadcastRole || null,
        sendWhatsApp: form.value.sendWhatsApp,
        sendEmail: form.value.sendEmail,
      })
      const count = res.data.data?.count || '?'
      success.value = `Broadcast sent to ${count} users!` +
        (form.value.sendWhatsApp ? ' + WhatsApp' : '') +
        (form.value.sendEmail ? ' + Email' : '')
    } else {
      await api.post('/notifications/send', {
        userId: form.value.userId,
        title: form.value.title,
        message: form.value.message,
        type: form.value.type,
        event: form.value.event,
        sendWhatsApp: form.value.sendWhatsApp,
        sendEmail: form.value.sendEmail,
      })
      const recipient = users.value.find(u => u.id === form.value.userId)
      success.value = `Sent to ${recipient?.fullName || 'user'}` +
        (form.value.sendWhatsApp ? ' + WhatsApp' : '') +
        (form.value.sendEmail ? ' + Email' : '') + '!'
    }
    history.value.unshift({
      title: form.value.title,
      message: form.value.message,
      recipient: form.value.broadcast
        ? `Broadcast (${form.value.broadcastRole || 'All'}) — ${form.value.broadcastRole ? users.value.filter(u => u.role === form.value.broadcastRole).length : users.value.length} users`
        : users.value.find(u => u.id === form.value.userId)?.fullName,
      time: new Date().toLocaleTimeString(),
      event: notificationEvents.find(e => e.value === form.value.event)?.label,
    })
    form.value.userId = ''; form.value.title = ''; form.value.message = ''
  } catch (e) {
    error.value = e.response?.data?.message || 'Failed to send.'
  } finally { sending.value = false }
}

onMounted(async () => {
  try {
    const res = await adminApi.getUsers({ pageSize: 500 })
    users.value = res.data.data?.items || []
  } catch {}
})
</script>

<template>
  <div class="space-y-6">
    <div class="flex items-start justify-between">
      <div>
        <h1 class="text-2xl font-bold text-white">Send Notifications</h1>
        <p class="text-slate-400 text-sm mt-1">Send to individual users or broadcast to entire groups</p>
      </div>
      <div class="text-right">
        <p class="text-slate-400 text-xs">Total Users</p>
        <p class="text-2xl font-bold text-primary-400">{{ users.length }}</p>
      </div>
    </div>

    <div v-if="success" class="bg-green-900/50 border border-green-700 text-green-300 rounded-lg px-4 py-3 flex justify-between text-sm">
      {{ success }}<button @click="success=''" class="text-green-500 ml-4">✕</button>
    </div>
    <div v-if="error" class="bg-red-900/50 border border-red-700 text-red-300 rounded-lg px-4 py-3 flex justify-between text-sm">
      {{ error }}<button @click="error=''" class="text-red-500 ml-4">✕</button>
    </div>

    <div class="grid grid-cols-1 xl:grid-cols-3 gap-6">

      <!-- LEFT: Users + Templates -->
      <div class="space-y-5">

        <!-- Quick Templates -->
        <div class="card">
          <h2 class="text-sm font-bold text-white uppercase tracking-wide mb-3">Quick Templates</h2>
          <div class="space-y-2">
            <button v-for="t in quickTemplates" :key="t.label"
              @click="applyTemplate(t)"
              class="w-full text-left p-2.5 rounded-lg border border-dark-600 hover:border-primary-700 hover:bg-dark-800 transition-all group">
              <div class="flex items-center gap-2">
                <span class="text-base">{{ t.icon }}</span>
                <span class="text-white text-xs font-medium group-hover:text-primary-300">{{ t.label }}</span>
              </div>
            </button>
          </div>
        </div>

        <!-- Broadcast Toggle -->
        <div class="card">
          <div class="flex items-center justify-between mb-3">
            <h2 class="text-sm font-bold text-white uppercase tracking-wide">Broadcast Mode</h2>
            <button @click="form.broadcast = !form.broadcast"
              :class="['w-11 h-6 rounded-full transition-colors relative', form.broadcast ? 'bg-primary-600' : 'bg-dark-600']">
              <span :class="['absolute top-0.5 w-5 h-5 bg-white rounded-full transition-transform shadow', form.broadcast ? 'translate-x-5' : 'translate-x-0.5']"></span>
            </button>
          </div>
          <div v-if="form.broadcast" class="space-y-3">
            <p class="text-xs text-gold-400">Send to all users in a group at once</p>
            <div>
              <label class="label text-xs">Send to Role</label>
              <select v-model="form.broadcastRole" class="input text-sm">
                <option value="">All Users ({{ users.length }})</option>
                <option value="Pilgrim">All Pilgrims ({{ users.filter(u=>u.role==='Pilgrim').length }})</option>
                <option value="Volunteer">All Volunteers ({{ users.filter(u=>u.role==='Volunteer').length }})</option>
                <option value="Admin">All Admins ({{ users.filter(u=>u.role==='Admin').length }})</option>
              </select>
            </div>
          </div>
          <p v-else class="text-xs text-slate-500">Enable to send to all users in a group</p>
        </div>

        <!-- User Search (individual mode) -->
        <div v-if="!form.broadcast" class="card">
          <h2 class="text-sm font-bold text-white uppercase tracking-wide mb-3">Select Recipient</h2>
          <input v-model="searchQuery" class="input text-sm mb-3" placeholder="🔍 Search by name or email..." />
          <div class="flex gap-2 mb-3 flex-wrap">
            <button @click="filterRole=''" :class="['text-xs px-2 py-1 rounded-full border', !filterRole ? 'bg-primary-700 border-primary-600 text-white' : 'border-dark-600 text-slate-400']">All</button>
            <button @click="filterRole='Pilgrim'" :class="['text-xs px-2 py-1 rounded-full border', filterRole==='Pilgrim' ? 'bg-primary-700 border-primary-600 text-white' : 'border-dark-600 text-slate-400']">Pilgrims</button>
            <button @click="filterRole='Volunteer'" :class="['text-xs px-2 py-1 rounded-full border', filterRole==='Volunteer' ? 'bg-primary-700 border-primary-600 text-white' : 'border-dark-600 text-slate-400']">Volunteers</button>
          </div>
          <div class="space-y-1.5 max-h-64 overflow-y-auto">
            <button v-for="u in filteredUsers" :key="u.id"
              @click="selectUser(u)"
              :class="['w-full text-left p-2.5 rounded-lg border transition-all', form.userId===u.id ? 'border-primary-500 bg-primary-900/30' : 'border-dark-600 hover:border-dark-500 hover:bg-dark-800']">
              <div class="flex items-center gap-2.5">
                <div class="w-7 h-7 rounded-full bg-primary-800 flex items-center justify-center text-xs font-bold text-white flex-shrink-0">
                  {{ u.fullName?.[0] || '?' }}
                </div>
                <div class="flex-1 min-w-0">
                  <p class="text-white text-xs font-medium truncate">{{ u.fullName }}</p>
                  <p class="text-slate-500 text-xs truncate">{{ u.email }}</p>
                </div>
                <span :class="['text-xs px-1.5 py-0.5 rounded', roleColors[u.role] || 'bg-slate-800 text-slate-400']">{{ u.role }}</span>
              </div>
            </button>
          </div>
        </div>

      </div>

      <!-- RIGHT: Compose + History -->
      <div class="xl:col-span-2 space-y-5">

        <!-- Compose -->
        <div class="card space-y-4">
          <div class="flex items-center justify-between">
            <h2 class="text-base font-semibold text-white">Compose Message</h2>
            <div v-if="form.broadcast" class="text-xs bg-gold-900 text-gold-400 border border-gold-800 px-2.5 py-1 rounded-full">
              📢 Broadcast to {{ form.broadcastRole || 'All' }} — {{ form.broadcastRole ? users.filter(u=>u.role===form.broadcastRole).length : users.length }} users
            </div>
            <div v-else-if="form.userId" class="text-xs bg-primary-900 text-primary-400 border border-primary-800 px-2.5 py-1 rounded-full">
              ✓ {{ users.find(u=>u.id===form.userId)?.fullName }}
            </div>
            <div v-else class="text-xs text-slate-500">← Select recipient first</div>
          </div>

          <div class="grid grid-cols-2 gap-4">
            <div>
              <label class="label">Notification Type</label>
              <select v-model.number="form.type" class="input">
                <option v-for="t in notificationTypes" :key="t.value" :value="t.value">{{ t.label }}</option>
              </select>
            </div>
            <div>
              <label class="label">Event Category</label>
              <select v-model.number="form.event" class="input">
                <option v-for="e in notificationEvents" :key="e.value" :value="e.value">{{ e.label }}</option>
              </select>
            </div>
          </div>

          <div>
            <label class="label">Title *</label>
            <input v-model="form.title" type="text" class="input" placeholder="Notification title" maxlength="100" />
            <p class="text-xs text-slate-600 text-right mt-1">{{ form.title.length }}/100</p>
          </div>

          <div>
            <label class="label">Message *</label>
            <textarea v-model="form.message" class="input h-32 resize-none" placeholder="Write your message here... Be clear and specific." maxlength="500"></textarea>
            <p class="text-xs text-slate-600 text-right mt-1">{{ form.message.length }}/500</p>
          </div>

          <!-- Delivery Channels -->
          <div>
            <label class="label">Also deliver via</label>
            <div class="flex gap-3 flex-wrap">
              <label class="flex items-center gap-2 cursor-pointer select-none bg-dark-700 border border-dark-600 rounded-lg px-3 py-2 hover:border-emerald-700 transition-colors"
                     :class="form.sendWhatsApp ? 'border-emerald-600 bg-emerald-950/30' : ''">
                <input type="checkbox" v-model="form.sendWhatsApp" class="w-4 h-4 accent-emerald-500" />
                <span class="text-sm">💬 WhatsApp</span>
                <span class="text-xs text-slate-500">(registered numbers)</span>
              </label>
              <label class="flex items-center gap-2 cursor-pointer select-none bg-dark-700 border border-dark-600 rounded-lg px-3 py-2 hover:border-blue-700 transition-colors"
                     :class="form.sendEmail ? 'border-blue-600 bg-blue-950/30' : ''">
                <input type="checkbox" v-model="form.sendEmail" class="w-4 h-4 accent-blue-500" />
                <span class="text-sm">📧 Email</span>
                <span class="text-xs text-slate-500">(requires SMTP config)</span>
              </label>
            </div>
          </div>

          <button @click="send" :disabled="sending"
            :class="['w-full py-3 rounded-lg font-semibold text-sm transition-all', form.broadcast ? 'bg-gold-600 hover:bg-gold-500 text-dark-950' : 'btn-primary', sending ? 'opacity-60 cursor-not-allowed' : '']">
            <span v-if="sending">Sending...</span>
            <span v-else-if="form.broadcast">📢 Broadcast to {{ form.broadcastRole || 'All Users' }}{{ form.sendWhatsApp ? ' + WhatsApp' : '' }}{{ form.sendEmail ? ' + Email' : '' }}</span>
            <span v-else>📨 Send{{ form.sendWhatsApp ? ' + WhatsApp' : '' }}{{ form.sendEmail ? ' + Email' : '' }}</span>
          </button>
        </div>

        <!-- Sent History -->
        <div v-if="history.length" class="card">
          <h2 class="text-sm font-bold text-white uppercase tracking-wide mb-3">Recently Sent</h2>
          <div class="space-y-2">
            <div v-for="(h, i) in history.slice(0,5)" :key="i"
              class="flex items-start gap-3 bg-dark-700 rounded-lg p-3">
              <div class="w-8 h-8 rounded-full bg-primary-900 flex items-center justify-center text-primary-400 text-sm flex-shrink-0">✓</div>
              <div class="flex-1 min-w-0">
                <p class="text-white text-sm font-medium">{{ h.title }}</p>
                <p class="text-slate-400 text-xs mt-0.5 truncate">To: {{ h.recipient }}</p>
                <p class="text-slate-600 text-xs">{{ h.event }} · {{ h.time }}</p>
              </div>
            </div>
          </div>
        </div>

      </div>
    </div>
  </div>
</template>
