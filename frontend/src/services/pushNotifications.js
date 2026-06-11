import { Capacitor } from '@capacitor/core'
import { PushNotifications } from '@capacitor/push-notifications'
import api from '@/api/client'

export async function registerPushNotifications() {
  if (!Capacitor.isNativePlatform()) return

  const permission = await PushNotifications.requestPermissions()
  if (permission.receive !== 'granted') return

  await PushNotifications.register()

  PushNotifications.addListener('registration', async (token) => {
    try {
      await api.post('/devices/register', { token: token.value, platform: 'android' })
    } catch {
      // silent — non-critical
    }
  })

  PushNotifications.addListener('pushNotificationReceived', (notification) => {
    // App is in foreground — notification arrives silently, SignalR bell handles it
    console.log('FCM foreground:', notification.title)
  })

  PushNotifications.addListener('pushNotificationActionPerformed', () => {
    // User tapped the notification — app opens normally
  })
}

export async function unregisterPushNotifications() {
  if (!Capacitor.isNativePlatform()) return
  try {
    const result = await PushNotifications.getDeliveredNotifications()
    const token = result?.notifications?.[0]
    if (token) {
      await api.post('/devices/unregister', { token })
    }
  } catch {
    // silent
  }
}
