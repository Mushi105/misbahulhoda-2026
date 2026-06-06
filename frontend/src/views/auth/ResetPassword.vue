<script setup>
import { ref, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import api from '@/api/client'
import MHLogo from '@/components/MHLogo.vue'

const route  = useRoute()
const router = useRouter()

const token       = ref('')
const password    = ref('')
const confirm     = ref('')
const loading     = ref(false)
const done        = ref(false)
const error       = ref('')
const showPass    = ref(false)

onMounted(() => {
  token.value = route.query.token || ''
  if (!token.value) error.value = 'Invalid reset link. Please request a new one.'
})

async function handleReset() {
  error.value = ''
  if (password.value.length < 8) return error.value = 'Password must be at least 8 characters.'
  if (password.value !== confirm.value) return error.value = 'Passwords do not match.'
  loading.value = true
  try {
    await api.post('/auth/reset-password', {
      token: token.value,
      newPassword: password.value,
      confirmPassword: confirm.value,
    })
    done.value = true
  } catch (e) {
    error.value = e.response?.data?.message || 'Reset failed. Link may have expired.'
  } finally {
    loading.value = false
  }
}
</script>

<template>
  <div class="min-h-screen bg-dark-950 flex items-center justify-center px-4">
    <div class="w-full max-w-md">

      <div class="text-center mb-8">
        <div class="flex justify-center mb-3"><MHLogo size="lg" /></div>
        <h1 class="text-2xl font-bold text-gray-900">Misbah ul Hoda</h1>
        <p class="text-gold-400 text-xs mt-1">Arbaeen 2026</p>
      </div>

      <div class="card border border-dark-600 p-8">

        <!-- Success -->
        <div v-if="done" class="text-center space-y-4">
          <div class="text-5xl">✅</div>
          <h2 class="text-gray-900 font-bold text-xl">Password Reset!</h2>
          <p class="text-gray-700 text-sm">Your password has been updated successfully. You can now sign in with your new password.</p>
          <button @click="router.push('/')" class="btn-primary w-full py-3 font-semibold">
            Go to Sign In →
          </button>
        </div>

        <!-- Form -->
        <template v-else>
          <h2 class="text-gray-900 font-bold text-xl mb-1">Reset Password</h2>
          <p class="text-gray-700 text-sm mb-6">Enter your new password below.</p>

          <div v-if="error" class="mb-4 bg-red-900/40 border border-red-700 text-red-300 text-sm rounded-xl px-4 py-3 flex gap-2">
            <span>⚠️</span><span>{{ error }}</span>
          </div>

          <div class="space-y-4">
            <div>
              <label class="label">New Password</label>
              <div class="relative">
                <input v-model="password" :type="showPass ? 'text' : 'password'" class="input pr-10" placeholder="Min 8 characters" />
                <button type="button" @click="showPass = !showPass" class="absolute right-3 top-1/2 -translate-y-1/2 text-slate-500 hover:text-gray-600 text-sm">
                  {{ showPass ? '🙈' : '👁️' }}
                </button>
              </div>
              <div class="mt-2 grid grid-cols-2 gap-1">
                <span :class="['text-xs', password.length >= 8 ? 'text-green-400' : 'text-slate-500']">{{ password.length >= 8 ? '✓' : '○' }} 8+ characters</span>
                <span :class="['text-xs', /[A-Z]/.test(password) ? 'text-green-400' : 'text-slate-500']">{{ /[A-Z]/.test(password) ? '✓' : '○' }} Uppercase</span>
                <span :class="['text-xs', /[0-9]/.test(password) ? 'text-green-400' : 'text-slate-500']">{{ /[0-9]/.test(password) ? '✓' : '○' }} Number</span>
                <span :class="['text-xs', /[^a-zA-Z0-9]/.test(password) ? 'text-green-400' : 'text-slate-500']">{{ /[^a-zA-Z0-9]/.test(password) ? '✓' : '○' }} Special char</span>
              </div>
            </div>
            <div>
              <label class="label">Confirm Password</label>
              <div class="relative">
                <input v-model="confirm" type="password" class="input" placeholder="Repeat password" />
                <span v-if="confirm && password === confirm" class="absolute right-3 top-1/2 -translate-y-1/2 text-amber-700">✓</span>
              </div>
            </div>
            <button @click="handleReset" :disabled="loading || !token" class="btn-primary w-full py-3 font-semibold disabled:opacity-50">
              <span v-if="loading" class="flex items-center justify-center gap-2">
                <svg class="animate-spin w-4 h-4" fill="none" viewBox="0 0 24 24">
                  <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"/>
                  <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8v8z"/>
                </svg> Resetting...
              </span>
              <span v-else>Reset Password</span>
            </button>
            <p class="text-center text-sm text-slate-500">
              <button @click="router.push('/')" class="text-gold-500 hover:text-gold-400 hover:underline">← Back to Sign In</button>
            </p>
          </div>
        </template>

      </div>
    </div>
  </div>
</template>
