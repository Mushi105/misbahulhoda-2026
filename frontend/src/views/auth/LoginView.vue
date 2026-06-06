<script setup>
import { ref, watch, computed, onMounted } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import api from '@/api/client'
import { pilgrimApi } from '@/api'
import MHLogo from '@/components/MHLogo.vue'

const router = useRouter()
const route  = useRoute()
const auth   = useAuthStore()

const tab = ref('login')
const loading = ref(false)
const error = ref('')
const showPassword = ref(false)
const regStep = ref(1) // 2-step registration

const loginForm = ref({ email: '', password: '' })

// Forgot password
const showForgot      = ref(false)
const forgotEmail     = ref('')
const forgotLoading   = ref(false)
const forgotSent      = ref(false)
const forgotError     = ref('')

async function handleForgotPassword() {
  forgotError.value = ''
  if (!forgotEmail.value || !forgotEmail.value.includes('@')) {
    forgotError.value = 'Please enter a valid email address.'
    return
  }
  forgotLoading.value = true
  try {
    await api.post('/auth/forgot-password', { email: forgotEmail.value })
    forgotSent.value = true
  } catch {
    forgotError.value = 'Something went wrong. Please try again.'
  } finally {
    forgotLoading.value = false
  }
}

function openForgot() {
  forgotEmail.value  = loginForm.value.email || ''
  forgotSent.value   = false
  forgotError.value  = ''
  showForgot.value   = true
}

const registerForm = ref({
  // Step 1 — Personal
  fullName: '',
  email: '',
  phoneNumber: '',
  whatsAppNumber: '',
  gender: '',
  dateOfBirth: '',
  // Step 2 — Address
  address1: '',
  address2: '',
  city: '',
  country: '',
  // Step 3 — Family Members (array, not count)
  familyMembers: [],
  // Step 3 — Travel
  passportNumber: '',
  passportExpiry: '',
  packageType: '',
  // Step 4 — Password
  password: '',
  confirmPassword: '',
})

// Real-time email check
const emailExists = ref(false)
const checkingEmail = ref(false)
let emailTimer = null

async function checkEmail() {
  const email = registerForm.value.email.trim()
  if (!email || !email.includes('@')) { emailExists.value = false; return }
  checkingEmail.value = true
  try {
    const res = await api.get('/auth/check-email', { params: { email } })
    emailExists.value = res.data.data?.exists === true
  } catch { emailExists.value = false }
  finally { checkingEmail.value = false }
}

watch(() => registerForm.value.email, () => {
  clearTimeout(emailTimer)
  emailTimer = setTimeout(checkEmail, 600)
})

const packages = [
  { value: 1, label: 'Najaf Only', desc: 'Visit Imam Ali (AS)', icon: '🕌', color: 'border-green-700 bg-green-900/20' },
  { value: 2, label: 'Karbala Only', desc: 'Visit Imam Hussain (AS)', icon: '🌹', color: 'border-red-700 bg-red-900/20' },
  { value: 3, label: 'Karbala + Najaf', desc: 'Both holy shrines', icon: '⭐', color: 'border-gold-700 bg-gold-900/20' },
  { value: 4, label: 'Karbala + Najaf + Baghdad', desc: 'Full Arbaeen package', icon: '🏆', color: 'border-primary-700 bg-primary-900/20' },
]

const countries = [
  'Pakistan', 'India', 'Bangladesh', 'UK', 'USA', 'Canada', 'Australia',
  'Saudi Arabia', 'UAE', 'Kuwait', 'Qatar', 'Bahrain', 'Oman',
  'Iran', 'Iraq', 'Germany', 'France', 'Sweden', 'Netherlands', 'Other'
]

function redirectByRole() {
  if (auth.isAdmin) router.replace('/admin/dashboard')
  else if (auth.isPilgrim) router.replace('/pilgrim/portal')
  else if (auth.isVolunteer) router.replace('/volunteer/dashboard')
  else if (auth.isDriver) router.replace('/tracking')
  else router.replace('/dashboard')
}

async function handleLogin() {
  if (!loginForm.value.email || !loginForm.value.password) {
    error.value = 'Please enter your email and password.'
    return
  }
  error.value = ''
  loading.value = true
  try {
    await auth.login(loginForm.value.email, loginForm.value.password)
    redirectByRole()
  } catch (e) {
    error.value = e.response?.data?.message || e.message || 'Login failed. Check your credentials.'
  } finally {
    loading.value = false
  }
}

function validateStep1() {
  if (!registerForm.value.fullName.trim()) return 'Full name is required.'
  if (!registerForm.value.email.trim() || !registerForm.value.email.includes('@')) return 'Valid email is required.'
  if (emailExists.value) return 'This email is already registered. Please sign in.'
  if (!registerForm.value.phoneNumber.trim()) return 'Phone number is required.'
  if (!registerForm.value.gender) return 'Please select your gender.'
  if (!registerForm.value.dateOfBirth) return 'Date of birth is required.'
  return null
}

function validateStep2() {
  if (!registerForm.value.address1.trim()) return 'Address line 1 is required.'
  if (!registerForm.value.city.trim()) return 'City is required.'
  if (!registerForm.value.country) return 'Please select your country.'
  return null
}

function validateStep3() {
  if (!registerForm.value.passportNumber.trim()) return 'Passport number is required.'
  if (!registerForm.value.passportExpiry) return 'Passport expiry date is required.'
  if (!registerForm.value.packageType) return 'Please select a travel package.'
  return null
}

