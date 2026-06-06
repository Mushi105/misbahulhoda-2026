<script setup>
import { ref, computed } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import NotificationBell from '@/components/NotificationBell.vue'
import MHLogo from '@/components/MHLogo.vue'

const auth   = useAuthStore()
const router = useRouter()
const route  = useRoute()

const drawerOpen = ref(false)
const appVersion = import.meta.env.VITE_APP_VERSION || 'dev'

function logout() { auth.logout(); router.replace('/') }
function navigate(path) { router.push(path); drawerOpen.value = false }

const pilgrimTabs = [
  { path: '/pilgrim/portal',    icon: '🕌', label: 'Portal'    },
  { path: '/pilgrim/tours',     icon: '🗓️', label: 'Apply'     },
  { path: '/pilgrim/guide',     icon: '📖', label: 'Guide'     },
  { path: '/pilgrim/documents', icon: '📂', label: 'Docs'      },
  { path: '/pilgrim/profile',   icon: '📋', label: 'Profile'   },
  { path: '/help',              icon: '❓', label: 'Help'       },
]

const volunteerTabs = [
  { path: '/volunteer/dashboard', icon: '🤝', label: 'Tasks'    },
  { path: '/tracking',            icon: '📍', label: 'Track'    },
  { path: '/majalis',             icon: '📿', label: 'Majalis'  },
  { path: '/volunteer/documents', icon: '📂', label: 'Docs'     },
  { path: '/help',                icon: '❓', label: 'Help'      },
]

const bottomTabs = computed(() => auth.isPilgrim ? pilgrimTabs : volunteerTabs)

function isActive(path) {
  return route.path === path || route.path.startsWith(path + '/')
}
</script>

