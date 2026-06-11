<template>
  <div class="p-6 space-y-6">

    <!-- Header -->
    <div class="flex items-center justify-between">
      <div>
        <h1 class="text-2xl font-bold text-white">Activity Log</h1>
        <p class="text-sm text-gray-400 mt-1">Track all user actions and system events</p>
      </div>
      <button @click="exportLogs" class="flex items-center gap-2 px-4 py-2 rounded text-sm font-medium"
        style="background:#c9a84c; color:#000;">
        ↓ Export CSV
      </button>
    </div>

    <!-- Summary Cards -->
    <div class="grid grid-cols-2 md:grid-cols-3 lg:grid-cols-6 gap-4" v-if="summary">
      <div v-for="card in summaryCards" :key="card.label"
        class="rounded-lg p-4 text-center"
        style="background:rgba(255,255,255,0.05); border:1px solid rgba(255,255,255,0.08);">
        <div class="text-2xl font-bold" :style="{ color: card.color }">{{ card.value }}</div>
        <div class="text-xs text-gray-400 mt-1">{{ card.label }}</div>
      </div>
    </div>

    <!-- Filters -->
    <div class="rounded-lg p-4 grid grid-cols-1 md:grid-cols-4 gap-3"
      style="background:rgba(255,255,255,0.04); border:1px solid rgba(255,255,255,0.08);">
      <div>
        <label class="text-xs text-gray-400 mb-1 block">Action</label>
        <select v-model="filters.action" @change="loadLogs"
          class="w-full rounded px-3 py-2 text-sm text-white"
          style="background:#1a1a2e; border:1px solid rgba(255,255,255,0.1);">
          <option value="">All Actions</option>
          <option value="LOGIN">Login</option>
          <option value="LOGIN_FAILED">Failed Login</option>
          <option value="PILGRIM_APPROVED">Pilgrim Approved</option>
          <option value="PILGRIM_REJECTED">Pilgrim Rejected</option>
          <option value="REGISTER">Register</option>
        </select>
      </div>
      <div>
        <label class="text-xs text-gray-400 mb-1 block">Entity</label>
        <select v-model="filters.entityName" @change="loadLogs"
          class="w-full rounded px-3 py-2 text-sm text-white"
          style="background:#1a1a2e; border:1px solid rgba(255,255,255,0.1);">
          <option value="">All Entities</option>
          <option value="User">User</option>
          <option value="Pilgrim">Pilgrim</option>
          <option value="Volunteer">Volunteer</option>
          <option value="Room">Room</option>
        </select>
      </div>
      <div>
        <label class="text-xs text-gray-400 mb-1 block">From Date</label>
        <input type="date" v-model="filters.from" @change="loadLogs"
          class="w-full rounded px-3 py-2 text-sm text-white"
          style="background:#1a1a2e; border:1px solid rgba(255,255,255,0.1);">
      </div>
      <div>
        <label class="text-xs text-gray-400 mb-1 block">To Date</label>
        <input type="date" v-model="filters.to" @change="loadLogs"
          class="w-full rounded px-3 py-2 text-sm text-white"
          style="background:#1a1a2e; border:1px solid rgba(255,255,255,0.1);">
      </div>
    </div>

    <!-- Logs Table -->
    <div class="rounded-lg overflow-hidden" style="border:1px solid rgba(255,255,255,0.08);">
      <div v-if="loading" class="text-center py-16 text-gray-400">
        <div class="text-3xl mb-3 animate-spin inline-block">⟳</div>
        <p>Loading logs...</p>
      </div>

      <table v-else class="w-full text-sm">
        <thead>
          <tr style="background:rgba(201,168,76,0.1); border-bottom:1px solid rgba(201,168,76,0.2);">
            <th class="px-4 py-3 text-left text-xs font-semibold" style="color:#c9a84c;">Time</th>
            <th class="px-4 py-3 text-left text-xs font-semibold" style="color:#c9a84c;">Action</th>
            <th class="px-4 py-3 text-left text-xs font-semibold" style="color:#c9a84c;">Entity</th>
            <th class="px-4 py-3 text-left text-xs font-semibold" style="color:#c9a84c;">User</th>
            <th class="px-4 py-3 text-left text-xs font-semibold" style="color:#c9a84c;">Description</th>
            <th class="px-4 py-3 text-left text-xs font-semibold" style="color:#c9a84c;">IP Address</th>
          </tr>
        </thead>
        <tbody>
          <tr v-if="logs.length === 0">
            <td colspan="6" class="text-center py-12 text-gray-500">No activity logs found</td>
          </tr>
          <tr v-for="log in logs" :key="log.id"
            class="border-b transition-colors"
            style="border-color:rgba(255,255,255,0.04);"
            :style="{ background: 'rgba(255,255,255,0.02)' }"
            @mouseover="e => e.currentTarget.style.background = 'rgba(255,255,255,0.05)'"
            @mouseout="e => e.currentTarget.style.background = 'rgba(255,255,255,0.02)'">
            <td class="px-4 py-3 text-gray-400 whitespace-nowrap">{{ formatTime(log.createdAt) }}</td>
            <td class="px-4 py-3">
              <span class="px-2 py-1 rounded text-xs font-medium" :style="actionStyle(log.action)">
                {{ log.action }}
              </span>
            </td>
            <td class="px-4 py-3 text-gray-300">{{ log.entityName }}</td>
            <td class="px-4 py-3">
              <div v-if="log.user" class="text-white text-xs">{{ log.user.fullName }}</div>
              <div v-if="log.user" class="text-gray-500 text-xs">{{ log.user.role }}</div>
              <div v-else class="text-gray-600 text-xs">System</div>
            </td>
            <td class="px-4 py-3 text-gray-400 text-xs max-w-xs truncate">{{ log.description || '—' }}</td>
            <td class="px-4 py-3 text-gray-500 text-xs font-mono">{{ log.ipAddress || '—' }}</td>
          </tr>
        </tbody>
      </table>
    </div>

    <!-- Pagination -->
    <div class="flex items-center justify-between text-sm text-gray-400" v-if="totalPages > 1">
      <span>Showing {{ (currentPage - 1) * pageSize + 1 }}–{{ Math.min(currentPage * pageSize, total) }} of {{ total }}</span>
      <div class="flex gap-2">
        <button @click="changePage(currentPage - 1)" :disabled="currentPage === 1"
          class="px-3 py-1 rounded disabled:opacity-30"
          style="background:rgba(255,255,255,0.06); border:1px solid rgba(255,255,255,0.1);">
          ← Prev
        </button>
        <span class="px-3 py-1" style="color:#c9a84c;">{{ currentPage }} / {{ totalPages }}</span>
        <button @click="changePage(currentPage + 1)" :disabled="currentPage === totalPages"
          class="px-3 py-1 rounded disabled:opacity-30"
          style="background:rgba(255,255,255,0.06); border:1px solid rgba(255,255,255,0.1);">
          Next →
        </button>
      </div>
    </div>

  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import api from '@/api/client'

