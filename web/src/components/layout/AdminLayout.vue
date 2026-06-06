<script setup>
import { ref } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import NotificationBell from '@/components/NotificationBell.vue'
import MHLogo from '@/components/MHLogo.vue'

const auth   = useAuthStore()
const router = useRouter()
const route  = useRoute()

const sidebarOpen = ref(false)   // collapsed by default, user can expand on desktop
const drawerOpen  = ref(false)   // mobile slide-in drawer

function logout() { auth.logout(); router.replace('/') }
function navigate(path) { router.push(path); drawerOpen.value = false }
function isActive(path) { return route.path === path || route.path.startsWith(path + '/') }

const navGroups = [
  {
    label: 'Main',
    links: [
      { path: '/admin/dashboard',  icon: '📊', label: 'Dashboard'  },
      { path: '/admin/pilgrims',   icon: '🕌', label: 'Pilgrims'   },
      { path: '/admin/volunteers', icon: '🤝', label: 'Volunteers' },
      { path: '/admin/users',      icon: '👥', label: 'Users'      },
    ]
  },
  {
    label: 'Operations',
    links: [
      { path: '/admin/tours',         icon: '🗓️', label: 'Tours'         },
      { path: '/admin/tour-tracking', icon: '🛰️', label: 'Tour Monitor'  },
      { path: '/admin/accommodation', icon: '🏨', label: 'Accommodation'  },
      { path: '/tracking',            icon: '📍', label: 'Live Tracking'  },
      { path: '/admin/itinerary',     icon: '✈️', label: 'Itinerary'     },
    ]
  },
  {
    label: 'Content',
    links: [
      { path: '/majalis',              icon: '📿', label: 'Majalis'       },
      { path: '/admin/scholars',       icon: '📚', label: 'Scholars'      },
      { path: '/admin/noticeboard',    icon: '📋', label: 'Notice Board'  },
      { path: '/admin/notifications',  icon: '🔔', label: 'Notifications' },
      { path: '/admin/documents',      icon: '📂', label: 'Documents'     },
    ]
  },
  {
    label: 'Finance',
    links: [
      { path: '/admin/finance',       icon: '💰', label: 'Finance'       },
      { path: '/admin/international', icon: '🌍', label: 'International' },
      { path: '/admin/reports',       icon: '📊', label: 'Reports'       },
    ]
  },
]
</script>

