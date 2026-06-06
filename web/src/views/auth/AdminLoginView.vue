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
    if (!auth.isAdmin) { auth.logout(); error.value = 'Access denied. Admin credentials required.'; return }
    router.replace('/admin/dashboard')
  } catch (e) {
    error.value = e.response?.data?.message || 'Login failed.'
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
        <span>🛡️</span>
        <span class="text-white font-semibold text-sm">Admin Panel</span>
      </div>
    </div>

    <div class="flex-1 flex items-center justify-center px-4 py-12">
      <div class="w-full max-w-sm">

        <div class="text-center mb-8">
          <div class="w-20 h-20 rounded-2xl bg-gold-900/50 border border-gold-700 flex items-center justify-center text-4xl mx-auto mb-4">🛡️</div>
          <h1 class="text-2xl font-bold text-white">Admin Access</h1>
          <p class="text-gold-400 text-sm mt-2">Misbahuda Control Panel</p>
          <p class="text-slate-500 text-xs mt-1">Restricted — Authorized personnel only</p>
        </div>

        <div v-if="error" class="mb-4 bg-red-900/50 border border-red-700 text-red-300 text-sm rounded-lg px-4 py-3">{{ error }}</div>

        <form @submit.prevent="login" class="space-y-4">
          <div>
            <label class="label">Admin Email</label>
            <input v-model="form.email" type="email" class="input" placeholder="admin@misbahuda.com" autocomplete="off" />
          </div>
          <div>
            <label class="label">Password</label>
            <input v-model="form.password" type="password" class="input" placeholder="••••••••" autocomplete="off" />
          </div>
          <button type="submit" :disabled="loading"
            class="w-full py-3 text-base font-semibold rounded-xl bg-gold-700 hover:bg-gold-600 text-white transition-colors disabled:opacity-60">
            {{ loading ? 'Verifying...' : 'Access Admin Panel' }}
          </button>
        </form>

        <p class="text-slate-600 text-xs text-center mt-6">
          Unauthorized access is prohibited and logged.
        </p>
      </div>
    </div>
  </div>
</template>
