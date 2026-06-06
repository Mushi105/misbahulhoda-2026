<script setup>
import { ref, onMounted, computed } from 'vue'
import { volunteerApi } from '@/api'
import { useAuthStore } from '@/stores/auth'
import { useNotificationStore } from '@/stores/notifications'
import NoticeBoard from '@/components/NoticeBoard.vue'

const auth = useAuthStore()
const notifStore = useNotificationStore()
const tasks = ref([])
const volunteerInfo = ref(null)
const loading = ref(true)
const checkingIn = ref(false)
const updatingStatus = ref(false)
const error = ref('')
const success = ref('')

const statusColor = {
  Assigned: 'text-gold-400 bg-gold-900/50 border-gold-800',
  InProgress: 'text-blue-400 bg-blue-900/50 border-blue-800',
  Completed: 'text-green-400 bg-green-900/50 border-green-800',
  Cancelled: 'text-slate-400 bg-slate-800 border-slate-700'
}

const volunteerStatusConfig = {
  Available: { color: 'text-green-400', label: 'Available', value: 1 },
  Busy: { color: 'text-gold-400', label: 'Busy', value: 2 },
  Offline: { color: 'text-slate-400', label: 'Offline', value: 3 },
  EmergencyAssigned: { color: 'text-red-400', label: 'Emergency', value: 4 }
}

const completedTasks = computed(() => tasks.value.filter(t => t.status === 'Completed').length)
const pendingTasks = computed(() => tasks.value.filter(t => t.status === 'Assigned' || t.status === 'InProgress').length)
const emergencyTasks = computed(() => tasks.value.filter(t => t.isEmergency && t.status !== 'Completed').length)

async function load() {
  loading.value = true
  try {
    const res = await volunteerApi.getMe()
    const mine = res.data.data
    if (mine) {
      volunteerInfo.value = mine
      tasks.value = mine.tasks || []
    }
  } catch (e) {
    if (e.response?.status === 404) {
      volunteerInfo.value = null
    } else {
      error.value = 'Failed to load data.'
    }
  } finally {
    loading.value = false
  }
}

async function checkIn() {
  if (!volunteerInfo.value) return
  checkingIn.value = true
  try {
    await volunteerApi.checkIn(volunteerInfo.value.id)
    volunteerInfo.value.isCheckedIn = true
    success.value = 'Checked in successfully! Your shift has started.'
  } catch (e) {
    error.value = 'Check-in failed. You may already be checked in.'
  } finally { checkingIn.value = false }
}

async function checkOut() {
  if (!volunteerInfo.value) return
  checkingIn.value = true
  try {
    await volunteerApi.checkOut(volunteerInfo.value.id)
    volunteerInfo.value.isCheckedIn = false
    success.value = 'Checked out. Jazak Allah Khair for your service!'
  } catch (e) {
    error.value = 'Check-out failed.'
  } finally { checkingIn.value = false }
}

async function completeTask(taskId) {
  try {
    await volunteerApi.completeTask(taskId)
    const t = tasks.value.find(t => t.id === taskId)
    if (t) t.status = 'Completed'
    success.value = 'Task marked as completed!'
  } catch {
    error.value = 'Failed to complete task.'
  }
}

async function updateStatus(status) {
  if (!volunteerInfo.value) return
  updatingStatus.value = true
  try {
    await volunteerApi.updateStatus(volunteerInfo.value.id, status)
    volunteerInfo.value.status = status
    success.value = `Status updated to ${status}`
  } catch { error.value = 'Status update failed.' }
  finally { updatingStatus.value = false }
}

function getShiftStatus() {
  if (!volunteerInfo.value?.shiftStart || !volunteerInfo.value?.shiftEnd) return null
  const now = new Date()
  const [sh, sm] = volunteerInfo.value.shiftStart.split(':').map(Number)
  const [eh, em] = volunteerInfo.value.shiftEnd.split(':').map(Number)
  const start = new Date(); start.setHours(sh, sm, 0)
  const end = new Date(); end.setHours(eh, em, 0)
  if (now >= start && now <= end) return 'active'
  if (now < start) return 'upcoming'
  return 'ended'
}

onMounted(async () => {
  await load()
  notifStore.fetchUnreadCount()
})
</script>

