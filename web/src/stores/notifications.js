import { defineStore } from 'pinia'
import { ref } from 'vue'
import { notificationApi } from '@/api'

export const useNotificationStore = defineStore('notifications', () => {
  const notifications = ref([])
  const unreadCount = ref(0)
  const loading = ref(false)

  async function fetchUnreadCount() {
    try {
      const res = await notificationApi.getUnreadCount()
      unreadCount.value = res.data.data?.count ?? 0
    } catch {}
  }

  async function fetchAll() {
    loading.value = true
    try {
      const res = await notificationApi.getMine()
      notifications.value = res.data.data || []
      unreadCount.value = notifications.value.filter(n => !n.isRead).length
    } catch {} finally {
      loading.value = false
    }
  }

  async function markRead(id) {
    try {
      await notificationApi.markRead(id)
      const n = notifications.value.find(x => x.id === id)
      if (n) { n.isRead = true; unreadCount.value = Math.max(0, unreadCount.value - 1) }
    } catch {}
  }

  async function markAllRead() {
    try {
      await notificationApi.markAllRead()
      notifications.value.forEach(n => (n.isRead = true))
      unreadCount.value = 0
    } catch {}
  }

  return { notifications, unreadCount, loading, fetchUnreadCount, fetchAll, markRead, markAllRead }
})
