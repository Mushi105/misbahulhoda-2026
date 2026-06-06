<script setup>
import { ref, computed } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import { pilgrimApi } from '@/api'
import api from '@/api/client'
import MHLogo from '@/components/MHLogo.vue'

const router = useRouter()
const auth = useAuthStore()

const step = ref(1)   // 1 = account, 2 = profile, 3 = family
const loading = ref(false)
const error = ref('')

// Step 1 — account
const account = ref({ fullName: '', email: '', phoneNumber: '', whatsAppNumber: '', password: '' })

// Step 2 — pilgrim profile
const profile = ref({
  passportNumber: '', visaNumber: '', country: '', familyMemberCount: 1,
  arrivalDate: '', departureDate: '', arrivalFlight: '', departureFlight: '',
  emergencyContactName: '', emergencyContactPhone: ''
})

// Step 3 — family members
const members = ref([])
const showMemberForm = ref(false)
const memberForm = ref(emptyMember())

const genderOptions = [
  { value: 1, label: 'Male (مرد)' },
  { value: 2, label: 'Female (عورت)' }
]
const relationshipOptions = [
  { value: 1, label: 'Spouse (شریک حیات)' },
  { value: 2, label: 'Son (بیٹا)' },
  { value: 3, label: 'Daughter (بیٹی)' },
  { value: 4, label: 'Father (والد)' },
  { value: 5, label: 'Mother (والدہ)' },
  { value: 6, label: 'Brother (بھائی)' },
  { value: 7, label: 'Sister (بہن)' },
  { value: 99, label: 'Other' }
]

const stepLabel = computed(() => ['Account', 'Profile', 'Family'][step.value - 1])

function emptyMember() {
  return { fullName: '', gender: 1, relationship: 1, dateOfBirth: '', passportNumber: '', nationality: '', requiresWheelchair: false, isMinor: false, specialNotes: '' }
}

// ── Step 1: Register account ──────────────────────────────
async function submitAccount() {
  error.value = ''
  loading.value = true
  try {
    await auth.register(account.value)
    step.value = 2
  } catch (e) {
    error.value = e.response?.data?.message || e.message || 'Registration failed.'
  } finally {
    loading.value = false
  }
}

// ── Step 2: Create profile ────────────────────────────────
async function submitProfile() {
  error.value = ''
  loading.value = true
  try {
    const data = { ...profile.value }
    if (!data.passportNumber) delete data.passportNumber
    await pilgrimApi.createProfile(data)
    step.value = 3
  } catch (e) {
    error.value = e.response?.data?.message || 'Failed to save profile.'
  } finally {
    loading.value = false
  }
}

// ── Step 3: Add family members ────────────────────────────
function openMemberForm() {
  memberForm.value = emptyMember()
  showMemberForm.value = true
}

async function addMember() {
  if (!memberForm.value.fullName.trim()) return
  loading.value = true
  try {
    const data = { ...memberForm.value }
    if (!data.dateOfBirth) delete data.dateOfBirth
    await pilgrimApi.addFamilyMember(data)
    members.value.push({ ...memberForm.value })
    showMemberForm.value = false
    memberForm.value = emptyMember()
  } catch (e) {
    error.value = e.response?.data?.message || 'Failed to add member.'
  } finally {
    loading.value = false }
}

function finish() {
  router.push('/pilgrim/portal')
}
</script>

