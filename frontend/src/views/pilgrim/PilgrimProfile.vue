<script setup>
import { useAuthStore } from '@/stores/auth'
const auth = useAuthStore()
</script>

<template>
  <div class="max-w-xl space-y-6">
    <h1 class="text-2xl font-bold text-gray-900">My Profile</h1>
    <div class="card">
      <div class="flex items-center gap-4 mb-6">
        <div class="w-16 h-16 rounded-full bg-amber-600 flex items-center justify-center text-2xl font-bold text-gray-900">
          {{ auth.user?.fullName?.[0] }}
        </div>
        <div>
          <p class="text-gray-900 text-lg font-semibold">{{ auth.user?.fullName }}</p>
          <p class="text-gray-700 text-sm">{{ auth.user?.email }}</p>
          <span class="badge-green mt-1 inline-block">{{ auth.role }}</span>
        </div>
      </div>
      <div class="grid grid-cols-2 gap-4 text-sm border-t border-gray-200 pt-4">
        <div><p class="text-gray-700">Phone</p><p class="text-gray-900">{{ auth.user?.phoneNumber }}</p></div>
        <div><p class="text-gray-700">Account Status</p><p class="text-amber-700">Active</p></div>
      </div>
    </div>
  </div>
</template>
