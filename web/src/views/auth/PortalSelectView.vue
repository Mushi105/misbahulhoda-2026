<script setup>
import { useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import { onMounted } from 'vue'
import MHLogo from '@/components/MHLogo.vue'

const router = useRouter()
const auth = useAuthStore()

onMounted(() => {
  if (auth.isAuthenticated) {
    if (auth.isAdmin) router.replace('/admin/dashboard')
    else if (auth.isPilgrim) router.replace('/pilgrim/portal')
    else if (auth.isVolunteer) router.replace('/volunteer/dashboard')
  }
})
</script>

<template>
  <div class="min-h-screen flex flex-col" style="background: linear-gradient(160deg, #020617 0%, #0f1a0a 50%, #1a1200 100%);">

    <!-- Islamic pattern overlay -->
    <div class="fixed inset-0 pointer-events-none opacity-5" style="background-image: repeating-linear-gradient(45deg, transparent, transparent 30px, rgba(180,83,9,0.5) 30px, rgba(180,83,9,0.5) 31px), repeating-linear-gradient(-45deg, transparent, transparent 30px, rgba(180,83,9,0.3) 30px, rgba(180,83,9,0.3) 31px);"></div>
    <!-- Bottom glow -->
    <div class="fixed bottom-0 left-0 right-0 h-96 pointer-events-none opacity-15" style="background: radial-gradient(ellipse at 50% 100%, #d97706 0%, transparent 65%);"></div>

    <!-- Top Bar -->
    <div class="relative border-b border-gold-900/30 px-6 py-4" style="background: rgba(2,6,23,0.8); backdrop-filter: blur(10px);">
      <div class="max-w-6xl mx-auto flex items-center justify-between">
        <div class="flex items-center gap-3">
          <!-- Logo circle -->
          <div class="w-10 h-10 rounded-full border border-gold-700/60 flex items-center justify-center"
               style="background: radial-gradient(circle, #1a1200, #020617); box-shadow: 0 0 15px rgba(180,83,9,0.25);">
            <MHLogo size="sm" />
          </div>
          <div>
            <p class="text-white font-bold text-base leading-none">Misbah ul Hoda</p>
            <p class="text-gold-500 text-xs tracking-wider">misbahulhoda.org · Arbaeen 2026</p>
          </div>
        </div>
        <div class="hidden md:flex items-center gap-2">
          <span class="w-2 h-2 rounded-full bg-gold-400 animate-pulse"></span>
          <p class="text-gold-400 text-sm">24 July – 7 Aug 2026</p>
        </div>
      </div>
    </div>

    <!-- Hero -->
    <div class="relative flex-1 flex flex-col items-center justify-center px-4 py-12">

      <!-- Center logo + title -->
      <div class="text-center mb-10 max-w-2xl">

        <!-- Ornate logo -->
        <div class="mx-auto w-24 h-24 rounded-full border-2 border-gold-600/50 flex items-center justify-center mb-6 shadow-2xl"
             style="background: radial-gradient(circle, #1a1200 0%, #020617 100%); box-shadow: 0 0 50px rgba(180,83,9,0.25);">
          <MHLogo size="3xl" />
        </div>

        <div class="flex items-center gap-3 justify-center mb-3">
          <div class="h-px w-16 bg-gradient-to-r from-transparent to-gold-600"></div>
          <span class="text-gold-400 text-lg">✦</span>
          <div class="h-px w-16 bg-gradient-to-l from-transparent to-gold-600"></div>
        </div>

        <h1 class="text-4xl md:text-5xl font-bold text-white leading-tight mb-1">
          Misbah ul <span class="text-gold-400">Hoda</span>
        </h1>
        <p class="text-gold-500 text-sm tracking-widest uppercase mb-3">Ziyarat with Ma'rifat</p>
        <p class="text-gold-300 font-arabic text-xl mb-4">یَا حُسَیْن</p>

        <p class="text-slate-400 text-base">Arbaeen 2026 — Complete Pilgrimage Management</p>
        <p class="text-slate-500 text-sm mt-1">Select your portal to continue</p>
      </div>

      <!-- Three Portal Cards -->
      <div class="grid grid-cols-1 md:grid-cols-3 gap-5 w-full max-w-4xl">

        <!-- Pilgrim -->
        <div class="group relative overflow-hidden rounded-2xl cursor-pointer transition-all duration-300 hover:-translate-y-1"
             style="background: rgba(15,26,10,0.8); border: 1px solid rgba(180,83,9,0.2);"
             @mouseenter="e => e.currentTarget.style.border='1px solid rgba(180,83,9,0.6)'"
             @mouseleave="e => e.currentTarget.style.border='1px solid rgba(180,83,9,0.2)'"
             @click="router.push('/pilgrim-login')">
          <div class="absolute inset-0 opacity-0 group-hover:opacity-100 transition-opacity" style="background: radial-gradient(ellipse at 50% 0%, rgba(180,83,9,0.15) 0%, transparent 70%);"></div>
          <div class="relative p-6">
            <div class="w-14 h-14 rounded-xl flex items-center justify-center text-3xl mb-4 group-hover:scale-110 transition-transform border border-gold-800/50"
                 style="background: rgba(180,83,9,0.2);">
              🕌
            </div>
            <h2 class="text-xl font-bold text-white mb-2">Pilgrim Portal</h2>
            <p class="text-slate-400 text-sm leading-relaxed mb-4">
              Register for Arbaeen 2026, manage your family, track room & bus allocation
            </p>
            <div class="space-y-1 text-xs text-slate-500 mb-5">
              <p class="flex items-center gap-1.5"><span class="text-gold-500">✦</span> Family registration</p>
              <p class="flex items-center gap-1.5"><span class="text-gold-500">✦</span> Application status</p>
              <p class="flex items-center gap-1.5"><span class="text-gold-500">✦</span> Room & bus details</p>
              <p class="flex items-center gap-1.5"><span class="text-gold-500">✦</span> Live Karwan tracking</p>
            </div>
            <div class="flex items-center gap-2 text-gold-400 font-semibold text-sm group-hover:gap-3 transition-all">
              <span>Enter Portal</span><span>→</span>
            </div>
          </div>
        </div>

        <!-- Volunteer — center card slightly elevated -->
        <div class="group relative overflow-hidden rounded-2xl cursor-pointer transition-all duration-300 hover:-translate-y-1 md:-mt-2 md:shadow-2xl"
             style="background: rgba(10,20,8,0.9); border: 1px solid rgba(180,83,9,0.35);"
             @mouseenter="e => e.currentTarget.style.border='1px solid rgba(180,83,9,0.7)'"
             @mouseleave="e => e.currentTarget.style.border='1px solid rgba(180,83,9,0.35)'"
             @click="router.push('/volunteer-login')">
          <!-- Top gold line -->
          <div class="h-0.5 w-full" style="background: linear-gradient(90deg, transparent, #d97706, transparent);"></div>
          <div class="absolute inset-0 opacity-30" style="background: radial-gradient(ellipse at 50% 0%, rgba(180,83,9,0.2) 0%, transparent 70%);"></div>
          <div class="relative p-6">
            <div class="w-14 h-14 rounded-xl flex items-center justify-center text-3xl mb-4 group-hover:scale-110 transition-transform border border-gold-700/60"
                 style="background: rgba(180,83,9,0.3);">
              🤝
            </div>
            <h2 class="text-xl font-bold text-white mb-2">Volunteer Portal</h2>
            <p class="text-slate-400 text-sm leading-relaxed mb-4">
              View tasks, manage shifts, check in/out, coordinate with your team
            </p>
            <div class="space-y-1 text-xs text-slate-500 mb-5">
              <p class="flex items-center gap-1.5"><span class="text-gold-500">✦</span> Task management</p>
              <p class="flex items-center gap-1.5"><span class="text-gold-500">✦</span> Attendance & check-in</p>
              <p class="flex items-center gap-1.5"><span class="text-gold-500">✦</span> Emergency assignments</p>
              <p class="flex items-center gap-1.5"><span class="text-gold-500">✦</span> Shift scheduling</p>
            </div>
            <div class="flex items-center gap-2 text-gold-300 font-semibold text-sm group-hover:gap-3 transition-all">
              <span>Enter Portal</span><span>→</span>
            </div>
          </div>
        </div>

        <!-- Admin -->
        <div class="group relative overflow-hidden rounded-2xl cursor-pointer transition-all duration-300 hover:-translate-y-1"
             style="background: rgba(15,26,10,0.8); border: 1px solid rgba(180,83,9,0.2);"
             @mouseenter="e => e.currentTarget.style.border='1px solid rgba(180,83,9,0.6)'"
             @mouseleave="e => e.currentTarget.style.border='1px solid rgba(180,83,9,0.2)'"
             @click="router.push('/admin-login')">
          <div class="absolute inset-0 opacity-0 group-hover:opacity-100 transition-opacity" style="background: radial-gradient(ellipse at 50% 0%, rgba(180,83,9,0.15) 0%, transparent 70%);"></div>
          <div class="relative p-6">
            <div class="w-14 h-14 rounded-xl flex items-center justify-center text-3xl mb-4 group-hover:scale-110 transition-transform border border-gold-800/50"
                 style="background: rgba(180,83,9,0.2);">
              🛡️
            </div>
            <h2 class="text-xl font-bold text-white mb-2">Admin Panel</h2>
            <p class="text-slate-400 text-sm leading-relaxed mb-4">
              Full system control — pilgrims, volunteers, accommodation and notices
            </p>
            <div class="space-y-1 text-xs text-slate-500 mb-5">
              <p class="flex items-center gap-1.5"><span class="text-gold-500">✦</span> Pilgrim approvals</p>
              <p class="flex items-center gap-1.5"><span class="text-gold-500">✦</span> Room & bus allocation</p>
              <p class="flex items-center gap-1.5"><span class="text-gold-500">✦</span> Notice board</p>
              <p class="flex items-center gap-1.5"><span class="text-gold-500">✦</span> Full user control</p>
            </div>
            <div class="flex items-center gap-2 text-gold-400 font-semibold text-sm group-hover:gap-3 transition-all">
              <span>Enter Panel</span><span>→</span>
            </div>
          </div>
        </div>

      </div>

      <!-- Footer -->
      <div class="mt-12 text-center">
        <div class="flex items-center gap-2 justify-center mb-2">
          <div class="h-px w-10 bg-gold-800/50"></div>
          <span class="text-gold-700 text-xs">✦</span>
          <div class="h-px w-10 bg-gold-800/50"></div>
        </div>
        <p class="text-slate-600 text-xs">
          Misbah ul Hoda · misbahulhoda.org · Arbaeen 2026 · Built with ❤️ for the Pilgrims of Imam Husain (A.S.)
        </p>
      </div>

    </div>
  </div>
</template>