<template>
  <div class="space-y-6">
    <!-- Header -->
    <div class="flex items-start justify-between">
      <div>
        <h1 class="text-2xl font-bold text-white">Volunteer Dashboard</h1>
        <p class="text-slate-400 text-sm">Welcome, {{ auth.user?.fullName }}</p>
      </div>
      <div class="flex items-center gap-2">
        <button v-if="volunteerInfo && !volunteerInfo.isCheckedIn"
          @click="checkIn" :disabled="checkingIn"
          class="btn-primary text-sm">
          {{ checkingIn ? '...' : '✅ Check In' }}
        </button>
        <template v-else-if="volunteerInfo?.isCheckedIn">
          <span class="text-xs bg-emerald-900/50 border border-emerald-700 text-emerald-400 rounded-lg px-3 py-1.5">● On Duty</span>
          <button @click="checkOut" :disabled="checkingIn"
            class="text-xs bg-slate-800 hover:bg-slate-700 border border-slate-600 text-slate-300 rounded-lg px-3 py-1.5 transition-colors">
            {{ checkingIn ? '...' : '🚪 Check Out' }}
          </button>
        </template>
      </div>
    </div>

    <div v-if="success" class="bg-green-900/50 border border-green-700 text-green-300 rounded-lg px-4 py-3 flex justify-between text-sm">
      {{ success }}<button @click="success=''" class="text-green-500">✕</button>
    </div>
    <div v-if="error" class="bg-red-900/50 border border-red-700 text-red-300 rounded-lg px-4 py-3 flex justify-between text-sm">
      {{ error }}<button @click="error=''" class="text-red-500">✕</button>
    </div>

    <div v-if="loading" class="text-center py-16 text-slate-400">Loading...</div>

    <template v-else>

      <!-- No Profile -->
      <div v-if="!volunteerInfo" class="card text-center py-10">
        <div class="text-5xl mb-3">🤝</div>
        <h2 class="text-white font-semibold text-lg">Not Registered as Volunteer</h2>
        <p class="text-slate-400 text-sm mt-2">Contact an admin to register you as a volunteer.</p>
      </div>

      <template v-else>
        <!-- Stats -->
        <div class="grid grid-cols-2 md:grid-cols-4 gap-4">
          <div class="card text-center py-4">
            <div class="text-3xl font-bold text-white">{{ tasks.length }}</div>
            <div class="text-slate-400 text-xs mt-1">Total Tasks</div>
          </div>
          <div class="card text-center py-4">
            <div class="text-3xl font-bold text-gold-400">{{ pendingTasks }}</div>
            <div class="text-slate-400 text-xs mt-1">Pending</div>
          </div>
          <div class="card text-center py-4">
            <div class="text-3xl font-bold text-green-400">{{ completedTasks }}</div>
            <div class="text-slate-400 text-xs mt-1">Completed</div>
          </div>
          <div class="card text-center py-4">
            <div :class="['text-3xl font-bold', emergencyTasks > 0 ? 'text-red-400' : 'text-slate-500']">{{ emergencyTasks }}</div>
            <div class="text-slate-400 text-xs mt-1">Emergency</div>
          </div>
        </div>

        <!-- Volunteer Info & Status -->
        <div class="grid grid-cols-1 lg:grid-cols-3 gap-6">
          <div class="card col-span-1 space-y-4">
            <h2 class="text-base font-semibold text-white border-b border-dark-600 pb-2">My Status</h2>
            <div class="space-y-3 text-sm">
              <div class="flex justify-between">
                <span class="text-slate-400">Status</span>
                <span :class="volunteerStatusConfig[volunteerInfo.status]?.color || 'text-slate-400'">
                  ● {{ volunteerInfo.status }}
                </span>
              </div>
              <div v-if="volunteerInfo.zone" class="flex justify-between">
                <span class="text-slate-400">Zone</span>
                <span class="text-primary-400 font-medium">{{ volunteerInfo.zone.name }}</span>
              </div>
              <div v-if="volunteerInfo.assignedArea" class="flex justify-between">
                <span class="text-slate-400">Area</span>
                <span class="text-white">{{ volunteerInfo.assignedArea }}</span>
              </div>
              <div v-if="volunteerInfo.shiftStart" class="flex justify-between">
                <span class="text-slate-400">Shift</span>
                <span :class="getShiftStatus() === 'active' ? 'text-green-400' : 'text-slate-300'">
                  {{ volunteerInfo.shiftStart }} – {{ volunteerInfo.shiftEnd }}
                  <span v-if="getShiftStatus() === 'active'" class="text-xs ml-1">(Active)</span>
                </span>
              </div>
              <div class="flex justify-between">
                <span class="text-slate-400">Tasks Done</span>
                <span class="text-primary-400 font-semibold">{{ volunteerInfo.totalTasksCompleted }}</span>
              </div>
            </div>
            <!-- Change Status -->
            <div class="pt-2 border-t border-dark-600">
              <p class="text-xs text-slate-500 mb-2">Update your status:</p>
              <div class="grid grid-cols-2 gap-2">
                <button v-for="(s, key) in volunteerStatusConfig" :key="key"
                  @click="updateStatus(key)"
                  :disabled="updatingStatus || volunteerInfo.status === key"
                  :class="['text-xs py-1.5 rounded-lg border transition-colors',
                    volunteerInfo.status === key
                      ? 'border-primary-600 bg-primary-900/40 text-primary-400'
                      : 'border-dark-600 hover:border-dark-500 text-slate-400 hover:text-white']">
                  {{ s.label }}
                </button>
              </div>
            </div>
          </div>

          <!-- Assigned Pilgrims -->
          <div class="card col-span-2">
            <h2 class="text-base font-semibold text-white mb-3">👥 My Assigned Pilgrims ({{ volunteerInfo.assignedPilgrims?.length || 0 }})</h2>
            <div v-if="!volunteerInfo.assignedPilgrims?.length" class="text-slate-500 text-sm text-center py-4">
              No pilgrims assigned yet.
            </div>
            <div v-else class="grid grid-cols-1 sm:grid-cols-2 gap-3">
              <div v-for="p in volunteerInfo.assignedPilgrims" :key="p.id"
                class="bg-dark-700 rounded-lg p-3 flex items-start gap-3">
                <div class="w-9 h-9 rounded-full bg-primary-800 flex items-center justify-center text-white font-bold text-sm shrink-0">
                  {{ p.fullName?.charAt(0) || '?' }}
                </div>
                <div class="text-sm">
                  <p class="text-white font-medium">{{ p.fullName }}</p>
                  <p class="text-slate-400 text-xs">{{ p.country || '' }} · {{ p.passportNumber || '—' }}</p>
                  <p v-if="p.notes" class="text-slate-500 text-xs mt-0.5">{{ p.notes }}</p>
                </div>
              </div>
            </div>
          </div>

          <!-- Notice Board for Volunteers -->
          <div class="card col-span-2 mb-0">
            <h2 class="text-base font-semibold text-white mb-3 flex items-center gap-2">
              <span>📋</span> Notice Board
            </h2>
            <NoticeBoard :compact="true" />
          </div>

        <!-- Tasks List -->
          <div class="card col-span-2">
            <h2 class="text-base font-semibold text-white mb-4">My Tasks</h2>
            <div v-if="!tasks.length" class="text-center py-8 text-slate-500">
              <div class="text-4xl mb-2">📋</div>
              No tasks assigned yet.
            </div>
            <div v-else class="space-y-3">
              <div v-for="task in tasks" :key="task.id"
                :class="['rounded-xl p-4 border', statusColor[task.status] || 'bg-dark-800 border-dark-600']">
                <div class="flex items-start justify-between gap-3">
                  <div class="flex-1">
                    <div class="flex items-center gap-2 flex-wrap">
                      <p class="text-white font-medium text-sm">{{ task.title }}</p>
                      <span v-if="task.isEmergency" class="text-xs bg-red-900 text-red-400 border border-red-700 rounded px-1.5 py-0.5">
                        🚨 EMERGENCY
                      </span>
                    </div>
                    <p class="text-slate-400 text-xs mt-1">{{ task.description }}</p>
                    <div class="flex gap-4 mt-2 text-xs text-slate-500">
                      <span>Category: {{ task.category }}</span>
                      <span v-if="task.dueAt">Due: {{ new Date(task.dueAt).toLocaleString('en-GB', {dateStyle:'short', timeStyle:'short'}) }}</span>
                    </div>
                  </div>
                  <div class="flex flex-col items-end gap-2 shrink-0">
                    <span class="text-xs font-medium px-2 py-1 rounded">{{ task.status }}</span>
                    <button v-if="task.status !== 'Completed' && task.status !== 'Cancelled'"
                      @click="completeTask(task.id)"
                      class="text-xs bg-emerald-900/50 hover:bg-emerald-800 text-emerald-400 border border-emerald-700 rounded px-2 py-1 transition-colors">
                      ✓ Done
                    </button>
                  </div>
                </div>
                <div v-if="task.notes" class="mt-2 text-xs text-slate-400 bg-dark-900/50 rounded p-2">
                  Note: {{ task.notes }}
                </div>
              </div>
            </div>
          </div>
        </div>
      </template>
    </template>
  </div>
</template>
