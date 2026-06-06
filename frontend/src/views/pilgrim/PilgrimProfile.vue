<script setup>
import { useAuthStore } from '@/stores/auth'
const auth = useAuthStore()
</script>

<template>
  <div class="max-w-xl space-y-6">
    <h1 class="text-2xl font-bold text-white">My Profile</h1>
    <div class="card">
      <div class="flex items-center gap-4 mb-6">
        <div class="w-16 h-16 rounded-full bg-primary-700 flex items-center justify-center text-2xl font-bold text-white">
          {{ auth.user?.fullName?.[0] }}
        </div>
        <div>
          <p class="text-white text-lg font-semibold">{{ auth.user?.fullName }}</p>
          <p class="text-slate-400 text-sm">{{ auth.user?.email }}</p>
          <span class="badge-green mt-1 inline-block">{{ auth.role }}</span>
        </div>
      </div>
      <div class="grid grid-cols-2 gap-4 text-sm border-t border-dark-700 pt-4">
        <div><p class="text-slate-400">Phone</p><p class="text-white">{{ auth.user?.phoneNumber }}</p></div>
        <div><p class="text-slate-400">Account Status</p><p class="text-primary-400">Active</p></div>
      </div>
    </div>
  </div>
</template>
