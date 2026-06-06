<script setup>
import { ref, onMounted, onUnmounted } from 'vue'
import { useNotificationStore } from '@/stores/notifications'

const store = useNotificationStore()
const open = ref(false)
let interval = null

const typeIcon = {
  General: '📢', Approval: '✅', RoomAllocation: '🏨',
  BusDeparture: '🚌', MajalisReminder: '📿', FoodTiming: '🍽️', Emergency: '🚨'
}

function toggle() {
  open.value = !open.value
  if (open.value) store.fetchAll()
}

function close() { open.value = false }

function timeAgo(date) {
  const diff = Date.now() - new Date(date).getTime()
  const m = Math.floor(diff / 60000)
  if (m < 1) return 'just now'
  if (m < 60) return `${m}m ago`
  const h = Math.floor(m / 60)
  if (h < 24) return `${h}h ago`
  return `${Math.floor(h / 24)}d ago`
}

onMounted(() => {
  store.fetchUnreadCount()
  interval = setInterval(() => store.fetchUnreadCount(), 30000)
  document.addEventListener('click', handleOutside)
})

onUnmounted(() => {
  clearInterval(interval)
  document.removeEventListener('click', handleOutside)
})

const bellRef = ref(null)
function handleOutside(e) {
  if (bellRef.value && !bellRef.value.contains(e.target)) close()
}
</script>

<template>
  <div ref="bellRef" class="relative">
    <button @click.stop="toggle"
      class="relative p-2 text-slate-400 hover:text-white transition-colors rounded-lg hover:bg-dark-700">
      <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
          d="M15 17h5l-1.405-1.405A2.032 2.032 0 0118 14.158V11a6.002 6.002 0 00-4-5.659V5a2 2 0 10-4 0v.341C7.67 6.165 6 8.388 6 11v3.159c0 .538-.214 1.055-.595 1.436L4 17h5m6 0v1a3 3 0 11-6 0v-1m6 0H9" />
      </svg>
      <span v-if="store.unreadCount > 0"
        class="absolute -top-1 -right-1 bg-red-500 text-white text-xs font-bold rounded-full min-w-[18px] h-[18px] flex items-center justify-center px-1">
        {{ store.unreadCount > 99 ? '99+' : store.unreadCount }}
      </span>
    </button>

    <div v-if="open"
      class="absolute right-0 top-12 w-80 bg-dark-800 border border-dark-600 rounded-xl shadow-2xl z-50 overflow-hidden">

      <div class="flex items-center justify-between px-4 py-3 border-b border-dark-600">
        <h3 class="text-white font-semibold text-sm">Notifications</h3>
        <button v-if="store.unreadCount > 0"
          @click="store.markAllRead()"
          class="text-primary-400 hover:text-primary-300 text-xs">Mark all read</button>
      </div>

      <div class="max-h-96 overflow-y-auto">
        <div v-if="store.loading" class="py-8 text-center text-slate-500 text-sm">Loading...</div>

        <div v-else-if="store.notifications.length === 0"
          class="py-8 text-center text-slate-500 text-sm">
          <div class="text-3xl mb-2">🔔</div>
          No notifications yet
        </div>

        <div v-else>
          <div v-for="n in store.notifications" :key="n.id"
            @click="store.markRead(n.id)"
            :class="['flex gap-3 px-4 py-3 hover:bg-dark-700 cursor-pointer transition-colors border-b border-dark-700', !n.isRead ? 'bg-dark-750' : '']">
            <div class="text-xl flex-shrink-0 mt-0.5">{{ typeIcon[n.type] || '📢' }}</div>
            <div class="flex-1 min-w-0">
              <div class="flex items-start justify-between gap-2">
                <p :class="['text-sm font-medium truncate', !n.isRead ? 'text-white' : 'text-slate-300']">
                  {{ n.title }}
                </p>
                <span v-if="!n.isRead" class="w-2 h-2 rounded-full bg-primary-500 flex-shrink-0 mt-1"></span>
              </div>
              <p class="text-xs text-slate-400 mt-0.5 line-clamp-2">{{ n.message }}</p>
              <p class="text-xs text-slate-600 mt-1">{{ timeAgo(n.createdAt) }}</p>
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>
