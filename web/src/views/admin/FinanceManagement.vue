<script setup>
import { ref, onMounted, computed } from 'vue'
import { financeApi } from '@/api'
import * as XLSX from 'xlsx'
import jsPDF from 'jspdf'
import autoTable from 'jspdf-autotable'
import AppPagination from '@/components/AppPagination.vue'

const summary    = ref(null)
const expenses   = ref([])
const expTotal   = ref(0)
const expPage    = ref(1)
const EXP_SIZE   = 25
const loading    = ref(true)
const msg       = ref({ type: '', text: '' })
const activeTab = ref('dashboard')

// ── Form ──────────────────────────────────────────────────────
const showForm    = ref(false)
const editingId   = ref(null)
const saving      = ref(false)
const deleting    = ref(null)
const form = ref({
  category: 1, unit: 1, title: '', unitCost: '', quantity: 1,
  currency: 'USD', date: new Date().toISOString().slice(0,10),
  paidTo: '', notes: '',
  breakfastIncluded: false, lunchIncluded: false, dinnerIncluded: false,
})

// ── Budget Form ───────────────────────────────────────────────
const showBudget  = ref(false)
const budgetForm  = ref({ category: 1, budgetedAmount: '', currency: 'USD', notes: '' })
const savingBudget = ref(false)

const categories = [
  { value: 1,  label: '🏨 Hotel',           icon: '🏨' },
  { value: 2,  label: '🍳 Breakfast',        icon: '🍳' },
  { value: 3,  label: '🍽 Lunch',            icon: '🍽' },
  { value: 4,  label: '🌙 Dinner',           icon: '🌙' },
  { value: 5,  label: '🚌 Bus Rent',         icon: '🚌' },
  { value: 6,  label: '⛽ Fuel',             icon: '⛽' },
  { value: 7,  label: '🏠 Accommodation',    icon: '🏠' },
  { value: 8,  label: '💊 Medical',          icon: '💊' },
  { value: 9,  label: '📋 Visa / Admin',     icon: '📋' },
  { value: 10, label: '📦 Miscellaneous',    icon: '📦' },
]

const units = [
  { value: 1, label: 'Lump Sum' },
  { value: 2, label: 'Per Room' },
  { value: 3, label: 'Per Person' },
  { value: 4, label: 'Per Day' },
  { value: 5, label: 'Per Bus' },
  { value: 6, label: 'Per Night' },
]

const currencies = [
  { code: 'USD', name: 'US Dollar' },
  { code: 'GBP', name: 'British Pound' },
  { code: 'IQD', name: 'Iraqi Dinar' },
  { code: 'SAR', name: 'Saudi Riyal' },
  { code: 'EUR', name: 'Euro' },
  { code: 'PKR', name: 'Pakistani Rupee' },
]

// ── Staff Payments ────────────────────────────────────────
const staffData     = ref(null)  // { payments, totalPkr, paidPkr, pendingPkr }
const showStaffForm = ref(false)
const editingStaffId = ref(null)
const savingStaff   = ref(false)
const deletingStaff = ref(null)
const staffForm     = ref({
  name: '', role: 1, phone: '', description: '', amount: '',
  currency: 'USD', date: new Date().toISOString().slice(0,10),
  isPaid: false, notes: ''
})

// staffRoles defined below as staffRolesAll (shared with tickets)

async function loadStaff() {
  try {
    const res = await financeApi.getStaff()
    staffData.value = res.data.data
  } catch { /* silent */ }
}

function openAddStaff() {
  editingStaffId.value = null
  staffForm.value = {
    name: '', role: 1, phone: '', description: '', amount: '',
    currency: 'USD', date: new Date().toISOString().slice(0,10),
    isPaid: false, notes: ''
  }
  showStaffForm.value = true
}

function openEditStaff(p) {
  editingStaffId.value = p.id
  staffForm.value = {
    name: p.name, role: staffRolesAll.find(r => r.label.includes(p.roleLabel?.split('/')[0].trim()))?.value || 1,
    phone: p.phone || '', description: p.description || '',
    amount: p.amount, currency: p.currency,
    date: p.date?.slice(0,10) || new Date().toISOString().slice(0,10),
    isPaid: p.isPaid, notes: p.notes || ''
  }
  showStaffForm.value = true
}

async function saveStaff() {
  if (!staffForm.value.name || !staffForm.value.amount) return notify('error', 'Name and amount required.')
  savingStaff.value = true
  try {
    const payload = { ...staffForm.value, amount: parseFloat(staffForm.value.amount), date: new Date(staffForm.value.date).toISOString() }
    if (editingStaffId.value) {
      await financeApi.updateStaff(editingStaffId.value, payload)
      notify('success', 'Updated.')
    } else {
      await financeApi.addStaff(payload)
      notify('success', 'Staff payment added.')
    }
    showStaffForm.value = false
    await loadStaff()
  } catch { notify('error', 'Failed to save.')
  } finally { savingStaff.value = false }
}

async function markStaffPaid(id) {
  try {
    await financeApi.markPaid(id)
    notify('success', 'Marked as paid.')
    await loadStaff()
  } catch { notify('error', 'Failed.') }
}

async function deleteStaff(id) {
  if (!confirm('Delete this payment record?')) return
  deletingStaff.value = id
  try {
    await financeApi.deleteStaff(id)
    notify('success', 'Deleted.')
    await loadStaff()
  } catch { notify('error', 'Delete failed.')
  } finally { deletingStaff.value = null }
}

// ── Travel Tickets ────────────────────────────────────────
const ticketData      = ref(null)
const showTicketForm  = ref(false)
const editingTicketId = ref(null)
const savingTicket    = ref(false)
const deletingTicket  = ref(null)
const ticketForm      = ref({
  passengerName: '', passengerRole: 1, phone: '',
  ticketType: 1, fromCity: '', toCity: '',
  travelDate: new Date().toISOString().slice(0,10),
  airline: '', ticketNumber: '', flightNumber: '',
  cost: '', currency: 'USD', status: 1, notes: ''
})

const ticketTypes   = [{ value:1,label:'✈️ Flight' },{ value:2,label:'🚌 Bus' },{ value:3,label:'🚆 Train' },{ value:4,label:'🚗 Other' }]
const ticketStatuses = [{ value:1,label:'⏳ Pending' },{ value:2,label:'✅ Booked' },{ value:3,label:'🟢 Used' },{ value:4,label:'❌ Cancelled' }]

const staffRolesAll = [
  { value:1,label:'🕌 Molana / Scholar' },
  { value:2,label:'👷 Part-Time Employee' },
  { value:3,label:'🧹 Servant / Staff' },
  { value:4,label:'🚌 Driver' },
  { value:5,label:'🗺 Tour Guide' },
  { value:6,label:'👨‍🍳 Cook' },
  { value:7,label:'🛡 Security' },
  { value:8,label:'🛬 Airport Receiver' },
  { value:9,label:'🗣 Translator' },
  { value:10,label:'🏥 Medical Staff' },
  { value:11,label:'📋 Volunteer Coordinator' },
  { value:12,label:'🧽 Cleaning Staff' },
  { value:13,label:'🎪 Event Manager' },
  { value:14,label:'👤 Other' },
]

async function loadTickets() {
  try {
    const res = await financeApi.getTickets()
    ticketData.value = res.data.data
  } catch { /* silent */ }
}

function openAddTicket() {
  editingTicketId.value = null
  ticketForm.value = {
    passengerName: '', passengerRole: 1, phone: '',
    ticketType: 1, fromCity: '', toCity: '',
    travelDate: new Date().toISOString().slice(0,10),
    airline: '', ticketNumber: '', flightNumber: '',
    cost: '', currency: 'USD', status: 1, notes: ''
  }
  showTicketForm.value = true
}

