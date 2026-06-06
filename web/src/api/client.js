import axios from 'axios'
import { useAuthStore } from '@/stores/auth'

const api = axios.create({
  baseURL: import.meta.env.VITE_API_URL || 'http://localhost:5000/api/v1',
  timeout: 15000,
})

api.interceptors.request.use(config => {
  const token = localStorage.getItem('access_token')
  if (token) config.headers.Authorization = `Bearer ${token}`
  return config
})

api.interceptors.response.use(
  res => res,
  async err => {
    const original = err.config
    const hasSession = !!localStorage.getItem('access_token')
    if (err.response?.status === 401 && !original._retry && hasSession) {
      original._retry = true
      try {
        const authStore = useAuthStore()
        await authStore.refreshToken()
        original.headers.Authorization = `Bearer ${localStorage.getItem('access_token')}`
        return api(original)
      } catch {
        useAuthStore().logout()
        window.location.href = '/login'
      }
    }
    return Promise.reject(err)
  }
)

export default api