// Family member helpers
const relationships = [
  { value: 1, label: 'Wife / Spouse', icon: '👩' },
  { value: 11, label: 'Husband', icon: '👨' },
  { value: 2, label: 'Son', icon: '👦' },
  { value: 3, label: 'Daughter', icon: '👧' },
  { value: 4, label: 'Father', icon: '👴' },
  { value: 5, label: 'Mother', icon: '👵' },
  { value: 6, label: 'Brother', icon: '🧑' },
  { value: 7, label: 'Sister', icon: '👩' },
  { value: 8, label: 'Grandmother', icon: '👵' },
  { value: 9, label: 'Grandfather', icon: '👴' },
  { value: 10, label: 'Aunt (Aunty)', icon: '👩' },
  { value: 12, label: 'Uncle', icon: '👨' },
  { value: 99, label: 'Other', icon: '🧑' },
]

const showAddMember = ref(false)
const memberFormError = ref('')
const newMember = ref({ fullName: '', gender: '', relationship: '', dateOfBirth: '', passportNumber: '', passportExpiry: '' })

function calcAge(dob) {
  if (!dob) return null
  const today = new Date()
  const birth = new Date(dob)
  let age = today.getFullYear() - birth.getFullYear()
  const m = today.getMonth() - birth.getMonth()
  if (m < 0 || (m === 0 && today.getDate() < birth.getDate())) age--
  return age
}

function addMemberToList() {
  memberFormError.value = ''
  if (!newMember.value.fullName.trim()) { memberFormError.value = 'Full name required'; return }
  if (!newMember.value.gender) { memberFormError.value = 'Gender required'; return }
  if (!newMember.value.relationship) { memberFormError.value = 'Relationship required'; return }
  if (!newMember.value.passportNumber.trim()) { memberFormError.value = 'Passport number required'; return }
  if (!newMember.value.passportExpiry) { memberFormError.value = 'Passport expiry required'; return }
  const age = calcAge(newMember.value.dateOfBirth)
  registerForm.value.familyMembers.push({
    ...newMember.value,
    gender: parseInt(newMember.value.gender),
    relationship: parseInt(newMember.value.relationship),
    isMinor: age !== null && age < 18,
  })
  newMember.value = { fullName: '', gender: '', relationship: '', dateOfBirth: '', passportNumber: '', passportExpiry: '' }
  showAddMember.value = false
}

function removeMemberFromList(i) {
  registerForm.value.familyMembers.splice(i, 1)
}

const totalTravelers = computed(() => registerForm.value.familyMembers.length + 1)

function validateStep4() {
  const pw = registerForm.value.password
  if (!pw || pw.length < 8) return 'Password must be at least 8 characters.'
  if (!/[A-Z]/.test(pw)) return 'Password must contain at least one uppercase letter (e.g. A, B, C).'
  if (!/[0-9]/.test(pw)) return 'Password must contain at least one number (e.g. 1, 2, 3).'
  if (!/[^a-zA-Z0-9]/.test(pw)) return 'Password must contain at least one special character (e.g. @, #, !).'
  if (pw !== registerForm.value.confirmPassword) return 'Passwords do not match.'
  return null
}

function nextStep() {
  error.value = ''
  const validators = [null, validateStep1, validateStep2, validateStep3]
  const err = validators[regStep.value]?.()
  if (err) { error.value = err; return }
  regStep.value++
}

function prevStep() {
  error.value = ''
  regStep.value--
}

async function handleRegister() {
  error.value = ''
  const err = validateStep4()
  if (err) { error.value = err; return }

  loading.value = true
  try {
    await auth.register({
      fullName: registerForm.value.fullName,
      email: registerForm.value.email,
      phoneNumber: registerForm.value.phoneNumber,
      whatsAppNumber: registerForm.value.whatsAppNumber || undefined,
      password: registerForm.value.password,
      address1: registerForm.value.address1,
      address2: registerForm.value.address2 || undefined,
      city: registerForm.value.city,
      country: registerForm.value.country,
      gender: parseInt(registerForm.value.gender),
      dateOfBirth: registerForm.value.dateOfBirth,
      passportNumber: registerForm.value.passportNumber,
      passportExpiry: registerForm.value.passportExpiry,
      familyMemberCount: totalTravelers.value,
      packageType: parseInt(registerForm.value.packageType),
      role: 5,
    })
    // Save each family member
    for (const member of registerForm.value.familyMembers) {
      try {
        await pilgrimApi.addFamilyMember({
          fullName: member.fullName,
          gender: member.gender,
          relationship: member.relationship,
          dateOfBirth: member.dateOfBirth || undefined,
          passportNumber: member.passportNumber,
          passportExpiry: member.passportExpiry || undefined,
          isMinor: member.isMinor,
        })
      } catch { /* non-fatal */ }
    }
    redirectByRole()
  } catch (e) {
    const msg = e.response?.data?.message || e.response?.data?.errors?.[0] || e.message || 'Registration failed. Please try again.'
    error.value = msg
    console.error('Register error:', e.response?.data || e.message)
  } finally {
    loading.value = false
  }
}

function switchTab(t) {
  tab.value = t
  error.value = ''
  regStep.value = 1
}

const stepLabels = ['Personal Info', 'Address', 'Travel Details', 'Set Password']

onMounted(() => {
  if (route.query.tab === 'register') tab.value = 'register'
})
</script>

