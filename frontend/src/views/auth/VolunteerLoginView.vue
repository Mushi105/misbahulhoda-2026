<script setup>
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'

const router = useRouter()
const auth = useAuthStore()
const loading = ref(false)
const error = ref('')
const form = ref({ email: '', password: '' })

async function login() {
  if (!form.value.email || !form.value.password) { error.value = 'Please fill all fields.'; return }
  loading.value = true; error.value = ''
  try {
    await auth.login(form.value.email, form.value.password)
    if (!auth.isVolunteer) { auth.logout(); error.value = 'This portal is for Volunteers only.'; return }
    router.replace('/volunteer/dashboard')
  } catch (e) {
    error.value = e.response?.data?.message || 'Login failed. Check your credentials.'
  } finally { loading.value = false }
}
</script>

<template>
  <div class="min-h-screen bg-dark-950 flex flex-col">

    <div class="flex items-center justify-between px-6 py-4 border-b border-dark-800">
      <button @click="router.push('/')" class="flex items-center gap-2 text-slate-400 hover:text-white transition-colors text-sm">
        <span>←</span> Back
      </button>
      <div class="flex items-center gap-2">
        <span>🤝</span>
        <span class="text-gray-900 font-semibold text-sm">Volunteer Portal</span>
      </div>
    </div>

    <div class="flex-1 flex items-center justify-center px-4 py-12">
      <div class="w-full max-w-md">

        <div class="text-center mb-8">
          <div class="w-20 h-20 rounded-2xl bg-green-900/50 border border-green-700 flex items-center justify-center text-4xl mx-auto mb-4">🤝</div>
          <h1 class="text-2xl font-bold text-gray-900">Volunteer Portal</h1>
          <p class="text-green-400 text-sm mt-2">Arbaeen 2026 — Volunteer Access</p>
          <p class="text-gray-700 text-sm mt-1">Your account is created by the admin team.</p>
        </div>

        <div v-if="error" class="mb-4 bg-red-900/50 border border-red-700 text-red-300 text-sm rounded-lg px-4 py-3">{{ error }}</div>

        <form @submit.prevent="login" class="space-y-4">
          <div>
            <label class="label">Email Address</label>
            <input v-model="form.email" type="email" class="input" placeholder="volunteer@misbahuda.com" autocomplete="email" />
          </div>
          <div>
            <label class="label">Password</label>
            <input v-model="form.password" type="password" class="input" placeholder="••••••••" autocomplete="current-password" />
          </div>
          <button type="submit" :disabled="loading" class="w-full py-3 text-base font-semibold rounded-xl bg-green-700 hover:bg-green-600 text-white transition-colors disabled:opacity-60">
            {{ loading ? 'Signing in...' : 'Sign In to Volunteer Portal' }}
          </button>
        </form>

        <div class="mt-6 p-4 bg-dark-800 rounded-xl border border-dark-600 text-sm text-gray-700 text-center">
          Don't have access? Contact the admin team to get your volunteer account created.
        </div>

      </div>
    </div>
  </div>
</template>