<template>
  <div class="flex flex-col"
       style="height:100dvh; max-height:100dvh; overflow:hidden; background:linear-gradient(160deg,#010f05 0%,#021a08 40%,#020617 100%);">

    <!-- Ambient glow -->
    <div class="fixed top-0 left-0 w-72 h-72 rounded-full pointer-events-none opacity-[0.05]"
         style="background:radial-gradient(circle,#10b981,transparent);"></div>

    <!-- ══ TOP HEADER ══ -->
    <header class="flex-shrink-0 flex items-center gap-3 px-4 z-30"
            style="padding-top:max(12px,env(safe-area-inset-top)); padding-bottom:12px; background:rgba(1,12,6,0.95); border-bottom:1px solid rgba(16,185,129,0.15); backdrop-filter:blur(12px);">

      <!-- Hamburger (opens full drawer) -->
      <button @click="drawerOpen=true"
        class="text-slate-400 hover:text-emerald-400 p-2 rounded-xl hover:bg-emerald-900/20 transition-colors -ml-1"
        style="min-width:44px; min-height:44px;">
        <svg class="w-5 h-5 mx-auto" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 6h16M4 12h16M4 18h16"/>
        </svg>
      </button>

      <!-- Logo + Title -->
      <div class="flex items-center gap-2 flex-1">
        <MHLogo size="sm" />
        <div class="leading-none">
          <p class="text-white font-bold text-sm">Misbah ul Hoda</p>
          <p class="text-emerald-500 text-xs">{{ auth.isPilgrim ? 'Pilgrim Portal' : 'Volunteer Portal' }}</p>
        </div>
      </div>

      <!-- Arabic + Bell -->
      <span class="font-arabic text-emerald-700 text-xs hidden sm:block">بِسْمِ اللَّهِ</span>
      <NotificationBell />
    </header>

    <!-- ══ PAGE CONTENT ══ -->
    <main style="flex:1; min-height:0; overflow-y:auto; -webkit-overflow-scrolling:touch; overscroll-behavior-y:contain; background:linear-gradient(160deg,#010f05 0%,#021208 40%,#020617 100%); padding-bottom:calc(64px + env(safe-area-inset-bottom));">
      <div class="p-4">
        <router-view />
      </div>
    </main>

    <!-- ══ BOTTOM TAB BAR (mobile) ══ -->
    <nav class="flex-shrink-0 fixed bottom-0 left-0 right-0 z-30 flex items-stretch"
         style="background:rgba(1,12,6,0.97); border-top:1px solid rgba(16,185,129,0.2); backdrop-filter:blur(16px); padding-bottom:env(safe-area-inset-bottom);">
      <button
        v-for="tab in bottomTabs" :key="tab.path"
        @click="navigate(tab.path)"
        :class="['flex-1 flex flex-col items-center justify-center py-2 gap-0.5 transition-all',
          isActive(tab.path)
            ? 'text-emerald-400'
            : 'text-slate-600 active:text-emerald-500']"
        style="min-height:56px;">
        <span class="text-xl leading-none">{{ tab.icon }}</span>
        <span class="text-xs leading-none font-medium"
          :class="isActive(tab.path) ? 'text-emerald-400' : 'text-slate-600'">
          {{ tab.label }}
        </span>
        <!-- active indicator dot -->
        <div v-if="isActive(tab.path)"
          class="w-1 h-1 rounded-full bg-emerald-400 mt-0.5"></div>
      </button>
    </nav>

    <!-- ══ SLIDE-IN DRAWER (full nav) ══ -->
    <Transition name="drawer">
      <div v-if="drawerOpen" class="fixed inset-0 z-50 flex">

        <!-- Backdrop -->
        <div class="absolute inset-0 bg-black/60 backdrop-blur-sm"
             @click="drawerOpen=false"></div>

        <!-- Drawer panel -->
        <div class="relative w-72 max-w-[85vw] flex flex-col h-full z-10"
             style="background:linear-gradient(180deg,rgba(2,20,10,0.99) 0%,rgba(1,12,6,1) 100%); border-right:1px solid rgba(16,185,129,0.2);">

          <!-- Top accent -->
          <div class="h-0.5 w-full flex-shrink-0"
               style="background:linear-gradient(90deg,transparent,#10b981,#d97706,#10b981,transparent);"></div>

          <!-- Drawer header -->
          <div class="flex items-center gap-3 px-4 py-5 border-b" style="border-color:rgba(16,185,129,0.12);">
            <MHLogo size="sm" />
            <div class="flex-1">
              <p class="text-white font-bold text-sm">Misbah ul Hoda</p>
              <p class="text-emerald-500 text-xs">{{ auth.isPilgrim ? 'Pilgrim Portal' : 'Volunteer Portal' }} · 2026</p>
            </div>
            <button @click="drawerOpen=false"
              class="text-slate-500 hover:text-white p-2 rounded-xl hover:bg-white/10 transition-colors"
              style="min-width:44px; min-height:44px;">
              <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12"/>
              </svg>
            </button>
          </div>

          <!-- Nav links -->
          <nav class="flex-1 overflow-y-auto p-3 space-y-0.5">

            <!-- Pilgrim links -->
            <template v-if="auth.isPilgrim">
              <p class="text-emerald-800 text-xs font-bold uppercase tracking-widest px-3 pt-2 pb-1">My Journey</p>
              <button v-for="tab in pilgrimTabs" :key="tab.path"
                @click="navigate(tab.path)"
                :class="['w-full flex items-center gap-3 px-3 py-3 rounded-xl transition-all text-left',
                  isActive(tab.path)
                    ? 'bg-emerald-900/40 text-emerald-300 border border-emerald-800/50'
                    : 'text-slate-400 hover:bg-emerald-900/20 hover:text-emerald-200']">
                <span class="text-xl">{{ tab.icon }}</span>
                <span class="text-sm font-medium">{{ tab.label }}</span>
              </button>

              <p class="text-emerald-800 text-xs font-bold uppercase tracking-widest px-3 pt-4 pb-1">Live</p>
              <button @click="navigate('/tracking')"
                :class="['w-full flex items-center gap-3 px-3 py-3 rounded-xl transition-all text-left',
                  isActive('/tracking')
                    ? 'bg-emerald-900/40 text-emerald-300 border border-emerald-800/50'
                    : 'text-slate-400 hover:bg-emerald-900/20 hover:text-emerald-200']">
                <span class="text-xl">📍</span>
                <span class="text-sm font-medium">Live Tracking</span>
              </button>
              <button @click="navigate('/majalis')"
                :class="['w-full flex items-center gap-3 px-3 py-3 rounded-xl transition-all text-left',
                  isActive('/majalis')
                    ? 'bg-emerald-900/40 text-emerald-300 border border-emerald-800/50'
                    : 'text-slate-400 hover:bg-emerald-900/20 hover:text-emerald-200']">
                <span class="text-xl">📿</span>
                <span class="text-sm font-medium">Majalis</span>
              </button>
            </template>

            <!-- Volunteer links -->
            <template v-if="auth.isVolunteer">
              <p class="text-emerald-800 text-xs font-bold uppercase tracking-widest px-3 pt-2 pb-1">My Work</p>
              <button v-for="tab in volunteerTabs" :key="tab.path"
                @click="navigate(tab.path)"
                :class="['w-full flex items-center gap-3 px-3 py-3 rounded-xl transition-all text-left',
                  isActive(tab.path)
                    ? 'bg-emerald-900/40 text-emerald-300 border border-emerald-800/50'
                    : 'text-slate-400 hover:bg-emerald-900/20 hover:text-emerald-200']">
                <span class="text-xl">{{ tab.icon }}</span>
                <span class="text-sm font-medium">{{ tab.label }}</span>
              </button>
            </template>
          </nav>

          <!-- User footer -->
          <div class="flex-shrink-0 p-4" style="border-top:1px solid rgba(16,185,129,0.12);">
            <div class="flex items-center gap-3 mb-3">
              <div class="w-10 h-10 rounded-full flex items-center justify-center font-bold text-white flex-shrink-0"
                   style="background:linear-gradient(135deg,#065f46,#047857);">
                {{ auth.user?.fullName?.[0] || '?' }}
              </div>
              <div class="flex-1 min-w-0">
                <p class="text-white text-sm font-medium truncate">{{ auth.user?.fullName }}</p>
                <p class="text-emerald-500 text-xs truncate">{{ auth.role }}</p>
              </div>
            </div>
            <button @click="logout"
              class="w-full flex items-center justify-center gap-2 py-3 rounded-xl text-red-400 hover:bg-red-950/40 border border-red-900/40 text-sm font-medium transition-colors">
              <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                  d="M17 16l4-4m0 0l-4-4m4 4H7m6 4v1a3 3 0 01-3 3H6a3 3 0 01-3-3V7a3 3 0 013-3h4a3 3 0 013 3v1"/>
              </svg>
              Logout
            </button>
            <p class="text-center text-emerald-900 text-xs mt-3">{{ appVersion }}</p>
          </div>
        </div>
      </div>
    </Transition>

  </div>
</template>

<style scoped>
.drawer-enter-active, .drawer-leave-active { transition: opacity 0.25s ease; }
.drawer-enter-from, .drawer-leave-to { opacity: 0; }
.drawer-enter-active > div:last-child,
.drawer-leave-active > div:last-child { transition: transform 0.25s ease; }
.drawer-enter-from > div:last-child,
.drawer-leave-to > div:last-child { transform: translateX(-100%); }
</style>