<template>
  <div class="flex overflow-hidden"
       style="height:100dvh; background:linear-gradient(160deg,#010f05 0%,#021a08 40%,#020617 100%);">

    <!-- Ambient glow -->
    <div class="fixed top-0 left-0 w-72 h-72 rounded-full pointer-events-none opacity-[0.06]"
         style="background:radial-gradient(circle,#10b981,transparent);"></div>

    <!-- ══ DESKTOP SIDEBAR ══ -->
    <aside :class="['hidden md:flex flex-shrink-0 flex-col transition-all duration-300 relative z-20',
                    sidebarOpen ? 'w-64' : 'w-16']"
           style="background:linear-gradient(180deg,rgba(2,20,10,0.95) 0%,rgba(1,12,6,0.98) 100%); border-right:1px solid rgba(16,185,129,0.15);">

      <!-- Top accent -->
      <div class="h-0.5 w-full flex-shrink-0"
           style="background:linear-gradient(90deg,transparent,#10b981,#d97706,#10b981,transparent);"></div>

      <!-- Logo -->
      <div class="flex-shrink-0 flex items-center gap-3 px-4 py-4 border-b" style="border-color:rgba(16,185,129,0.12);">
        <div class="relative flex-shrink-0">
          <div class="absolute inset-0 rounded-full opacity-30"
               style="background:radial-gradient(circle,#10b981,transparent); transform:scale(1.5);"></div>
          <MHLogo size="sm" />
        </div>
        <div v-if="sidebarOpen">
          <p class="text-white font-bold text-sm leading-none">Misbah ul Hoda</p>
          <p class="text-emerald-500 text-xs leading-none mt-0.5">Admin Panel · 2026</p>
        </div>
      </div>

      <!-- Nav links -->
      <nav class="flex-1 min-h-0 p-3 space-y-0.5 overflow-y-auto">
        <template v-for="group in navGroups" :key="group.label">
          <p v-if="sidebarOpen" class="text-emerald-800 text-xs font-bold uppercase tracking-widest px-3 pt-3 pb-1">
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
      <div class="flex-shrink-0 p-3" style="border-top:1px solid rgba(16,185,129,0.12);">
        <div class="flex items-center gap-3 px-2 py-2 rounded-xl" style="background:rgba(16,185,129,0.06);">
          <div class="w-8 h-8 rounded-full flex items-center justify-center text-sm font-bold text-white flex-shrink-0"
               style="background:linear-gradient(135deg,#065f46,#047857); box-shadow:0 0 10px rgba(16,185,129,0.3);">
            {{ auth.user?.fullName?.[0] || 'A' }}
          </div>
          <div v-if="sidebarOpen" class="flex-1 min-w-0">
            <p class="text-white text-sm font-medium truncate">{{ auth.user?.fullName }}</p>
            <p class="text-emerald-500 text-xs truncate">{{ auth.role }}</p>
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
    <div class="flex-1 flex flex-col overflow-hidden">

      <!-- ── TOP HEADER ── -->
      <header class="flex-shrink-0 px-4 flex items-center gap-3 z-30"
              style="padding-top:max(12px,env(safe-area-inset-top)); padding-bottom:12px; background:rgba(1,12,6,0.92); border-bottom:1px solid rgba(16,185,129,0.15); backdrop-filter:blur(12px);">

        <!-- Hamburger: desktop toggles sidebar, mobile opens drawer -->
        <button @click="window?.innerWidth < 768 ? (drawerOpen=true) : (sidebarOpen=!sidebarOpen)"
          class="text-slate-400 hover:text-emerald-400 transition-colors p-2 rounded-xl hover:bg-emerald-900/20"
          style="min-width:44px; min-height:44px;">
          <svg class="w-5 h-5 mx-auto" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 6h16M4 12h16M4 18h16"/>
          </svg>
        </button>

        <div class="flex items-center gap-2 flex-1">
          <span class="text-white font-semibold text-sm">Admin Panel</span>
          <span class="text-emerald-800 text-xs hidden md:block">· Arbaeen 2026</span>
        </div>

        <span class="font-arabic text-emerald-700 text-sm hidden lg:block">بِسْمِ اللَّهِ الرَّحْمَٰنِ الرَّحِيمِ</span>
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
      <main class="flex-1 overflow-y-auto"
            style="-webkit-overflow-scrolling:touch; overscroll-behavior-y:contain; background:linear-gradient(160deg,#010f05 0%,#021208 40%,#020617 100%); padding-bottom:env(safe-area-inset-bottom);">
        <div class="p-4 md:p-6">
          <router-view />
        </div>
      </main>
    </div>

    <!-- ══ MOBILE SLIDE-IN DRAWER ══ -->
    <Transition name="drawer">
      <div v-if="drawerOpen" class="fixed inset-0 z-50 flex md:hidden">

        <!-- Backdrop -->
        <div class="absolute inset-0 bg-black/60 backdrop-blur-sm"
             @click="drawerOpen=false"></div>

        <!-- Panel -->
        <div class="relative w-72 max-w-[85vw] flex flex-col h-full z-10"
             style="background:linear-gradient(180deg,rgba(2,20,10,0.99) 0%,rgba(1,12,6,1) 100%); border-right:1px solid rgba(16,185,129,0.2);">

          <div class="h-0.5 w-full flex-shrink-0"
               style="background:linear-gradient(90deg,transparent,#10b981,#d97706,#10b981,transparent);"></div>

          <!-- Drawer header -->
          <div class="flex items-center gap-3 px-4 py-4 border-b" style="border-color:rgba(16,185,129,0.12);">
            <MHLogo size="sm" />
            <div class="flex-1">
              <p class="text-white font-bold text-sm">Misbah ul Hoda</p>
              <p class="text-emerald-500 text-xs">Admin Panel · 2026</p>
            </div>
            <button @click="drawerOpen=false"
              class="text-slate-500 hover:text-white p-2 rounded-xl hover:bg-white/10 transition-colors"
              style="min-width:44px; min-height:44px;">
              <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12"/>
              </svg>
            </button>
          </div>

          <!-- Nav groups -->
          <nav class="flex-1 overflow-y-auto p-3 space-y-0.5">
            <template v-for="group in navGroups" :key="group.label">
              <p class="text-emerald-800 text-xs font-bold uppercase tracking-widest px-3 pt-4 pb-1 first:pt-2">
                {{ group.label }}
              </p>
              <button v-for="link in group.links" :key="link.path"
                @click="navigate(link.path)"
                :class="['w-full flex items-center gap-3 px-3 py-3 rounded-xl transition-all text-left',
                  isActive(link.path)
                    ? 'bg-emerald-900/40 text-emerald-300 border border-emerald-800/50'
                    : 'text-slate-400 hover:bg-emerald-900/20 hover:text-emerald-200']">
                <span class="text-xl">{{ link.icon }}</span>
                <span class="text-sm font-medium">{{ link.label }}</span>
              </button>
            </template>
          </nav>

          <!-- User footer -->
          <div class="flex-shrink-0 p-4" style="border-top:1px solid rgba(16,185,129,0.12);">
            <div class="flex items-center gap-3 mb-3">
              <div class="w-10 h-10 rounded-full flex items-center justify-center font-bold text-white flex-shrink-0"
                   style="background:linear-gradient(135deg,#065f46,#047857);">
                {{ auth.user?.fullName?.[0] || 'A' }}
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
.sidebar-link:hover { background: rgba(16,185,129,0.08); color: #6ee7b7; }
.sidebar-link.active { background: rgba(16,185,129,0.12); color: #6ee7b7; border-color: rgba(16,185,129,0.2); }

.drawer-enter-active, .drawer-leave-active { transition: opacity 0.25s ease; }
.drawer-enter-from, .drawer-leave-to { opacity: 0; }
.drawer-enter-active > div:last-child,
.drawer-leave-active > div:last-child { transition: transform 0.25s ease; }
.drawer-enter-from > div:last-child,
.drawer-leave-to > div:last-child { transform: translateX(-100%); }
</style>