<template>
  <div class="min-h-screen bg-dark-950 flex items-center justify-center px-4 py-10">
    <div class="w-full max-w-2xl">

      <!-- Logo -->
      <div class="text-center mb-6">
        <div class="flex justify-center mb-3"><MHLogo size="lg" /></div>
        <h1 class="text-3xl font-bold text-gray-900">Misbahuda</h1>
        <p class="text-gold-400 font-arabic text-lg mt-1">Arbaeen 2026</p>
      </div>

      <!-- Step indicator -->
      <div class="flex items-center justify-center gap-2 mb-6">
        <template v-for="n in 3" :key="n">
          <div :class="['w-8 h-8 rounded-full flex items-center justify-center text-sm font-bold transition-all',
            step > n ? 'bg-green-600 text-white' :
            step === n ? 'bg-gold-600 text-white' :
            'bg-dark-700 text-slate-500']">
            {{ step > n ? '✓' : n }}
          </div>
          <div v-if="n < 3" :class="['w-12 h-0.5 transition-all', step > n ? 'bg-green-600' : 'bg-dark-700']"></div>
        </template>
      </div>
      <p class="text-center text-gray-700 text-sm mb-6">
        Step {{ step }} of 3 — <span class="text-gold-400">{{ stepLabel }}</span>
      </p>

      <!-- Error -->
      <div v-if="error" class="bg-red-900/30 border border-red-700 text-red-300 text-sm rounded-lg px-4 py-3 mb-4 flex justify-between">
        {{ error }}<button @click="error=''" class="text-red-500 ml-2">✕</button>
      </div>

      <!-- ── STEP 1: Account ─────────────────────────────── -->
      <div v-if="step === 1" class="card border-primary-900">
        <h2 class="text-xl font-semibold text-gray-900 mb-1">Create Account</h2>
        <p class="text-gray-700 text-sm mb-5">Basic account information</p>

        <form @submit.prevent="submitAccount" class="space-y-4">
          <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div class="md:col-span-2">
              <label class="label">Full Name *</label>
              <input v-model="account.fullName" type="text" class="input" placeholder="As on passport" required />
            </div>
            <div>
              <label class="label">Email *</label>
              <input v-model="account.email" type="email" class="input" placeholder="you@example.com" required />
            </div>
            <div>
              <label class="label">Phone Number *</label>
              <input v-model="account.phoneNumber" type="tel" class="input" placeholder="+923001234567" required />
            </div>
            <div>
              <label class="label">WhatsApp <span class="text-gray-600 text-xs">(optional)</span></label>
              <input v-model="account.whatsAppNumber" type="tel" class="input" placeholder="+923001234567" />
            </div>
            <div>
              <label class="label">Password *</label>
              <input v-model="account.password" type="password" class="input" placeholder="Min 8 chars, uppercase, number, special" required />
            </div>
          </div>
          <button type="submit" class="btn-primary w-full mt-2" :disabled="loading">
            {{ loading ? 'Creating account...' : 'Continue →' }}
          </button>
        </form>

        <p class="text-center text-gray-700 text-sm mt-5">
          Already registered? <router-link to="/login" class="text-amber-700 hover:text-amber-600">Sign in</router-link>
        </p>
      </div>

      <!-- ── STEP 2: Pilgrim Profile ────────────────────── -->
      <div v-else-if="step === 2" class="card border-gold-900">
        <h2 class="text-xl font-semibold text-gray-900 mb-1">Travel Details</h2>
        <p class="text-gray-700 text-sm mb-5">Fill in your travel information</p>

        <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
          <div>
            <label class="label">Passport Number</label>
            <input v-model="profile.passportNumber" type="text" class="input" placeholder="AB1234567" />
          </div>
          <div>
            <label class="label">Visa Number</label>
            <input v-model="profile.visaNumber" type="text" class="input" placeholder="Optional" />
          </div>
          <div>
            <label class="label">Country *</label>
            <input v-model="profile.country" type="text" class="input" placeholder="Pakistan" required />
          </div>
          <div>
            <label class="label">Total Family Members (including yourself)</label>
            <input v-model.number="profile.familyMemberCount" type="number" min="1" max="30" class="input" />
          </div>
          <div>
            <label class="label">Arrival Date *</label>
            <input v-model="profile.arrivalDate" type="date" class="input" required />
          </div>
          <div>
            <label class="label">Departure Date *</label>
            <input v-model="profile.departureDate" type="date" class="input" required />
          </div>
          <div>
            <label class="label">Arrival Flight</label>
            <input v-model="profile.arrivalFlight" type="text" class="input" placeholder="PK-301" />
          </div>
          <div>
            <label class="label">Departure Flight</label>
            <input v-model="profile.departureFlight" type="text" class="input" placeholder="PK-302" />
          </div>
          <div>
            <label class="label">Emergency Contact Name</label>
            <input v-model="profile.emergencyContactName" type="text" class="input" />
          </div>
          <div>
            <label class="label">Emergency Contact Phone</label>
            <input v-model="profile.emergencyContactPhone" type="text" class="input" placeholder="+92..." />
          </div>
        </div>

        <button @click="submitProfile"
          :disabled="loading || !profile.country || !profile.arrivalDate || !profile.departureDate"
          class="btn-primary w-full mt-6">
          {{ loading ? 'Saving...' : 'Continue →' }}
        </button>
      </div>

      <!-- ── STEP 3: Family Members ─────────────────────── -->
      <div v-else-if="step === 3" class="card border-green-900 space-y-5">
        <div>
          <h2 class="text-xl font-semibold text-gray-900 mb-1">Family Members</h2>
          <p class="text-gray-700 text-sm">Add family members traveling with you. <span class="text-slate-500">(You can skip this and add later from your portal)</span></p>
        </div>

        <!-- Added members list -->
        <div v-if="members.length" class="space-y-2">
          <div v-for="(m, i) in members" :key="i"
               class="flex items-center gap-3 bg-dark-800 rounded-lg px-4 py-2.5 text-sm">
            <span class="text-white font-medium">{{ m.fullName }}</span>
            <span :class="m.gender === 1 ? 'text-blue-400' : 'text-pink-400'" class="text-xs">
              {{ m.gender === 1 ? '♂' : '♀' }}
            </span>
            <span class="text-gray-600 text-xs">{{ relationshipOptions.find(r => r.value === m.relationship)?.label }}</span>
            <span v-if="m.isMinor" class="text-xs bg-gold-900 text-gold-400 px-1.5 py-0.5 rounded ml-auto">Minor</span>
          </div>
        </div>
        <div v-else class="bg-dark-800 rounded-lg p-4 text-center text-slate-500 text-sm">
          No family members added yet
        </div>

        <!-- Add member form -->
        <div v-if="showMemberForm" class="bg-dark-800 rounded-xl p-4 border border-dark-600">
          <h3 class="text-white font-medium mb-3 text-sm">New Family Member</h3>
          <div class="grid grid-cols-1 md:grid-cols-2 gap-3">
            <div class="md:col-span-2">
              <label class="label">Full Name *</label>
              <input v-model="memberForm.fullName" type="text" class="input" placeholder="Name as on passport" />
            </div>
            <div>
              <label class="label">Gender *</label>
              <select v-model.number="memberForm.gender" class="input">
                <option v-for="g in genderOptions" :key="g.value" :value="g.value">{{ g.label }}</option>
              </select>
            </div>
            <div>
              <label class="label">Relationship *</label>
              <select v-model.number="memberForm.relationship" class="input">
                <option v-for="r in relationshipOptions" :key="r.value" :value="r.value">{{ r.label }}</option>
              </select>
            </div>
            <div>
              <label class="label">Date of Birth</label>
              <input v-model="memberForm.dateOfBirth" type="date" class="input" />
            </div>
            <div>
              <label class="label">Passport Number</label>
              <input v-model="memberForm.passportNumber" type="text" class="input" />
            </div>
            <div>
              <label class="label">Nationality</label>
              <input v-model="memberForm.nationality" type="text" class="input" placeholder="Pakistani" />
            </div>
            <div class="flex items-center gap-6 pt-1">
              <label class="flex items-center gap-2 cursor-pointer">
                <input v-model="memberForm.requiresWheelchair" type="checkbox" class="w-4 h-4" />
                <span class="text-gray-600 text-sm">Needs Wheelchair ♿</span>
              </label>
              <label class="flex items-center gap-2 cursor-pointer">
                <input v-model="memberForm.isMinor" type="checkbox" class="w-4 h-4" />
                <span class="text-gray-600 text-sm">Minor (under 18)</span>
              </label>
            </div>
            <div>
              <label class="label">Special Notes</label>
              <input v-model="memberForm.specialNotes" type="text" class="input" placeholder="Medical needs, etc." />
            </div>
          </div>
          <div class="flex gap-2 mt-4">
            <button @click="addMember" :disabled="loading || !memberForm.fullName" class="btn-primary text-sm">
              {{ loading ? 'Saving...' : '+ Add Member' }}
            </button>
            <button @click="showMemberForm = false" class="px-4 py-2 text-sm text-gray-700 hover:text-white">Cancel</button>
          </div>
        </div>

        <div v-else class="flex gap-3">
          <button @click="openMemberForm" class="flex-1 py-2.5 rounded-xl border border-dark-600 text-slate-300 hover:text-white hover:border-gold-700 text-sm transition-colors">
            + Add Family Member
          </button>
        </div>

        <div class="border-t border-dark-700 pt-4">
          <button @click="finish" class="btn-primary w-full">
            {{ members.length ? `Go to Portal (${members.length} member${members.length > 1 ? 's' : ''} added)` : 'Skip & Go to Portal →' }}
          </button>
        </div>
      </div>

    </div>
  </div>
</template>
