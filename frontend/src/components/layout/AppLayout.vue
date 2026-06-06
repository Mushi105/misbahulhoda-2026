<script setup>
import { ref, computed } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import NotificationBell from '@/components/NotificationBell.vue'
import MHLogo from '@/components/MHLogo.vue'

const auth   = useAuthStore()
const router = useRouter()
const route  = useRoute()

const sidebarOpen = ref(window.innerWidth >= 768)
const drawerOpen  = ref(false)
const appVersion  = import.meta.env.VITE_APP_VERSION || 'dev'

function logout() { auth.logout(); router.replace('/') }
function navigate(path) { router.push(path); drawerOpen.value = false }
function isActive(path) { return route.path === path || route.path.startsWith(path + '/') }
function onHamburger() {
  if (window.innerWidth < 768) drawerOpen.value = true
  else sidebarOpen.value = !sidebarOpen.value
}

const pilgrimTabs = [
  { path: '/pilgrim/portal',    icon: '🕌', label: 'Portal'    },
  { path: '/pilgrim/tours',     icon: '🗓️', label: 'Apply'     },
  { path: '/pilgrim/guide',     icon: '📖', label: 'Guide'     },
  { path: '/pilgrim/documents', icon: '📂', label: 'Docs'      },
  { path: '/pilgrim/profile',   icon: '📋', label: 'Profile'   },
  { path: '/pilgrim/help',      icon: '❓', label: 'Help'       },
]

const volunteerTabs = [
  { path: '/volunteer/dashboard', icon: '🤝', label: 'Tasks'    },
  { path: '/volunteer/tracking',  icon: '📍', label: 'Track'    },
  { path: '/volunteer/majalis',   icon: '📿', label: 'Majalis'  },
  { path: '/volunteer/documents', icon: '📂', label: 'Docs'     },
  { path: '/volunteer/help',      icon: '❓', label: 'Help'      },
]

const bottomTabs = computed(() => auth.isPilgrim ? pilgrimTabs : volunteerTabs)

const pilgrimGroups = [
  { label: 'My Journey', links: pilgrimTabs },
  { label: 'Live', links: [
    { path: '/pilgrim/tracking', icon: '📍', label: 'Live Tracking' },
    { path: '/pilgrim/majalis',  icon: '📿', label: 'Majalis'       },
  ]},
]

const volunteerGroups = [
  { label: 'My Work', links: volunteerTabs },
]

const navGroups = computed(() => auth.isPilgrim ? pilgrimGroups : volunteerGroups)
</script>

