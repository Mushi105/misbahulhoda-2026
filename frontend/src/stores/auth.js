import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import { authApi } from '@/api/auth'

export const useAuthStore = defineStore('auth', () => {
  const user = ref(JSON.parse(localStorage.getItem('user') || 'null'))
  const accessToken = ref(localStorage.getItem('access_token') || null)
  const refreshTokenVal = ref(localStorage.getItem('refresh_token') || null)

  const isAuthenticated = computed(() => !!accessToken.value && !!user.value)
  const role = computed(() => user.value?.role || null)
  const isAdmin = computed(() => ['SuperAdmin', 'Admin'].includes(role.value))
  const isPilgrim = computed(() => role.value === 'Pilgrim')
  const isVolunteer = computed(() => role.value === 'Volunteer')
  const isDriver = computed(() => role.value === 'Driver')

  async function login(email, password) {
    const res = await authApi.login(email, password)
    const { data, message } = res.data
    if (!data) throw new Error(message || 'Login failed.')
    setSession(data)
    return data
  }

  async function register(payload) {
    const res = await authApi.register(payload)
    const { data } = res.data
    setSession(data)
    return data
  }

  async function refreshToken() {
    const res = await authApi.refresh(accessToken.value, refreshTokenVal.value)
    const { data } = res.data
    setSession(data)
  }

  function setSession(data) {
    user.value = data.user
    accessToken.value = data.accessToken
    refreshTokenVal.value = data.refreshToken
    localStorage.setItem('user', JSON.stringify(data.user))
    localStorage.setItem('access_token', data.accessToken)
    localStorage.setItem('refresh_token', data.refreshToken)
  }

  function logout() {
    user.value = null
    accessToken.value = null
    refreshTokenVal.value = null
    localStorage.removeItem('user')
    localStorage.removeItem('access_token')
    localStorage.removeItem('refresh_token')
  }

  return { user, isAuthenticated, role, isAdmin, isPilgrim, isVolunteer, isDriver, login, register, refreshToken, logout }
})
