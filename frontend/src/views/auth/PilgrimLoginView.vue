<script setup>
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'

const router = useRouter()
const auth = useAuthStore()

const tab = ref('login')
const loading = ref(false)
const error = ref('')

const loginForm = ref({ email: '', password: '' })
const registerForm = ref({
  fullName: '', email: '', phoneNumber: '', password: '', confirmPassword: '', role: 3
})

async function login() {
  if (!loginForm.value.email || !loginForm.value.password) { error.value = 'Please fill all fields.'; return }
  loading.value = true; error.value = ''
  try {
    await auth.login(loginForm.value.email, loginForm.value.password)
    if (!auth.isPilgrim) { auth.logout(); error.value = 'This portal is for Pilgrims only.'; return }
    router.replace('/pilgrim/portal')
  } catch (e) {
    error.value = e.response?.data?.message || 'Login failed. Check your credentials.'
  } finally { loading.value = false }
}

async function register() {
  if (registerForm.value.password !== registerForm.value.confirmPassword) { error.value = 'Passwords do not match.'; return }
  loading.value = true; error.value = ''
  try {
    await auth.register({ ...registerForm.value, role: 3 })
    router.replace('/pilgrim/portal')
  } catch (e) {
    error.value = e.response?.data?.message || 'Registration failed.'
  } finally { loading.value = false }
}
</script>

<template>
  <div class="min-h-screen bg-dark-950 flex flex-col">

    <!-- Header -->
    <div class="flex items-center justify-between px-6 py-4 border-b border-dark-800">
      <button @click="router.push('/')" class="flex items-center gap-2 text-slate-400 hover:text-white transition-colors">
        <span>←</span> <span class="text-sm">Back</span>
      </button>
      <div class="flex items-center gap-2">
        <span>🕌</span>
        <span class="text-gray-900 font-semibold text-sm">Pilgrim Portal</span>
      </div>
    </div>

    <div class="flex-1 flex items-center justify-center px-4 py-12">
      <div class="w-full max-w-md">

        <!-- Icon + Title -->
        <div class="text-center mb-8">
          <div class="w-20 h-20 rounded-2xl bg-primary-900/50 border border-primary-700 flex items-center justify-center text-4xl mx-auto mb-4">🕌</div>
          <h1 class="text-2xl font-bold text-gray-900">Pilgrim Portal</h1>
          <p class="text-gold-400 font-arabic mt-1">لَبَّيْكَ اللَّهُمَّ لَبَّيْكَ</p>
          <p class="text-gray-700 text-sm mt-2">Arbaeen 2026 Registration & Access</p>
        </div>

        <!-- Tabs -->
        <div class="flex bg-dark-800 rounded-xl p-1 mb-6">
          <button @click="tab = 'login'; error = ''"
            :class="['flex-1 py-2 rounded-lg text-sm font-medium transition-all', tab === 'login' ? 'bg-amber-600 text-white' : 'text-slate-400 hover:text-white']">
            Sign In
          </button>
          <button @click="tab = 'register'; error = ''"
            :class="['flex-1 py-2 rounded-lg text-sm font-medium transition-all', tab === 'register' ? 'bg-amber-600 text-white' : 'text-slate-400 hover:text-white']">
            Register
          </button>
        </div>

        <div v-if="error" class="mb-4 bg-red-900/50 border border-red-700 text-red-300 text-sm rounded-lg px-4 py-3">{{ error }}</div>

        <!-- Login Form -->
        <form v-if="tab === 'login'" @submit.prevent="login" class="space-y-4">
          <div>
            <label class="label">Email Address</label>
            <input v-model="loginForm.email" type="email" class="input" placeholder="your@email.com" autocomplete="email" />
          </div>
          <div>
            <label class="label">Password</label>
            <input v-model="loginForm.password" type="password" class="input" placeholder="••••••••" autocomplete="current-password" />
          </div>
          <button type="submit" :disabled="loading" class="btn-primary w-full py-3 text-base">
            {{ loading ? 'Signing in...' : 'Sign In to Pilgrim Portal' }}
          </button>
        </form>

        <!-- Register Form -->
        <form v-else @submit.prevent="register" class="space-y-4">
          <div>
            <label class="label">Full Name (as on passport)</label>
            <input v-model="registerForm.fullName" type="text" class="input" placeholder="Muhammad Ali" />
          </div>
          <div>
            <label class="label">Email Address</label>
            <input v-model="registerForm.email" type="email" class="input" placeholder="your@email.com" />
          </div>
          <div>
            <label class="label">Phone / WhatsApp Number</label>
            <input v-model="registerForm.phoneNumber" type="tel" class="input" placeholder="+92 300 1234567" />
          </div>
          <div>
            <label class="label">Password</label>
            <input v-model="registerForm.password" type="password" class="input" placeholder="At least 8 characters" />
          </div>
          <div>
            <label class="label">Confirm Password</label>
            <input v-model="registerForm.confirmPassword" type="password" class="input" placeholder="Repeat password" />
          </div>
          <button type="submit" :disabled="loading" class="btn-primary w-full py-3 text-base">
            {{ loading ? 'Registering...' : 'Register as Pilgrim' }}
          </button>
          <p class="text-gray-600 text-xs text-center">
            After registering, complete your travel profile to submit your application.
          </p>
        </form>

      </div>
    </div>
  </div>
</template>
