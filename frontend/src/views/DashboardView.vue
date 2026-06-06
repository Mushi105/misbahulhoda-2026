<script setup>
import { useAuthStore } from '@/stores/auth'
import { useRouter } from 'vue-router'
import { onMounted } from 'vue'
import MHLogo from '@/components/MHLogo.vue'

const auth = useAuthStore()
const router = useRouter()

onMounted(() => {
  if (auth.isAdmin) router.replace('/admin/dashboard')
  else if (auth.isPilgrim) router.replace('/pilgrim/portal')
  else if (auth.isVolunteer) router.replace('/volunteer/dashboard')
  else if (auth.isDriver) router.replace('/tracking')
})
</script>

<template>
  <div class="flex items-center justify-center h-full">
    <div class="text-center">
      <div class="flex justify-center mb-4"><MHLogo size="xl" /></div>
      <p class="text-gray-700">Redirecting to your dashboard...</p>
    </div>
  </div>
</template>