function openEditTicket(t) {
  editingTicketId.value = t.id
  const roleVal = staffRolesAll.find(r => r.label.toLowerCase().includes(t.roleLabel?.toLowerCase().split('/')[0].trim().toLowerCase()))?.value || 1
  const typeVal = ticketTypes.find(x => x.label.toLowerCase().includes(t.ticketType?.toLowerCase()))?.value || 1
  const statusVal = ticketStatuses.find(x => x.label.toLowerCase().includes(t.status?.toLowerCase()))?.value || 1
  ticketForm.value = {
    passengerName: t.passengerName, passengerRole: roleVal, phone: t.phone || '',
    ticketType: typeVal, fromCity: t.fromCity, toCity: t.toCity,
    travelDate: t.travelDate?.slice(0,10) || new Date().toISOString().slice(0,10),
    airline: t.airline || '', ticketNumber: t.ticketNumber || '', flightNumber: t.flightNumber || '',
    cost: t.cost, currency: t.currency, status: statusVal, notes: t.notes || ''
  }
  showTicketForm.value = true
}

async function saveTicket() {
  if (!ticketForm.value.passengerName || !ticketForm.value.cost || !ticketForm.value.fromCity || !ticketForm.value.toCity)
    return notify('error', 'Name, route and cost required.')
  savingTicket.value = true
  try {
    const payload = { ...ticketForm.value, cost: parseFloat(ticketForm.value.cost), travelDate: new Date(ticketForm.value.travelDate).toISOString() }
    if (editingTicketId.value) {
      await financeApi.updateTicket(editingTicketId.value, payload)
      notify('success', 'Ticket updated.')
    } else {
      await financeApi.addTicket(payload)
      notify('success', 'Ticket added.')
    }
    showTicketForm.value = false
    await loadTickets()
  } catch { notify('error', 'Failed to save.')
  } finally { savingTicket.value = false }
}

async function deleteTicket(id) {
  if (!confirm('Delete this ticket?')) return
  deletingTicket.value = id
  try {
    await financeApi.deleteTicket(id)
    notify('success', 'Deleted.')
    await loadTickets()
  } catch { notify('error', 'Delete failed.')
  } finally { deletingTicket.value = null }
}

// ── Exchange Rates ────────────────────────────────────────
const rates       = ref([])
const rateEdits   = ref({})
const savingRate  = ref(null)

async function loadRates() {
  try {
    const res = await financeApi.getRates()
    rates.value = res.data.data || []
    rateEdits.value = {}
    rates.value.forEach(r => { rateEdits.value[r.currency] = r.rateToPkr || '' })
  } catch { /* silent */ }
}

async function saveRate(currency) {
  const val = parseFloat(rateEdits.value[currency])
  if (!val || val <= 0) return notify('error', 'Enter a valid rate.')
  savingRate.value = currency
  try {
    await financeApi.setRate({ currency, rateToPkr: val })
    notify('success', `Rate for ${currency} saved.`)
    await loadRates()
    await load()
  } catch { notify('error', 'Failed to save rate.')
  } finally { savingRate.value = null }
}

function catIcon(cat) { return categories.find(c => c.value === cat)?.icon || '💰' }
function catLabel(cat) { return categories.find(c => c.label.includes(cat) || c.value === cat)?.label?.replace(/^.\s/, '') || cat }

function notify(type, text) {
  msg.value = { type, text }
  setTimeout(() => msg.value = { type: '', text: '' }, 4000)
}

function fmt(n) { return Number(n || 0).toLocaleString('en-PK', { minimumFractionDigits: 0, maximumFractionDigits: 2 }) }

const totalCalc = computed(() => {
  const cost = parseFloat(form.value.unitCost) || 0
  const qty  = parseInt(form.value.quantity) || 1
  return cost * qty
})

// ── Load ──────────────────────────────────────────────────────
async function load() {
  loading.value = true
  try {
    const [sumRes, expRes] = await Promise.all([financeApi.getSummary(), financeApi.getExpenses({ page: expPage.value, pageSize: EXP_SIZE })])
    summary.value  = sumRes.data.data
    const expData = expRes.data.data
    expenses.value = expData?.items || expData || []
    expTotal.value = expData?.totalCount ?? expenses.value.length
  } catch { notify('error', 'Failed to load financial data.') }
  finally { loading.value = false }
}

// ── Expense CRUD ──────────────────────────────────────────────
function openAdd() {
  editingId.value = null
  form.value = { category: 1, unit: 1, title: '', unitCost: '', quantity: 1,
    currency: 'USD', date: new Date().toISOString().slice(0,10), paidTo: '', notes: '' }
  showForm.value = true
}

function openEdit(e) {
  editingId.value = e.id
  form.value = {
    category: categories.find(c => c.label.includes(e.category))?.value || 1,
    unit: units.find(u => u.label === e.unit)?.value || 1,
    title: e.title, unitCost: e.unitCost, quantity: e.quantity,
    currency: e.currency, date: e.date?.slice(0,10) || new Date().toISOString().slice(0,10),
    paidTo: e.paidTo || '', notes: e.notes || '',
    breakfastIncluded: e.breakfastIncluded || false,
    lunchIncluded: e.lunchIncluded || false,
    dinnerIncluded: e.dinnerIncluded || false,
  }
  showForm.value = true
}

async function saveExpense() {
  if (!form.value.title || !form.value.unitCost) return notify('error', 'Title and cost are required.')
  saving.value = true
  try {
    const payload = { ...form.value, unitCost: parseFloat(form.value.unitCost), quantity: parseInt(form.value.quantity) || 1, date: new Date(form.value.date).toISOString() }
    if (editingId.value) {
      await financeApi.updateExpense(editingId.value, payload)
      notify('success', 'Expense updated.')
    } else {
      await financeApi.addExpense(payload)
      notify('success', 'Expense added.')
    }
    showForm.value = false
    await load()
  } catch (e) { notify('error', e.response?.data?.message || 'Failed to save.')
  } finally { saving.value = false }
}

async function deleteExpense(id) {
  if (!confirm('Delete this expense?')) return
  deleting.value = id
  try {
    await financeApi.deleteExpense(id)
    expenses.value = expenses.value.filter(e => e.id !== id)
    notify('success', 'Expense deleted.')
    await load()
  } catch { notify('error', 'Delete failed.')
  } finally { deleting.value = null }
}

// ── Budget ────────────────────────────────────────────────────
function openBudget(cat) {
  budgetForm.value = { category: cat?.value || 1, budgetedAmount: '', currency: 'USD', notes: '' }
  showBudget.value = true
}

async function saveBudget() {
  if (!budgetForm.value.budgetedAmount) return notify('error', 'Enter budget amount.')
  savingBudget.value = true
  try {
    await financeApi.setBudget({ ...budgetForm.value, budgetedAmount: parseFloat(budgetForm.value.budgetedAmount) })
    notify('success', 'Budget saved.')
    showBudget.value = false
    await load()
  } catch { notify('error', 'Failed to save budget.')
  } finally { savingBudget.value = false }
}

// ── Export Excel ──────────────────────────────────────────────
async function exportExcel() {
  try {
    const res = await financeApi.export()
    const rows = res.data.data
    const ws = XLSX.utils.json_to_sheet(rows)
    ws['!cols'] = Object.keys(rows[0] || {}).map(k => ({ wch: Math.max(k.length, 12) }))
    const wb = XLSX.utils.book_new()
    XLSX.utils.book_append_sheet(wb, ws, 'Finance')
    XLSX.writeFile(wb, `Misbahuda_Finance_${new Date().toISOString().slice(0,10)}.xlsx`)
  } catch { notify('error', 'Export failed.') }
}

