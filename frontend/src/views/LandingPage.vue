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
  { value: '5000+', label: 'Pilgrims Served',  icon: '🕌' },
  { value: '200+',  label: 'Volunteers',        icon: '🤝' },
  { value: '15+',   label: 'Years of Service',  icon: '📿' },
  { value: '12',    label: 'Countries Reached', icon: '🌍' },
]

const features = [
  { icon: '🛡️', title: 'Smart Registration',   desc: 'Complete pilgrim profile with passport, family members, and visa management in minutes.' },
  { icon: '🏨', title: 'Room & Bus Allocation', desc: 'Automated accommodation and transport assignment with real-time availability tracking.' },
  { icon: '📍', title: 'Live Karwan Tracking',  desc: 'Real-time GPS location of your Karwan so family always knows where you are.' },
  { icon: '📿', title: 'Majalis & Ziyarat',     desc: 'Complete schedule of Majalis, Molana profiles, Namaz timings, and Ziyarat guides.' },
  { icon: '🤝', title: 'Volunteer Network',      desc: 'Dedicated volunteers at every step — airport, accommodation, food, and guidance.' },
  { icon: '📲', title: 'Instant Notifications', desc: 'Push alerts for approvals, departures, Majalis reminders, and emergencies.' },
]

const tours = [
  { name: 'Arbaeen Walk 2026',    dests: ['Karbala','Najaf'],           icon:'🌹' },
  { name: 'Iraq + Iran Combined', dests: ['Mashhad','Karbala','Najaf'], icon:'🕌' },
  { name: 'Sham & Iraq Ziyarat', dests: ['Damascus','Karbala'],        icon:'✨' },
  { name: 'Hajj & Umrah Package', dests: ['Makkah','Madinah'],         icon:'⭐' },
]

const steps = [
  { n:'01', icon:'📝', title:'Register',     desc:'Create your account with personal, passport, and family details.' },
  { n:'02', icon:'✅', title:'Get Approved',  desc:'Admin reviews and approves your application within 24 hours.' },
  { n:'03', icon:'🏨', title:'Get Allocated', desc:'Room, bus, and Karwan are assigned automatically.' },
  { n:'04', icon:'🕌', title:'Travel & Pray', desc:'Arrive with full support from our dedicated volunteer network.' },
]

function onScroll() { scrolled.value = window.scrollY > 50 }
function goLogin()    { router.push('/login') }
function goRegister() { router.push('/login?tab=register') }
function scrollTop()  { window.scrollTo({ top: 0, behavior: 'smooth' }) }

onMounted(() => {
  if (auth.isAuthenticated) {
    if (auth.isAdmin)     return router.replace('/admin/dashboard')
    if (auth.isPilgrim)   return router.replace('/pilgrim/portal')
    if (auth.isVolunteer) return router.replace('/volunteer/dashboard')
  }
  window.addEventListener('scroll', onScroll)
  const io = new IntersectionObserver(([e]) => {
    if (e.isIntersecting) { statsVisible.value = true; io.disconnect() }
  }, { threshold: 0.2 })
  if (statsRef.value) io.observe(statsRef.value)
})
onUnmounted(() => window.removeEventListener('scroll', onScroll))
</script>