const logs = ref([])
const summary = ref(null)
const loading = ref(false)
const currentPage = ref(1)
const pageSize = ref(50)
const total = ref(0)
const totalPages = ref(1)

const filters = ref({ action: '', entityName: '', from: '', to: '' })

const summaryCards = computed(() => summary.value ? [
  { label: 'Total Logs',       value: summary.value.totalLogs,        color: '#c9a84c' },
  { label: 'Today',            value: summary.value.todayLogs,        color: '#60a5fa' },
  { label: 'Logins',           value: summary.value.loginCount,       color: '#34d399' },
  { label: 'Failed Logins',    value: summary.value.failedLogins,     color: '#f87171' },
  { label: 'Approved',         value: summary.value.approvedPilgrims, color: '#34d399' },
  { label: 'Rejected',         value: summary.value.rejectedPilgrims, color: '#f87171' },
] : [])

const actionStyle = (action) => {
  const styles = {
    LOGIN:             'background:rgba(52,211,153,0.15); color:#34d399;',
    LOGIN_FAILED:      'background:rgba(248,113,113,0.15); color:#f87171;',
    PILGRIM_APPROVED:  'background:rgba(52,211,153,0.15); color:#34d399;',
    PILGRIM_REJECTED:  'background:rgba(248,113,113,0.15); color:#f87171;',
    REGISTER:          'background:rgba(96,165,250,0.15); color:#60a5fa;',
  }
  return styles[action] || 'background:rgba(255,255,255,0.08); color:#94a3b8;'
}

const formatTime = (dt) => {
  if (!dt) return '—'
  return new Date(dt).toLocaleString('en-GB', { dateStyle: 'short', timeStyle: 'medium' })
}

async function loadLogs() {
  loading.value = true
  try {
    const params = {
      page: currentPage.value,
      pageSize: pageSize.value,
      ...(filters.value.action && { action: filters.value.action }),
      ...(filters.value.entityName && { entityName: filters.value.entityName }),
      ...(filters.value.from && { from: filters.value.from }),
      ...(filters.value.to && { to: filters.value.to }),
    }
    const res = await api.get('/audit-logs', { params })
    const data = res.data.data
    logs.value = data.data
    total.value = data.total
    totalPages.value = data.totalPages
  } catch (e) {
    console.error(e)
  } finally {
    loading.value = false
  }
}

async function loadSummary() {
  try {
    const res = await api.get('/audit-logs/summary')
    summary.value = res.data.data
  } catch (e) { console.error(e) }
}

function changePage(page) {
  if (page < 1 || page > totalPages.value) return
  currentPage.value = page
  loadLogs()
}

function exportLogs() {
  if (!logs.value.length) return
  const headers = ['Time', 'Action', 'Entity', 'User', 'Role', 'Description', 'IP Address']
  const rows = logs.value.map(l => [
    formatTime(l.createdAt),
    l.action,
    l.entityName,
    l.user?.fullName || 'System',
    l.user?.role || '',
    l.description || '',
    l.ipAddress || ''
  ])
  const csv = [headers, ...rows].map(r => r.map(v => `"${v}"`).join(',')).join('\n')
  const blob = new Blob([csv], { type: 'text/csv' })
  const url = URL.createObjectURL(blob)
  const a = document.createElement('a')
  a.href = url
  a.download = `activity-log-${new Date().toISOString().slice(0,10)}.csv`
  a.click()
}

onMounted(() => {
  loadLogs()
  loadSummary()
})
</script>