// ── Export PDF ────────────────────────────────────────────────
async function exportPdf() {
  try {
    const res = await financeApi.export()
    const rows = res.data.data
    const today = new Date().toLocaleDateString('en-GB', { day:'2-digit', month:'short', year:'numeric' })
    const doc = new jsPDF({ orientation: 'landscape', unit: 'mm', format: 'a4' })

    doc.setFillColor(15, 23, 42); doc.rect(0, 0, 297, 22, 'F')
    doc.setTextColor(217, 119, 6); doc.setFontSize(14); doc.setFont('helvetica', 'bold')
    doc.text('Misbah ul Hoda — Arbaeen 2026', 14, 10)
    doc.setFontSize(9); doc.setTextColor(148, 163, 184)
    doc.text(`Financial Report  |  Generated: ${today}`, 14, 17)
    if (summary.value) {
      doc.setTextColor(217, 119, 6)
      doc.text(`Budget: ${fmt(summary.value.totalBudgetPkr)} PKR  |  Spent: ${fmt(summary.value.totalSpentPkr)} PKR  |  Saving: ${fmt(summary.value.totalSavingPkr)} PKR`, 140, 17)
    }

    autoTable(doc, {
      startY: 25,
      head: [['No','Date','Category','Title','Unit','Unit Cost','Qty','Total','Curr','≈ PKR','Paid To']],
      body: rows.map(r => [r.no, r.date, r.category, r.title, r.unit, fmt(r.unitCost), r.quantity, fmt(r.totalCost), r.currency, fmt(r.totalCostPkr), r.paidTo]),
      theme: 'grid',
      styles: { fontSize: 7, cellPadding: 2, textColor: [30,41,59] },
      headStyles: { fillColor: [15,23,42], textColor: [217,119,6], fontStyle: 'bold', fontSize: 7.5 },
      alternateRowStyles: { fillColor: [248,250,252] },
      columnStyles: { 6: { halign: 'right' }, 7: { halign: 'right', fontStyle: 'bold' } },
      margin: { left: 14, right: 14 },
    })

    const pages = doc.internal.getNumberOfPages()
    for (let i = 1; i <= pages; i++) {
      doc.setPage(i); doc.setFontSize(7); doc.setTextColor(148, 163, 184)
      doc.text(`Page ${i} of ${pages}  |  Misbahuda Finance — Confidential`, 14, doc.internal.pageSize.height - 6)
    }
    doc.save(`Misbahuda_Finance_${new Date().toISOString().slice(0,10)}.pdf`)
  } catch (e) { console.error(e); notify('error', 'PDF export failed.') }
}

onMounted(() => { load(); loadRates(); loadStaff(); loadTickets() })
</script>

