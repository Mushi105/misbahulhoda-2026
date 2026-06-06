<script setup>
import { ref, onMounted, computed } from 'vue'
import { pilgrimApi, itineraryApi } from '@/api'
import api from '@/api/client'
import { useAuthStore } from '@/stores/auth'
import { useRouter } from 'vue-router'
import NoticeBoard from '@/components/NoticeBoard.vue'

const router = useRouter()

const auth = useAuthStore()
const profile = ref(null)
const loading = ref(true)
const showProfileForm = ref(false)
const showFamilyForm = ref(false)
const saving = ref(false)
const error = ref('')
const success = ref('')

const genderOptions = [
  { value: 1, label: 'Male (مرد)' },
  { value: 2, label: 'Female (عورت)' }
]

const relationshipOptions = [
  { value: 0, label: 'Self' },
  { value: 1, label: 'Spouse (شریک حیات)' },
  { value: 2, label: 'Son (بیٹا)' },
  { value: 3, label: 'Daughter (بیٹی)' },
  { value: 4, label: 'Father (والد)' },
  { value: 5, label: 'Mother (والدہ)' },
  { value: 6, label: 'Brother (بھائی)' },
  { value: 7, label: 'Sister (بہن)' },
  { value: 99, label: 'Other' }
]

const profileForm = ref({
  passportNumber: '', visaNumber: '', country: '', familyMemberCount: 1,
  arrivalDate: '', departureDate: '', arrivalFlight: '', departureFlight: '',
  emergencyContactName: '', emergencyContactPhone: ''
})

const familyForm = ref({
  fullName: '', gender: 1, relationship: 1, dateOfBirth: '',
  passportNumber: '', visaNumber: '', nationality: '',
  requiresWheelchair: false, isMinor: false, specialNotes: ''
})

const editingMemberId = ref(null)

const statusConfig = {
  Pending: { color: 'text-gold-400 bg-gold-900', label: 'Pending Review', icon: '⏳' },
  UnderReview: { color: 'text-blue-400 bg-blue-900', label: 'Under Review', icon: '🔍' },
  Approved: { color: 'text-green-400 bg-green-900', label: 'Approved', icon: '✅' },
  Rejected: { color: 'text-red-400 bg-red-900', label: 'Rejected', icon: '❌' },
  Cancelled: { color: 'text-slate-400 bg-slate-800', label: 'Cancelled', icon: '🚫' }
}

const maleCount = computed(() => (profile.value?.familyMembers?.filter(m => m.gender === 1).length ?? 0) + 1)
const femaleCount = computed(() => profile.value?.familyMembers?.filter(m => m.gender === 2).length ?? 0)

async function load() {
  loading.value = true
  try {
    const res = await pilgrimApi.getMyProfile()
    profile.value = res.data.data
    // Auto-show profile form if profile is incomplete (empty country means not filled yet)
    if (!profile.value?.country) {
      showProfileForm.value = true
      populateProfileForm(profile.value)
    } else {
      showProfileForm.value = false
    }
  } catch (e) {
    if (e.response?.status === 404) showProfileForm.value = true
  } finally {
    loading.value = false
  }
}

function populateProfileForm(p) {
  if (!p) return
  profileForm.value = {
    passportNumber: p.passportNumber || '',
    visaNumber: p.visaNumber || '',
    country: p.country || '',
    familyMemberCount: p.familyMemberCount || 1,
    arrivalDate: p.arrivalDate ? p.arrivalDate.split('T')[0] : '',
    departureDate: p.departureDate ? p.departureDate.split('T')[0] : '',
    arrivalFlight: p.arrivalFlight || '',
    departureFlight: p.departureFlight || '',
    emergencyContactName: p.emergencyContactName || '',
    emergencyContactPhone: p.emergencyContactPhone || '',
  }
}

function openEditProfile() {
  populateProfileForm(profile.value)
  showProfileForm.value = true
}

async function submitProfile() {
  saving.value = true; error.value = ''
  try {
    await pilgrimApi.createProfile(profileForm.value)
    success.value = 'Profile saved successfully!'
    await load()
  } catch (e) {
    error.value = e.response?.data?.message || 'Failed to save profile.'
  } finally { saving.value = false }
}

function openAddMember() {
  editingMemberId.value = null
  familyForm.value = { fullName: '', gender: 1, relationship: 1, dateOfBirth: '', passportNumber: '', visaNumber: '', nationality: '', requiresWheelchair: false, isMinor: false, specialNotes: '' }
  showFamilyForm.value = true
}

function openEditMember(m) {
  editingMemberId.value = m.id
  familyForm.value = {
    fullName: m.fullName,
    gender: m.gender,
    relationship: m.relationship,
    dateOfBirth: m.dateOfBirth ? m.dateOfBirth.split('T')[0] : '',
    passportNumber: m.passportNumber || '',
    visaNumber: m.visaNumber || '',
    nationality: m.nationality || '',
    requiresWheelchair: m.requiresWheelchair,
    isMinor: m.isMinor,
    specialNotes: m.specialNotes || '',
  }
  showFamilyForm.value = true
}

async function saveMember() {
  saving.value = true; error.value = ''
  try {
    const data = { ...familyForm.value }
    if (!data.dateOfBirth) delete data.dateOfBirth
    if (editingMemberId.value) {
      await pilgrimApi.updateFamilyMember(editingMemberId.value, data)
      success.value = 'Family member updated!'
    } else {
      await pilgrimApi.addFamilyMember(data)
      success.value = 'Family member added!'
    }
    showFamilyForm.value = false
    editingMemberId.value = null
    await load()
  } catch (e) {
    error.value = e.response?.data?.message || 'Failed to save.'
  } finally { saving.value = false }
}

async function removeMember(id) {
  if (!confirm('Remove this family member?')) return
  try {
    await pilgrimApi.removeFamilyMember(id)
    success.value = 'Family member removed.'
    await load()
  } catch { error.value = 'Failed to remove.' }
}


// ── Itinerary ─────────────────────────────────────────────
const itinerary       = ref(null)
const showTravelForm  = ref(false)
const savingTravel    = ref(false)
const travelForm      = ref({
  arrivalFlight: '', departureFlight: '', arrivalAirport: '', departureAirport: '',
  arrivalTime: '', departureTime: ''
})