<template>
  <div class="min-h-screen" style="font-family:'Inter',sans-serif; color:#1e293b;">

    <!-- ══ STICKY WHITE NAVBAR ══ -->
    <header :class="['fixed top-0 left-0 right-0 z-50 transition-all duration-300', scrolled ? 'shadow-md' : 'shadow-sm']"
            style="background:#ffffff; border-bottom:1px solid #e2e8f0;">
      <div class="max-w-7xl mx-auto px-4 sm:px-6">
        <div class="flex items-center justify-between" style="height:64px;">

          <!-- Logo -->
          <div class="flex items-center gap-3 flex-shrink-0">
            <div class="w-10 h-10 rounded-full overflow-hidden flex items-center justify-center flex-shrink-0"
                 style="border:2px solid #D4A800; background:#0d0a00;">
              <img src="https://www.misbahulhoda.org/wp-content/uploads/2023/10/Logo-white-512-512.png"
                   alt="Logo" class="w-8 h-8 object-contain" @error="e => e.target.style.display='none'" />
            </div>
            <div>
              <p class="font-bold text-sm leading-none" style="color:#1e293b;">Misbah ul Hoda</p>
              <p class="text-xs leading-none mt-0.5 font-semibold" style="color:#D4A800; letter-spacing:0.05em;">ZIYARAT WITH MA'RIFAT</p>
            </div>
          </div>

          <!-- Desktop Nav -->
          <nav class="hidden md:flex items-center gap-1">
            <a href="#home"     class="desk-link">Home</a>
            <a href="#about"    class="desk-link">About</a>
            <a href="#features" class="desk-link">Features</a>
            <a href="#tours"    class="desk-link">Tours 2026</a>
            <a href="#contact"  class="desk-link">Contact</a>
          </nav>

          <!-- Buttons -->
          <div class="flex items-center gap-2">
            <button @click="goLogin"
              class="hidden sm:block text-sm font-medium px-4 py-2 transition-all"
              style="color:#334155; border:1px solid #cbd5e1; border-radius:3px; background:#f8fafc;"
              onmouseover="this.style.borderColor='#D4A800';this.style.color='#9a6f00'"
              onmouseout="this.style.borderColor='#cbd5e1';this.style.color='#334155'">
              Sign In
            </button>
            <button @click="goRegister"
              class="text-sm font-bold px-5 py-2.5 transition-all hover:opacity-90"
              style="background:#D4A800; color:#000000; border-radius:3px; letter-spacing:0.03em;">
              REGISTER NOW
            </button>
            <button @click="mobileMenuOpen=!mobileMenuOpen" class="md:hidden p-2 ml-1" style="color:#334155;">
              <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path v-if="!mobileMenuOpen" stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 6h16M4 12h16M4 18h16"/>
                <path v-else stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12"/>
              </svg>
            </button>
          </div>
        </div>

        <!-- Mobile dropdown -->
        <transition name="slide-down">
          <div v-if="mobileMenuOpen" class="md:hidden pb-4 border-t" style="border-color:#e2e8f0;">
            <div class="pt-2 space-y-1">
              <a href="#home"     @click="mobileMenuOpen=false" class="mob-link">Home</a>
              <a href="#about"    @click="mobileMenuOpen=false" class="mob-link">About</a>
              <a href="#features" @click="mobileMenuOpen=false" class="mob-link">Features</a>
              <a href="#tours"    @click="mobileMenuOpen=false" class="mob-link">Tours 2026</a>
              <a href="#contact"  @click="mobileMenuOpen=false" class="mob-link">Contact</a>
            </div>
            <div class="flex gap-2 pt-3">
              <button @click="goLogin" class="flex-1 py-2.5 text-sm font-medium border"
                style="color:#334155; border-color:#cbd5e1; border-radius:3px;">Sign In</button>
              <button @click="goRegister" class="flex-1 py-2.5 text-sm font-bold"
                style="background:#D4A800; color:#000; border-radius:3px;">REGISTER</button>
            </div>
          </div>
        </transition>
      </div>
    </header>

    <!-- ══ HERO — DARK ══ -->
    <section id="home" class="relative flex items-center justify-center overflow-hidden"
             style="min-height:100vh; padding-top:64px; background:#1d2931;">

      <!-- BG layers -->
      <div class="absolute inset-0 pointer-events-none">
        <div class="absolute inset-0" style="background:linear-gradient(160deg,#1d2931 0%,#0f1923 40%,#1a1400 70%,#0d0900 100%); opacity:0.96;"></div>
        <div class="absolute inset-0" style="background:radial-gradient(ellipse at 60% 40%,rgba(212,168,0,0.1) 0%,transparent 65%);"></div>
        <div class="absolute inset-0" style="opacity:0.03; background-image:repeating-linear-gradient(0deg,transparent,transparent 48px,rgba(212,168,0,1) 48px,rgba(212,168,0,1) 49px),repeating-linear-gradient(90deg,transparent,transparent 48px,rgba(212,168,0,1) 48px,rgba(212,168,0,1) 49px);"></div>
      </div>
      <div class="absolute top-0 left-0 right-0 h-1 pointer-events-none" style="background:#D4A800;"></div>
      <div class="absolute left-6 top-1/3 font-arabic select-none pointer-events-none hidden lg:block" style="font-size:100px; color:rgba(212,168,0,0.05); line-height:1;">الله</div>
      <div class="absolute right-6 bottom-1/3 font-arabic select-none pointer-events-none hidden lg:block" style="font-size:84px; color:rgba(212,168,0,0.04); line-height:1;">محمد</div>

      <div class="relative z-10 text-center px-4 max-w-5xl mx-auto py-20">

        <!-- Registration Open badge -->
        <div class="inline-flex items-center gap-2 px-4 py-1.5 mb-8"
             style="background:rgba(212,168,0,0.1); border:1px solid rgba(212,168,0,0.35); border-radius:2px;">
          <span class="w-2 h-2 rounded-full animate-pulse" style="background:#D4A800;"></span>
          <span class="text-xs font-bold tracking-widest uppercase" style="color:#D4A800;">Arbaeen 2026 — Registrations Open</span>
        </div>

        <!-- Bismillah -->
        <p class="font-arabic text-3xl md:text-4xl mb-6 leading-loose"
           style="color:#D4A800; text-shadow:0 0 30px rgba(212,168,0,0.25);">
          بِسْمِ اللَّهِ الرَّحْمَٰنِ الرَّحِيمِ
        </p>

        <!-- Main headings — styled like their large text -->
        <h1 class="font-bold text-white mb-3 leading-tight"
            style="font-size:clamp(2.2rem,6vw,5rem); letter-spacing:0.02em; text-shadow:0 2px 40px rgba(0,0,0,0.5);">
          MISBA-UL-HODA
        </h1>
        <h2 class="font-bold mb-8"
            style="font-size:clamp(1.4rem,4vw,2.8rem); background:linear-gradient(135deg,#f5c800,#D4A800); -webkit-background-clip:text; -webkit-text-fill-color:transparent; background-clip:text; letter-spacing:0.06em;">
          ARBAEEN 2026
        </h2>

        <!-- Divider -->
        <div class="flex items-center justify-center gap-4 mb-8">
          <div class="h-px flex-1 max-w-28" style="background:linear-gradient(to right,transparent,rgba(212,168,0,0.5));"></div>
          <span class="font-arabic text-lg" style="color:rgba(212,168,0,0.7);">يَا صَاحِبَ الزَّمَان</span>
          <div class="h-px flex-1 max-w-28" style="background:linear-gradient(to left,transparent,rgba(212,168,0,0.5));"></div>
        </div>

        <p class="text-lg md:text-xl mb-12 max-w-2xl mx-auto leading-relaxed"
           style="color:rgba(255,255,255,0.6);">
          Ziyarat with Ma'rifat — From Islamabad to Karbala, we manage every step of your sacred journey.
          Registration, accommodation, transport, Majalis, and live tracking.
        </p>

        <!-- CTAs — sharp corners like their site -->
        <div class="flex flex-col sm:flex-row items-center justify-center gap-4 mb-12">
          <button @click="goRegister"
            class="w-full sm:w-auto px-10 py-4 font-bold text-base md:text-lg transition-all hover:opacity-90 hover:-translate-y-0.5"
            style="background:#D4A800; color:#000000; border-radius:3px; letter-spacing:0.04em; box-shadow:0 8px 32px rgba(212,168,0,0.4);">
            🕌 REGISTER NOW — FREE
          </button>
          <button @click="goLogin"
            class="w-full sm:w-auto px-10 py-4 font-bold text-base md:text-lg border transition-all hover:-translate-y-0.5"
            style="border-color:rgba(255,255,255,0.25); color:rgba(255,255,255,0.85); border-radius:3px; background:transparent;"
            onmouseover="this.style.borderColor='#D4A800';this.style.color='#D4A800'"
            onmouseout="this.style.borderColor='rgba(255,255,255,0.25)';this.style.color='rgba(255,255,255,0.85)'">
            SIGN IN →
          </button>
        </div>

        <!-- Trust chips -->
        <div class="flex flex-wrap items-center justify-center gap-3">
          <span v-for="t in ['Government Approved','15+ Years of Service','5000+ Pilgrims','24/7 Support']" :key="t"
            class="inline-flex items-center gap-1.5 px-3 py-1.5 text-xs font-medium"
            style="background:rgba(255,255,255,0.05); border:1px solid rgba(255,255,255,0.1); border-radius:2px; color:rgba(255,255,255,0.55);">
            <span style="color:#D4A800;">✓</span> {{ t }}
          </span>
        </div>
      </div>

      <!-- Scroll arrow -->
      <div class="absolute bottom-8 left-1/2 -translate-x-1/2 flex flex-col items-center gap-1 animate-bounce" style="color:rgba(212,168,0,0.4);">
        <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 9l-7 7-7-7"/>
        </svg>
      </div>
    </section>

    <!-- ══ STATS — LIGHT ══ -->
    <section ref="statsRef" id="about" style="background:#f8fafc; border-top:4px solid #D4A800; border-bottom:1px solid #e2e8f0;">
      <div class="max-w-5xl mx-auto px-4 py-14 grid grid-cols-2 md:grid-cols-4 gap-8">
        <div v-for="(s, i) in stats" :key="s.label"
          class="text-center transition-all duration-700"
          :class="statsVisible ? 'opacity-100 translate-y-0' : 'opacity-0 translate-y-8'"
          :style="`transition-delay:${i*120}ms`">
          <div class="text-3xl mb-2">{{ s.icon }}</div>
          <p class="text-3xl font-bold mb-1" style="color:#1e293b;">{{ s.value }}</p>
          <div class="h-0.5 w-8 mx-auto mb-2" style="background:#D4A800;"></div>
          <p class="text-sm font-medium" style="color:#64748b;">{{ s.label }}</p>
        </div>
      </div>
    </section>

    <!-- ══ ABOUT / MISSION — WHITE ══ -->
    <section style="background:#ffffff; padding:80px 16px;">
      <div class="max-w-4xl mx-auto text-center">
        <p class="text-xs font-bold uppercase tracking-widest mb-4" style="color:#D4A800; letter-spacing:0.15em;">Our Mission</p>
        <h2 class="text-3xl md:text-4xl font-bold mb-5" style="color:#1e293b;">Ziyarat with Ma'rifat</h2>
        <div class="flex items-center justify-center gap-3 mb-8">
          <div class="h-0.5 w-14" style="background:#D4A800;"></div>
          <div class="w-2 h-2 rounded-full" style="background:#D4A800;"></div>
          <div class="h-0.5 w-14" style="background:#D4A800;"></div>
        </div>
        <p class="font-arabic text-3xl leading-loose mb-2" style="color:#D4A800;">اللَّهُمَّ عَجِّلْ لِوَلِيِّكَ الْفَرَجَ</p>
        <p class="text-xs mb-8 font-medium uppercase tracking-wider" style="color:#94a3b8;">O Allah, hasten the reappearance of Your Wali</p>
        <p class="text-lg leading-relaxed max-w-2xl mx-auto mb-10" style="color:#475569;">
          Our mission is to transform every pilgrimage into a spiritual awakening.
          We don't just manage logistics — we nurture the connection between every pilgrim
          and the Imam (AS). From Islamabad to Karbala, we are with you every step of the way.
        </p>
        <button @click="goRegister"
          class="px-10 py-4 font-bold text-base transition-all hover:opacity-90"
          style="background:#D4A800; color:#000; border-radius:3px; letter-spacing:0.04em;">
          JOIN ARBAEEN 2026
        </button>
      </div>
    </section>

    <!-- ══ FEATURES — LIGHT ══ -->
    <section id="features" style="background:#f8fafc; padding:80px 16px; border-top:1px solid #e2e8f0;">
      <div class="max-w-6xl mx-auto">
        <div class="text-center mb-14">
          <p class="text-xs font-bold uppercase tracking-widest mb-3" style="color:#D4A800; letter-spacing:0.15em;">Platform Features</p>
          <h2 class="text-3xl md:text-4xl font-bold mb-5" style="color:#1e293b;">Everything for Your Sacred Journey</h2>
          <div class="flex items-center justify-center gap-3">
            <div class="h-0.5 w-12" style="background:#D4A800;"></div>
            <div class="w-1.5 h-1.5 rounded-full" style="background:#D4A800;"></div>
            <div class="h-0.5 w-12" style="background:#D4A800;"></div>
          </div>
        </div>
        <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-5">
          <div v-for="f in features" :key="f.title"
            class="bg-white p-6 transition-all duration-300 hover:-translate-y-1"
            style="border:1px solid #e2e8f0; border-radius:4px; box-shadow:0 1px 6px rgba(0,0,0,0.04);"
            @mouseover="e=>{ e.currentTarget.style.borderColor='#D4A800'; e.currentTarget.style.boxShadow='0 8px 24px rgba(0,0,0,0.08)'; }"
            @mouseleave="e=>{ e.currentTarget.style.borderColor='#e2e8f0'; e.currentTarget.style.boxShadow='0 1px 6px rgba(0,0,0,0.04)'; }">
            <div class="w-12 h-12 flex items-center justify-center text-2xl mb-4"
                 style="background:rgba(212,168,0,0.1); border:1px solid rgba(212,168,0,0.3); border-radius:3px;">
              {{ f.icon }}
            </div>
            <h3 class="font-bold text-base mb-2" style="color:#1e293b;">{{ f.title }}</h3>
            <p class="text-sm leading-relaxed" style="color:#64748b;">{{ f.desc }}</p>
          </div>
        </div>
      </div>
    </section>

    <!-- ══ TOURS — WHITE ══ -->
    <section id="tours" style="background:#ffffff; padding:80px 16px; border-top:1px solid #e2e8f0;">
      <div class="max-w-6xl mx-auto">
        <div class="text-center mb-14">
          <p class="text-xs font-bold uppercase tracking-widest mb-3" style="color:#D4A800; letter-spacing:0.15em;">2026 Packages</p>
          <h2 class="text-3xl md:text-4xl font-bold mb-5" style="color:#1e293b;">Choose Your Ziyarat Journey</h2>
          <div class="flex items-center justify-center gap-3">
            <div class="h-0.5 w-12" style="background:#D4A800;"></div>
            <div class="w-1.5 h-1.5 rounded-full" style="background:#D4A800;"></div>
            <div class="h-0.5 w-12" style="background:#D4A800;"></div>
          </div>
        </div>
        <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-5">
          <div v-for="t in tours" :key="t.name"
            class="transition-all duration-300 hover:-translate-y-1 overflow-hidden"
            style="border:1px solid #e2e8f0; border-radius:4px; background:#ffffff; box-shadow:0 1px 6px rgba(0,0,0,0.04);"
            @mouseover="e=>{ e.currentTarget.style.borderColor='#D4A800'; e.currentTarget.style.boxShadow='0 8px 24px rgba(0,0,0,0.08)'; }"
            @mouseleave="e=>{ e.currentTarget.style.borderColor='#e2e8f0'; e.currentTarget.style.boxShadow='0 1px 6px rgba(0,0,0,0.04)'; }">
            <div class="h-1" style="background:#D4A800;"></div>
            <div class="p-5">
              <div class="text-3xl mb-3">{{ t.icon }}</div>
              <h3 class="font-bold text-sm mb-3" style="color:#1e293b;">{{ t.name }}</h3>
              <div class="flex flex-wrap gap-1.5 mb-4">
                <span v-for="d in t.dests" :key="d"
                  class="text-xs px-2 py-0.5 font-medium"
                  style="background:rgba(212,168,0,0.1); color:#9a6f00; border:1px solid rgba(212,168,0,0.25); border-radius:2px;">
                  📍 {{ d }}
                </span>
              </div>
              <button @click="goRegister"
                class="w-full text-xs font-bold py-2.5 transition-all"
                style="border:1px solid #D4A800; color:#9a6f00; background:rgba(212,168,0,0.06); border-radius:3px;"
                onmouseover="this.style.background='#D4A800';this.style.color='#000'"
                onmouseout="this.style.background='rgba(212,168,0,0.06)';this.style.color='#9a6f00'">
                Register Interest →
              </button>
            </div>
          </div>
        </div>
        <div class="mt-12 text-center">
          <button @click="goRegister"
            class="px-12 py-4 font-bold text-base transition-all hover:opacity-90"
            style="background:#D4A800; color:#000; border-radius:3px; letter-spacing:0.04em; box-shadow:0 4px 20px rgba(212,168,0,0.3);">
            🕌 REGISTER FOR ARBAEEN 2026
          </button>
        </div>
      </div>
    </section>

    <!-- ══ HOW IT WORKS — LIGHT ══ -->
    <section style="background:#f8fafc; padding:80px 16px; border-top:1px solid #e2e8f0;">
      <div class="max-w-4xl mx-auto">
        <div class="text-center mb-14">
          <p class="text-xs font-bold uppercase tracking-widest mb-3" style="color:#D4A800; letter-spacing:0.15em;">Simple Process</p>
          <h2 class="text-3xl md:text-4xl font-bold mb-5" style="color:#1e293b;">How It Works</h2>
          <div class="flex items-center justify-center gap-3">
            <div class="h-0.5 w-12" style="background:#D4A800;"></div>
            <div class="w-1.5 h-1.5 rounded-full" style="background:#D4A800;"></div>
            <div class="h-0.5 w-12" style="background:#D4A800;"></div>
          </div>
        </div>
        <div class="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-4 gap-8">
          <div v-for="(step, i) in steps" :key="step.n" class="text-center relative">
            <div v-if="i < steps.length - 1" class="absolute top-8 left-[calc(50%+32px)] right-0 hidden md:block">
              <div class="h-0.5" style="background:linear-gradient(to right,#D4A800,rgba(212,168,0,0.15));"></div>
            </div>
            <div class="w-16 h-16 mx-auto flex items-center justify-center text-2xl mb-4"
                 style="background:rgba(212,168,0,0.1); border:2px solid #D4A800; border-radius:3px;">
              {{ step.icon }}
            </div>
            <p class="text-xs font-bold mb-1.5 uppercase tracking-wider" style="color:#D4A800;">Step {{ step.n }}</p>
            <h4 class="font-bold mb-2" style="color:#1e293b;">{{ step.title }}</h4>
            <p class="text-sm leading-relaxed" style="color:#64748b;">{{ step.desc }}</p>
          </div>
        </div>
      </div>
    </section>

    <!-- ══ CTA BANNER — DARK ══ -->
    <section class="relative overflow-hidden" style="background:#1d2931; padding:80px 16px;">
      <div class="absolute top-0 left-0 right-0 h-1" style="background:#D4A800;"></div>
      <div class="absolute inset-0 pointer-events-none" style="background:radial-gradient(ellipse at 50% 0%,rgba(212,168,0,0.09),transparent 60%);"></div>
      <div class="relative z-10 max-w-3xl mx-auto text-center">
        <p class="font-arabic text-4xl mb-2 leading-loose" style="color:#D4A800;">لَبَّيْكَ يَا حُسَيْن</p>
        <p class="text-xs mb-10 uppercase tracking-widest font-medium" style="color:rgba(212,168,0,0.45);">We answer your call, O Hussain</p>
        <h2 class="text-3xl md:text-4xl font-bold text-white mb-5">Join Arbaeen 2026</h2>
        <p class="text-lg mb-10 leading-relaxed max-w-xl mx-auto" style="color:rgba(255,255,255,0.55);">
          Secure your place in the world's largest peaceful gathering.
          Karbala awaits — let us handle everything while you focus on your Ma'rifat.
        </p>
        <div class="flex flex-col sm:flex-row gap-4 justify-center">
          <button @click="goRegister"
            class="px-10 py-4 font-bold text-base transition-all hover:opacity-90"
            style="background:#D4A800; color:#000; border-radius:3px; letter-spacing:0.04em; box-shadow:0 8px 32px rgba(212,168,0,0.4);">
            🕌 REGISTER NOW — FREE
          </button>
          <button @click="goLogin"
            class="px-10 py-4 font-bold text-base border transition-all"
            style="border-color:rgba(255,255,255,0.2); color:rgba(255,255,255,0.8); border-radius:3px;"
            onmouseover="this.style.borderColor='#D4A800';this.style.color='#D4A800'"
            onmouseout="this.style.borderColor='rgba(255,255,255,0.2)';this.style.color='rgba(255,255,255,0.8)'">
            SIGN IN
          </button>
        </div>
      </div>
    </section>

    <!-- ══ FOOTER — DARK ══ -->
    <footer id="contact" style="background:#111111; padding:60px 16px 0;">
      <div class="h-1 -mt-px" style="background:linear-gradient(90deg,transparent,#D4A800,#f5c800,#D4A800,transparent);"></div>
      <div class="max-w-6xl mx-auto pt-12">
        <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-10 mb-12">

          <!-- Brand -->
          <div class="sm:col-span-2 lg:col-span-1">
            <div class="flex items-center gap-3 mb-5">
              <div class="w-11 h-11 rounded-full flex items-center justify-center flex-shrink-0"
                   style="background:#1d2931; border:2px solid #D4A800;">
                <img src="https://www.misbahulhoda.org/wp-content/uploads/2023/10/Logo-white-512-512.png"
                     alt="Logo" class="w-9 h-9 object-contain" @error="e => e.target.style.display='none'" />
              </div>
              <div>
                <p class="text-white font-bold">Misbah ul Hoda</p>
                <p class="text-xs font-bold" style="color:#D4A800; letter-spacing:0.06em;">ZIYARAT WITH MA'RIFAT</p>
              </div>
            </div>
            <p class="text-sm leading-relaxed" style="color:rgba(255,255,255,0.38);">
              Facilitating sacred journeys to the holy shrines since 2010. Serving pilgrims with love and dedication.
            </p>
          </div>

          <!-- Quick Links -->
          <div>
            <p class="text-white font-bold text-sm mb-5 uppercase" style="letter-spacing:0.08em;">Quick Links</p>
            <div class="space-y-2.5 text-sm" style="color:rgba(255,255,255,0.42);">
              <a href="#home"     class="block footer-link">Home</a>
              <a href="#about"    class="block footer-link">About Us</a>
              <a href="#features" class="block footer-link">Features</a>
              <a href="#tours"    class="block footer-link">Tour Packages</a>
              <button @click="goRegister" class="text-left footer-link">Register Now</button>
            </div>
          </div>

          <!-- Contact -->
          <div>
            <p class="text-white font-bold text-sm mb-5 uppercase" style="letter-spacing:0.08em;">Contact</p>
            <div class="space-y-2.5 text-sm" style="color:rgba(255,255,255,0.42);">
              <p>📧 info@misbahulhoda.org</p>
              <p>🌐 misbahulhoda.org</p>
              <p>📱 WhatsApp Support</p>
              <p>🕐 24/7 during Arbaeen</p>
            </div>
          </div>

          <!-- Platform -->
          <div>
            <p class="text-white font-bold text-sm mb-5 uppercase" style="letter-spacing:0.08em;">Our Platform</p>
            <div class="space-y-3">
              <div class="flex items-center gap-3 p-3 transition-all cursor-pointer"
                   style="background:rgba(255,255,255,0.05); border:1px solid rgba(212,168,0,0.2); border-radius:3px;"
                   onmouseover="this.style.borderColor='#D4A800'"
                   onmouseout="this.style.borderColor='rgba(212,168,0,0.2)'">
                <span class="text-xl">🌐</span>
                <div>
                  <p class="text-white text-xs font-medium">Web App</p>
                  <p class="text-xs" style="color:rgba(255,255,255,0.3);">misbahulhoda.mubashirhasan.dev</p>
                </div>
              </div>
              <a href="/misbahuda.apk" download="MisbahulHoda.apk"
                 class="flex items-center gap-3 p-3 transition-all cursor-pointer"
                 style="background:rgba(201,168,76,0.08); border:1px solid rgba(212,168,0,0.4); border-radius:3px; text-decoration:none;"
                 onmouseover="this.style.borderColor='#D4A800'; this.style.background='rgba(201,168,76,0.15)'"
                 onmouseout="this.style.borderColor='rgba(212,168,0,0.4)'; this.style.background='rgba(201,168,76,0.08)'">
                <span class="text-xl">📱</span>
                <div class="flex-1">
                  <p class="text-white text-xs font-medium">Android App</p>
                  <p class="text-xs" style="color:rgba(255,255,255,0.4);">Download APK — v1.0.0</p>
                </div>
                <span class="text-xs font-bold px-2 py-1 rounded" style="background:#D4A800; color:#000;">↓ Download</span>
              </a>
            </div>
          </div>
        </div>

        <!-- Bottom -->
        <div class="py-5 border-t flex flex-col sm:flex-row items-center justify-between gap-3"
             style="border-color:rgba(255,255,255,0.07);">
          <p class="text-xs text-center sm:text-left" style="color:rgba(255,255,255,0.22);">
            © 2026 Misbah ul Hoda. All rights reserved. Built for the Lovers of Ahlul Bayt (AS).
          </p>
          <div class="flex items-center gap-2">
            <span class="w-2 h-2 rounded-full animate-pulse" style="background:#D4A800; box-shadow:0 0 6px #D4A800;"></span>
            <p class="text-xs font-medium" style="color:rgba(212,168,0,0.6);">System Operational</p>
          </div>
        </div>
      </div>
    </footer>

    <!-- Scroll to top button -->
    <Transition name="fade">
      <button v-if="scrolled" @click="scrollTop"
        class="fixed bottom-6 right-6 z-50 w-11 h-11 flex items-center justify-center transition-all hover:opacity-90"
        style="background:#D4A800; color:#000; border-radius:3px; box-shadow:0 4px 16px rgba(212,168,0,0.4);">
        <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2.5" d="M5 15l7-7 7 7"/>
        </svg>
      </button>
    </Transition>

  </div>
</template>

<style scoped>
.desk-link {
  display: inline-block;
  padding: 8px 14px;
  font-size: 0.875rem;
  font-weight: 500;
  color: #334155;
  text-decoration: none;
  transition: color 0.2s, background 0.2s;
  border-radius: 2px;
}
.desk-link:hover { color: #D4A800; background: #faf8f2; }

.mob-link {
  display: block;
  padding: 10px 8px;
  font-size: 0.9rem;
  font-weight: 500;
  color: #334155;
  text-decoration: none;
  transition: color 0.2s;
}
.mob-link:hover { color: #D4A800; }

.footer-link { transition: color 0.2s; }
.footer-link:hover { color: #D4A800 !important; }

.slide-down-enter-active, .slide-down-leave-active { transition: opacity 0.2s, transform 0.2s; }
.slide-down-enter-from, .slide-down-leave-to { opacity: 0; transform: translateY(-8px); }

.fade-enter-active, .fade-leave-active { transition: opacity 0.3s; }
.fade-enter-from, .fade-leave-to { opacity: 0; }
</style>
