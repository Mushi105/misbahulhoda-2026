<script setup>
import { ref, onMounted, onUnmounted } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'

const router = useRouter()
const auth   = useAuthStore()

const mobileMenuOpen = ref(false)
const scrolled       = ref(false)
const statsVisible   = ref(false)
const statsRef       = ref(null)

const stats = [
  { value: '5000+', label: 'Pilgrims Served',   icon: '🕌' },
  { value: '200+',  label: 'Volunteers',         icon: '🤝' },
  { value: '15+',   label: 'Years of Service',   icon: '📿' },
  { value: '12',    label: 'Countries Reached',  icon: '🌍' },
]

const features = [
  { icon: '🛡️', title: 'Smart Registration',    desc: 'Complete pilgrim profile with passport, family members, and visa management in minutes.' },
  { icon: '🏨', title: 'Room & Bus Allocation',  desc: 'Automated accommodation and transport assignment with real-time availability tracking.' },
  { icon: '📍', title: 'Live Karwan Tracking',   desc: 'Real-time GPS location of your Karwan so family members always know where you are.' },
  { icon: '📿', title: 'Majalis & Ziyarat',      desc: 'Complete schedule of Majalis, Molana profiles, Namaz timings, and Ziyarat guides.' },
  { icon: '🤝', title: 'Volunteer Network',       desc: 'Dedicated volunteers at every step — airport, accommodation, food, and guidance.' },
  { icon: '📲', title: 'Instant Notifications',  desc: 'WhatsApp and push alerts for approvals, departures, Majalis reminders, and emergencies.' },
]

const tours = [
  { name: 'Arbaeen Walk 2026',     dests: ['Karbala','Najaf'],              type:'Long',  icon:'🌹', c:'rgba(220,38,38,0.12)',  b:'rgba(220,38,38,0.25)',  t:'#fca5a5' },
  { name: 'Iraq + Iran Combined',  dests: ['Mashhad','Karbala','Najaf'],    type:'Long',  icon:'🕌', c:'rgba(16,185,129,0.10)', b:'rgba(16,185,129,0.25)', t:'#6ee7b7' },
  { name: 'Sham & Iraq Ziyarat',   dests: ['Damascus','Karbala'],           type:'Short', icon:'✨', c:'rgba(139,92,246,0.10)', b:'rgba(139,92,246,0.25)', t:'#c4b5fd' },
  { name: 'Hajj & Umrah Package',  dests: ['Makkah','Madinah'],             type:'Long',  icon:'⭐', c:'rgba(217,119,6,0.10)',  b:'rgba(217,119,6,0.25)',  t:'#fcd34d' },
]

const steps = [
  { n:'01', icon:'📝', title:'Register',      desc:'Create your account with personal, passport, and family details.' },
  { n:'02', icon:'✅', title:'Get Approved',   desc:'Admin reviews and approves your application within 24 hours.' },
  { n:'03', icon:'🏨', title:'Get Allocated',  desc:'Room, bus, and Karwan are assigned automatically.' },
  { n:'04', icon:'🕌', title:'Travel & Pray',  desc:'Arrive with full support from our dedicated volunteer network.' },
]

function onScroll() { scrolled.value = window.scrollY > 40 }
function goLogin()    { router.push('/login') }
function goRegister() { router.push('/login?tab=register') }

onMounted(() => {
  if (auth.isAuthenticated) {
    if (auth.isAdmin)      return router.replace('/admin/dashboard')
    if (auth.isPilgrim)    return router.replace('/pilgrim/portal')
    if (auth.isVolunteer)  return router.replace('/volunteer/dashboard')
  }
  window.addEventListener('scroll', onScroll)
  const io = new IntersectionObserver(([e]) => {
    if (e.isIntersecting) { statsVisible.value = true; io.disconnect() }
  }, { threshold: 0.25 })
  if (statsRef.value) io.observe(statsRef.value)
})
onUnmounted(() => window.removeEventListener('scroll', onScroll))
</script>