async function loadItinerary() {
  try {
    const res = await itineraryApi.getMy()
    itinerary.value = res.data.data
  } catch { /* pilgrim may not have profile yet */ }
}

function openTravelEdit() {
  const it = itinerary.value
  travelForm.value = {
    arrivalFlight:    it?.arrivalFlight    || '',
    departureFlight:  it?.departureFlight  || '',
    arrivalAirport:   it?.arrivalAirport   || '',
    departureAirport: it?.departureAirport || '',
    arrivalTime:      it?.arrivalTime      || '',
    departureTime:    it?.departureTime    || '',
  }
  showTravelForm.value = true
}

async function saveTravel() {
  savingTravel.value = true; error.value = ''
  try {
    await itineraryApi.updateTravel(travelForm.value)
    success.value = 'Travel details updated!'
    showTravelForm.value = false
    await loadItinerary()
  } catch (e) { error.value = e.response?.data?.message || 'Failed to save.' }
  finally { savingTravel.value = false }
}

const transferStatusConfig = {
  Unassigned:     { color: 'text-slate-400', bg: 'bg-slate-800/60',  icon: '⚪', label: 'Not yet assigned' },
  Pending:        { color: 'text-gold-400',  bg: 'bg-gold-900/40',   icon: '⏳', label: 'Pending assignment' },
  DriverAssigned: { color: 'text-blue-400',  bg: 'bg-blue-900/40',   icon: '🚗', label: 'Driver assigned' },
  EnRoute:        { color: 'text-purple-400',bg: 'bg-purple-900/40', icon: '🛣️', label: 'Driver en route' },
  Completed:      { color: 'text-green-400', bg: 'bg-green-900/40',  icon: '✅', label: 'Completed' },
  Cancelled:      { color: 'text-red-400',   bg: 'bg-red-900/40',    icon: '❌', label: 'Cancelled' },
}

// ── Check Out ─────────────────────────────────────────────
const checkingOut = ref(false)
const showCheckoutConfirm = ref(false)

async function confirmCheckout() {
  checkingOut.value = true
  try {
    await api.post('/pilgrims/checkout')
    showCheckoutConfirm.value = false
    success.value = 'Checked out successfully. The team will assign your next room.'
    await load()
  } catch (e) {
    error.value = e.response?.data?.message || 'Checkout failed.'
  } finally { checkingOut.value = false }
}

onMounted(() => { load(); loadItinerary() })
</script>

