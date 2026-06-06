<script setup>
import { ref, computed } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'

const auth = useAuthStore()
const router = useRouter()
const isAdmin = computed(() => ['SuperAdmin','Admin'].includes(auth.user?.role))
const isPilgrim = computed(() => auth.user?.role === 'Pilgrim')
const isVolunteer = computed(() => ['Volunteer','VolunteerManager'].includes(auth.user?.role))

const activeSection = ref('overview')

const pilgrimActions = [
  ['Approve pilgrim', 'Click pilgrim row → Approve — pilgrim receives instant notification + email'],
  ['Reject pilgrim', 'Click pilgrim row → Reject → enter rejection reason'],
  ['Mark Under Review', 'Click pilgrim row → Under Review'],
  ['Allocate Room', 'Click pilgrim → Allocate Room → select available room'],
  ['Allocate Bus', 'Click pilgrim → Allocate Bus → select bus'],
  ['Search pilgrims', 'Use search box — filter by name, status, or country'],
]

const sections = [
  { id: 'overview',       label: '📖 Overview'             },
  { id: 'pilgrim',        label: '🕌 Pilgrim Guide'        },
  { id: 'volunteer',      label: '🤝 Volunteer Guide'      },
  { id: 'admin',          label: '⚙️ Admin Guide'          },
  { id: 'notifications',  label: '🔔 Notifications'        },
  { id: 'benefits',       label: '✅ Key Benefits'         },
]
</script>