<template>
  <!-- ══ Full-canvas — Imam Zaman emerald theme ══ -->
  <div class="min-h-screen relative overflow-x-hidden" style="background:linear-gradient(160deg,#010f05 0%,#021a08 30%,#041a10 55%,#020d1a 80%,#020617 100%); color:#f1f5f9;">

    <!-- Global ambient glows -->
    <div class="fixed top-0 left-1/3 w-[700px] h-[700px] rounded-full pointer-events-none opacity-[0.08]"
         style="background:radial-gradient(circle,#10b981,transparent); animation:pulseGlow 6s ease-in-out infinite;"></div>
    <div class="fixed bottom-0 right-1/4 w-[500px] h-[500px] rounded-full pointer-events-none opacity-[0.06]"
         style="background:radial-gradient(circle,#10b981,transparent); animation:pulseGlow 9s ease-in-out infinite reverse;"></div>
    <div class="fixed top-1/2 left-1/2 -translate-x-1/2 -translate-y-1/2 w-[900px] h-[400px] rounded-full pointer-events-none opacity-[0.03]"
         style="background:radial-gradient(ellipse,#d97706,transparent); animation:pulseGlow 12s ease-in-out infinite;"></div>

    <!-- Global Islamic grid pattern -->
    <div class="fixed inset-0 pointer-events-none opacity-[0.03]"
         style="background-image:repeating-linear-gradient(0deg,transparent,transparent 40px,rgba(16,185,129,0.8) 40px,rgba(16,185,129,0.8) 41px),repeating-linear-gradient(90deg,transparent,transparent 40px,rgba(16,185,129,0.8) 40px,rgba(16,185,129,0.8) 41px),repeating-linear-gradient(45deg,transparent,transparent 28px,rgba(16,185,129,0.4) 28px,rgba(16,185,129,0.4) 29px),repeating-linear-gradient(-45deg,transparent,transparent 28px,rgba(16,185,129,0.4) 28px,rgba(16,185,129,0.4) 29px);"></div>

    <!-- Top accent bar -->
    <div class="fixed top-0 left-0 right-0 h-0.5 z-50 pointer-events-none"
         style="background:linear-gradient(90deg,transparent,#10b981,#d97706,#10b981,transparent);"></div>

    <!-- ══════════════ NAVBAR ══════════════ -->
    <nav :class="['fixed top-0.5 left-0 right-0 z-40 transition-all duration-300', scrolled ? 'py-2' : 'py-4']"
         :style="scrolled ? 'background:rgba(1,15,5,0.96); backdrop-filter:blur(20px); border-bottom:1px solid rgba(16,185,129,0.18); box-shadow:0 4px 32px rgba(0,0,0,0.4);' : ''">

      <div class="max-w-6xl mx-auto px-4 flex items-center justify-between">

        <!-- Logo -->
        <div class="flex items-center gap-3">
          <div class="w-9 h-9 rounded-full border flex items-center justify-center flex-shrink-0"
               style="background:radial-gradient(circle,#031a0a,#020617); border-color:rgba(16,185,129,0.4); box-shadow:0 0 14px rgba(16,185,129,0.2);">
            <img src="https://www.misbahulhoda.org/wp-content/uploads/2023/10/Logo-white-512-512.png"
                 alt="MH" class="w-7 h-7 object-contain" @error="e => e.target.style.display='none'" />
          </div>
          <div>
            <p class="text-white font-bold text-sm leading-none">Misbah ul Hoda</p>
            <p class="text-emerald-400 text-xs leading-none mt-0.5">Ziyarat with Ma'rifat</p>
          </div>
        </div>

        <!-- Desktop nav links -->
        <div class="hidden md:flex items-center gap-7 text-sm font-medium">
          <a href="#about"    class="text-slate-400 hover:text-emerald-400 transition-colors">About</a>
          <a href="#features" class="text-slate-400 hover:text-emerald-400 transition-colors">Features</a>
          <a href="#tours"    class="text-slate-400 hover:text-emerald-400 transition-colors">Tours</a>
          <a href="#contact"  class="text-slate-400 hover:text-emerald-400 transition-colors">Contact</a>
        </div>

        <!-- Buttons -->
        <div class="flex items-center gap-2">
          <button @click="goLogin"
            class="hidden sm:block text-sm font-medium px-4 py-2 rounded-lg text-slate-300 hover:text-white border transition-all"
            style="border-color:rgba(16,185,129,0.2); background:rgba(16,185,129,0.05);">
            Sign In
          </button>
          <button @click="goRegister"
            class="text-sm font-bold px-4 py-2 rounded-lg text-white transition-all hover:scale-105"
            style="background:linear-gradient(135deg,#065f46,#047857); box-shadow:0 4px 16px rgba(16,185,129,0.35);">
            Register Now
          </button>
          <button @click="mobileMenuOpen=!mobileMenuOpen" class="md:hidden ml-1 text-slate-400 hover:text-emerald-400 p-2 transition-colors">
            <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path v-if="!mobileMenuOpen" stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 6h16M4 12h16M4 18h16"/>
              <path v-else                stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12"/>
            </svg>
          </button>
        </div>
      </div>

      <!-- Mobile menu -->
      <transition name="fade">
        <div v-if="mobileMenuOpen" class="md:hidden mt-2 mx-4 rounded-2xl border p-4 space-y-3"
             style="background:rgba(2,20,10,0.96); border-color:rgba(16,185,129,0.2); backdrop-filter:blur(16px);">
          <a href="#about"    @click="mobileMenuOpen=false" class="block text-slate-300 hover:text-emerald-400 py-1.5 text-sm transition-colors">About</a>
          <a href="#features" @click="mobileMenuOpen=false" class="block text-slate-300 hover:text-emerald-400 py-1.5 text-sm transition-colors">Features</a>
          <a href="#tours"    @click="mobileMenuOpen=false" class="block text-slate-300 hover:text-emerald-400 py-1.5 text-sm transition-colors">Tours</a>
          <a href="#contact"  @click="mobileMenuOpen=false" class="block text-slate-300 hover:text-emerald-400 py-1.5 text-sm transition-colors">Contact</a>
          <div class="border-t pt-3 space-y-2" style="border-color:rgba(16,185,129,0.15);">
            <button @click="goLogin" class="w-full py-2.5 rounded-xl text-sm font-medium text-slate-300 border transition-all"
              style="border-color:rgba(16,185,129,0.2); background:rgba(16,185,129,0.05);">Sign In</button>
            <button @click="goRegister" class="w-full py-2.5 rounded-xl text-sm font-bold text-white"
              style="background:linear-gradient(135deg,#065f46,#047857); box-shadow:0 4px 16px rgba(16,185,129,0.3);">
              🕌 Register as Pilgrim
            </button>
          </div>
        </div>
      </transition>
    </nav>

    <!-- ══════════════ HERO ══════════════ -->
    <section class="relative min-h-screen flex items-center justify-center overflow-hidden pt-20">

      <!-- Extra hero glow -->
      <div class="absolute top-1/4 left-1/2 -translate-x-1/2 w-[600px] h-[600px] rounded-full pointer-events-none"
           style="background:radial-gradient(circle,rgba(16,185,129,0.12) 0%,transparent 65%);"></div>

      <!-- Bottom section fade -->
      <div class="absolute bottom-0 left-0 right-0 h-40 pointer-events-none"
           style="background:linear-gradient(to bottom,transparent,rgba(1,15,5,0.8));"></div>

      <div class="relative z-10 text-center px-4 max-w-5xl mx-auto">

        <!-- Bismillah -->
        <p class="font-arabic text-emerald-400 text-3xl md:text-4xl mb-3 leading-loose"
           style="text-shadow:0 0 30px rgba(16,185,129,0.5);">
          بِسْمِ اللَّهِ الرَّحْمَٰنِ الرَّحِيمِ
        </p>

        <!-- Ya Sahib al-Zaman divider -->
        <div class="flex items-center justify-center gap-3 mb-6">
          <div class="h-px flex-1 max-w-20" style="background:linear-gradient(to right,transparent,rgba(16,185,129,0.5));"></div>
          <span class="font-arabic text-emerald-300 text-lg" style="text-shadow:0 0 16px rgba(16,185,129,0.4);">يَا صَاحِبَ الزَّمَان</span>
          <div class="h-px flex-1 max-w-20" style="background:linear-gradient(to left,transparent,rgba(16,185,129,0.5));"></div>
        </div>

        <!-- Live pill -->
        <div class="inline-flex items-center gap-2 px-4 py-1.5 rounded-full border mb-8"
             style="background:rgba(16,185,129,0.08); border-color:rgba(16,185,129,0.3);">
          <span class="w-2 h-2 rounded-full bg-emerald-400 animate-pulse"></span>
          <span class="text-emerald-300 text-xs font-semibold tracking-wider uppercase">Arbaeen 2026 — Registrations Open</span>
        </div>

        <!-- Main heading -->
        <h1 class="text-4xl sm:text-5xl md:text-7xl font-bold text-white mb-5 leading-tight">
          Ziyarat with<br>
          <span style="background:linear-gradient(135deg,#34d399,#10b981,#6ee7b7); -webkit-background-clip:text; -webkit-text-fill-color:transparent; background-clip:text;">
            Ma'rifat
          </span>
        </h1>

        <p class="text-lg sm:text-xl md:text-2xl font-medium mb-3" style="color:#d1fae5;">
          Misbah ul Hoda — Arbaeen Pilgrimage Management
        </p>
        <p class="text-slate-400 text-sm sm:text-base max-w-2xl mx-auto mb-10 leading-relaxed">
          From Islamabad to Karbala — we handle every step of your sacred journey.
          Registration, accommodation, transport, Majalis, and live tracking — all in one place.
        </p>

        <!-- CTAs -->
        <div class="flex flex-col sm:flex-row items-center justify-center gap-4 mb-12">
          <button @click="goRegister"
            class="w-full sm:w-auto px-10 py-4 rounded-xl text-white font-bold text-lg transition-all duration-300 hover:scale-105"
            style="background:linear-gradient(135deg,#065f46,#047857,#059669); box-shadow:0 8px 32px rgba(16,185,129,0.45);">
            🕌 Start Your Journey
          </button>
          <button @click="goLogin"
            class="w-full sm:w-auto px-10 py-4 rounded-xl font-semibold text-emerald-300 hover:text-white text-lg border transition-all duration-300 hover:scale-105"
            style="border-color:rgba(16,185,129,0.35); background:rgba(16,185,129,0.06);">
            Already registered? Sign In →
          </button>
        </div>

        <!-- Trust badges -->
        <div class="flex flex-wrap items-center justify-center gap-4 sm:gap-8 text-xs text-slate-500">
          <span class="flex items-center gap-1.5"><span class="text-emerald-500">✦</span> Government Approved</span>
          <span class="flex items-center gap-1.5"><span class="text-emerald-500">✦</span> 15+ Years of Service</span>
          <span class="flex items-center gap-1.5"><span class="text-emerald-500">✦</span> 5000+ Pilgrims</span>
          <span class="flex items-center gap-1.5"><span class="text-emerald-500">✦</span> 24/7 Support</span>
        </div>
      </div>

      <!-- Floating Arabic — desktop -->
      <div class="hidden lg:block absolute top-1/3 left-8 font-arabic text-emerald-900/30 text-6xl select-none pointer-events-none" style="text-shadow:0 0 40px rgba(16,185,129,0.1);">الله</div>
      <div class="hidden lg:block absolute top-1/2 right-8 font-arabic text-emerald-900/25 text-5xl select-none pointer-events-none">محمد</div>
      <div class="hidden lg:block absolute bottom-1/3 left-1/4 font-arabic text-emerald-900/20 text-4xl select-none pointer-events-none">يَا عَلِيّ</div>
    </section>

    <!-- ══════════════ STATS ══════════════ -->
    <section ref="statsRef" id="about" class="py-16 px-4 relative z-10">
      <div class="max-w-5xl mx-auto">
        <div class="grid grid-cols-2 md:grid-cols-4 gap-4">
          <div v-for="(s, i) in stats" :key="s.label"
            class="rounded-2xl p-6 text-center transition-all duration-700"
            :class="statsVisible ? 'opacity-100 translate-y-0' : 'opacity-0 translate-y-10'"
            :style="`background:rgba(2,20,10,0.6); border:1px solid rgba(16,185,129,0.18); box-shadow:0 4px 24px rgba(0,0,0,0.3); backdrop-filter:blur(8px); transition-delay:${i*100}ms;`">
            <div class="text-3xl mb-2">{{ s.icon }}</div>
            <p class="text-2xl sm:text-3xl font-bold text-white">{{ s.value }}</p>
            <p class="text-emerald-500/70 text-xs sm:text-sm mt-1">{{ s.label }}</p>
          </div>
        </div>
      </div>
    </section>

    <!-- ══════════════ ABOUT STRIP ══════════════ -->
    <section class="py-8 px-4 relative z-10">
      <div class="max-w-4xl mx-auto rounded-3xl p-8 sm:p-12 text-center relative overflow-hidden"
           style="background:rgba(2,20,10,0.55); border:1px solid rgba(16,185,129,0.2); backdrop-filter:blur(12px); box-shadow:0 8px 40px rgba(0,0,0,0.3);">
        <div class="absolute inset-0 opacity-[0.06]"
             style="background:radial-gradient(circle at 20% 50%,#10b981,transparent 50%),radial-gradient(circle at 80% 50%,#d97706,transparent 50%);"></div>
        <div class="relative z-10">
          <p class="font-arabic text-emerald-300 text-2xl mb-1 leading-loose" style="text-shadow:0 0 20px rgba(16,185,129,0.3);">
            اللَّهُمَّ عَجِّلْ لِوَلِيِّكَ الْفَرَجَ
          </p>
          <p class="text-emerald-600 text-xs mb-4">O Allah, hasten the reappearance of Your Wali</p>
          <p class="text-slate-300 text-base sm:text-lg font-medium max-w-2xl mx-auto leading-relaxed">
            "Ziyarat with Ma'rifat" — Our mission is to transform every pilgrimage into a spiritual awakening.
            We don't just manage logistics; we nurture the connection between the pilgrim and the Imam (AS).
          </p>
          <div class="mt-6 flex items-center justify-center gap-2">
            <span class="text-emerald-600">✦</span>
            <span class="text-emerald-500 text-sm font-semibold uppercase tracking-wider">Misbah ul Hoda — misbahulhoda.org</span>
            <span class="text-emerald-600">✦</span>
          </div>
        </div>
      </div>
    </section>

    <!-- ══════════════ FEATURES ══════════════ -->
    <section id="features" class="py-16 px-4 relative z-10">
      <div class="max-w-6xl mx-auto">
        <div class="text-center mb-12">
          <p class="text-emerald-500 text-xs font-bold uppercase tracking-widest mb-3">Platform Features</p>
          <h2 class="text-3xl sm:text-4xl font-bold text-white mb-3">Everything for Your Sacred Journey</h2>
          <p class="text-slate-400 max-w-xl mx-auto">A complete digital companion for pilgrims, volunteers, and organizers — all under one roof.</p>
        </div>
        <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-5">
          <div v-for="f in features" :key="f.title"
            class="group rounded-2xl p-6 border transition-all duration-300 hover:-translate-y-1 cursor-default"
            style="background:rgba(2,20,10,0.5); border-color:rgba(16,185,129,0.15); backdrop-filter:blur(8px);"
            @mouseover="e => e.currentTarget.style.borderColor='rgba(16,185,129,0.4)'"
            @mouseleave="e => e.currentTarget.style.borderColor='rgba(16,185,129,0.15)'">
            <div class="w-12 h-12 rounded-xl flex items-center justify-center text-2xl mb-4"
                 style="background:rgba(16,185,129,0.12); border:1px solid rgba(16,185,129,0.25);">
              {{ f.icon }}
            </div>
            <h3 class="text-white font-semibold text-base mb-2 group-hover:text-emerald-300 transition-colors">{{ f.title }}</h3>
            <p class="text-slate-400 text-sm leading-relaxed">{{ f.desc }}</p>
          </div>
        </div>
      </div>
    </section>

    <!-- ══════════════ TOURS ══════════════ -->
    <section id="tours" class="py-16 px-4 relative z-10">
      <div class="max-w-6xl mx-auto">
        <div class="text-center mb-12">
          <p class="text-emerald-500 text-xs font-bold uppercase tracking-widest mb-3">2026 Packages</p>
          <h2 class="text-3xl sm:text-4xl font-bold text-white mb-3">Choose Your Ziyarat Journey</h2>
          <p class="text-slate-400 max-w-xl mx-auto">Carefully curated packages to ensure maximum spiritual benefit and comfort throughout your journey.</p>
        </div>
        <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-5">
          <div v-for="t in tours" :key="t.name"
            class="rounded-2xl overflow-hidden hover:-translate-y-1 transition-all duration-300 group"
            :style="`background:${t.c}; border:1px solid ${t.b}; backdrop-filter:blur(8px);`">
            <div class="h-0.5 w-full" :style="`background:linear-gradient(90deg,transparent,${t.t},transparent);`"></div>
            <div class="p-5">
              <div class="flex items-center justify-between mb-4">
                <span class="text-xs px-2.5 py-1 rounded-full border font-medium"
                      :style="`color:${t.t}; border-color:${t.b}; background:${t.c};`">
                  {{ t.icon }} {{ t.type }} Trip
                </span>
              </div>
              <h3 class="text-white font-bold text-base mb-3 group-hover:text-emerald-300 transition-colors">{{ t.name }}</h3>
              <div class="flex flex-wrap gap-1.5 mb-4">
                <span v-for="d in t.dests" :key="d"
                  class="text-xs px-2 py-0.5 rounded-full"
                  style="background:rgba(16,185,129,0.1); color:#6ee7b7; border:1px solid rgba(16,185,129,0.2);">
                  📍 {{ d }}
                </span>
              </div>
              <button @click="goRegister"
                class="w-full text-xs font-semibold py-2.5 rounded-xl border transition-all hover:scale-[1.02]"
                style="border-color:rgba(16,185,129,0.3); color:#6ee7b7; background:rgba(16,185,129,0.08);">
                Register Interest →
              </button>
            </div>
          </div>
        </div>
        <div class="mt-10 text-center">
          <p class="text-slate-500 text-sm mb-5">Prices and exact dates confirmed after registration. Early birds get priority allocation.</p>
          <button @click="goRegister"
            class="px-10 py-4 rounded-xl font-bold text-white transition-all hover:scale-105"
            style="background:linear-gradient(135deg,#065f46,#047857,#059669); box-shadow:0 8px 32px rgba(16,185,129,0.4);">
            🕌 Register for Arbaeen 2026 →
          </button>
        </div>
      </div>
    </section>

    <!-- ══════════════ HOW IT WORKS ══════════════ -->
    <section class="py-16 px-4 relative z-10">
      <div class="max-w-4xl mx-auto">
        <div class="text-center mb-12">
          <p class="text-emerald-500 text-xs font-bold uppercase tracking-widest mb-3">Simple Process</p>
          <h2 class="text-3xl sm:text-4xl font-bold text-white mb-3">How It Works</h2>
        </div>
        <div class="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-4 gap-6">
          <div v-for="(step, i) in steps" :key="step.n" class="text-center relative">
            <div class="inline-flex w-16 h-16 rounded-2xl items-center justify-center text-3xl mb-4"
                 style="background:rgba(16,185,129,0.1); border:1px solid rgba(16,185,129,0.25);">
              {{ step.icon }}
            </div>
            <div v-if="i < steps.length - 1" class="absolute top-8 left-[calc(50%+32px)] right-0 hidden md:block">
              <div class="h-px" style="background:linear-gradient(to right,rgba(16,185,129,0.4),transparent);"></div>
            </div>
            <p class="text-emerald-600 text-xs font-bold mb-1 uppercase tracking-wide">Step {{ step.n }}</p>
            <h4 class="text-white font-semibold mb-2">{{ step.title }}</h4>
            <p class="text-slate-400 text-sm leading-relaxed">{{ step.desc }}</p>
          </div>
        </div>
      </div>
    </section>

    <!-- ══════════════ CTA BANNER ══════════════ -->
    <section class="py-16 px-4 relative z-10">
      <div class="max-w-4xl mx-auto rounded-3xl overflow-hidden relative"
           style="background:rgba(2,20,10,0.7); border:1px solid rgba(16,185,129,0.25); backdrop-filter:blur(16px); box-shadow:0 20px 60px rgba(0,0,0,0.5), 0 0 60px rgba(16,185,129,0.06);">

        <!-- Pattern inside banner -->
        <div class="absolute inset-0 opacity-[0.05]"
             style="background-image:repeating-linear-gradient(0deg,transparent,transparent 28px,rgba(16,185,129,0.8) 28px,rgba(16,185,129,0.8) 29px),repeating-linear-gradient(90deg,transparent,transparent 28px,rgba(16,185,129,0.8) 28px,rgba(16,185,129,0.8) 29px);"></div>

        <!-- Inner glow -->
        <div class="absolute inset-0 opacity-10"
             style="background:radial-gradient(ellipse at 50% 0%,#10b981,transparent 60%);"></div>

        <div class="h-0.5 w-full" style="background:linear-gradient(90deg,transparent,#10b981,#d97706,#10b981,transparent);"></div>

        <div class="relative z-10 p-10 sm:p-16 text-center">
          <p class="font-arabic text-emerald-300 text-3xl mb-2 leading-loose" style="text-shadow:0 0 24px rgba(16,185,129,0.4);">
            لَبَّيْكَ يَا حُسَيْن
          </p>
          <p class="text-emerald-600 text-xs mb-6 uppercase tracking-widest">We answer your call, O Hussain</p>
          <h2 class="text-3xl sm:text-4xl font-bold text-white mb-4">Join Arbaeen 2026</h2>
          <p class="text-slate-300 max-w-lg mx-auto mb-8 text-base sm:text-lg leading-relaxed">
            Secure your place in the world's largest peaceful gathering.
            Karbala awaits. Let us handle everything while you focus on your Ma'rifat.
          </p>
          <div class="flex flex-col sm:flex-row gap-4 justify-center">
            <button @click="goRegister"
              class="px-10 py-4 rounded-xl font-bold text-white text-lg transition-all hover:scale-105"
              style="background:linear-gradient(135deg,#065f46,#047857,#059669); box-shadow:0 8px 32px rgba(16,185,129,0.5);">
              🕌 Register Now — Free
            </button>
            <button @click="goLogin"
              class="px-10 py-4 rounded-xl font-semibold text-emerald-300 text-lg border hover:text-white transition-all"
              style="border-color:rgba(16,185,129,0.35); background:rgba(16,185,129,0.06);">
              Already registered? Sign In
            </button>
          </div>
        </div>
      </div>
    </section>

    <!-- ══════════════ FOOTER ══════════════ -->
    <footer id="contact" class="py-12 px-4 relative z-10"
            style="border-top:1px solid rgba(16,185,129,0.15); background:rgba(1,9,4,0.7); backdrop-filter:blur(8px);">
      <div class="max-w-6xl mx-auto">

        <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-8 mb-10">

          <!-- Brand -->
          <div class="sm:col-span-2 lg:col-span-1">
            <div class="flex items-center gap-3 mb-4">
              <div class="w-11 h-11 rounded-full border flex items-center justify-center"
                   style="background:radial-gradient(circle,#031a0a,#020617); border-color:rgba(16,185,129,0.35); box-shadow:0 0 14px rgba(16,185,129,0.2);">
                <img src="https://www.misbahulhoda.org/wp-content/uploads/2023/10/Logo-white-512-512.png"
                     alt="Logo" class="w-9 h-9 object-contain" @error="e => e.target.style.display='none'" />
              </div>
              <div>
                <p class="text-white font-bold">Misbah ul Hoda</p>
                <p class="text-emerald-500 text-xs">Ziyarat with Ma'rifat</p>
              </div>
            </div>
            <p class="text-slate-500 text-sm leading-relaxed">Facilitating sacred journeys to the holy shrines since 2010. Serving pilgrims with love and dedication.</p>
          </div>

          <!-- Links -->
          <div>
            <p class="text-white font-semibold text-sm mb-4">Quick Links</p>
            <div class="space-y-2 text-sm text-slate-400">
              <a href="#about"    class="block hover:text-emerald-400 transition-colors">About Us</a>
              <a href="#features" class="block hover:text-emerald-400 transition-colors">Features</a>
              <a href="#tours"    class="block hover:text-emerald-400 transition-colors">Tour Packages</a>
              <button @click="goRegister" class="text-left hover:text-emerald-400 transition-colors">Register</button>
            </div>
          </div>

          <!-- Contact -->
          <div>
            <p class="text-white font-semibold text-sm mb-4">Contact</p>
            <div class="space-y-2 text-sm text-slate-400">
              <p>📧 info@misbahulhoda.org</p>
              <p>🌐 misbahulhoda.org</p>
              <p>📱 WhatsApp Support</p>
              <p>🕐 24/7 during Arbaeen</p>
            </div>
          </div>

          <!-- Platforms -->
          <div>
            <p class="text-white font-semibold text-sm mb-4">Platforms</p>
            <div class="space-y-3">
              <div class="flex items-center gap-3 p-3 rounded-xl border transition-all cursor-pointer"
                   style="border-color:rgba(16,185,129,0.2); background:rgba(16,185,129,0.05);"
                   @mouseover="e=>e.currentTarget.style.borderColor='rgba(16,185,129,0.4)'"
                   @mouseleave="e=>e.currentTarget.style.borderColor='rgba(16,185,129,0.2)'">
                <span class="text-xl">🌐</span>
                <div>
                  <p class="text-white text-xs font-medium">Web App</p>
                  <p class="text-slate-500 text-xs">app.misbahulhoda.org</p>
                </div>
              </div>
              <div class="flex items-center gap-3 p-3 rounded-xl border opacity-50 cursor-not-allowed"
                   style="border-color:rgba(16,185,129,0.1);">
                <span class="text-xl">📱</span>
                <div>
                  <p class="text-white text-xs font-medium">Mobile App</p>
                  <p class="text-slate-500 text-xs">Coming Soon — iOS & Android</p>
                </div>
              </div>
            </div>
          </div>
        </div>

        <!-- Bottom bar -->
        <div class="border-t pt-6 flex flex-col sm:flex-row items-center justify-between gap-3"
             style="border-color:rgba(16,185,129,0.1);">
          <p class="text-slate-600 text-xs text-center sm:text-left">
            © 2026 Misbah ul Hoda. All rights reserved. Built with ❤️ for the Lovers of Ahlul Bayt (AS)
          </p>
          <div class="flex items-center gap-2">
            <span class="w-2 h-2 rounded-full bg-emerald-500 animate-pulse"></span>
            <p class="text-emerald-500 text-xs font-medium">System Operational</p>
          </div>
        </div>

      </div>
    </footer>

  </div>
</template>

<style scoped>
@keyframes pulseGlow {
  0%, 100% { opacity: 0.06; transform: scale(1); }
  50%       { opacity: 0.14; transform: scale(1.06); }
}
.fade-enter-active, .fade-leave-active { transition: opacity 0.2s, transform 0.2s; }
.fade-enter-from, .fade-leave-to { opacity: 0; transform: translateY(-8px); }
</style>