<template>
  <div class="min-h-screen bg-dark-950 p-4 md:p-6 space-y-6">
    <div class="flex items-start justify-between">
      <div>
        <h1 class="text-2xl font-bold text-white">My Pilgrim Portal</h1>
        <p class="text-slate-400 text-sm mt-1">Welcome, {{ auth.user?.fullName }}</p>
      </div>
      <div class="text-gold-400 font-arabic text-lg hidden sm:block">لَبَّيْكَ اللَّهُمَّ لَبَّيْكَ</div>
    </div>

    <div v-if="success" class="bg-green-900/50 border border-green-700 text-green-300 rounded-lg px-4 py-3 flex justify-between">
      {{ success }}<button @click="success=''" class="text-green-500">✕</button>
    </div>
    <div v-if="error" class="bg-red-900/50 border border-red-700 text-red-300 rounded-lg px-4 py-3 flex justify-between">
      {{ error }}<button @click="error=''" class="text-red-500">✕</button>
    </div>

    <!-- Quick Access Banners -->
    <div class="grid gap-4 sm:grid-cols-2">

      <!-- Tour Guide Banner -->
      <div
        @click="router.push('/pilgrim/guide')"
        class="cursor-pointer rounded-xl overflow-hidden border border-primary-700 bg-gradient-to-r from-primary-950 via-dark-800 to-gold-950 hover:border-primary-500 transition-all group">
        <div class="flex items-center gap-4 px-5 py-4">
          <div class="text-4xl">📖</div>
          <div class="flex-1">
            <p class="text-xs font-bold uppercase tracking-wider text-primary-400 mb-0.5">Misbah ul Hoda — Arbaeen 2026</p>
            <h2 class="text-lg font-bold text-white leading-tight">Complete Tour Guide</h2>
            <p class="text-slate-400 text-sm mt-0.5">14-day schedule · Hotels · Scholars · Arbaeen Walk · Holy Sites</p>
          </div>
          <div class="shrink-0 flex items-center gap-2 bg-primary-700 group-hover:bg-primary-600 text-white text-sm font-semibold px-4 py-2 rounded-lg transition-colors">
            Open <span class="ml-1">›</span>
          </div>
        </div>
        <div class="flex gap-3 px-5 pb-3 flex-wrap">
          <span class="text-xs bg-dark-900/60 text-gold-400 border border-gold-900 px-2.5 py-1 rounded-full">4–17 Aug 2025</span>
          <span class="text-xs bg-dark-900/60 text-primary-400 border border-primary-900 px-2.5 py-1 rounded-full">Kadhimiyyah · Najaf · Karbala</span>
          <span class="text-xs bg-dark-900/60 text-slate-300 border border-dark-600 px-2.5 py-1 rounded-full">🚶 80km Arbaeen Walk</span>
        </div>
      </div>

      <!-- Documents Banner -->
      <div
        @click="router.push('/pilgrim/documents')"
        class="cursor-pointer rounded-xl overflow-hidden border border-gold-800/50 bg-gradient-to-r from-gold-950/40 via-dark-800 to-dark-900 hover:border-gold-600/70 transition-all group">
        <div class="flex items-center gap-4 px-5 py-4">
          <div class="text-4xl">📂</div>
          <div class="flex-1">
            <p class="text-xs font-bold uppercase tracking-wider text-gold-500 mb-0.5">Team Uploads</p>
            <h2 class="text-lg font-bold text-white leading-tight">Documents & Guides</h2>
            <p class="text-slate-400 text-sm mt-0.5">Manuals, notices, maps and official documents</p>
          </div>
          <div class="shrink-0 flex items-center gap-2 bg-gold-800/60 group-hover:bg-gold-700/70 text-white text-sm font-semibold px-4 py-2 rounded-lg transition-colors">
            View <span class="ml-1">›</span>
          </div>
        </div>
        <div class="flex gap-3 px-5 pb-3 flex-wrap">
          <span class="text-xs bg-dark-900/60 text-gold-400 border border-gold-900 px-2.5 py-1 rounded-full">📕 Tour Guides</span>
          <span class="text-xs bg-dark-900/60 text-slate-300 border border-dark-600 px-2.5 py-1 rounded-full">📢 Notices</span>
          <span class="text-xs bg-dark-900/60 text-slate-300 border border-dark-600 px-2.5 py-1 rounded-full">🗾 Maps</span>
        </div>
      </div>

    </div>

    <div v-if="loading" class="flex justify-center py-20 text-slate-400">Loading your profile...</div>

    <!-- Profile Form (create OR edit) -->
    <div v-else-if="showProfileForm" class="max-w-2xl">
      <div class="card">
        <div class="flex items-center justify-between mb-1">
          <h2 class="text-lg font-semibold text-white">{{ profile ? 'Edit Your Profile' : 'Complete Your Pilgrim Profile' }}</h2>
          <button v-if="profile" @click="showProfileForm = false" class="text-slate-500 hover:text-white text-xl leading-none">✕</button>
        </div>
        <p class="text-slate-400 text-sm mb-6">Fill in your travel details to register for Arbaeen 2026.</p>
        <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
          <div><label class="label">Passport Number *</label><input v-model="profileForm.passportNumber" type="text" class="input" placeholder="AB1234567" /></div>
          <div><label class="label">Visa Number</label><input v-model="profileForm.visaNumber" type="text" class="input" placeholder="Optional" /></div>
          <div><label class="label">Country *</label><input v-model="profileForm.country" type="text" class="input" placeholder="Pakistan" /></div>
          <div><label class="label">Total Family Members</label><input v-model.number="profileForm.familyMemberCount" type="number" min="1" max="20" class="input" /></div>
          <div><label class="label">Arrival Date *</label><input v-model="profileForm.arrivalDate" type="date" class="input" /></div>
          <div><label class="label">Departure Date *</label><input v-model="profileForm.departureDate" type="date" class="input" /></div>
          <div><label class="label">Arrival Flight</label><input v-model="profileForm.arrivalFlight" type="text" class="input" placeholder="PK-301" /></div>
          <div><label class="label">Departure Flight</label><input v-model="profileForm.departureFlight" type="text" class="input" placeholder="PK-302" /></div>
          <div><label class="label">Emergency Contact Name</label><input v-model="profileForm.emergencyContactName" type="text" class="input" /></div>
          <div><label class="label">Emergency Contact Phone</label><input v-model="profileForm.emergencyContactPhone" type="text" class="input" placeholder="+92..." /></div>
        </div>
        <div class="mt-6 flex gap-3">
          <button @click="submitProfile" :disabled="saving || !profileForm.country || !profileForm.arrivalDate || !profileForm.departureDate" class="btn-primary">
            {{ saving ? 'Saving...' : 'Save Profile' }}
          </button>
          <button v-if="profile" @click="showProfileForm = false" class="px-4 py-2 text-slate-400 hover:text-white text-sm">Cancel</button>
        </div>
      </div>
    </div>

    <!-- Profile Dashboard -->
    <template v-else-if="profile && !showProfileForm">

      <!-- Status Banner -->
      <div :class="['rounded-xl border overflow-hidden',
        profile.status === 'Approved'     ? 'border-emerald-700' :
        profile.status === 'Rejected'     ? 'border-red-800' :
        profile.status === 'UnderReview'  ? 'border-blue-700' : 'border-amber-800/60']"
        style="background:rgba(2,20,10,0.5); backdrop-filter:blur(8px);">

        <!-- Top colour bar per status -->
        <div :class="['h-1 w-full',
          profile.status === 'Approved'    ? 'bg-gradient-to-r from-emerald-600 to-green-500' :
          profile.status === 'Rejected'    ? 'bg-gradient-to-r from-red-700 to-red-500' :
          profile.status === 'UnderReview' ? 'bg-gradient-to-r from-blue-700 to-blue-500' :
          'bg-gradient-to-r from-amber-700 to-yellow-600']"></div>

        <div class="p-5">
          <!-- Header row -->
          <div class="flex items-center gap-4 mb-3">
            <div :class="['w-12 h-12 rounded-xl flex items-center justify-center text-2xl shrink-0',
              profile.status === 'Approved'    ? 'bg-emerald-900/60' :
              profile.status === 'Rejected'    ? 'bg-red-900/60' :
              profile.status === 'UnderReview' ? 'bg-blue-900/60' : 'bg-amber-900/60']">
              {{ statusConfig[profile.status]?.icon }}
            </div>
            <div>
              <p class="text-xs font-bold uppercase tracking-wider mb-0.5"
                :class="profile.status === 'Approved' ? 'text-emerald-400' : profile.status === 'Rejected' ? 'text-red-400' : profile.status === 'UnderReview' ? 'text-blue-400' : 'text-amber-400'">
                Application Status
              </p>
              <p class="text-white font-bold text-lg leading-tight">{{ statusConfig[profile.status]?.label }}</p>
            </div>
          </div>

          <!-- Pending -->
          <template v-if="profile.status === 'Pending'">
            <p class="text-slate-300 text-sm mb-3">Your application has been submitted successfully. Our team will begin reviewing it shortly.</p>
            <div class="grid grid-cols-1 sm:grid-cols-3 gap-2">
              <div class="rounded-lg px-3 py-2.5 text-xs" style="background:rgba(16,185,129,0.08); border:1px solid rgba(16,185,129,0.15);">
                <p class="text-emerald-400 font-semibold mb-0.5">✅ Submitted</p>
                <p class="text-slate-400">Your details are saved</p>
              </div>
              <div class="rounded-lg px-3 py-2.5 text-xs" style="background:rgba(245,158,11,0.08); border:1px solid rgba(245,158,11,0.15);">
                <p class="text-amber-400 font-semibold mb-0.5">⏳ Awaiting Review</p>
                <p class="text-slate-400">Team will review soon</p>
              </div>
              <div class="rounded-lg px-3 py-2.5 text-xs" style="background:rgba(100,116,139,0.08); border:1px solid rgba(100,116,139,0.15);">
                <p class="text-slate-400 font-semibold mb-0.5">📲 Stay Alert</p>
                <p class="text-slate-500">Check WhatsApp & email</p>
              </div>
            </div>
          </template>

          <!-- Under Review -->
          <template v-else-if="profile.status === 'UnderReview'">
            <p class="text-slate-300 text-sm mb-3">Our team is currently reviewing your application. Please ensure all your profile details and documents are complete.</p>
            <div class="grid grid-cols-1 sm:grid-cols-3 gap-2">
              <div class="rounded-lg px-3 py-2.5 text-xs" style="background:rgba(59,130,246,0.08); border:1px solid rgba(59,130,246,0.2);">
                <p class="text-blue-400 font-semibold mb-0.5">🔍 In Progress</p>
                <p class="text-slate-400">Active review underway</p>
              </div>
              <div class="rounded-lg px-3 py-2.5 text-xs" style="background:rgba(16,185,129,0.08); border:1px solid rgba(16,185,129,0.15);">
                <p class="text-emerald-400 font-semibold mb-0.5">📋 Action Needed</p>
                <p class="text-slate-400">Complete your profile below</p>
              </div>
              <div class="rounded-lg px-3 py-2.5 text-xs" style="background:rgba(100,116,139,0.08); border:1px solid rgba(100,116,139,0.15);">
                <p class="text-slate-400 font-semibold mb-0.5">📲 We May Contact You</p>
                <p class="text-slate-500">Keep WhatsApp active</p>
              </div>
            </div>
          </template>

          <!-- Approved -->
          <template v-else-if="profile.status === 'Approved'">
            <div class="flex items-center gap-2 mb-3">
              <span class="font-arabic text-emerald-400 text-base">مُبَارَک</span>
              <p class="text-emerald-300 text-sm font-medium">Your application has been approved!</p>
            </div>
            <div class="grid grid-cols-1 sm:grid-cols-3 gap-2">
              <div class="rounded-lg px-3 py-2.5 text-xs" style="background:rgba(16,185,129,0.10); border:1px solid rgba(16,185,129,0.25);">
                <p class="text-emerald-400 font-semibold mb-0.5">🏨 Room Allocation</p>
                <p class="text-slate-400">{{ profile.room ? `Room ${profile.room.roomNumber} assigned` : 'Being arranged by admin' }}</p>
              </div>
              <div class="rounded-lg px-3 py-2.5 text-xs" style="background:rgba(16,185,129,0.08); border:1px solid rgba(16,185,129,0.15);">
                <p class="text-emerald-400 font-semibold mb-0.5">🚌 Bus Allocation</p>
                <p class="text-slate-400">{{ profile.bus ? `Bus ${profile.bus.busNumber} assigned` : 'Being arranged by admin' }}</p>
              </div>
              <div class="rounded-lg px-3 py-2.5 text-xs" style="background:rgba(16,185,129,0.08); border:1px solid rgba(16,185,129,0.15);">
                <p class="text-emerald-400 font-semibold mb-0.5">📖 Tour Guide</p>
                <p class="text-slate-400">View the complete 14-day guide</p>
              </div>
            </div>
          </template>

          <!-- Rejected -->
          <template v-else-if="profile.status === 'Rejected'">
            <p class="text-slate-300 text-sm mb-3">Unfortunately your application was not approved at this time. See the reason below and contact us if you have questions.</p>
            <div v-if="profile.rejectionReason"
                 class="rounded-lg px-4 py-3 mb-3 flex items-start gap-3"
                 style="background:rgba(127,29,29,0.4); border:1px solid rgba(239,68,68,0.3);">
              <span class="text-red-400 text-lg shrink-0 mt-0.5">⚠️</span>
              <div>
                <p class="text-red-300 text-xs font-semibold uppercase tracking-wider mb-1">Rejection Reason</p>
                <p class="text-red-200 text-sm">{{ profile.rejectionReason }}</p>
              </div>
            </div>
            <p class="text-slate-400 text-xs">If you believe this is an error, please contact us on WhatsApp or email for assistance.</p>
          </template>

        </div>
      </div>

      <!-- Stats -->
      <div class="grid grid-cols-2 md:grid-cols-4 gap-4">
        <div class="card text-center py-4">
          <div class="text-3xl font-bold text-primary-400">{{ (profile.familyMembers?.length ?? 0) + 1 }}</div>
          <div class="text-slate-400 text-xs mt-1">Total Members</div>
        </div>
        <div class="card text-center py-4">
          <div class="text-3xl font-bold text-blue-400">{{ maleCount }}</div>
          <div class="text-slate-400 text-xs mt-1">Male (مرد)</div>
        </div>
        <div class="card text-center py-4">
          <div class="text-3xl font-bold text-pink-400">{{ femaleCount }}</div>
          <div class="text-slate-400 text-xs mt-1">Female (عورت)</div>
        </div>
        <div class="card text-center py-4">
          <div class="text-3xl font-bold text-gold-400">{{ profile.familyMembers?.filter(m => m.isMinor).length ?? 0 }}</div>
          <div class="text-slate-400 text-xs mt-1">Minors</div>
        </div>
      </div>

      <!-- Info Cards -->
      <div class="grid grid-cols-1 lg:grid-cols-2 gap-6">
        <div class="card space-y-3">
          <div class="flex items-center justify-between border-b border-dark-600 pb-2">
            <h2 class="text-base font-semibold text-white">Personal Details</h2>
            <button @click="openEditProfile" class="text-xs text-gold-400 hover:text-gold-300 border border-gold-800/50 hover:border-gold-700 px-2.5 py-1 rounded-lg transition-colors">✏️ Edit</button>
          </div>
          <div class="grid grid-cols-2 gap-3 text-sm">
            <div><p class="text-slate-500 text-xs">Full Name</p><p class="text-white">{{ profile.user?.fullName }}</p></div>
            <div><p class="text-slate-500 text-xs">Email</p><p class="text-white truncate">{{ profile.user?.email }}</p></div>
            <div><p class="text-slate-500 text-xs">Phone</p><p class="text-white">{{ profile.user?.phoneNumber }}</p></div>
            <div><p class="text-slate-500 text-xs">Country</p><p class="text-white">{{ profile.country }}</p></div>
            <div><p class="text-slate-500 text-xs">Passport</p><p class="text-white">{{ profile.passportNumber }}</p></div>
            <div><p class="text-slate-500 text-xs">Visa</p><p class="text-white">{{ profile.visaNumber || '—' }}</p></div>
          </div>
        </div>
        <!-- Travel Details Card -->
        <div class="rounded-xl border overflow-hidden"
             style="background:rgba(2,20,10,0.5); border-color:rgba(16,185,129,0.2);">
          <div class="px-4 py-3 flex items-center justify-between border-b"
               style="border-color:rgba(16,185,129,0.12); background:rgba(16,185,129,0.05);">
            <div class="flex items-center gap-2">
              <span class="text-lg">✈️</span>
              <h2 class="text-sm font-semibold text-white">My Travel Dates</h2>
            </div>
            <button @click="openTravelEdit"
              class="text-xs px-3 py-1.5 rounded-lg border border-emerald-800/50 text-emerald-400 hover:bg-emerald-900/30 transition"
              style="touch-action:manipulation; min-height:36px;">
              ✏️ Update
            </button>
          </div>
          <div class="p-4 grid grid-cols-2 gap-3 text-sm">
            <div class="rounded-lg p-2.5" style="background:rgba(16,185,129,0.06);">
              <p class="text-slate-500 text-xs mb-0.5">🛬 Arrival (Iraq)</p>
              <p class="text-white font-semibold">{{ new Date(profile.arrivalDate).toLocaleDateString('en-GB', {day:'2-digit', month:'short', year:'numeric'}) }}</p>
              <p v-if="profile.arrivalFlight" class="text-emerald-400 text-xs mt-0.5">{{ profile.arrivalFlight }}</p>
              <p v-if="itinerary?.arrivalTime" class="text-slate-400 text-xs">{{ itinerary.arrivalTime }}</p>
              <p v-if="itinerary?.arrivalAirport" class="text-slate-400 text-xs truncate">{{ itinerary.arrivalAirport }}</p>
            </div>
            <div class="rounded-lg p-2.5" style="background:rgba(16,185,129,0.06);">
              <p class="text-slate-500 text-xs mb-0.5">🛫 Departure</p>
              <p class="text-white font-semibold">{{ new Date(profile.departureDate).toLocaleDateString('en-GB', {day:'2-digit', month:'short', year:'numeric'}) }}</p>
              <p v-if="profile.departureFlight" class="text-emerald-400 text-xs mt-0.5">{{ profile.departureFlight }}</p>
              <p v-if="itinerary?.departureTime" class="text-slate-400 text-xs">{{ itinerary.departureTime }}</p>
              <p v-if="itinerary?.departureAirport" class="text-slate-400 text-xs truncate">{{ itinerary.departureAirport }}</p>
            </div>
            <div class="col-span-2 rounded-lg p-2.5" style="background:rgba(16,185,129,0.06);">
              <p class="text-slate-500 text-xs mb-0.5">🚌 Bus Assignment</p>
              <p v-if="profile.bus" class="text-emerald-400 font-semibold text-sm">Bus {{ profile.bus?.busNumber }} — {{ profile.bus?.driverName || '' }}</p>
              <p v-else class="text-yellow-500 text-xs">Admin will assign your bus — you'll be notified</p>
            </div>
          </div>
          <!-- important note for pilgrim -->
          <div class="px-4 pb-3">
            <p class="text-xs text-slate-500 bg-slate-900/50 rounded-lg px-3 py-2 border border-slate-800">
              ℹ️ Update your exact flight details — the admin uses these dates to prepare the airport team.
            </p>
          </div>
        </div>
      </div>

      <!-- Hotel & Room Card -->
      <div v-if="profile.room" class="rounded-xl border border-primary-800 overflow-hidden"
           style="background: linear-gradient(135deg, rgba(5,46,22,0.8), rgba(2,6,23,0.9));">

        <!-- Header -->
        <div class="px-5 py-3 flex items-center gap-2 border-b border-primary-800/50"
             style="background:rgba(16,185,129,0.08);">
          <span class="text-xl">🏨</span>
          <h2 class="text-base font-semibold text-white flex-1">Your Hotel & Room</h2>
          <span class="text-xs text-emerald-400 font-medium">✅ Allocated</span>
        </div>

        <div class="p-5 space-y-4">

          <!-- Hotel name + room number -->
          <div class="flex items-start justify-between gap-4 flex-wrap">
            <div class="flex-1 min-w-0">
              <p class="text-white text-xl font-bold leading-tight">{{ profile.room.hotelName || 'Hotel' }}</p>
              <p class="text-emerald-400 text-sm mt-0.5 font-medium">{{ profile.room.hotelCity }}</p>
              <p class="text-slate-500 text-xs mt-1">{{ profile.room.hotelAddress }}</p>
            </div>
            <div class="text-right shrink-0 bg-emerald-950/50 border border-emerald-800/50 rounded-xl px-4 py-2">
              <p class="text-slate-500 text-xs">Room</p>
              <p class="text-emerald-400 font-bold text-2xl">{{ profile.room.roomNumber }}</p>
              <p class="text-slate-500 text-xs">{{ profile.room.floorLabel || `Floor ${profile.room.floorNumber}` }}</p>
            </div>
          </div>

          <!-- Haram distance — the key info pilgrim needs on arrival -->
          <div v-if="profile.room.haramDistanceText || profile.room.nearHaram"
               class="rounded-xl p-3 flex items-center gap-3"
               style="background:rgba(180,83,9,0.12); border:1px solid rgba(180,83,9,0.3);">
            <span class="text-2xl flex-shrink-0">🕌</span>
            <div>
              <p v-if="profile.room.haramDistanceText"
                 class="text-gold-300 font-bold text-base">
                {{ profile.room.haramDistanceText }}
              </p>
              <p v-if="profile.room.nearHaram"
                 class="text-gold-400/80 text-sm mt-0.5">
                {{ profile.room.nearHaram }}
              </p>
            </div>
          </div>

          <!-- Action buttons -->
          <div class="grid grid-cols-1 gap-2">

            <!-- Navigate to Hotel — most important button for arriving pilgrim -->
            <a v-if="profile.room.latitude && profile.room.longitude"
               :href="`https://www.google.com/maps/dir/?api=1&destination=${profile.room.latitude},${profile.room.longitude}&travelmode=driving`"
               target="_blank" rel="noopener noreferrer"
               class="flex items-center justify-center gap-2 py-3 rounded-xl font-bold text-base transition-colors"
               style="background:linear-gradient(135deg,#065f46,#047857); box-shadow:0 4px 16px rgba(16,185,129,0.25);">
              <span class="text-xl">🧭</span>
              <span class="text-white">Navigate to Hotel (for taxi)</span>
            </a>

            <!-- Haram navigation -->
            <a v-if="profile.room.haramLatitude && profile.room.haramLongitude"
               :href="`https://www.google.com/maps/dir/?api=1&destination=${profile.room.haramLatitude},${profile.room.haramLongitude}&travelmode=walking`"
               target="_blank" rel="noopener noreferrer"
               class="flex items-center justify-center gap-2 py-2.5 rounded-xl font-semibold text-sm transition-colors border border-gold-700/50"
               style="background:rgba(180,83,9,0.15);">
              <span>🕌</span>
              <span class="text-gold-300">Navigate to Imam Hussain Haram</span>
            </a>

            <div class="grid grid-cols-2 gap-2">
              <a v-if="profile.room.hotelPhone"
                 :href="`tel:${profile.room.hotelPhone}`"
                 class="flex items-center justify-center gap-2 py-2.5 rounded-xl border border-slate-700 text-slate-300 text-sm transition-colors hover:bg-slate-800">
                📞 Hotel Call
              </a>
              <button @click="showCheckoutConfirm = true"
                class="flex items-center justify-center gap-2 py-2.5 rounded-xl border border-red-900/50 text-red-400 text-sm transition-colors hover:bg-red-950/30">
                🚪 Check Out
              </button>
            </div>
          </div>

        </div>
      </div>

      <!-- Checkout Confirm Modal -->
      <div v-if="showCheckoutConfirm" class="fixed inset-0 bg-black/70 z-50 flex items-center justify-center p-4">
        <div class="bg-dark-900 border border-red-800 rounded-2xl p-6 max-w-sm w-full shadow-2xl">
          <div class="text-center mb-5">
            <div class="text-5xl mb-3">🚪</div>
            <h3 class="text-white text-lg font-bold">Check Out of Room?</h3>
            <p class="text-slate-400 text-sm mt-2">
              You are checking out of <span class="text-white font-semibold">Room {{ profile.room?.roomNumber }}</span>
              at <span class="text-white font-semibold">{{ profile.room?.hotelName }}</span>.
            </p>
            <p class="text-gold-400 text-xs mt-3 bg-gold-950 border border-gold-800 rounded-lg px-3 py-2">
              ⚠️ The admin team will be notified. Your next room will be assigned when you arrive at the new hotel.
            </p>
          </div>
          <div class="flex gap-3">
            <button @click="showCheckoutConfirm = false" class="flex-1 py-2.5 rounded-xl border border-dark-600 text-slate-300 hover:text-white text-sm transition-colors">
              Cancel
            </button>
            <button @click="confirmCheckout" :disabled="checkingOut"
              class="flex-1 py-2.5 rounded-xl bg-red-700 hover:bg-red-600 text-white font-semibold text-sm transition-colors disabled:opacity-60">
              {{ checkingOut ? 'Checking out...' : 'Yes, Check Out' }}
            </button>
          </div>
        </div>
      </div>
      <div v-else class="rounded-xl border border-dark-700 bg-dark-900 px-5 py-4 flex items-center gap-3">
        <span class="text-2xl">🏨</span>
        <div>
          <p class="text-white font-medium text-sm">Hotel & Room</p>
          <p class="text-slate-500 text-xs mt-0.5">Not allocated yet — admin will assign your room soon.</p>
        </div>
      </div>

      <!-- ── Itinerary & Airport Transfers ─────────────────────────── -->
      <div v-if="itinerary" class="space-y-4">

        <!-- Section header -->
        <div class="flex items-center justify-between">
          <h2 class="text-lg font-semibold text-white flex items-center gap-2">✈️ My Itinerary</h2>
          <button @click="openTravelEdit"
            class="text-xs text-gold-400 hover:text-gold-300 border border-gold-800/50 hover:border-gold-700 px-2.5 py-1 rounded-lg transition-colors">
            ✏️ Edit Travel Details
          </button>
        </div>

        <div class="grid grid-cols-1 md:grid-cols-2 gap-4">

          <!-- Arrival Card -->
          <div class="rounded-xl border border-green-800/60 bg-dark-900 overflow-hidden">
            <div class="px-4 py-3 bg-green-900/30 border-b border-green-800/50 flex items-center gap-2">
              <span class="text-xl">🛬</span>
              <h3 class="text-white font-semibold text-sm">Arrival</h3>
            </div>
            <div class="p-4 space-y-3">
              <div class="grid grid-cols-2 gap-3 text-sm">
                <div>
                  <p class="text-slate-500 text-xs">Date</p>
                  <p class="text-white">{{ new Date(itinerary.arrivalDate).toLocaleDateString('en-GB', { day:'2-digit', month:'short', year:'numeric' }) }}</p>
                </div>
                <div>
                  <p class="text-slate-500 text-xs">Time</p>
                  <p class="text-white">{{ itinerary.arrivalTime || 'Not set' }}</p>
                </div>
                <div>
                  <p class="text-slate-500 text-xs">Flight</p>
                  <p class="text-white">{{ itinerary.arrivalFlight || '—' }}</p>
                </div>
                <div>
                  <p class="text-slate-500 text-xs">Airport</p>
                  <p class="text-white">{{ itinerary.arrivalAirport || 'Not set' }}</p>
                </div>
              </div>

              <!-- Pickup Transfer Status -->
              <div :class="['rounded-lg p-3', (transferStatusConfig[itinerary.arrivalTransfer?.status] || transferStatusConfig.Unassigned).bg]">
                <div class="flex items-center gap-2 mb-1">
                  <span>{{ (transferStatusConfig[itinerary.arrivalTransfer?.status] || transferStatusConfig.Unassigned).icon }}</span>
                  <span :class="['text-xs font-semibold', (transferStatusConfig[itinerary.arrivalTransfer?.status] || transferStatusConfig.Unassigned).color]">
                    Pickup: {{ (transferStatusConfig[itinerary.arrivalTransfer?.status] || transferStatusConfig.Unassigned).label }}
                  </span>
                </div>
                <template v-if="itinerary.arrivalTransfer?.driverName">
                  <div class="space-y-0.5 text-xs">
                    <p class="text-white">🚗 {{ itinerary.arrivalTransfer.driverName }}
                      <span v-if="itinerary.arrivalTransfer.vehicleType"> · {{ itinerary.arrivalTransfer.vehicleType }}</span>
                      <span v-if="itinerary.arrivalTransfer.vehicleNumber"> ({{ itinerary.arrivalTransfer.vehicleNumber }})</span>
                    </p>
                    <p v-if="itinerary.arrivalTransfer.driverPhone" class="text-slate-300">
                      📞 {{ itinerary.arrivalTransfer.driverPhone }}
                      <a :href="'https://wa.me/' + itinerary.arrivalTransfer.driverPhone.replace(/\D/g,'')"
                        target="_blank" class="text-green-400 hover:underline ml-1">WhatsApp</a>
                    </p>
                    <p v-if="itinerary.arrivalTransfer.meetingPoint" class="text-slate-400">
                      📍 {{ itinerary.arrivalTransfer.meetingPoint }}
                    </p>
                    <p v-if="itinerary.arrivalTransfer.scheduledTime" class="text-slate-400">
                      🕐 {{ new Date(itinerary.arrivalTransfer.scheduledTime).toLocaleString('en-GB',{day:'2-digit',month:'short',hour:'2-digit',minute:'2-digit'}) }}
                    </p>
                  </div>
                </template>
                <p v-else class="text-slate-500 text-xs">Admin will assign a driver before your arrival.</p>
              </div>
            </div>
          </div>

          <!-- Departure Card -->
          <div class="rounded-xl border border-blue-800/60 bg-dark-900 overflow-hidden">
            <div class="px-4 py-3 bg-blue-900/30 border-b border-blue-800/50 flex items-center gap-2">
              <span class="text-xl">🛫</span>
              <h3 class="text-white font-semibold text-sm">Departure</h3>
            </div>
            <div class="p-4 space-y-3">
              <div class="grid grid-cols-2 gap-3 text-sm">
                <div>
                  <p class="text-slate-500 text-xs">Date</p>
                  <p class="text-white">{{ new Date(itinerary.departureDate).toLocaleDateString('en-GB', { day:'2-digit', month:'short', year:'numeric' }) }}</p>
                </div>
                <div>
                  <p class="text-slate-500 text-xs">Time</p>
                  <p class="text-white">{{ itinerary.departureTime || 'Not set' }}</p>
                </div>
                <div>
                  <p class="text-slate-500 text-xs">Flight</p>
                  <p class="text-white">{{ itinerary.departureFlight || '—' }}</p>
                </div>
                <div>
                  <p class="text-slate-500 text-xs">Airport</p>
                  <p class="text-white">{{ itinerary.departureAirport || 'Not set' }}</p>
                </div>
              </div>

              <!-- Dropoff Transfer Status -->
              <div :class="['rounded-lg p-3', (transferStatusConfig[itinerary.departureTransfer?.status] || transferStatusConfig.Unassigned).bg]">
                <div class="flex items-center gap-2 mb-1">
                  <span>{{ (transferStatusConfig[itinerary.departureTransfer?.status] || transferStatusConfig.Unassigned).icon }}</span>
                  <span :class="['text-xs font-semibold', (transferStatusConfig[itinerary.departureTransfer?.status] || transferStatusConfig.Unassigned).color]">
                    Dropoff: {{ (transferStatusConfig[itinerary.departureTransfer?.status] || transferStatusConfig.Unassigned).label }}
                  </span>
                </div>
                <template v-if="itinerary.departureTransfer?.driverName">
                  <div class="space-y-0.5 text-xs">
                    <p class="text-white">🚗 {{ itinerary.departureTransfer.driverName }}
                      <span v-if="itinerary.departureTransfer.vehicleType"> · {{ itinerary.departureTransfer.vehicleType }}</span>
                      <span v-if="itinerary.departureTransfer.vehicleNumber"> ({{ itinerary.departureTransfer.vehicleNumber }})</span>
                    </p>
                    <p v-if="itinerary.departureTransfer.driverPhone" class="text-slate-300">
                      📞 {{ itinerary.departureTransfer.driverPhone }}
                      <a :href="'https://wa.me/' + itinerary.departureTransfer.driverPhone.replace(/\D/g,'')"
                        target="_blank" class="text-green-400 hover:underline ml-1">WhatsApp</a>
                    </p>
                    <p v-if="itinerary.departureTransfer.meetingPoint" class="text-slate-400">
                      📍 {{ itinerary.departureTransfer.meetingPoint }}
                    </p>
                    <p v-if="itinerary.departureTransfer.scheduledTime" class="text-slate-400">
                      🕐 {{ new Date(itinerary.departureTransfer.scheduledTime).toLocaleString('en-GB',{day:'2-digit',month:'short',hour:'2-digit',minute:'2-digit'}) }}
                    </p>
                  </div>
                </template>
                <p v-else class="text-slate-500 text-xs">Admin will assign a driver for your departure.</p>
              </div>
            </div>
          </div>
        </div>

        <!-- Edit Travel Details Form -->
        <div v-if="showTravelForm" class="card">
          <div class="flex items-center justify-between mb-4">
            <h3 class="text-white font-semibold">Edit Travel Details</h3>
            <button @click="showTravelForm = false" class="text-slate-500 hover:text-white text-xl">✕</button>
          </div>
          <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div>
              <label class="label">Arrival Flight Number</label>
              <input v-model="travelForm.arrivalFlight" type="text" class="input" placeholder="PK-301" />
            </div>
            <div>
              <label class="label">Arrival Airport</label>
              <input v-model="travelForm.arrivalAirport" type="text" class="input" placeholder="Najaf International Airport" />
            </div>
            <div>
              <label class="label">Arrival Time (HH:MM)</label>
              <input v-model="travelForm.arrivalTime" type="time" class="input" />
            </div>
            <div>
              <label class="label">Departure Flight Number</label>
              <input v-model="travelForm.departureFlight" type="text" class="input" placeholder="PK-302" />
            </div>
            <div>
              <label class="label">Departure Airport</label>
              <input v-model="travelForm.departureAirport" type="text" class="input" placeholder="Baghdad International Airport" />
            </div>
            <div>
              <label class="label">Departure Time (HH:MM)</label>
              <input v-model="travelForm.departureTime" type="time" class="input" />
            </div>
          </div>
          <div class="mt-4 flex gap-3">
            <button @click="saveTravel" :disabled="savingTravel" class="btn-primary">
              {{ savingTravel ? 'Saving...' : 'Save Travel Details' }}
            </button>
            <button @click="showTravelForm = false" class="px-4 py-2 text-sm text-slate-400 hover:text-white">Cancel</button>
          </div>
        </div>

      </div>

      <!-- Notice Board -->
      <div class="card">
        <h2 class="text-lg font-semibold text-white mb-4 flex items-center gap-2">
          <span>📋</span> Notice Board
        </h2>
        <NoticeBoard />
      </div>

      <!-- Family Members -->
      <div class="card">
        <div class="flex items-center justify-between mb-4">
          <div>
            <h2 class="text-lg font-semibold text-white">Family Members</h2>
            <p class="text-slate-400 text-xs mt-0.5">Add all family members traveling with you</p>
          </div>
          <button @click="openAddMember" class="btn-primary text-sm px-3 py-1.5">+ Add Member</button>
        </div>

        <div v-if="showFamilyForm" class="bg-dark-800 rounded-xl p-4 mb-5 border border-dark-600">
          <h3 class="text-white font-medium mb-3 text-sm">{{ editingMemberId ? 'Edit Family Member' : 'New Family Member' }}</h3>
          <div class="grid grid-cols-1 md:grid-cols-3 gap-3">
            <div><label class="label">Full Name *</label><input v-model="familyForm.fullName" type="text" class="input" placeholder="Full name as on passport" /></div>
            <div>
              <label class="label">Gender *</label>
              <select v-model.number="familyForm.gender" class="input">
                <option v-for="g in genderOptions" :key="g.value" :value="g.value">{{ g.label }}</option>
              </select>
            </div>
            <div>
              <label class="label">Relationship *</label>
              <select v-model.number="familyForm.relationship" class="input">
                <option v-for="r in relationshipOptions" :key="r.value" :value="r.value">{{ r.label }}</option>
              </select>
            </div>
            <div><label class="label">Date of Birth</label><input v-model="familyForm.dateOfBirth" type="date" class="input" /></div>
            <div><label class="label">Passport Number</label><input v-model="familyForm.passportNumber" type="text" class="input" /></div>
            <div><label class="label">Nationality</label><input v-model="familyForm.nationality" type="text" class="input" placeholder="Pakistani" /></div>
            <div class="flex items-end gap-4 pb-1">
              <label class="flex items-center gap-2 cursor-pointer">
                <input v-model="familyForm.requiresWheelchair" type="checkbox" class="w-4 h-4" />
                <span class="text-slate-300 text-sm">Needs Wheelchair ♿</span>
              </label>
            </div>
            <div class="flex items-end gap-4 pb-1">
              <label class="flex items-center gap-2 cursor-pointer">
                <input v-model="familyForm.isMinor" type="checkbox" class="w-4 h-4" />
                <span class="text-slate-300 text-sm">Minor (under 18)</span>
              </label>
            </div>
            <div><label class="label">Special Notes</label><input v-model="familyForm.specialNotes" type="text" class="input" placeholder="Medical needs, etc." /></div>
          </div>
          <div class="mt-4 flex gap-2">
            <button @click="saveMember" :disabled="saving || !familyForm.fullName" class="btn-primary text-sm">
              {{ saving ? 'Saving...' : editingMemberId ? 'Update Member' : 'Add Member' }}
            </button>
            <button @click="showFamilyForm = false; editingMemberId = null" class="px-4 py-2 text-sm text-slate-400 hover:text-white">Cancel</button>
          </div>
        </div>

        <div v-if="profile.familyMembers?.length" class="overflow-x-auto">
          <table class="w-full text-sm">
            <thead>
              <tr class="text-slate-500 border-b border-dark-600 text-xs uppercase">
                <th class="text-left pb-2 font-medium pr-4">Name</th>
                <th class="text-left pb-2 font-medium pr-4">Gender</th>
                <th class="text-left pb-2 font-medium pr-4">Relationship</th>
                <th class="text-left pb-2 font-medium pr-4">Passport</th>
                <th class="text-left pb-2 font-medium pr-4">Notes</th>
                <th class="pb-2"></th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="m in profile.familyMembers" :key="m.id" class="border-b border-dark-700">
                <td class="py-2.5 text-white font-medium pr-4">{{ m.fullName }}</td>
                <td class="py-2.5 pr-4">
                  <span :class="m.gender === 1 ? 'text-blue-400' : 'text-pink-400'">
                    {{ m.gender === 1 ? '♂ Male' : '♀ Female' }}
                  </span>
                </td>
                <td class="py-2.5 text-slate-300 pr-4">{{ relationshipOptions.find(r => r.value === m.relationship)?.label }}</td>
                <td class="py-2.5 text-slate-400 pr-4">{{ m.passportNumber || '—' }}</td>
                <td class="py-2.5 pr-4">
                  <span v-if="m.isMinor" class="text-xs bg-gold-900 text-gold-400 rounded px-1.5 py-0.5 mr-1">Minor</span>
                  <span v-if="m.requiresWheelchair" class="text-xs bg-slate-800 text-slate-300 rounded px-1.5 py-0.5">♿</span>
                </td>
                <td class="py-2.5">
                  <div class="flex items-center gap-1">
                    <button @click="openEditMember(m)" class="text-gold-400 hover:text-gold-300 text-xs px-2 py-1 rounded hover:bg-gold-950/40">✏️ Edit</button>
                    <button @click="removeMember(m.id)" class="text-red-500 hover:text-red-400 text-xs px-2 py-1 rounded hover:bg-red-950">Remove</button>
                  </div>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
        <div v-else class="text-center py-8 text-slate-500">
          <div class="text-4xl mb-2">👨‍👩‍👧‍👦</div>
          <p>No family members added yet.</p>
          <p class="text-xs mt-1">Click "+ Add Member" to add your family.</p>
        </div>
      </div>

    </template>
  </div>
</template>