<template>
  <div class="space-y-6">

    <!-- Header -->
    <div class="flex items-center justify-between flex-wrap gap-3">
      <div>
        <h1 class="text-2xl font-bold text-white">Finance</h1>
        <p class="text-slate-400 text-sm mt-0.5">Budget tracking · Expenses · Cost analysis</p>
      </div>
      <div class="flex gap-2 flex-wrap">
        <button @click="exportExcel" class="flex items-center gap-1.5 px-3 py-2 rounded-xl text-xs font-semibold border border-emerald-800/60 text-emerald-400 hover:bg-emerald-950/40 transition-colors">
          📊 Excel
        </button>
        <button @click="exportPdf" class="flex items-center gap-1.5 px-3 py-2 rounded-xl text-xs font-semibold border border-red-800/60 text-red-400 hover:bg-red-950/40 transition-colors">
          📄 PDF
        </button>
        <button @click="openBudget(null)" class="flex items-center gap-1.5 px-3 py-2 rounded-xl text-xs font-semibold border border-blue-800/60 text-blue-400 hover:bg-blue-950/40 transition-colors">
          🎯 Set Budget
        </button>
        <button @click="openAdd(); activeTab='expenses'"
                class="flex items-center gap-2 px-4 py-2 rounded-xl text-sm font-semibold text-white"
                style="background:rgba(180,83,9,0.7);border:1px solid rgba(180,83,9,0.5);">
          + Add Expense
        </button>
      </div>
    </div>

    <!-- Alert -->
    <div v-if="msg.text"
         :class="msg.type==='success' ? 'bg-emerald-900/50 border-emerald-700 text-emerald-300' : 'bg-red-900/50 border-red-700 text-red-300'"
         class="border rounded-lg px-4 py-3 text-sm flex justify-between">
      {{ msg.text }}<button @click="msg.text=''" class="opacity-60 hover:opacity-100">✕</button>
    </div>

    <div v-if="loading" class="card text-center py-16 text-slate-400">Loading financial data...</div>

    <template v-else-if="summary">

      <!-- ── Tabs ──────────────────────────────────────────── -->
      <div class="flex gap-1 bg-dark-800 p-1 rounded-xl w-fit">
        <button v-for="t in [{k:'dashboard',l:'📊 Dashboard'},{k:'expenses',l:'📋 Expenses'},{k:'budget',l:'🎯 Budget'},{k:'staff',l:'👥 Staff Pay'},{k:'tickets',l:'✈️ Tickets'},{k:'rates',l:'💱 Rates'}]"
                :key="t.k" @click="activeTab = t.k"
                :class="['px-4 py-2 rounded-lg text-sm font-semibold transition-all', activeTab===t.k ? 'bg-dark-600 text-white' : 'text-slate-400 hover:text-white']">
          {{ t.l }}
        </button>
      </div>

      <!-- ── DASHBOARD TAB ──────────────────────────────────── -->
      <template v-if="activeTab==='dashboard'">

        <!-- Summary Cards -->
        <div class="grid grid-cols-1 sm:grid-cols-3 gap-4">
          <div class="card border border-blue-900/50 text-center py-5">
            <p class="text-slate-400 text-xs uppercase tracking-wider mb-2">💰 Total Budget</p>
            <p class="text-3xl font-bold text-blue-400">{{ fmt(summary.totalBudgetPkr) }}</p>
            <p class="text-slate-500 text-xs mt-1">PKR equivalent</p>
          </div>
          <div class="card border border-red-900/50 text-center py-5">
            <p class="text-slate-400 text-xs uppercase tracking-wider mb-2">💸 Total Spent</p>
            <p class="text-3xl font-bold text-red-400">{{ fmt(summary.totalSpentPkr) }}</p>
            <p class="text-slate-500 text-xs mt-1">{{ summary.expenseCount }} transactions</p>
          </div>
          <div class="card text-center py-5"
               :class="summary.totalSavingPkr >= 0 ? 'border border-emerald-900/50' : 'border border-orange-900/50'">
            <p class="text-slate-400 text-xs uppercase tracking-wider mb-2">
              {{ summary.totalSavingPkr >= 0 ? '✅ Saving' : '⚠️ Over Budget' }}
            </p>
            <p class="text-3xl font-bold" :class="summary.totalSavingPkr >= 0 ? 'text-emerald-400' : 'text-orange-400'">
              {{ fmt(Math.abs(summary.totalSavingPkr)) }}
            </p>
            <p class="text-slate-500 text-xs mt-1">
              {{ summary.totalBudgetPkr > 0 ? Math.round(summary.totalSpentPkr / summary.totalBudgetPkr * 100) : 0 }}% of budget used
            </p>
          </div>
        </div>

        <!-- Overall Progress Bar -->
        <div v-if="summary.totalBudgetPkr > 0" class="card border border-dark-600">
          <div class="flex justify-between text-xs text-slate-400 mb-2">
            <span>Budget utilization</span>
            <span>{{ fmt(summary.totalSpentPkr) }} / {{ fmt(summary.totalBudgetPkr) }} PKR</span>
          </div>
          <div class="w-full bg-dark-700 rounded-full h-4">
            <div class="h-4 rounded-full transition-all text-xs text-white flex items-center justify-end pr-2 font-semibold"
                 :class="summary.totalSpentPkr/summary.totalBudgetPkr > 0.9 ? 'bg-red-500' : summary.totalSpentPkr/summary.totalBudgetPkr > 0.7 ? 'bg-orange-500' : 'bg-emerald-500'"
                 :style="`width:${Math.min(100, Math.round(summary.totalSpentPkr/summary.totalBudgetPkr*100))}%`">
              {{ Math.round(summary.totalSpentPkr/summary.totalBudgetPkr*100) }}%
            </div>
          </div>
        </div>

        <!-- Category Breakdown -->
        <div>
          <h2 class="text-sm font-semibold text-gold-500 uppercase tracking-wider mb-3">Category Breakdown</h2>
          <div class="grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
            <div v-for="cat in summary.byCategory" :key="cat.category"
                 class="card border border-dark-600 hover:border-gold-800/40 transition-all">
              <div class="flex items-center justify-between mb-3">
                <div class="flex items-center gap-2">
                  <span class="text-xl">{{ catIcon(categories.find(c => c.label.includes(cat.category))?.value) }}</span>
                  <span class="text-white font-semibold text-sm">{{ cat.label }}</span>
                </div>
                <span class="text-xs text-slate-500">{{ cat.count }} entries</span>
              </div>
              <div class="space-y-1 text-xs">
                <div class="flex justify-between">
                  <span class="text-slate-400">Spent</span>
                  <span class="text-red-400 font-semibold">{{ fmt(cat.spentPkr) }} PKR</span>
                </div>
                <div v-if="cat.budgetPkr > 0" class="flex justify-between">
                  <span class="text-slate-400">Budget</span>
                  <span class="text-blue-400">{{ fmt(cat.budgetPkr) }} PKR</span>
                </div>
                <div v-if="cat.budgetPkr > 0" class="flex justify-between">
                  <span class="text-slate-400">{{ cat.remainingPkr >= 0 ? 'Remaining' : 'Over by' }}</span>
                  <span :class="cat.remainingPkr >= 0 ? 'text-emerald-400' : 'text-orange-400'" class="font-semibold">
                    {{ fmt(Math.abs(cat.remainingPkr)) }} PKR
                  </span>
                </div>
              </div>
              <div v-if="cat.budgetPkr > 0" class="mt-3">
                <div class="w-full bg-dark-700 rounded-full h-1.5">
                  <div class="h-1.5 rounded-full transition-all"
                       :class="cat.spentPkr/cat.budgetPkr > 1 ? 'bg-red-500' : cat.spentPkr/cat.budgetPkr > 0.8 ? 'bg-orange-500' : 'bg-emerald-500'"
                       :style="`width:${Math.min(100, Math.round(cat.spentPkr/cat.budgetPkr*100))}%`"></div>
                </div>
              </div>
              <button @click="openBudget(categories.find(c => c.label.includes(cat.category)))"
                      class="mt-2 text-xs text-slate-500 hover:text-gold-400 transition-colors">
                {{ cat.budgetPkr > 0 ? '✏️ Edit Budget' : '+ Set Budget' }}
              </button>
            </div>
          </div>
        </div>

        <!-- Recent Expenses -->
        <div v-if="summary.recentExpenses?.length" class="card border border-dark-600">
          <h3 class="text-sm font-semibold text-white mb-3">Recent Transactions</h3>
          <div class="space-y-2">
            <div v-for="e in summary.recentExpenses" :key="e.id"
                 class="flex items-center gap-3 py-2 border-b border-dark-700 last:border-0">
              <span class="text-2xl w-8 text-center">{{ catIcon(e.category) }}</span>
              <div class="flex-1 min-w-0">
                <p class="text-white text-sm font-medium truncate">{{ e.title }}</p>
                <p class="text-slate-500 text-xs">{{ e.label }} · {{ new Date(e.date).toLocaleDateString('en-GB') }}</p>
              </div>
              <div class="text-right shrink-0">
                <p class="text-red-400 font-semibold text-sm">{{ fmt(e.totalCost) }} <span class="text-xs font-normal">{{ e.currency }}</span></p>
                <p v-if="e.currency !== 'PKR'" class="text-slate-500 text-xs">≈ {{ fmt(e.totalCostPkr) }} PKR</p>
              </div>
            </div>
          </div>
          <button @click="activeTab='expenses'" class="mt-3 text-xs text-gold-500 hover:text-gold-400">View all expenses →</button>
        </div>

      </template>

      <!-- ── EXPENSES TAB ───────────────────────────────────── -->
      <template v-if="activeTab==='expenses'">

        <div class="flex justify-end">
          <button @click="openAdd" class="flex items-center gap-2 px-4 py-2 rounded-xl text-sm font-semibold text-white"
                  style="background:rgba(180,83,9,0.7);border:1px solid rgba(180,83,9,0.5);">
            + Add Expense
          </button>
        </div>

        <div v-if="!expenses.length" class="card text-center py-12">
          <div class="text-5xl mb-4">💰</div>
          <p class="text-white font-semibold">No expenses yet</p>
          <p class="text-slate-400 text-sm mt-2">Start adding hotel, meals, transport costs etc.</p>
        </div>

        <div v-else class="overflow-x-auto rounded-xl border border-dark-600">
          <table class="w-full text-sm">
            <thead>
              <tr class="border-b border-dark-600" style="background:#0f172a">
                <th class="text-left px-4 py-3 text-gold-500 font-semibold text-xs uppercase">Date</th>
                <th class="text-left px-4 py-3 text-gold-500 font-semibold text-xs uppercase">Category</th>
                <th class="text-left px-4 py-3 text-gold-500 font-semibold text-xs uppercase">Title</th>
                <th class="text-right px-4 py-3 text-gold-500 font-semibold text-xs uppercase">Unit Cost</th>
                <th class="text-right px-4 py-3 text-gold-500 font-semibold text-xs uppercase">Qty</th>
                <th class="text-right px-4 py-3 text-gold-500 font-semibold text-xs uppercase">Total</th>
                <th class="text-right px-4 py-3 text-gold-500 font-semibold text-xs uppercase">≈ PKR</th>
                <th class="text-left px-4 py-3 text-gold-500 font-semibold text-xs uppercase">Paid To</th>
                <th class="px-4 py-3"></th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="e in expenses" :key="e.id"
                  class="border-b border-dark-700 hover:bg-dark-800/50 transition-colors">
                <td class="px-4 py-3 text-slate-400 text-xs whitespace-nowrap">{{ new Date(e.date).toLocaleDateString('en-GB') }}</td>
                <td class="px-4 py-3">
                  <span class="inline-flex items-center gap-1.5 text-xs px-2 py-1 rounded-lg bg-dark-700">
                    {{ catIcon(categories.find(c => c.label.includes(e.category))?.value) }} {{ e.label }}
                  </span>
                </td>
                <td class="px-4 py-3 text-white font-medium">
                  {{ e.title }}
                  <p v-if="e.notes" class="text-slate-500 text-xs mt-0.5 truncate max-w-48">{{ e.notes }}</p>
                  <div v-if="e.breakfastIncluded || e.lunchIncluded || e.dinnerIncluded" class="flex gap-1 mt-1">
                    <span v-if="e.breakfastIncluded" class="text-xs px-1.5 py-0.5 rounded bg-emerald-900/40 text-emerald-400">🍳 B</span>
                    <span v-if="e.lunchIncluded"     class="text-xs px-1.5 py-0.5 rounded bg-emerald-900/40 text-emerald-400">🍽 L</span>
                    <span v-if="e.dinnerIncluded"    class="text-xs px-1.5 py-0.5 rounded bg-emerald-900/40 text-emerald-400">🌙 D</span>
                  </div>
                </td>
                <td class="px-4 py-3 text-right text-slate-300 text-xs">{{ fmt(e.unitCost) }} <span class="text-slate-600">{{ e.unit }}</span></td>
                <td class="px-4 py-3 text-right text-slate-400">{{ e.quantity }}</td>
                <td class="px-4 py-3 text-right font-bold text-red-400">{{ fmt(e.totalCost) }} <span class="text-slate-600 font-normal text-xs">{{ e.currency }}</span></td>
                <td class="px-4 py-3 text-right text-slate-400 text-xs">{{ e.currency === 'PKR' ? '—' : fmt(e.totalCostPkr) }}</td>
                <td class="px-4 py-3 text-slate-400 text-xs">{{ e.paidTo || '—' }}</td>
                <td class="px-4 py-3">
                  <div class="flex gap-1">
                    <button @click="openEdit(e)" class="text-xs px-2 py-1 rounded border border-blue-800/60 text-blue-400 hover:bg-blue-950/40">✏️</button>
                    <button @click="deleteExpense(e.id)" :disabled="deleting===e.id"
                            class="text-xs px-2 py-1 rounded border border-red-800/60 text-red-400 hover:bg-red-950/40 disabled:opacity-50">🗑</button>
                  </div>
                </td>
              </tr>
            </tbody>
            <tfoot>
              <tr style="background:#0f172a">
                <td colspan="5" class="px-4 py-3 text-right text-xs font-semibold text-slate-400 uppercase tracking-wider">Grand Total (PKR)</td>
                <td colspan="2" class="px-4 py-3 text-right font-bold text-xl text-red-400">{{ fmt(summary.totalSpentPkr) }}</td>
                <td colspan="2"></td>
              </tr>
            </tfoot>
          </table>
        </div>
        <AppPagination :page="expPage" :total="expTotal" :pageSize="EXP_SIZE"
          @update:page="p => { expPage = p; load() }" />
      </template>

      <!-- ── BUDGET TAB ─────────────────────────────────────── -->
      <template v-if="activeTab==='budget'">
        <div class="grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
          <div v-for="cat in categories" :key="cat.value"
               class="card border border-dark-600 hover:border-gold-800/40 transition-all">
            <div class="flex items-center justify-between">
              <div class="flex items-center gap-2">
                <span class="text-2xl">{{ cat.icon }}</span>
                <div>
                  <p class="text-white font-semibold text-sm">{{ cat.label.replace(/^.\s/,'') }}</p>
                  <p class="text-xs text-slate-500">
                    Spent: <span class="text-red-400">{{ fmt(summary.byCategory.find(c=>c.category===cat.label.replace(/^.\s/,''))?.spentPkr || 0) }}</span> PKR
                  </p>
                </div>
              </div>
              <div class="text-right">
                <p class="text-blue-400 font-bold">
                  {{ fmt(summary.byCategory.find(c=>c.category===cat.label.replace(/^.\s/,''))?.budgetPkr || 0) }}
                </p>
                <p class="text-slate-600 text-xs">PKR budgeted</p>
              </div>
            </div>
            <button @click="openBudget(cat)"
                    class="mt-3 w-full text-xs py-1.5 rounded-lg border border-dark-600 text-slate-400 hover:border-gold-700 hover:text-white transition-colors">
              {{ summary.byCategory.find(c=>c.category===cat.label.replace(/^.\s/,''))?.budgetPkr ? '✏️ Edit Budget' : '+ Set Budget' }}
            </button>
          </div>
        </div>
      </template>

      <!-- ── STAFF PAY TAB ──────────────────────────────────── -->
      <template v-if="activeTab==='staff'">

        <!-- Summary cards -->
        <div v-if="staffData" class="grid grid-cols-1 sm:grid-cols-3 gap-4">
          <div class="card border border-slate-700 text-center py-4">
            <p class="text-slate-400 text-xs uppercase tracking-wider mb-1">👥 Total Payable</p>
            <p class="text-2xl font-bold text-white">{{ fmt(staffData.totalPkr) }}</p>
            <p class="text-slate-500 text-xs mt-0.5">PKR equivalent</p>
          </div>
          <div class="card border border-emerald-900/50 text-center py-4">
            <p class="text-slate-400 text-xs uppercase tracking-wider mb-1">✅ Paid</p>
            <p class="text-2xl font-bold text-emerald-400">{{ fmt(staffData.paidPkr) }}</p>
            <p class="text-slate-500 text-xs mt-0.5">PKR</p>
          </div>
          <div class="card text-center py-4" :class="staffData.pendingPkr > 0 ? 'border border-orange-900/50' : 'border border-slate-700'">
            <p class="text-slate-400 text-xs uppercase tracking-wider mb-1">⏳ Pending</p>
            <p class="text-2xl font-bold" :class="staffData.pendingPkr > 0 ? 'text-orange-400' : 'text-slate-400'">{{ fmt(staffData.pendingPkr) }}</p>
            <p class="text-slate-500 text-xs mt-0.5">PKR</p>
          </div>
        </div>

        <div class="flex justify-end">
          <button @click="openAddStaff"
                  class="flex items-center gap-2 px-4 py-2 rounded-xl text-sm font-semibold text-white"
                  style="background:rgba(180,83,9,0.7);border:1px solid rgba(180,83,9,0.5);">
            + Add Payment
          </button>
        </div>

        <div v-if="!staffData?.payments?.length" class="card text-center py-12">
          <div class="text-5xl mb-3">👥</div>
          <p class="text-white font-semibold">No staff payments yet</p>
          <p class="text-slate-400 text-sm mt-1">Add Molana honorarium, part-time staff, servants etc.</p>
        </div>

        <div v-else class="overflow-x-auto rounded-xl border border-dark-600">
          <table class="w-full text-sm">
            <thead>
              <tr class="border-b border-dark-600" style="background:#0f172a">
                <th class="text-left px-4 py-3 text-gold-500 font-semibold text-xs uppercase">Name</th>
                <th class="text-left px-4 py-3 text-gold-500 font-semibold text-xs uppercase">Role</th>
                <th class="text-left px-4 py-3 text-gold-500 font-semibold text-xs uppercase">Description</th>
                <th class="text-right px-4 py-3 text-gold-500 font-semibold text-xs uppercase">Amount</th>
                <th class="text-right px-4 py-3 text-gold-500 font-semibold text-xs uppercase">≈ PKR</th>
                <th class="text-left px-4 py-3 text-gold-500 font-semibold text-xs uppercase">Date</th>
                <th class="text-left px-4 py-3 text-gold-500 font-semibold text-xs uppercase">Status</th>
                <th class="px-4 py-3"></th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="p in staffData.payments" :key="p.id"
                  class="border-b border-dark-700 hover:bg-dark-800/50 transition-colors">
                <td class="px-4 py-3">
                  <p class="text-white font-medium">{{ p.name }}</p>
                  <p v-if="p.phone" class="text-slate-500 text-xs">
                    <a :href="`https://wa.me/${p.phone.replace(/[^\d]/g,'')}`" target="_blank" class="text-emerald-500 hover:underline">💬 {{ p.phone }}</a>
                  </p>
                </td>
                <td class="px-4 py-3">
                  <span class="text-xs px-2 py-1 rounded-lg bg-dark-700 text-slate-300">{{ p.roleLabel }}</span>
                </td>
                <td class="px-4 py-3 text-slate-400 text-xs max-w-40 truncate">{{ p.description || '—' }}</td>
                <td class="px-4 py-3 text-right font-bold text-white">{{ fmt(p.amount) }} <span class="text-slate-500 font-normal text-xs">{{ p.currency }}</span></td>
                <td class="px-4 py-3 text-right text-slate-400 text-xs">{{ p.currency === 'PKR' ? '—' : fmt(p.amountPkr) }}</td>
                <td class="px-4 py-3 text-slate-400 text-xs whitespace-nowrap">{{ new Date(p.date).toLocaleDateString('en-GB') }}</td>
                <td class="px-4 py-3">
                  <button v-if="!p.isPaid" @click="markStaffPaid(p.id)"
                          class="text-xs px-2 py-1 rounded border border-orange-700/60 text-orange-400 hover:bg-orange-950/40 whitespace-nowrap">
                    ⏳ Pending
                  </button>
                  <span v-else class="text-xs px-2 py-1 rounded bg-emerald-900/40 text-emerald-400">✅ Paid</span>
                </td>
                <td class="px-4 py-3">
                  <div class="flex gap-1">
                    <button @click="openEditStaff(p)" class="text-xs px-2 py-1 rounded border border-blue-800/60 text-blue-400 hover:bg-blue-950/40">✏️</button>
                    <button @click="deleteStaff(p.id)" :disabled="deletingStaff===p.id"
                            class="text-xs px-2 py-1 rounded border border-red-800/60 text-red-400 hover:bg-red-950/40 disabled:opacity-50">🗑</button>
                  </div>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </template>

      <!-- ── TICKETS TAB ───────────────────────────────────── -->
      <template v-if="activeTab==='tickets'">

        <!-- Summary -->
        <div v-if="ticketData" class="grid grid-cols-2 sm:grid-cols-4 gap-4">
          <div class="card border border-slate-700 text-center py-4">
            <p class="text-slate-400 text-xs uppercase tracking-wider mb-1">Total Cost</p>
            <p class="text-xl font-bold text-white">{{ fmt(ticketData.totalCostPkr) }}</p>
            <p class="text-slate-500 text-xs mt-0.5">PKR</p>
          </div>
          <div class="card border border-slate-700 text-center py-4">
            <p class="text-slate-400 text-xs uppercase tracking-wider mb-1">Total</p>
            <p class="text-xl font-bold text-white">{{ ticketData.totalCount }}</p>
            <p class="text-slate-500 text-xs mt-0.5">tickets</p>
          </div>
          <div class="card border border-emerald-900/50 text-center py-4">
            <p class="text-slate-400 text-xs uppercase tracking-wider mb-1">✅ Booked/Used</p>
            <p class="text-xl font-bold text-emerald-400">{{ ticketData.bookedCount }}</p>
          </div>
          <div class="card text-center py-4" :class="ticketData.pendingCount > 0 ? 'border border-orange-900/50' : 'border border-slate-700'">
            <p class="text-slate-400 text-xs uppercase tracking-wider mb-1">⏳ Pending</p>
            <p class="text-xl font-bold" :class="ticketData.pendingCount > 0 ? 'text-orange-400' : 'text-slate-400'">{{ ticketData.pendingCount }}</p>
          </div>
        </div>

        <div class="flex justify-end">
          <button @click="openAddTicket"
                  class="flex items-center gap-2 px-4 py-2 rounded-xl text-sm font-semibold text-white"
                  style="background:rgba(180,83,9,0.7);border:1px solid rgba(180,83,9,0.5);">
            + Add Ticket
          </button>
        </div>

        <div v-if="!ticketData?.tickets?.length" class="card text-center py-12">
          <div class="text-5xl mb-3">✈️</div>
          <p class="text-white font-semibold">No tickets yet</p>
          <p class="text-slate-400 text-sm mt-1">Add flight/bus tickets for Molana, staff, airport receivers etc.</p>
        </div>

        <div v-else class="overflow-x-auto rounded-xl border border-dark-600">
          <table class="w-full text-sm">
            <thead>
              <tr class="border-b border-dark-600" style="background:#0f172a">
                <th class="text-left px-4 py-3 text-gold-500 font-semibold text-xs uppercase">Passenger</th>
                <th class="text-left px-4 py-3 text-gold-500 font-semibold text-xs uppercase">Route</th>
                <th class="text-left px-4 py-3 text-gold-500 font-semibold text-xs uppercase">Travel Date</th>
                <th class="text-left px-4 py-3 text-gold-500 font-semibold text-xs uppercase">Airline / No.</th>
                <th class="text-right px-4 py-3 text-gold-500 font-semibold text-xs uppercase">Cost</th>
                <th class="text-right px-4 py-3 text-gold-500 font-semibold text-xs uppercase">≈ PKR</th>
                <th class="text-left px-4 py-3 text-gold-500 font-semibold text-xs uppercase">Status</th>
                <th class="px-4 py-3"></th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="t in ticketData.tickets" :key="t.id"
                  class="border-b border-dark-700 hover:bg-dark-800/50 transition-colors">
                <td class="px-4 py-3">
                  <p class="text-white font-medium">{{ t.passengerName }}</p>
                  <p class="text-slate-500 text-xs">{{ t.roleLabel }}</p>
                  <a v-if="t.phone" :href="`https://wa.me/${t.phone.replace(/[^\d]/g,'')}`" target="_blank"
                     class="text-emerald-500 text-xs hover:underline">💬 {{ t.phone }}</a>
                </td>
                <td class="px-4 py-3">
                  <p class="text-white text-xs font-medium">{{ t.fromCity }} → {{ t.toCity }}</p>
                  <p class="text-slate-500 text-xs">{{ t.ticketType }}</p>
                </td>
                <td class="px-4 py-3 text-slate-400 text-xs whitespace-nowrap">{{ new Date(t.travelDate).toLocaleDateString('en-GB') }}</td>
                <td class="px-4 py-3 text-slate-400 text-xs">
                  <p>{{ t.airline || '—' }}</p>
                  <p v-if="t.flightNumber" class="text-slate-600">{{ t.flightNumber }}</p>
                  <p v-if="t.ticketNumber" class="text-slate-600">{{ t.ticketNumber }}</p>
                </td>
                <td class="px-4 py-3 text-right font-bold text-white">{{ fmt(t.cost) }} <span class="text-slate-500 font-normal text-xs">{{ t.currency }}</span></td>
                <td class="px-4 py-3 text-right text-slate-400 text-xs">{{ t.currency === 'PKR' ? '—' : fmt(t.costPkr) }}</td>
                <td class="px-4 py-3">
                  <span class="text-xs px-2 py-1 rounded-lg"
                        :class="{
                          'bg-orange-900/40 text-orange-400': t.status==='Pending',
                          'bg-emerald-900/40 text-emerald-400': t.status==='Booked' || t.status==='Used',
                          'bg-red-900/40 text-red-400': t.status==='Cancelled',
                        }">
                    {{ t.statusLabel }}
                  </span>
                </td>
                <td class="px-4 py-3">
                  <div class="flex gap-1">
                    <button @click="openEditTicket(t)" class="text-xs px-2 py-1 rounded border border-blue-800/60 text-blue-400 hover:bg-blue-950/40">✏️</button>
                    <button @click="deleteTicket(t.id)" :disabled="deletingTicket===t.id"
                            class="text-xs px-2 py-1 rounded border border-red-800/60 text-red-400 hover:bg-red-950/40 disabled:opacity-50">🗑</button>
                  </div>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </template>

      <!-- ── RATES TAB ─────────────────────────────────────── -->
      <template v-if="activeTab==='rates'">
        <div class="card border border-dark-600 max-w-lg">
          <h2 class="text-white font-bold mb-1">💱 Exchange Rates to PKR</h2>
          <p class="text-slate-400 text-xs mb-5">Set how much 1 unit of each currency equals in Pakistani Rupees. All dashboard totals are auto-converted to PKR using these rates.</p>
          <div class="space-y-3">
            <div v-for="c in currencies.filter(x => x.code !== 'PKR')" :key="c.code"
                 class="flex items-center gap-3 p-3 rounded-xl bg-dark-800 border border-dark-700">
              <div class="w-14 text-center">
                <span class="text-white font-bold text-sm">{{ c.code }}</span>
                <p class="text-slate-500 text-xs">{{ c.name }}</p>
              </div>
              <div class="flex-1 flex items-center gap-2">
                <span class="text-slate-400 text-xs">1 {{ c.code }} =</span>
                <input v-model="rateEdits[c.code]" type="number" min="0" step="0.01"
                       class="input flex-1 text-right" :placeholder="`e.g. ${c.code==='USD'?'280':c.code==='GBP'?'355':c.code==='IQD'?'0.21':c.code==='SAR'?'75':'120'}`" />
                <span class="text-slate-400 text-xs">PKR</span>
              </div>
              <button @click="saveRate(c.code)" :disabled="savingRate===c.code"
                      class="px-3 py-1.5 rounded-lg text-xs font-semibold text-white disabled:opacity-40 transition-colors"
                      style="background:rgba(180,83,9,0.6);border:1px solid rgba(180,83,9,0.4);">
                {{ savingRate===c.code ? '...' : 'Save' }}
              </button>
            </div>
          </div>
          <p class="text-slate-600 text-xs mt-4">Last updated rates are shown in the input fields. Update any time exchange rates change.</p>
        </div>
      </template>

    </template>

    <!-- ── Add/Edit Expense Modal ─────────────────────────── -->
    <Teleport to="body">
      <div v-if="showForm" class="fixed inset-0 z-50 flex items-center justify-center p-4" style="background:rgba(0,0,0,0.85);" @click.self="showForm=false">
        <div class="w-full max-w-lg rounded-2xl overflow-hidden shadow-2xl" style="background:#0f172a;border:1px solid rgba(180,83,9,0.35);">
          <div class="h-0.5 w-full" style="background:linear-gradient(90deg,transparent,#d97706,transparent)"></div>
          <div class="px-6 py-4 border-b border-dark-700 flex items-center justify-between">
            <h3 class="text-white font-bold text-lg">{{ editingId ? 'Edit Expense' : 'Add Expense' }}</h3>
            <button @click="showForm=false" class="text-slate-400 hover:text-white text-2xl">✕</button>
          </div>
          <div class="p-6 space-y-4">
            <div class="grid grid-cols-2 gap-3">
              <div>
                <label class="form-label">Category *</label>
                <select v-model="form.category" class="input w-full">
                  <option v-for="c in categories" :key="c.value" :value="c.value">{{ c.label }}</option>
                </select>
              </div>
              <div>
                <label class="form-label">Unit</label>
                <select v-model="form.unit" class="input w-full">
                  <option v-for="u in units" :key="u.value" :value="u.value">{{ u.label }}</option>
                </select>
              </div>
            </div>
            <div>
              <label class="form-label">Title *</label>
              <input v-model="form.title" class="input w-full" placeholder="e.g. Kazimain Hotel — Night 1" />
            </div>
            <div class="grid grid-cols-3 gap-3">
              <div class="col-span-1">
                <label class="form-label">Unit Cost *</label>
                <input v-model="form.unitCost" type="number" min="0" class="input w-full" placeholder="0" />
              </div>
              <div>
                <label class="form-label">Quantity</label>
                <input v-model="form.quantity" type="number" min="1" class="input w-full" />
              </div>
              <div>
                <label class="form-label">Currency</label>
                <select v-model="form.currency" class="input w-full">
                  <option v-for="c in currencies" :key="c.code" :value="c.code">{{ c.code }} — {{ c.name }}</option>
                </select>
              </div>
            </div>
            <!-- Meal inclusion — only for Hotel category -->
            <div v-if="form.category === 1" class="rounded-xl border border-emerald-900/40 bg-emerald-950/20 p-4">
              <p class="text-emerald-400 text-xs font-semibold uppercase tracking-wider mb-3">🍽 Meals Included in Hotel Deal?</p>
              <div class="flex gap-6">
                <label class="flex items-center gap-2 cursor-pointer">
                  <input type="checkbox" v-model="form.breakfastIncluded" class="w-4 h-4 accent-emerald-500 rounded" />
                  <span class="text-sm text-white">🍳 Breakfast</span>
                </label>
                <label class="flex items-center gap-2 cursor-pointer">
                  <input type="checkbox" v-model="form.lunchIncluded" class="w-4 h-4 accent-emerald-500 rounded" />
                  <span class="text-sm text-white">🍽 Lunch</span>
                </label>
                <label class="flex items-center gap-2 cursor-pointer">
                  <input type="checkbox" v-model="form.dinnerIncluded" class="w-4 h-4 accent-emerald-500 rounded" />
                  <span class="text-sm text-white">🌙 Dinner</span>
                </label>
              </div>
              <p class="text-slate-500 text-xs mt-2">Tick jo hotel nay include kiya — alag expense add karne ki zaroorat nahi hogi</p>
            </div>

            <!-- Total preview -->
            <div class="bg-dark-700 rounded-xl p-3 flex justify-between items-center">
              <span class="text-slate-400 text-sm">Total Amount</span>
              <span class="text-white font-bold text-xl">{{ fmt(totalCalc) }} <span class="text-slate-500 text-sm font-normal">{{ form.currency }}</span></span>
            </div>
            <div class="grid grid-cols-2 gap-3">
              <div>
                <label class="form-label">Date</label>
                <input v-model="form.date" type="date" class="input w-full" />
              </div>
              <div>
                <label class="form-label">Paid To</label>
                <input v-model="form.paidTo" class="input w-full" placeholder="Vendor / Hotel name" />
              </div>
            </div>
            <div>
              <label class="form-label">Notes</label>
              <textarea v-model="form.notes" class="input w-full" rows="2" placeholder="Optional notes..."></textarea>
            </div>
            <div class="flex gap-3">
              <button @click="saveExpense" :disabled="saving"
                      class="flex-1 py-2.5 rounded-xl text-sm font-semibold text-white disabled:opacity-40"
                      style="background:rgba(180,83,9,0.7);">
                {{ saving ? 'Saving...' : (editingId ? 'Update' : 'Add Expense') }}
              </button>
              <button @click="showForm=false" class="btn-outline px-5">Cancel</button>
            </div>
          </div>
        </div>
      </div>
    </Teleport>

    <!-- ── Ticket Modal ─────────────────────────────────── -->
    <Teleport to="body">
      <div v-if="showTicketForm" class="fixed inset-0 z-50 flex items-center justify-center p-4" style="background:rgba(0,0,0,0.85);" @click.self="showTicketForm=false">
        <div class="w-full max-w-xl rounded-2xl overflow-hidden shadow-2xl" style="background:#0f172a;border:1px solid rgba(180,83,9,0.35);">
          <div class="h-0.5 w-full" style="background:linear-gradient(90deg,transparent,#d97706,transparent)"></div>
          <div class="px-6 py-4 border-b border-dark-700 flex items-center justify-between">
            <h3 class="text-white font-bold text-lg">{{ editingTicketId ? 'Edit Ticket' : '✈️ Add Travel Ticket' }}</h3>
            <button @click="showTicketForm=false" class="text-slate-400 hover:text-white text-2xl">✕</button>
          </div>
          <div class="p-6 space-y-4">
            <div class="grid grid-cols-2 gap-3">
              <div>
                <label class="form-label">Passenger Name *</label>
                <input v-model="ticketForm.passengerName" class="input w-full" placeholder="Full name" />
              </div>
              <div>
                <label class="form-label">Role</label>
                <select v-model="ticketForm.passengerRole" class="input w-full">
                  <option v-for="r in staffRolesAll" :key="r.value" :value="r.value">{{ r.label }}</option>
                </select>
              </div>
            </div>
            <div class="grid grid-cols-2 gap-3">
              <div>
                <label class="form-label">WhatsApp / Phone</label>
                <input v-model="ticketForm.phone" class="input w-full" placeholder="+92..." />
              </div>
              <div>
                <label class="form-label">Ticket Type</label>
                <select v-model="ticketForm.ticketType" class="input w-full">
                  <option v-for="tt in ticketTypes" :key="tt.value" :value="tt.value">{{ tt.label }}</option>
                </select>
              </div>
            </div>
            <div class="grid grid-cols-2 gap-3">
              <div>
                <label class="form-label">From City *</label>
                <input v-model="ticketForm.fromCity" class="input w-full" placeholder="e.g. Karachi, London" />
              </div>
              <div>
                <label class="form-label">To City *</label>
                <input v-model="ticketForm.toCity" class="input w-full" placeholder="e.g. Najaf, Baghdad" />
              </div>
            </div>
            <div class="grid grid-cols-2 gap-3">
              <div>
                <label class="form-label">Travel Date</label>
                <input v-model="ticketForm.travelDate" type="date" class="input w-full" />
              </div>
              <div>
                <label class="form-label">Airline / Company</label>
                <input v-model="ticketForm.airline" class="input w-full" placeholder="e.g. PIA, Iraqi Airways" />
              </div>
            </div>
            <div class="grid grid-cols-2 gap-3">
              <div>
                <label class="form-label">Flight / Bus No.</label>
                <input v-model="ticketForm.flightNumber" class="input w-full" placeholder="e.g. PK-743" />
              </div>
              <div>
                <label class="form-label">Ticket No.</label>
                <input v-model="ticketForm.ticketNumber" class="input w-full" placeholder="Booking reference" />
              </div>
            </div>
            <div class="grid grid-cols-3 gap-3">
              <div>
                <label class="form-label">Cost *</label>
                <input v-model="ticketForm.cost" type="number" min="0" class="input w-full" placeholder="0" />
              </div>
              <div>
                <label class="form-label">Currency</label>
                <select v-model="ticketForm.currency" class="input w-full">
                  <option v-for="c in currencies" :key="c.code" :value="c.code">{{ c.code }}</option>
                </select>
              </div>
              <div>
                <label class="form-label">Status</label>
                <select v-model="ticketForm.status" class="input w-full">
                  <option v-for="s in ticketStatuses" :key="s.value" :value="s.value">{{ s.label }}</option>
                </select>
              </div>
            </div>
            <div>
              <label class="form-label">Notes</label>
              <textarea v-model="ticketForm.notes" class="input w-full" rows="2" placeholder="Optional..."></textarea>
            </div>
            <div class="flex gap-3">
              <button @click="saveTicket" :disabled="savingTicket"
                      class="flex-1 py-2.5 rounded-xl text-sm font-semibold text-white disabled:opacity-40"
                      style="background:rgba(180,83,9,0.7);">
                {{ savingTicket ? 'Saving...' : (editingTicketId ? 'Update' : 'Add Ticket') }}
              </button>
              <button @click="showTicketForm=false" class="btn-outline px-5">Cancel</button>
            </div>
          </div>
        </div>
      </div>
    </Teleport>

    <!-- ── Staff Payment Modal ──────────────────────────── -->
    <Teleport to="body">
      <div v-if="showStaffForm" class="fixed inset-0 z-50 flex items-center justify-center p-4" style="background:rgba(0,0,0,0.85);" @click.self="showStaffForm=false">
        <div class="w-full max-w-lg rounded-2xl overflow-hidden shadow-2xl" style="background:#0f172a;border:1px solid rgba(59,130,246,0.35);">
          <div class="h-0.5 w-full" style="background:linear-gradient(90deg,transparent,#3b82f6,transparent)"></div>
          <div class="px-6 py-4 border-b border-dark-700 flex items-center justify-between">
            <h3 class="text-white font-bold text-lg">{{ editingStaffId ? 'Edit Payment' : '👥 Add Staff Payment' }}</h3>
            <button @click="showStaffForm=false" class="text-slate-400 hover:text-white text-2xl">✕</button>
          </div>
          <div class="p-6 space-y-4">
            <div class="grid grid-cols-2 gap-3">
              <div>
                <label class="form-label">Full Name *</label>
                <input v-model="staffForm.name" class="input w-full" placeholder="e.g. Molana Abdul Qadir" />
              </div>
              <div>
                <label class="form-label">Role *</label>
                <select v-model="staffForm.role" class="input w-full">
                  <option v-for="r in staffRolesAll" :key="r.value" :value="r.value">{{ r.label }}</option>
                </select>
              </div>
            </div>
            <div class="grid grid-cols-2 gap-3">
              <div>
                <label class="form-label">WhatsApp / Phone</label>
                <input v-model="staffForm.phone" class="input w-full" placeholder="+92300..." />
              </div>
              <div>
                <label class="form-label">Date</label>
                <input v-model="staffForm.date" type="date" class="input w-full" />
              </div>
            </div>
            <div>
              <label class="form-label">Description</label>
              <input v-model="staffForm.description" class="input w-full" placeholder="e.g. 3 Majalis, 10 days work, monthly salary" />
            </div>
            <div class="grid grid-cols-2 gap-3">
              <div>
                <label class="form-label">Amount *</label>
                <input v-model="staffForm.amount" type="number" min="0" class="input w-full" placeholder="0" />
              </div>
              <div>
                <label class="form-label">Currency</label>
                <select v-model="staffForm.currency" class="input w-full">
                  <option v-for="c in currencies" :key="c.code" :value="c.code">{{ c.code }} — {{ c.name }}</option>
                </select>
              </div>
            </div>
            <div>
              <label class="form-label">Notes</label>
              <textarea v-model="staffForm.notes" class="input w-full" rows="2" placeholder="Optional..."></textarea>
            </div>
            <label class="flex items-center gap-3 cursor-pointer">
              <input type="checkbox" v-model="staffForm.isPaid" class="w-4 h-4 accent-emerald-500 rounded" />
              <span class="text-sm text-white">Already Paid</span>
              <span class="text-xs text-slate-500">(uncheck = pending)</span>
            </label>
            <div class="flex gap-3">
              <button @click="saveStaff" :disabled="savingStaff"
                      class="flex-1 py-2.5 rounded-xl text-sm font-semibold text-white bg-blue-700 hover:bg-blue-600 disabled:opacity-40">
                {{ savingStaff ? 'Saving...' : (editingStaffId ? 'Update' : 'Add Payment') }}
              </button>
              <button @click="showStaffForm=false" class="btn-outline px-5">Cancel</button>
            </div>
          </div>
        </div>
      </div>
    </Teleport>

    <!-- ── Set Budget Modal ───────────────────────────────── -->
    <Teleport to="body">
      <div v-if="showBudget" class="fixed inset-0 z-50 flex items-center justify-center p-4" style="background:rgba(0,0,0,0.85);" @click.self="showBudget=false">
        <div class="w-full max-w-sm rounded-2xl overflow-hidden shadow-2xl" style="background:#0f172a;border:1px solid rgba(59,130,246,0.35);">
          <div class="h-0.5 w-full" style="background:linear-gradient(90deg,transparent,#3b82f6,transparent)"></div>
          <div class="px-6 py-4 border-b border-dark-700 flex items-center justify-between">
            <h3 class="text-white font-bold text-lg">🎯 Set Budget</h3>
            <button @click="showBudget=false" class="text-slate-400 hover:text-white text-2xl">✕</button>
          </div>
          <div class="p-6 space-y-4">
            <div>
              <label class="form-label">Category</label>
              <select v-model="budgetForm.category" class="input w-full">
                <option v-for="c in categories" :key="c.value" :value="c.value">{{ c.label }}</option>
              </select>
            </div>
            <div>
              <label class="form-label">Budgeted Amount *</label>
              <input v-model="budgetForm.budgetedAmount" type="number" min="0" class="input w-full" placeholder="Enter budget amount" />
            </div>
            <div>
              <label class="form-label">Currency</label>
              <select v-model="budgetForm.currency" class="input w-full">
                <option v-for="c in currencies" :key="c.code" :value="c.code">{{ c.code }} — {{ c.name }}</option>
              </select>
            </div>
            <div>
              <label class="form-label">Notes</label>
              <input v-model="budgetForm.notes" class="input w-full" placeholder="Optional" />
            </div>
            <div class="flex gap-3">
              <button @click="saveBudget" :disabled="savingBudget"
                      class="flex-1 py-2.5 rounded-xl text-sm font-semibold bg-blue-700 hover:bg-blue-600 text-white disabled:opacity-40">
                {{ savingBudget ? 'Saving...' : 'Save Budget' }}
              </button>
              <button @click="showBudget=false" class="btn-outline px-5">Cancel</button>
            </div>
          </div>
        </div>
      </div>
    </Teleport>

  </div>
</template>