<template>
  <!-- ══ Full-page canvas — Imam Zaman emerald theme ══ -->
  <div class="relative flex flex-col md:flex-row"
       style="min-height:100dvh; overflow-x:hidden; overflow-y:auto; -webkit-overflow-scrolling:touch; background: #f8fafc;">

    <!-- Global ambient glows — full page -->
    <div class="absolute top-0 left-1/4 w-[600px] h-[600px] rounded-full pointer-events-none"
         style="background: radial-gradient(circle, rgba(212,168,0,0.10) 0%, transparent 70%); "></div>
    <div class="absolute bottom-0 right-1/4 w-[500px] h-[500px] rounded-full pointer-events-none"
         style="background: radial-gradient(circle, rgba(212,168,0,0.07) 0%, transparent 70%); "></div>
    <div class="absolute top-1/2 left-1/2 -translate-x-1/2 -translate-y-1/2 w-[800px] h-[400px] rounded-full pointer-events-none"
         style="background: radial-gradient(ellipse, rgba(180,83,9,0.04) 0%, transparent 70%); "></div>

    <!-- Global Islamic grid pattern — full page -->
    <div class="absolute inset-0 opacity-[0.035] pointer-events-none"
         style="background-image: repeating-linear-gradient(0deg, transparent, transparent 36px, rgba(212,168,0,0.7) 36px, rgba(212,168,0,0.7) 37px), repeating-linear-gradient(90deg, transparent, transparent 36px, rgba(212,168,0,0.7) 36px, rgba(212,168,0,0.7) 37px), repeating-linear-gradient(45deg, transparent, transparent 25px, rgba(212,168,0,0.35) 25px, rgba(212,168,0,0.35) 26px), repeating-linear-gradient(-45deg, transparent, transparent 25px, rgba(212,168,0,0.35) 25px, rgba(212,168,0,0.35) 26px);"></div>

    <!-- Top accent bar -->
    <div class="absolute top-0 left-0 right-0 h-0.5 z-50 pointer-events-none"
         style="background: linear-gradient(90deg, transparent, #D4A800, #d97706, #D4A800, transparent);"></div>

    <!-- ══════════ LEFT PANEL ══════════ -->
    <div class="hidden md:flex md:w-[42%] relative flex-col items-center justify-center px-10 overflow-hidden"
         style="border-right: 1px solid #e2e8f0;">

      <!-- Back to home -->
      <router-link to="/" class="absolute top-6 left-6 flex items-center gap-1.5 text-xs hover:text-amber-700 transition-colors z-20">
        ← Back to Home
      </router-link>

      <div class="relative text-center z-10 max-w-xs">

        <!-- Logo with emerald glow ring -->
        <div class="relative mx-auto w-32 h-32 mb-6">
          <!-- Outer glow ring -->
          <div class="absolute inset-0 rounded-full opacity-40"
               style="background: conic-gradient(from 0deg, #D4A800, #d97706, #059669, #d97706, #D4A800); padding: 2px; animation: spinSlow 8s linear infinite;">
            <div class="w-full h-full rounded-full" style="background:#f8fafc;"></div>
          </div>
          <!-- Inner ring -->
          <div class="absolute inset-1 rounded-full border border-emerald-500/30 flex items-center justify-center"
               style="background: #f8fafc; box-shadow: 0 0 24px rgba(212,168,0,0.2);">
            <MHLogo size="3xl" />
          </div>
        </div>

        <!-- Brand -->
        <h1 class="text-3xl font-bold text-gray-900 tracking-wide">Misbah ul Hoda</h1>
        <p class="text-xs tracking-widest font-semibold uppercase mt-1 font-medium">misbahulhoda.org</p>

        <!-- Imam Zaman calligraphy block -->
        <div class="mt-5 mb-5 py-4 px-5 rounded-2xl relative overflow-hidden"
             style="background: rgba(212,168,0,0.06); border: 1px solid rgba(212,168,0,0.2);">
          <p class="font-arabic text-3xl mb-1.5 leading-relaxed"
             style="color:#D4A800; font-size:1.8rem; text-shadow: 0 0 20px rgba(212,168,0,0.3);">
            يَا صَاحِبَ الزَّمَان
          </p>
          <p class="text-xs font-medium tracking-wider">O Master of the Age</p>
          <div class="mt-2.5 pt-2.5 border-t border-amber-200">
            <p class="font-arabic text-gold-400/80 text-sm leading-relaxed">
              اللَّهُمَّ عَجِّلْ لِوَلِيِّكَ الْفَرَجَ
            </p>
            <p class="text-gray-600 text-xs mt-0.5">O Allah, hasten the reappearance of Your Wali</p>
          </div>
        </div>

        <!-- Tagline -->
        <div class="flex items-center gap-2 justify-center mb-1">
          <div class="h-px w-10" style="background: linear-gradient(to right, transparent, rgba(212,168,0,0.5));"></div>
          <span class="text-emerald-600 text-xs">✦</span>
          <div class="h-px w-10" style="background: linear-gradient(to left, transparent, rgba(212,168,0,0.5));"></div>
        </div>
        <p class="text-gray-900 text-sm font-semibold tracking-wider">Ziyarat with Ma'rifat</p>
        <p class="text-gray-600 text-xs mt-1">Arbaeen 2026 — Pilgrimage Management</p>

        <!-- Features -->
        <div class="mt-7 space-y-2.5 text-left">
          <div v-for="f in [
            { icon: '🕌', text: 'Pilgrim registration & family management', color: 'rgba(212,168,0,0.15)', border: 'rgba(212,168,0,0.3)' },
            { icon: '🚌', text: 'Room, bus & Karwan allocation', color: 'rgba(212,168,0,0.1)', border: 'rgba(212,168,0,0.2)' },
            { icon: '📍', text: 'Live GPS Karwan tracking', color: 'rgba(180,83,9,0.12)', border: 'rgba(180,83,9,0.25)' },
            { icon: '📿', text: 'Majalis, Namaz & Ziyarat schedule', color: 'rgba(180,83,9,0.1)', border: 'rgba(180,83,9,0.2)' },
          ]" :key="f.text" class="flex items-center gap-2.5">
            <span class="w-8 h-8 rounded-lg flex items-center justify-center text-sm flex-shrink-0"
                  :style="`background:${f.color}; border:1px solid ${f.border};`">{{ f.icon }}</span>
            <span class="text-gray-600 text-xs">{{ f.text }}</span>
          </div>
        </div>

        <!-- Date pill -->
        <div class="mt-6 inline-flex items-center gap-2 px-4 py-2 rounded-full"
             style="background:rgba(212,168,0,0.08); border:1px solid rgba(212,168,0,0.25);">
          <span class="w-2 h-2 rounded-full animate-pulse" style="background:#D4A800;"></span>
          <span class="text-xs font-medium" style="color:#9a6f00;">Registrations Open · Arbaeen 2026</span>
        </div>

      </div>

      <!-- Bottom accent line -->
      <div class="absolute bottom-0 left-0 right-0 h-0.5" style="background: linear-gradient(90deg, transparent, rgba(212,168,0,0.4), transparent);"></div>
    </div>

    <!-- ══════════ RIGHT PANEL — Form ══════════ -->
    <div class="flex-1 flex items-center justify-center px-4 py-8 overflow-y-auto relative z-10">
      <div class="w-full max-w-lg">

        <!-- Glass card wraps entire form area -->
        <div class="rounded-2xl overflow-hidden"
             style="background: #ffffff; border: 1px solid #e2e8f0; box-shadow: 0 4px 24px rgba(0,0,0,0.08);">

          <!-- Emerald top bar on card -->
          <div class="h-0.5 w-full" style="background: linear-gradient(90deg, transparent, #D4A800, #d97706, #D4A800, transparent);"></div>

        <div class="p-6 sm:p-8">

        <!-- Mobile Logo -->
        <div class="md:hidden text-center mb-6">
          <router-link to="/" class="inline-flex items-center gap-1 text-xs text-gray-700 hover:text-amber-700 transition-colors mb-4">← Back to Home</router-link>
          <div class="flex justify-center mb-3">
            <div class="relative">
              <div class="absolute inset-0 rounded-full" style="background:rgba(212,168,0,0.1); transform:scale(1.4);"></div>
              <MHLogo size="lg" />
            </div>
          </div>
          <h1 class="text-2xl font-bold text-gray-900">Misbah ul Hoda</h1>
          <p class="text-xs mt-1 font-medium" style="color:#D4A800;">Ziyarat with Ma'rifat · Arbaeen 2026</p>
        </div>

        <!-- Tabs -->
        <div class="flex rounded-xl p-1 mb-5" style="background:#f1f5f9; border:1px solid #e2e8f0;">
          <button @click="switchTab('login')"
            :class="['flex-1 py-2.5 rounded-lg text-sm font-semibold transition-all duration-200',
              tab === 'login'
                ? 'text-white shadow-lg'
                : 'text-gray-700 hover:text-gray-900']"
            :style="tab === 'login' ? 'background:linear-gradient(135deg,#b88a00,#D4A800); box-shadow:0 4px 16px rgba(212,168,0,0.3);' : ''">
            🔐 Sign In
          </button>
          <button @click="switchTab('register')"
            :class="['flex-1 py-2.5 rounded-lg text-sm font-semibold transition-all duration-200',
              tab === 'register'
                ? 'text-white shadow-lg'
                : 'text-gray-700 hover:text-gray-900']"
            :style="tab === 'register' ? 'background:linear-gradient(135deg,#b88a00,#D4A800); box-shadow:0 4px 16px rgba(212,168,0,0.3);' : ''">
            🕌 New Pilgrim? Register
          </button>
        </div>

        <!-- Error -->
        <div v-if="error" class="mb-4 flex items-start gap-2 text-sm rounded-lg px-4 py-3" style="background:#fef2f2; border:1px solid #fca5a5; color:#dc2626;">
          <span class="mt-0.5 flex-shrink-0">⚠️</span>
          <span>{{ error }}</span>
        </div>

        <!-- ════════════════ LOGIN FORM ════════════════ -->
        <form v-if="tab === 'login'" @submit.prevent="handleLogin" class="space-y-4">

          <!-- Imam Zaman dua strip -->
          <div class="flex items-center gap-3 px-4 py-3 rounded-xl mb-1"
               style="background:rgba(212,168,0,0.07); border:1px solid rgba(212,168,0,0.2);">
            <span class="text-amber-700 text-xl flex-shrink-0">☪</span>
            <div>
              <p class="font-arabic text-sm leading-snug" style="color:#D4A800;">بِسْمِ اللَّهِ الرَّحْمَٰنِ الرَّحِيمِ</p>
              <p class="text-gray-600 text-xs mt-0.5">In the name of Allah, the Most Gracious</p>
            </div>
          </div>

          <div>
            <label class="label" style="color:#7a5800;">Email Address</label>
            <input v-model="loginForm.email" type="email" class="input" placeholder="you@example.com"
                   style=""
                   autocomplete="email" autofocus />
          </div>
          <div>
            <label class="label" style="color:#7a5800;">Password</label>
            <div class="relative">
              <input v-model="loginForm.password" :type="showPassword ? 'text' : 'password'" class="input pr-10"
                     style=""
                     placeholder="••••••••" autocomplete="current-password" />
              <button type="button" @click="showPassword = !showPassword"
                class="absolute right-3 top-1/2 -translate-y-1/2 text-slate-500 hover:text-amber-700 text-sm">
                {{ showPassword ? '🙈' : '👁️' }}
              </button>
            </div>
          </div>
          <div class="flex justify-end -mt-1">
            <button type="button" @click="openForgot" class="text-xs hover:underline transition-colors" style="color:#9a6f00;">
              Forgot Password?
            </button>
          </div>

          <button type="submit" :disabled="loading"
            class="w-full py-3.5 rounded-xl text-base font-bold text-gray-900 transition-all duration-200 hover:scale-[1.01] disabled:opacity-60 disabled:cursor-not-allowed"
            style="background: #D4A800; box-shadow: 0 4px 20px rgba(212,168,0,0.35);">
            <span v-if="loading" class="flex items-center justify-center gap-2">
              <svg class="animate-spin w-4 h-4" fill="none" viewBox="0 0 24 24">
                <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"/>
                <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8v8z"/>
              </svg> Signing in...
            </span>
            <span v-else>🔐 Sign In</span>
          </button>

          <div class="mt-2 p-3.5 rounded-xl text-xs text-gray-600 space-y-1.5"
               style="background:#faf8f2; border:1px solid #e8dfc0;">
            <p class="text-gray-600 font-medium mb-2 flex items-center gap-1.5">
              <span style="color:#D4A800;">ℹ</span> How access works:
            </p>
            <p>• <span style="color:#D4A800;" class="font-medium">Pilgrims</span> — register below, then login</p>
            <p>• <span class="text-gray-700 font-medium">Volunteers</span> — account created by admin</p>
            <p>• <span class="text-gray-700 font-medium">Admins</span> — credentials assigned by SuperAdmin</p>
          </div>
        </form>

        <!-- ════════════════ REGISTER FORM ════════════════ -->
        <div v-else>
          <div class="p-3 rounded-xl text-xs mb-4 flex items-start gap-2"
               style="background:rgba(212,168,0,0.07); border:1px solid rgba(212,168,0,0.25); color:#7a5800;">
            <span class="text-lg flex-shrink-0">🕌</span>
            <span>Pilgrim self-registration. Fill all steps to complete your Arbaeen 2026 application.</span>
          </div>

          <!-- Step Indicator -->
          <div class="flex items-center mb-6">
            <template v-for="(label, i) in stepLabels" :key="i">
              <div class="flex flex-col items-center">
                <div :class="[
                  'w-7 h-7 rounded-full flex items-center justify-center text-xs font-bold border-2 transition-all',
                  regStep > i + 1 ? 'text-white' :
                  regStep === i + 1 ? 'text-white' :
                  'bg-dark-800 border-dark-600 text-slate-500'
                ]"
                :style="regStep > i + 1 ? 'background:#059669; border-color:#059669;' : regStep === i + 1 ? 'background:linear-gradient(135deg,#b88a00,#D4A800); border-color:#D4A800; box-shadow:0 0 10px rgba(212,168,0,0.4);' : ''">
                  {{ regStep > i + 1 ? '✓' : i + 1 }}
                </div>
                <span class="text-xs mt-1 hidden sm:block"
                  :style="regStep === i + 1 ? 'color:#9a6f00;' : 'color:#475569;'">{{ label }}</span>
              </div>
              <div v-if="i < stepLabels.length - 1" :class="['flex-1 h-0.5 mx-1', regStep > i + 1 ? 'bg-emerald-600' : 'bg-dark-700']"></div>
            </template>
          </div>

          <!-- ─ STEP 1: Personal Info ─ -->
          <div v-if="regStep === 1" class="space-y-4">
            <h3 class="text-gray-900 font-semibold">Personal Information</h3>

            <div>
              <label class="label">Full Name <span class="text-red-400">*</span> <span class="text-gray-600 text-xs">(as on passport)</span></label>
              <input v-model="registerForm.fullName" class="input" placeholder="Muhammad Ali Khan" />
            </div>

            <div>
              <label class="label">Email Address <span class="text-red-400">*</span></label>
              <div class="relative">
                <input v-model="registerForm.email" type="email" class="input pr-10"
                  :class="emailExists ? 'border-red-600 focus:border-red-500' : ''"
                  placeholder="you@example.com" />
                <div class="absolute right-3 top-1/2 -translate-y-1/2 text-sm">
                  <span v-if="checkingEmail" class="text-slate-400">⌛</span>
                  <span v-else-if="emailExists" class="text-red-400" title="Already registered">✗</span>
                  <span v-else-if="registerForm.email && registerForm.email.includes('@')" class="text-amber-700">✓</span>
                </div>
              </div>
              <p v-if="emailExists" class="text-red-400 text-xs mt-1">
                This email is already registered. <button type="button" @click="switchTab('login')" class="underline">Sign in instead</button>
              </p>
            </div>

            <div class="grid grid-cols-2 gap-3">
              <div>
                <label class="label">Phone Number <span class="text-red-400">*</span></label>
                <input v-model="registerForm.phoneNumber" type="tel" class="input" placeholder="+92 300 1234567" />
              </div>
              <div>
                <label class="label">WhatsApp <span class="text-gray-600 text-xs">(optional)</span></label>
                <input v-model="registerForm.whatsAppNumber" type="tel" class="input" placeholder="+92 300..." />
              </div>
            </div>

            <div class="grid grid-cols-2 gap-3">
              <div>
                <label class="label">Gender <span class="text-red-400">*</span></label>
                <select v-model="registerForm.gender" class="input">
                  <option value="">Select gender</option>
                  <option value="1">Male (مرد)</option>
                  <option value="2">Female (خاتون)</option>
                </select>
              </div>
              <div>
                <label class="label">Date of Birth <span class="text-red-400">*</span></label>
                <input v-model="registerForm.dateOfBirth" type="date" class="input"
                  :max="new Date().toISOString().split('T')[0]" />
              </div>
            </div>

            <button type="button" @click="nextStep" :disabled="emailExists"
              class="w-full py-3 rounded-xl font-semibold text-gray-900 transition-all hover:scale-[1.01] disabled:opacity-50"
              style="background:linear-gradient(135deg,#b88a00,#D4A800); box-shadow:0 6px 20px rgba(212,168,0,0.3);">
              Continue → Address Details
            </button>
          </div>

          <!-- ─ STEP 2: Address ─ -->
          <div v-if="regStep === 2" class="space-y-4">
            <h3 class="text-gray-900 font-semibold">Home Address</h3>

            <div>
              <label class="label">Address Line 1 <span class="text-red-400">*</span></label>
              <input v-model="registerForm.address1" class="input" placeholder="House no, Street / Mohalla" />
            </div>
            <div>
              <label class="label">Address Line 2 <span class="text-gray-600 text-xs">(optional)</span></label>
              <input v-model="registerForm.address2" class="input" placeholder="Area / Sector" />
            </div>

            <div class="grid grid-cols-2 gap-3">
              <div>
                <label class="label">City <span class="text-red-400">*</span></label>
                <input v-model="registerForm.city" class="input" placeholder="e.g. Lahore, Karachi" />
              </div>
              <div>
                <label class="label">Country <span class="text-red-400">*</span></label>
                <select v-model="registerForm.country" class="input">
                  <option value="">Select country</option>
                  <option v-for="c in countries" :key="c" :value="c">{{ c }}</option>
                </select>
              </div>
            </div>

            <div class="flex gap-3">
              <button type="button" @click="prevStep" class="flex-1 py-3 rounded-xl font-medium border transition-all" style="color:#64748b; border-color:#d1d5db; background:#f8fafc;">← Back</button>
              <button type="button" @click="nextStep" class="flex-1 py-3 rounded-xl font-bold text-black transition-all hover:scale-[1.01]" style="background:#D4A800; box-shadow:0 6px 20px rgba(212,168,0,0.3);">Continue → Travel Details</button>
            </div>
          </div>

          <!-- ─ STEP 3: Travel Details ─ -->
          <div v-if="regStep === 3" class="space-y-4">
            <h3 class="text-gray-900 font-semibold">Travel & Passport Details</h3>

            <div class="grid grid-cols-2 gap-3">
              <div>
                <label class="label">Passport Number <span class="text-red-400">*</span></label>
                <input v-model="registerForm.passportNumber" class="input" placeholder="e.g. AB1234567" />
              </div>
              <div>
                <label class="label">Passport Expiry <span class="text-red-400">*</span></label>
                <input v-model="registerForm.passportExpiry" type="date" class="input"
                  :min="new Date().toISOString().split('T')[0]" />
              </div>
            </div>

            <!-- Family Members Grid -->
            <div>
              <div class="flex items-center justify-between mb-2">
                <div>
                  <label class="label mb-0">Family Members Traveling With You</label>
                  <p class="text-xs mt-0.5" style="color:#64748b;">Total: <span class="text-amber-700 font-semibold">{{ totalTravelers }} {{ totalTravelers === 1 ? 'person (just you)' : 'people' }}</span></p>
                </div>
                <button type="button" @click="showAddMember = !showAddMember"
                  class="text-xs font-bold px-3 py-1.5 rounded" style="background:#D4A800; color:#000;">
                  {{ showAddMember ? '✕ Cancel' : '+ Add Member' }}
                </button>
              </div>

              <!-- Add Member Form -->
              <div v-if="showAddMember" class="rounded-xl p-4 mb-3 space-y-3" style="background:#f8fafc; border:1px solid #e2e8f0;">
                <p class="text-xs font-semibold uppercase tracking-wide" style="color:#D4A800;">New Family Member</p>
                <p v-if="memberFormError" class="text-red-400 text-xs">{{ memberFormError }}</p>

                <div class="grid grid-cols-2 gap-3">
                  <div class="col-span-2">
                    <label class="label">Full Name (as on passport) *</label>
                    <input v-model="newMember.fullName" class="input" placeholder="e.g. Fatima Bibi" />
                  </div>
                  <div>
                    <label class="label">Relationship *</label>
                    <select v-model="newMember.relationship" class="input">
                      <option value="">Select...</option>
                      <option v-for="r in relationships" :key="r.value" :value="r.value">{{ r.icon }} {{ r.label }}</option>
                    </select>
                  </div>
                  <div>
                    <label class="label">Gender *</label>
                    <select v-model="newMember.gender" class="input">
                      <option value="">Select...</option>
                      <option value="1">♂ Male</option>
                      <option value="2">♀ Female</option>
                    </select>
                  </div>
                  <div>
                    <label class="label">Date of Birth <span class="text-gray-600 text-xs">(for age)</span></label>
                    <input v-model="newMember.dateOfBirth" type="date" class="input" />
                    <p v-if="newMember.dateOfBirth" class="text-xs text-gray-600 mt-1">
                      Age: <span :class="calcAge(newMember.dateOfBirth) < 18 ? 'text-amber-600' : 'text-amber-700'">
                        {{ calcAge(newMember.dateOfBirth) }} yrs {{ calcAge(newMember.dateOfBirth) < 18 ? '(Minor)' : '' }}
                      </span>
                    </p>
                  </div>
                  <div>
                    <label class="label">Passport Number *</label>
                    <input v-model="newMember.passportNumber" class="input" placeholder="AB1234567" />
                  </div>
                  <div class="col-span-2">
                    <label class="label">Passport Expiry *</label>
                    <input v-model="newMember.passportExpiry" type="date" class="input" :min="new Date().toISOString().split('T')[0]" />
                  </div>
                </div>

                <button type="button" @click="addMemberToList"
                  class="w-full bg-primary-600 hover:bg-amber-600 text-white py-2 rounded-lg text-sm font-semibold">
                  ✓ Add to List
                </button>
              </div>

              <!-- Members List -->
              <div v-if="registerForm.familyMembers.length" class="space-y-2">
                <div v-for="(m, i) in registerForm.familyMembers" :key="i"
                  class="flex items-center gap-3 bg-dark-700 border border-dark-500 rounded-xl px-4 py-3">
                  <div class="text-xl">{{ relationships.find(r => r.value === m.relationship)?.icon || '🧑' }}</div>
                  <div class="flex-1 min-w-0">
                    <p class="text-white text-sm font-semibold truncate">{{ m.fullName }}</p>
                    <p class="text-gray-600 text-xs">
                      {{ relationships.find(r => r.value === m.relationship)?.label || 'Other' }} ·
                      {{ m.gender === 1 ? '♂ Male' : '♀ Female' }}
                      <span v-if="m.dateOfBirth"> · {{ calcAge(m.dateOfBirth) }} yrs</span>
                      <span v-if="m.isMinor" class="text-gold-400"> · Minor</span>
                    </p>
                  </div>
                  <div class="text-right shrink-0">
                    <p class="text-gray-600 text-xs">{{ m.passportNumber }}</p>
                    <p class="text-gray-600 text-xs">Exp: {{ m.passportExpiry }}</p>
                  </div>
                  <button type="button" @click="removeMemberFromList(i)"
                    class="text-red-500 hover:text-red-400 text-lg ml-1">✕</button>
                </div>
              </div>

              <div v-else class="text-center py-4 text-slate-500 text-sm border border-dashed border-dark-500 rounded-xl">
                No family members added yet — just you traveling solo 🚶
              </div>
            </div>

            <!-- Package Selection -->
            <div>
              <label class="label">Select Travel Package <span class="text-red-400">*</span></label>
              <div class="grid grid-cols-1 sm:grid-cols-2 gap-3 mt-2">
                <button v-for="pkg in packages" :key="pkg.value"
                  type="button"
                  @click="registerForm.packageType = pkg.value.toString()"
                  :class="[
                    'text-left p-3 rounded-xl border-2 transition-all',
                    registerForm.packageType == pkg.value
                      ? 'border-primary-500 bg-primary-900/30 ring-1 ring-primary-500'
                      : `border-dark-600 hover:${pkg.color} ${pkg.color} opacity-70 hover:opacity-100`
                  ]">
                  <div class="flex items-center gap-2 mb-1">
                    <span class="text-xl">{{ pkg.icon }}</span>
                    <span class="text-gray-900 font-semibold text-sm">{{ pkg.label }}</span>
                    <span v-if="registerForm.packageType == pkg.value" class="ml-auto text-amber-700 text-sm">✓</span>
                  </div>
                  <p class="text-gray-600 text-xs">{{ pkg.desc }}</p>
                </button>
              </div>
            </div>

            <div class="flex gap-3">
              <button type="button" @click="prevStep" class="flex-1 py-3 rounded-xl font-medium border transition-all" style="color:#64748b; border-color:#d1d5db; background:#f8fafc;">← Back</button>
              <button type="button" @click="nextStep" class="flex-1 py-3 rounded-xl font-bold text-black transition-all hover:scale-[1.01]" style="background:#D4A800; box-shadow:0 6px 20px rgba(212,168,0,0.3);">Continue → Set Password</button>
            </div>
          </div>

          <!-- ─ STEP 4: Password ─ -->
          <div v-if="regStep === 4" class="space-y-4">
            <h3 class="text-gray-900 font-semibold">Create Your Password</h3>

            <!-- Summary -->
            <div class="bg-dark-700 rounded-xl p-4 border border-dark-600 space-y-2 text-sm">
              <p class="text-gray-600 text-xs uppercase tracking-wide mb-2">Registration Summary</p>
              <div class="grid grid-cols-2 gap-y-1 text-xs">
                <span class="text-slate-500">Name:</span><span class="text-white">{{ registerForm.fullName }}</span>
                <span class="text-slate-500">Email:</span><span class="text-white">{{ registerForm.email }}</span>
                <span class="text-slate-500">Country:</span><span class="text-white">{{ registerForm.country }}</span>
                <span class="text-slate-500">City:</span><span class="text-white">{{ registerForm.city }}</span>
                <span class="text-slate-500">Passport:</span><span class="text-white">{{ registerForm.passportNumber }}</span>
                <span class="text-slate-500">Family:</span><span class="text-white">{{ totalTravelers }} travelers ({{ registerForm.familyMembers.length }} members + you)</span>
                <span class="text-slate-500">Package:</span>
                <span class="text-amber-700 font-medium">{{ packages.find(p => p.value == registerForm.packageType)?.label || '—' }}</span>
              </div>
            </div>

            <div>
              <label class="label">Password <span class="text-red-400">*</span></label>
              <input v-model="registerForm.password" type="password" class="input" placeholder="e.g. Misbah@2026" autocomplete="new-password" />
              <div class="mt-2 grid grid-cols-2 gap-1">
                <span :class="['text-xs flex items-center gap-1', registerForm.password.length >= 8 ? 'text-green-400' : 'text-slate-500']">{{ registerForm.password.length >= 8 ? '✓' : '○' }} 8+ characters</span>
                <span :class="['text-xs flex items-center gap-1', /[A-Z]/.test(registerForm.password) ? 'text-green-400' : 'text-slate-500']">{{ /[A-Z]/.test(registerForm.password) ? '✓' : '○' }} Uppercase (A-Z)</span>
                <span :class="['text-xs flex items-center gap-1', /[0-9]/.test(registerForm.password) ? 'text-green-400' : 'text-slate-500']">{{ /[0-9]/.test(registerForm.password) ? '✓' : '○' }} Number (0-9)</span>
                <span :class="['text-xs flex items-center gap-1', /[^a-zA-Z0-9]/.test(registerForm.password) ? 'text-green-400' : 'text-slate-500']">{{ /[^a-zA-Z0-9]/.test(registerForm.password) ? '✓' : '○' }} Special (@#!)</span>
              </div>
            </div>
            <div>
              <label class="label">Confirm Password <span class="text-red-400">*</span></label>
              <div class="relative">
                <input v-model="registerForm.confirmPassword" type="password" class="input" placeholder="Repeat password" autocomplete="new-password" />
                <span v-if="registerForm.confirmPassword && registerForm.password === registerForm.confirmPassword"
                  class="absolute right-3 top-1/2 -translate-y-1/2 text-amber-700">✓</span>
              </div>
            </div>

            <div class="flex gap-3">
              <button type="button" @click="prevStep" class="py-3 px-5 rounded-xl font-medium text-slate-300 hover:text-white border transition-all" style="border-color:rgba(212,168,0,0.2); background:rgba(212,168,0,0.05);">← Back</button>
              <button type="button" @click="handleRegister" :disabled="loading"
                class="flex-1 py-3 rounded-xl font-bold text-gray-900 transition-all hover:scale-[1.01] disabled:opacity-60"
                style="background:linear-gradient(135deg,#7a5800,#b88a00,#059669); box-shadow:0 8px 28px rgba(212,168,0,0.35);">
                <span v-if="loading" class="flex items-center justify-center gap-2">
                  <svg class="animate-spin w-4 h-4" fill="none" viewBox="0 0 24 24">
                    <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"/>
                    <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8v8z"/>
                  </svg> Submitting...
                </span>
                <span v-else>✅ Complete Registration</span>
              </button>
            </div>
          </div>

        </div><!-- /register form -->
        </div><!-- /glass card inner padding -->
        </div><!-- /glass card -->

        <!-- Bottom attribution -->
        <p class="text-center text-slate-600 text-xs mt-4">
          <span class="text-emerald-800">✦</span>
          Misbah ul Hoda · misbahulhoda.org · Arbaeen 2026
          <span class="text-emerald-800">✦</span>
        </p>
      </div>
    </div>

  <!-- ── Forgot Password Modal ────────────────────────────── -->
  <Teleport to="body">
    <div v-if="showForgot" class="fixed inset-0 z-50 flex items-center justify-center p-4"
         style="background:rgba(1,9,4,0.92); backdrop-filter:blur(8px);"
         @click.self="showForgot=false">
      <div class="w-full max-w-sm rounded-2xl overflow-hidden shadow-2xl"
           style="background:linear-gradient(160deg,#080808,#0d0d0d,#020617); border:1px solid rgba(212,168,0,0.25); box-shadow:0 24px 64px rgba(0,0,0,0.6), 0 0 40px rgba(212,168,0,0.08);">
        <div class="h-0.5 w-full" style="background:linear-gradient(90deg,transparent,#D4A800,#d97706,#D4A800,transparent)"></div>
        <div class="px-6 py-4 border-b flex items-center justify-between" style="border-color:rgba(212,168,0,0.15);">
          <h3 class="text-gray-900 font-bold text-lg flex items-center gap-2">
            <span class="text-amber-700">🔑</span> Forgot Password
          </h3>
          <button @click="showForgot=false" class="text-slate-500 hover:text-amber-700 text-2xl leading-none transition-colors">✕</button>
        </div>
        <div class="p-6">

          <!-- Sent confirmation -->
          <div v-if="forgotSent" class="text-center space-y-4 py-2">
            <div class="w-16 h-16 rounded-full flex items-center justify-center mx-auto text-3xl"
                 style="background:rgba(212,168,0,0.12); border:1px solid rgba(212,168,0,0.3);">📧</div>
            <p class="text-gray-900 font-semibold text-lg">Check Your Email</p>
            <p class="text-gray-700 text-sm">If <span class="text-amber-700 font-medium">{{ forgotEmail }}</span> is registered, a reset link has been sent.</p>
            <p class="text-gray-600 text-xs">Link expires in 2 hours. Check spam folder too.</p>
            <button @click="showForgot=false"
              class="w-full py-2.5 rounded-xl font-semibold text-gray-900 transition-all"
              style="background:linear-gradient(135deg,#b88a00,#D4A800); box-shadow:0 6px 20px rgba(212,168,0,0.3);">
              ✓ OK, Got It
            </button>
          </div>

          <!-- Email form -->
          <div v-else class="space-y-4">
            <p class="text-sm" style="color:#64748b;">Enter your registered email and we'll send a reset link.</p>

            <div v-if="forgotError" class="text-sm rounded px-3 py-2 flex gap-2" style="background:#fef2f2; border:1px solid #fca5a5; color:#dc2626;">
              <span>⚠️</span><span>{{ forgotError }}</span>
            </div>

            <div>
              <label class="label" style="color:#7a5800;">Email Address</label>
              <input v-model="forgotEmail" type="email" class="input w-full" placeholder="you@example.com"
                     style=""
                     @keyup.enter="handleForgotPassword" autofocus />
            </div>

            <div class="flex gap-3">
              <button @click="handleForgotPassword" :disabled="forgotLoading"
                class="flex-1 py-2.5 rounded-xl text-sm font-semibold text-gray-900 disabled:opacity-40 transition-all"
                style="background:linear-gradient(135deg,#b88a00,#D4A800); box-shadow:0 4px 16px rgba(212,168,0,0.3);">
                <span v-if="forgotLoading" class="flex items-center justify-center gap-2">
                  <svg class="animate-spin w-4 h-4" fill="none" viewBox="0 0 24 24">
                    <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"/>
                    <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8v8z"/>
                  </svg> Sending...
                </span>
                <span v-else>Send Reset Link</span>
              </button>
              <button @click="showForgot=false"
                class="px-4 rounded-xl text-sm font-medium text-slate-400 hover:text-white border transition-all"
                style="border-color:rgba(212,168,0,0.2); background:rgba(212,168,0,0.05);">Cancel</button>
            </div>
          </div>

        </div>
      </div>
    </div>
  </Teleport>

  </div>
</template>

<style scoped>
@keyframes pulseGlow {
  0%, 100% { opacity: 0.10; transform: scale(1); }
  50%       { opacity: 0.20; transform: scale(1.08); }
}
@keyframes spinSlow {
  from { transform: rotate(0deg); }
  to   { transform: rotate(360deg); }
}
.input:focus {
  border-color: rgba(212,168,0,0.5) !important;
  box-shadow: 0 0 0 3px rgba(212,168,0,0.12);
  outline: none;
}
</style>