<template>
  <div class="flex"
       style="height:100dvh; max-height:100dvh; overflow:hidden; background:linear-gradient(160deg,#0a0800 0%,#0d0a00 50%,#060410 100%);">

    <!-- Ambient glow -->
    <div class="fixed top-0 left-0 w-72 h-72 rounded-full pointer-events-none opacity-[0.05]"
         style="background:radial-gradient(circle,#D4A800,transparent);"></div>

    <!-- ══ DESKTOP SIDEBAR ══ -->
    <aside :class="['hidden md:flex flex-shrink-0 flex-col transition-all duration-300 relative z-20',
                    sidebarOpen ? 'w-64' : 'w-16']"
           style="background:linear-gradient(180deg,rgba(10,10,10,0.95) 0%,rgba(8,8,8,0.98) 100%); border-right:1px solid rgba(212,168,0,0.15);">

      <!-- Top accent -->
      <div class="h-0.5 w-full flex-shrink-0"
           style="background:linear-gradient(90deg,transparent,#D4A800,#d97706,#D4A800,transparent);"></div>

      <!-- Logo -->
      <div class="flex-shrink-0 flex items-center gap-3 px-4 py-4 border-b" style="border-color:rgba(212,168,0,0.12);">
        <div class="relative flex-shrink-0">
          <div class="absolute inset-0 rounded-full opacity-30"
               style="background:radial-gradient(circle,#D4A800,transparent); transform:scale(1.5);"></div>
          <MHLogo size="sm" />
        </div>
        <div v-if="sidebarOpen">
          <p class="text-white font-bold text-sm leading-none">Misbah ul Hoda</p>
          <p class="text-xs mt-0.5" style="color:#D4A800;">{{ auth.isPilgrim ? 'Pilgrim Portal' : 'Volunteer Portal' }} · 2026</p>
        </div>
      </div>

      <!-- Nav links -->
      <nav class="flex-1 min-h-0 p-3 space-y-0.5 overflow-y-auto">
        <template v-for="group in navGroups" :key="group.label">
          <p v-if="sidebarOpen" class="text-xs font-bold uppercase tracking-widest px-3 pt-3 pb-1" style="color:rgba(212,168,0,0.55);">
            {{ group.label }}
          </p>
          <router-link v-for="link in group.links" :key="link.path"
            :to="link.path" class="sidebar-link" active-class="active">
            <span class="text-lg flex-shrink-0">{{ link.icon }}</span>
            <span v-if="sidebarOpen" class="text-sm">{{ link.label }}</span>
          </router-link>
        </template>
      </nav>

      <!-- User footer -->
      <div class="flex-shrink-0 p-3" style="border-top:1px solid rgba(212,168,0,0.12);">
        <div v-if="sidebarOpen" class="text-center mb-2">
          <span class="text-xs" style="color:rgba(212,168,0,0.4);">{{ appVersion }}</span>
        </div>
        <div class="flex items-center gap-3 px-2 py-2 rounded-xl" style="background:rgba(212,168,0,0.06);">
          <div class="w-8 h-8 rounded-full flex items-center justify-center text-sm font-bold text-white flex-shrink-0"
               style="background:linear-gradient(135deg,#b88a00,#D4A800); box-shadow:0 0 10px rgba(212,168,0,0.3);">
            {{ auth.user?.fullName?.[0] || '?' }}
          </div>
          <div v-if="sidebarOpen" class="flex-1 min-w-0">
            <p class="text-white text-sm font-medium truncate">{{ auth.user?.fullName }}</p>
            <p class="text-xs truncate" style="color:#D4A800;">{{ auth.role }}</p>
          </div>
          <button @click="logout" title="Logout"
            class="text-slate-500 hover:text-red-400 hover:bg-red-950/40 p-1.5 rounded-lg transition-colors flex-shrink-0">
            <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                d="M17 16l4-4m0 0l-4-4m4 4H7m6 4v1a3 3 0 01-3 3H6a3 3 0 01-3-3V7a3 3 0 013-3h4a3 3 0 013 3v1"/>
            </svg>
          </button>
        </div>
      </div>
    </aside>

    <!-- ══ MAIN AREA ══ -->
    <div class="flex-1 flex flex-col min-w-0" style="min-height:0; overflow:hidden;">

      <!-- ── TOP HEADER ── -->
      <header class="flex-shrink-0 px-4 flex items-center gap-3 z-30"
              style="padding-top:max(12px,env(safe-area-inset-top)); padding-bottom:12px; background:#000000; border-bottom:1px solid rgba(212,168,0,0.15); backdrop-filter:blur(12px);">

        <button @click="onHamburger"
          class="text-slate-400 hover:text-amber-400 transition-colors p-2 rounded-xl hover:bg-amber-900/20"
          style="min-width:44px; min-height:44px; touch-action:manipulation;">
          <svg class="w-5 h-5 mx-auto" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 6h16M4 12h16M4 18h16"/>
          </svg>
        </button>

        <!-- Logo + Title -->
        <div class="flex items-center gap-2 flex-1">
          <MHLogo size="sm" class="md:hidden" />
          <div class="leading-none md:hidden">
            <p class="text-white font-bold text-sm">Misbah ul Hoda</p>
            <p class="text-xs font-semibold" style="color:#D4A800;">{{ auth.isPilgrim ? 'Pilgrim Portal' : 'Volunteer Portal' }}</p>
          </div>
          <div class="hidden md:block">
            <span class="text-white font-semibold text-sm">{{ auth.isPilgrim ? 'Pilgrim Portal' : 'Volunteer Portal' }}</span>
            <span class="text-xs ml-2 hidden lg:inline" style="color:rgba(212,168,0,0.6);">· Arbaeen 2026</span>
          </div>
        </div>

        <span class="font-arabic text-xs hidden sm:block" style="color:rgba(212,168,0,0.6);">بِسْمِ اللَّهِ</span>
        <NotificationBell />
        <button @click="logout"
          class="hidden sm:flex items-center gap-1.5 text-slate-400 hover:text-red-400 hover:bg-red-950/40 px-3 py-2 rounded-lg text-sm transition-colors border border-transparent hover:border-red-900/50">
          <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
              d="M17 16l4-4m0 0l-4-4m4 4H7m6 4v1a3 3 0 01-3 3H6a3 3 0 01-3-3V7a3 3 0 013-3h4a3 3 0 013 3v1"/>
          </svg>
          Logout
        </button>
      </header>

      <!-- Page content -->
      <main style="flex:1; min-height:0; overflow-y:auto; -webkit-overflow-scrolling:touch; overscroll-behavior-y:contain; background:#f7f6f2; padding-bottom:calc(64px + env(safe-area-inset-bottom));">
        <div class="p-4 md:p-6">
          <router-view />
        </div>
      </main>
    </div>

    <!-- ══ BOTTOM TAB BAR (mobile only) ══ -->
    <nav class="md:hidden fixed bottom-0 left-0 right-0 z-30 flex items-stretch"
         style="background:#000000; border-top:1px solid rgba(212,168,0,0.2); backdrop-filter:blur(16px); padding-bottom:env(safe-area-inset-bottom);">
      <button
        v-for="tab in bottomTabs" :key="tab.path"
        @click="navigate(tab.path)"
        :class="['flex-1 flex flex-col items-center justify-center py-2 gap-0.5 transition-all',
          isActive(tab.path) ? '' : 'text-slate-500']"
        :style="isActive(tab.path) ? 'color:#D4A800;' : ''"
        style="min-height:56px;">
        <span class="text-xl leading-none">{{ tab.icon }}</span>
        <span class="text-xs leading-none font-medium">{{ tab.label }}</span>
        <div v-if="isActive(tab.path)" class="w-1 h-1 rounded-full mt-0.5" style="background:#D4A800;"></div>
      </button>
    </nav>

    <!-- ══ MOBILE SLIDE-IN DRAWER ══ -->
    <Transition name="drawer">
      <div v-if="drawerOpen" class="fixed inset-0 z-50 flex md:hidden">

        <!-- Backdrop -->
        <div class="absolute inset-0 bg-black/60 backdrop-blur-sm"
             @click="drawerOpen=false"></div>

        <!-- Drawer panel -->
        <div class="relative w-72 max-w-[85vw] flex flex-col h-full z-10"
             style="background:linear-gradient(180deg,rgba(10,10,10,0.99) 0%,rgba(8,8,8,1) 100%); border-right:1px solid rgba(212,168,0,0.2);">

          <div class="h-0.5 w-full flex-shrink-0"
               style="background:linear-gradient(90deg,transparent,#D4A800,#d97706,#D4A800,transparent);"></div>

          <!-- Drawer header -->
          <div class="flex items-center gap-3 px-4 py-5 border-b" style="border-color:rgba(212,168,0,0.12);">
            <MHLogo size="sm" />
            <div class="flex-1">
              <p class="text-white font-bold text-sm">Misbah ul Hoda</p>
              <p class="text-xs font-semibold" style="color:#D4A800;">{{ auth.isPilgrim ? 'Pilgrim Portal' : 'Volunteer Portal' }} · 2026</p>
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
            <template v-for="group in navGroups" :key="group.label">
              <p class="text-xs font-bold uppercase tracking-widest px-3 pt-4 pb-1 first:pt-2" style="color:rgba(212,168,0,0.55);">
                {{ group.label }}
              </p>
              <button v-for="link in group.links" :key="link.path"
                @click="navigate(link.path)"
                :class="['w-full flex items-center gap-3 px-3 py-3 rounded-xl transition-all text-left border',
                  isActive(link.path) ? '' : 'text-slate-400 border-transparent hover:bg-amber-900/20 hover:text-amber-300']"
                :style="isActive(link.path) ? 'background:rgba(212,168,0,0.12); color:#f5c800; border-color:rgba(212,168,0,0.3);' : ''">
                <span class="text-xl">{{ link.icon }}</span>
                <span class="text-sm font-medium">{{ link.label }}</span>
              </button>
            </template>
          </nav>

          <!-- User footer -->
          <div class="flex-shrink-0 p-4" style="border-top:1px solid rgba(212,168,0,0.12);">
            <div class="flex items-center gap-3 mb-3">
              <div class="w-10 h-10 rounded-full flex items-center justify-center font-bold text-white flex-shrink-0"
                   style="background:linear-gradient(135deg,#b88a00,#D4A800);">
                {{ auth.user?.fullName?.[0] || '?' }}
              </div>
              <div class="flex-1 min-w-0">
                <p class="text-white text-sm font-medium truncate">{{ auth.user?.fullName }}</p>
                <p class="text-xs font-semibold truncate" style="color:#D4A800;">{{ auth.role }}</p>
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
            <p class="text-center text-xs mt-3" style="color:rgba(212,168,0,0.4);">{{ appVersion }}</p>
          </div>
        </div>
      </div>
    </Transition>

  </div>
</template>

<style scoped>
.sidebar-link {
  display: flex; align-items: center; gap: 0.75rem;
  padding: 0.5rem 0.75rem; border-radius: 0.75rem;
  color: #94a3b8; transition: all 0.15s; font-size: 0.875rem;
  border: 1px solid transparent;
}
.sidebar-link:hover { background: rgba(212,168,0,0.08); color: #6ee7b7; }
.sidebar-link.active { background: rgba(212,168,0,0.12); color: #f5c800; border-color: rgba(212,168,0,0.25); }

.drawer-enter-active, .drawer-leave-active { transition: opacity 0.25s ease; }
.drawer-enter-from, .drawer-leave-to { opacity: 0; }
.drawer-enter-active > div:last-child,
.drawer-leave-active > div:last-child { transition: transform 0.25s ease; }
.drawer-enter-from > div:last-child,
.drawer-leave-to > div:last-child { transform: translateX(-100%); }
</style>