<template>
  <div class="max-w-4xl mx-auto space-y-6 pb-16">

    <!-- Back button -->
    <button @click="router.back()"
      class="flex items-center gap-2 text-sm font-medium transition-colors"
      style="color:#9a6f00;">
      <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 19l-7-7 7-7"/>
      </svg>
      Back
    </button>

    <!-- Header -->
    <div class="rounded-2xl overflow-hidden"
         style="background:linear-gradient(135deg,rgba(212,168,0,0.15),rgba(124,58,237,0.15));border:1px solid rgba(212,168,0,0.25);">
      <div class="px-6 py-8 text-center">
        <div class="text-5xl mb-3">📿</div>
        <h1 class="text-3xl font-bold text-gray-900">Misbah ul Hoda</h1>
        <p class="text-amber-700 font-semibold mt-1">Arbaeen 2026 — User Guide</p>
        <p class="text-gray-700 text-sm mt-2">Everything you need to know about using this system</p>
      </div>
    </div>

    <!-- Tab nav -->
    <div class="flex gap-2 flex-wrap">
      <button v-for="s in sections" :key="s.id" @click="activeSection = s.id"
        :class="['px-4 py-2 rounded-xl text-sm font-medium transition-all',
          activeSection === s.id
            ? 'text-white shadow-lg' : 'text-gray-700 hover:text-gray-900 bg-gray-100']"
        :style="activeSection === s.id ? 'background:linear-gradient(135deg,#b88a00,#D4A800)' : ''">
        {{ s.label }}
      </button>
    </div>

    <!-- ── OVERVIEW ─────────────────────────────────────────────────────── -->
    <div v-if="activeSection === 'overview'" class="space-y-5">
      <div class="card">
        <h2 class="text-gray-900 font-bold text-lg mb-3">What is Misbah ul Hoda?</h2>
        <p class="text-gray-700 leading-relaxed">
          Misbah ul Hoda is a complete digital management platform for Arbaeen 2026 pilgrims traveling to Karbala.
          Everything that was previously done on paper, WhatsApp groups, or phone calls is now managed in one place.
        </p>
        <div class="grid grid-cols-2 sm:grid-cols-3 gap-3 mt-5">
          <div v-for="f in [
            {icon:'🕌',text:'Pilgrim Registration'},
            {icon:'🏨',text:'Hotel & Room Allocation'},
            {icon:'✈️',text:'Airport Pickup'},
            {icon:'📿',text:'Majalis Schedule'},
            {icon:'📍',text:'Live Karwan Tracking'},
            {icon:'🔔',text:'Automatic Reminders'},
            {icon:'🗓️',text:'Ziyarat Tours'},
            {icon:'🤝',text:'Volunteer Coordination'},
            {icon:'💰',text:'Finance Tracking'},
          ]" :key="f.text"
            class="flex items-center gap-3 rounded-xl p-3"
            style="background:rgba(212,168,0,0.06);border:1px solid rgba(212,168,0,0.15);">
            <span class="text-2xl">{{ f.icon }}</span>
            <span class="text-gray-600 text-sm font-medium">{{ f.text }}</span>
          </div>
        </div>
      </div>

      <div class="card">
        <h2 class="text-gray-900 font-bold text-lg mb-3">User Roles</h2>
        <div class="space-y-3">
          <div v-for="r in [
            {role:'Pilgrim',icon:'🕌',color:'text-amber-700',bg:'rgba(212,168,0,0.1)',border:'rgba(212,168,0,0.25)',desc:'Registers, submits travel details, tracks status, views hotel & room, sees Majalis schedule'},
            {role:'Volunteer',icon:'🤝',color:'text-blue-400',bg:'rgba(59,130,246,0.1)',border:'rgba(59,130,246,0.25)',desc:'Views assigned tasks, checks in/out, coordinates pilgrim support on ground'},
            {role:'Admin',icon:'⚙️',color:'text-amber-400',bg:'rgba(217,119,6,0.1)',border:'rgba(217,119,6,0.25)',desc:'Manages everything — approvals, rooms, buses, Majalis, itinerary, announcements'},
            {role:'Super Admin',icon:'👑',color:'text-purple-400',bg:'rgba(124,58,237,0.1)',border:'rgba(124,58,237,0.25)',desc:'Full access including user creation, finance, and all system settings'},
          ]" :key="r.role"
            class="flex items-start gap-4 rounded-xl p-4"
            :style="`background:${r.bg};border:1px solid ${r.border}`">
            <span class="text-2xl mt-0.5">{{ r.icon }}</span>
            <div>
              <p :class="[r.color, 'font-bold']">{{ r.role }}</p>
              <p class="text-gray-700 text-sm mt-0.5">{{ r.desc }}</p>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- ── PILGRIM GUIDE ────────────────────────────────────────────────── -->
    <div v-if="activeSection === 'pilgrim'" class="space-y-5">

      <div class="card">
        <h2 class="text-gray-900 font-bold text-lg mb-4">Step 1 — Register & Log In</h2>
        <ol class="space-y-2 text-gray-600 text-sm list-decimal list-inside leading-relaxed">
          <li>Go to the website and click <strong class="text-gray-900">"Pilgrim Portal"</strong></li>
          <li>Click <strong class="text-gray-900">"Register"</strong> — enter your full name, email, and password</li>
          <li>Log in with your new account</li>
          <li>You will land on your personal Pilgrim Dashboard</li>
        </ol>
      </div>

      <div class="card">
        <h2 class="text-gray-900 font-bold text-lg mb-4">Step 2 — Complete Your Profile</h2>
        <div class="overflow-x-auto">
          <table class="w-full text-sm">
            <thead><tr class="border-b border-gray-200">
              <th class="text-left py-2 text-gray-700 font-medium">Field</th>
              <th class="text-left py-2 text-gray-700 font-medium">What to Enter</th>
            </tr></thead>
            <tbody class="divide-y divide-gray-200">
              <tr v-for="f in [
                ['Full Name','Your complete name as on passport'],
                ['Phone Number','With country code, e.g. +923001234567'],
                ['WhatsApp Number','If different from phone number'],
                ['Country','Your home country'],
                ['Passport Number','As shown on your passport'],
                ['Visa Number','Your Iraq visa number'],
                ['Family Members','Number of people traveling with you'],
              ]" :key="f[0]">
                <td class="py-2.5 text-gray-900 font-medium pr-4">{{ f[0] }}</td>
                <td class="py-2.5 text-gray-700">{{ f[1] }}</td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>

      <div class="card border-l-4 border-l-amber-500">
        <h2 class="text-gray-900 font-bold text-lg mb-3">Step 3 — Set Your Travel Dates</h2>
        <p class="text-gray-600 text-sm mb-3">Under <strong class="text-gray-900">"Travel Details"</strong> enter your arrival and departure information:</p>
        <ul class="space-y-1 text-gray-600 text-sm list-disc list-inside">
          <li>Arrival date & time — when you land in Iraq</li>
          <li>Arrival flight number (e.g. PK-745)</li>
          <li>Arrival airport (e.g. Baghdad International)</li>
          <li>Departure date, time, and flight number</li>
        </ul>
        <div class="mt-4 rounded-xl p-3 text-sm" style="background:rgba(217,119,6,0.1);border:1px solid rgba(217,119,6,0.3);">
          <p class="text-amber-300 font-semibold">⚠️ Important</p>
          <p class="text-amber-200/80 mt-1">Enter accurate travel dates. The admin team uses this information to arrange your airport pickup and prepare the reception team before you arrive.</p>
        </div>
      </div>

      <div class="card">
        <h2 class="text-gray-900 font-bold text-lg mb-3">Step 4 — Track Your Application Status</h2>
        <div class="space-y-3">
          <div v-for="s in [
            {label:'Pending',color:'bg-amber-900/40 border-amber-700 text-amber-300',desc:'Your application is submitted and waiting for admin review'},
            {label:'Under Review',color:'bg-blue-900/40 border-blue-700 text-blue-300',desc:'Admin is reviewing your profile and documents'},
            {label:'Approved',color:'bg-emerald-900/40 border-emerald-700 text-amber-600',desc:'You are confirmed for Arbaeen 2026 — congratulations!'},
            {label:'Rejected',color:'bg-red-900/40 border-red-700 text-red-300',desc:'Application was not accepted. The reason will be shown in your portal'},
          ]" :key="s.label" class="flex items-start gap-4">
            <span :class="['text-xs px-3 py-1 rounded-full border font-semibold shrink-0 mt-0.5', s.color]">{{ s.label }}</span>
            <p class="text-gray-700 text-sm">{{ s.desc }}</p>
          </div>
        </div>
        <div class="mt-4 rounded-xl p-3 text-sm" style="background:rgba(212,168,0,0.08);border:1px solid rgba(212,168,0,0.2);">
          <p class="text-amber-600 text-sm">✅ You will receive an email and in-app notification whenever your status changes.</p>
        </div>
      </div>

      <div class="card">
        <h2 class="text-gray-900 font-bold text-lg mb-3">Step 5 — Your Hotel & Room</h2>
        <p class="text-gray-600 text-sm mb-3">Once approved and a room is assigned by admin, your portal shows:</p>
        <ul class="space-y-2 text-gray-600 text-sm">
          <li class="flex gap-2"><span>🏨</span> Hotel name and room number</li>
          <li class="flex gap-2"><span>🕌</span> Distance from Haram Imam Hussain (AS)</li>
          <li class="flex gap-2"><span>🧭</span> <strong class="text-white">"Navigate to Hotel"</strong> — opens Google Maps for taxi directions from airport</li>
          <li class="flex gap-2"><span>🕌</span> <strong class="text-white">"Navigate to Haram"</strong> — walking directions to Haram Imam Hussain (AS)</li>
        </ul>
      </div>

      <div class="card">
        <h2 class="text-gray-900 font-bold text-lg mb-3">Majalis & Schedule</h2>
        <div class="space-y-3">
          <div v-for="tab in [
            {icon:'📿',name:'Majalis Tab',desc:'Today\'s Majalis with scholar photo, full program timeline (Hadith → Marsiya → Salam → Noha) with performer names and photos, venue and time'},
            {icon:'🕌',name:'Namaz Timings Tab',desc:'Today\'s prayer times — Fajr, Zuhr, Asr, Maghrib, Isha — with location for each prayer'},
            {icon:'🍽️',name:'Food Schedule Tab',desc:'Meal times (Breakfast, Lunch, Dinner, Sehri, Iftaar) with location and estimated servings'},
          ]" :key="tab.name"
            class="flex gap-3 rounded-xl p-3"
            style="background:rgba(124,58,237,0.07);border:1px solid rgba(124,58,237,0.2);">
            <span class="text-2xl shrink-0">{{ tab.icon }}</span>
            <div>
              <p class="text-purple-300 font-semibold text-sm">{{ tab.name }}</p>
              <p class="text-gray-700 text-sm mt-0.5">{{ tab.desc }}</p>
            </div>
          </div>
        </div>
      </div>

      <div class="card">
        <h2 class="text-gray-900 font-bold text-lg mb-3">Live Karwan Tracking</h2>
        <ol class="space-y-1 text-gray-600 text-sm list-decimal list-inside">
          <li>Go to <strong class="text-white">"Live Tracking"</strong> from the menu</li>
          <li>Select your Karwan from the list</li>
          <li>A live GPS map shows the Karwan's current location</li>
          <li>Map updates in real-time as the Karwan moves</li>
        </ol>
      </div>

    </div>

    <!-- ── VOLUNTEER GUIDE ─────────────────────────────────────────────── -->
    <div v-if="activeSection === 'volunteer'" class="space-y-5">

      <div class="card">
        <h2 class="text-gray-900 font-bold text-lg mb-4">Volunteer Dashboard</h2>
        <p class="text-gray-600 text-sm mb-4">Log in via the <strong class="text-white">Volunteer Portal</strong> using credentials provided by your admin.</p>
        <div class="grid grid-cols-2 gap-3">
          <div v-for="item in [
            {icon:'🟢',label:'Current Status',desc:'Available / Busy / Offline'},
            {icon:'📋',label:'Your Tasks',desc:'Assigned tasks with priority and deadline'},
            {icon:'📍',label:'Your Zone',desc:'Area you are responsible for'},
            {icon:'👥',label:'Assigned Pilgrims',desc:'Pilgrims under your care'},
            {icon:'🕐',label:'Attendance',desc:'Your check-in/check-out history'},
            {icon:'📊',label:'Reports',desc:'Your performance report'},
          ]" :key="item.label"
            class="rounded-xl p-3"
            style="background:rgba(59,130,246,0.07);border:1px solid rgba(59,130,246,0.2);">
            <span class="text-xl">{{ item.icon }}</span>
            <p class="text-blue-300 font-semibold text-sm mt-1">{{ item.label }}</p>
            <p class="text-gray-600 text-xs mt-0.5">{{ item.desc }}</p>
          </div>
        </div>
      </div>

      <div class="card">
        <h2 class="text-gray-900 font-bold text-lg mb-3">Check In / Check Out</h2>
        <ul class="space-y-2 text-gray-600 text-sm">
          <li class="flex gap-2"><span>✅</span> Click <strong class="text-white">"Check In"</strong> when your shift starts — attendance is recorded automatically</li>
          <li class="flex gap-2"><span>🔴</span> Click <strong class="text-white">"Check Out"</strong> when your shift ends</li>
          <li class="flex gap-2"><span>📊</span> Your full attendance history is available in the report section</li>
        </ul>
      </div>

      <div class="card">
        <h2 class="text-gray-900 font-bold text-lg mb-3">Tasks</h2>
        <ul class="space-y-2 text-gray-600 text-sm">
          <li class="flex gap-2"><span>👁️</span> Each task shows description, priority level, and deadline</li>
          <li class="flex gap-2"><span>✅</span> Click <strong class="text-white">"Mark Complete"</strong> when a task is done</li>
          <li class="flex gap-2"><span>⚡</span> Emergency tasks are highlighted in red — act immediately</li>
        </ul>
      </div>

    </div>

    <!-- ── ADMIN GUIDE ─────────────────────────────────────────────────── -->
    <div v-if="activeSection === 'admin'" class="space-y-5">

      <div class="card">
        <h2 class="text-gray-900 font-bold text-lg mb-4">Dashboard Overview</h2>
        <p class="text-gray-600 text-sm mb-3">The admin dashboard gives a real-time snapshot of the entire operation:</p>
        <ul class="space-y-1.5 text-gray-600 text-sm">
          <li class="flex gap-2"><span>📊</span> Total pilgrims broken down by status (Pending / Approved / Rejected)</li>
          <li class="flex gap-2"><span>🤝</span> Volunteer availability (Available / Busy / Offline)</li>
          <li class="flex gap-2"><span>🏨</span> Accommodation occupancy rate with progress bar</li>
          <li class="flex gap-2"><span>✈️</span> Today's arrivals and departures with flight details</li>
          <li class="flex gap-2"><span>⚠️</span> Pilgrims with no driver assigned — flagged in red</li>
          <li class="flex gap-2"><span>📅</span> Next 3-day travel schedule at a glance</li>
        </ul>
      </div>

      <div class="card">
        <h2 class="text-gray-900 font-bold text-lg mb-4">Managing Pilgrims</h2>
        <div class="overflow-x-auto">
          <table class="w-full text-sm">
            <thead><tr class="border-b border-dark-700">
              <th class="text-left py-2 text-slate-400 font-medium">Action</th>
              <th class="text-left py-2 text-slate-400 font-medium">How</th>
            </tr></thead>
            <tbody class="divide-y divide-dark-700">
              <tr v-for="row in pilgrimActions" :key="row[0]">
                <td class="py-2.5 text-white font-medium pr-4">{{ row[0] }}</td>
                <td class="py-2.5 text-slate-400">{{ row[1] }}</td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>

      <div class="card">
        <h2 class="text-gray-900 font-bold text-lg mb-4">Majalis Management</h2>
        <ol class="space-y-3 text-gray-600 text-sm list-decimal list-inside leading-relaxed">
          <li>Go to <strong class="text-white">Admin → Majalis</strong></li>
          <li>Click <strong class="text-white">"+ Add Majalis"</strong> — enter title, language, venue, times, Molana name, scholar photo</li>
          <li>Click <strong class="text-white">"📋 Program"</strong> on any Majalis card to set the full program</li>
          <li>Add items in order: Opening → Hadith → Marsiya → Salam → Noha → Closing</li>
          <li>For each item, enter: time, performer name, and upload a photo</li>
          <li>Reorder items using the ▲▼ buttons, then click <strong class="text-white">"Save Program"</strong></li>
        </ol>
        <div class="mt-4 rounded-xl p-3 text-sm" style="background:rgba(124,58,237,0.1);border:1px solid rgba(124,58,237,0.3);">
          <p class="text-purple-300 font-semibold">🔔 Automatic Reminder</p>
          <p class="text-purple-200/80 mt-1">30 minutes before any Majalis starts, the system automatically sends a notification + email to all approved pilgrims — including the scholar's photo. No manual work needed.</p>
        </div>
      </div>

      <div class="card">
        <h2 class="text-gray-900 font-bold text-lg mb-4">Itinerary & Airport Transfers</h2>
        <ol class="space-y-2 text-gray-600 text-sm list-decimal list-inside">
          <li>Go to <strong class="text-white">Admin → Itinerary</strong></li>
          <li>View today's arrivals and departures with flight details</li>
          <li>Click any traveler → <strong class="text-white">"Assign Driver"</strong></li>
          <li>Enter: driver name, phone, vehicle type, meeting point, scheduled pickup time</li>
          <li>The pilgrim automatically receives driver details via notification + email</li>
        </ol>
        <div class="mt-4 rounded-xl p-3 text-sm" style="background:rgba(239,68,68,0.1);border:1px solid rgba(239,68,68,0.3);">
          <p class="text-red-300 font-semibold">⚠️ Nightly Auto-Alert</p>
          <p class="text-red-200/80 mt-1">Every evening at 20:00, the system automatically emails all admins listing every pilgrim arriving tomorrow who has no driver assigned. Never miss an unassigned pickup.</p>
        </div>
      </div>

      <div class="card">
        <h2 class="text-gray-900 font-bold text-lg mb-4">Accommodation</h2>
        <ol class="space-y-2 text-gray-600 text-sm list-decimal list-inside">
          <li>Go to <strong class="text-white">Admin → Accommodation</strong></li>
          <li>Click <strong class="text-white">"Add Hotel"</strong> — enter name, city, address, phone</li>
          <li>Enter GPS coordinates so pilgrims can navigate directly by taxi</li>
          <li>Enter Haram distance text (e.g. "800 meters from Haram") — pilgrims see this</li>
          <li>Click <strong class="text-white">"Add Rooms"</strong> on a hotel — set floor, room type, capacity</li>
          <li>Rooms become available for pilgrim allocation</li>
        </ol>
      </div>

      <div class="card">
        <h2 class="text-gray-900 font-bold text-lg mb-4">Other Admin Features</h2>
        <div class="space-y-3">
          <div v-for="f in [
            {icon:'🗓️',title:'Tours',desc:'Create Ziyarat tours with day-by-day schedule, packages, and pricing. Pilgrims apply and get notified when tour date approaches.'},
            {icon:'🔔',title:'Notifications',desc:'Send notifications to specific users, all pilgrims, all volunteers, or everyone. Emergency broadcasts go out immediately.'},
            {icon:'📋',title:'Notice Board',desc:'Post announcements visible to all pilgrims on their dashboard. Set expiry dates for time-sensitive notices.'},
            {icon:'💰',title:'Finance',desc:'Track all expenses by category, set budgets, manage staff salaries, record tickets. Export full report anytime.'},
            {icon:'🌍',title:'International Finance',desc:'Record incoming payments from pilgrims, manage vendor contracts (hotels, buses), track money transfers to Iraq.'},
            {icon:'📊',title:'Reports',desc:'Export full pilgrim list and volunteer list as CSV. View summary statistics for the entire operation.'},
          ]" :key="f.title"
            class="flex gap-3 rounded-xl p-3"
            style="background:rgba(217,119,6,0.06);border:1px solid rgba(217,119,6,0.15);">
            <span class="text-xl shrink-0">{{ f.icon }}</span>
            <div>
              <p class="text-amber-300 font-semibold text-sm">{{ f.title }}</p>
              <p class="text-gray-700 text-sm mt-0.5">{{ f.desc }}</p>
            </div>
          </div>
        </div>
      </div>

    </div>

    <!-- ── NOTIFICATIONS ───────────────────────────────────────────────── -->
    <div v-if="activeSection === 'notifications'" class="space-y-5">
      <div class="card">
        <h2 class="text-gray-900 font-bold text-lg mb-4">Automatic Notifications</h2>
        <p class="text-gray-700 text-sm mb-4">The system sends these automatically — no manual work needed from admin or pilgrim:</p>
        <div class="space-y-3">
          <div v-for="n in [
            {icon:'✅',color:'text-amber-700 bg-emerald-900/30 border-emerald-800',title:'Application Approved',who:'Pilgrim',desc:'Email + in-app notification sent instantly when admin approves your application'},
            {icon:'❌',color:'text-red-400 bg-red-900/30 border-red-800',title:'Application Rejected',who:'Pilgrim',desc:'Email + notification with rejection reason'},
            {icon:'🏨',color:'text-blue-400 bg-blue-900/30 border-blue-800',title:'Room Allocated',who:'Pilgrim',desc:'Notification when a room is assigned to you'},
            {icon:'📿',color:'text-purple-400 bg-purple-900/30 border-purple-800',title:'Majalis Reminder',who:'All Pilgrims',desc:'Email + notification 30 minutes before every Majalis — includes scholar photo, time, and venue'},
            {icon:'✈️',color:'text-amber-400 bg-amber-900/30 border-amber-800',title:'2-Day Travel Reminder',who:'Pilgrim',desc:'Reminder 2 days before your flight with flight details and packing checklist'},
            {icon:'🛫',color:'text-amber-400 bg-amber-900/30 border-amber-800',title:'5-Hour Flight Reminder',who:'Pilgrim',desc:'Final reminder 5 hours before your flight with driver details if assigned'},
            {icon:'🚗',color:'text-amber-700 bg-emerald-900/30 border-emerald-800',title:'Driver Details',who:'Pilgrim',desc:'Driver name, phone, vehicle, and meeting point sent automatically when admin assigns your pickup'},
            {icon:'⚠️',color:'text-red-400 bg-red-900/30 border-red-800',title:'Unassigned Driver Alert',who:'Admin',desc:'Every evening at 20:00 — email listing all tomorrow\'s travelers with no driver assigned'},
          ]" :key="n.title"
            :class="['flex items-start gap-3 rounded-xl p-3 border', n.color]">
            <span class="text-xl shrink-0 mt-0.5">{{ n.icon }}</span>
            <div class="flex-1">
              <div class="flex items-center gap-2 flex-wrap">
                <p class="font-semibold text-sm">{{ n.title }}</p>
                <span class="text-xs px-2 py-0.5 rounded-full bg-white/10">→ {{ n.who }}</span>
              </div>
              <p class="text-xs mt-1 opacity-80">{{ n.desc }}</p>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- ── BENEFITS ────────────────────────────────────────────────────── -->
    <div v-if="activeSection === 'benefits'" class="space-y-5">

      <div class="card">
        <h2 class="text-gray-900 font-bold text-lg mb-4">🕌 For Pilgrims</h2>
        <div class="space-y-3">
          <div v-for="b in [
            {icon:'📱',title:'Everything in one place',desc:'No more asking on WhatsApp for hotel name, room number, or Majalis time'},
            {icon:'🗺️',title:'Hotel navigation',desc:'One tap opens Google Maps — show your taxi driver exactly where to go'},
            {icon:'🕌',title:'Haram navigation',desc:'Walking directions from your hotel to Haram Imam Hussain (AS)'},
            {icon:'✈️',title:'Travel reminders',desc:'Automatic reminders 2 days and 5 hours before your flight — never miss it'},
            {icon:'🚗',title:'Driver details',desc:'Your pickup driver\'s name, phone, and vehicle sent directly to your email'},
            {icon:'📿',title:'Majalis alerts',desc:'30-minute advance notification before every Majalis with scholar photo'},
            {icon:'📍',title:'Live Karwan tracking',desc:'See exactly where your Karwan bus is at any moment on a live map'},
            {icon:'🏨',title:'Know your room',desc:'Hotel name, floor, room number — all visible in your portal before you arrive'},
          ]" :key="b.title"
            class="flex gap-3 items-start">
            <span class="text-xl shrink-0 mt-0.5">{{ b.icon }}</span>
            <div>
              <p class="text-amber-600 font-semibold text-sm">{{ b.title }}</p>
              <p class="text-gray-700 text-sm">{{ b.desc }}</p>
            </div>
          </div>
        </div>
      </div>

      <div class="card">
        <h2 class="text-gray-900 font-bold text-lg mb-4">⚙️ For Admin</h2>
        <div class="space-y-3">
          <div v-for="b in [
            {icon:'📊',title:'Real-time dashboard',desc:'See everything at a glance — arrivals, occupancy, volunteers, pending approvals'},
            {icon:'⚠️',title:'Automatic safety alerts',desc:'System notifies you every evening if any tomorrow\'s pilgrim has no driver assigned'},
            {icon:'📧',title:'Automatic emails',desc:'Approval, reminders, Majalis alerts — sent without any manual effort'},
            {icon:'🏨',title:'Full room management',desc:'Track every room — available, occupied, capacity — across all hotels'},
            {icon:'📋',title:'Professional Majalis program',desc:'Program with performer photos looks impressive to pilgrims'},
            {icon:'💰',title:'Complete finance tracking',desc:'All expenses, payments, staff salaries, and budgets in one place'},
            {icon:'📤',title:'Export reports',desc:'Download complete pilgrim lists for authorities or documentation in seconds'},
          ]" :key="b.title"
            class="flex gap-3 items-start">
            <span class="text-xl shrink-0 mt-0.5">{{ b.icon }}</span>
            <div>
              <p class="text-amber-300 font-semibold text-sm">{{ b.title }}</p>
              <p class="text-gray-700 text-sm">{{ b.desc }}</p>
            </div>
          </div>
        </div>
      </div>

      <!-- Closing -->
      <div class="rounded-2xl text-center py-8 px-6"
           style="background:linear-gradient(135deg,rgba(212,168,0,0.12),rgba(124,58,237,0.12));border:1px solid rgba(212,168,0,0.2);">
        <p class="text-2xl mb-3">🌿</p>
        <p class="text-gray-900 font-bold text-lg">May Allah (SWT) accept the ziyarat of all pilgrims</p>
        <p class="text-gray-700 text-sm mt-2">Misbah ul Hoda — Arbaeen 2026</p>
      </div>

    </div>

  </div>
</template>
