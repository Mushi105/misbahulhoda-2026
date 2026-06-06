<script setup>
import { ref, onMounted } from 'vue'
import { internationalApi } from '@/api'

const msg       = ref({ type: '', text: '' })
const activeTab = ref('balance')

const balance    = ref(null)
const incoming   = ref(null)
const contracts  = ref(null)
const transfers  = ref(null)

function notify(type, text) {
  msg.value = { type, text }
  setTimeout(() => msg.value = { type: '', text: '' }, 4000)
}
function fmt(n) { return Number(n || 0).toLocaleString('en-US', { minimumFractionDigits: 0, maximumFractionDigits: 2 }) }

const currencies = [
  { code: 'USD', name: 'US Dollar' },
  { code: 'GBP', name: 'British Pound' },
  { code: 'IQD', name: 'Iraqi Dinar' },
  { code: 'SAR', name: 'Saudi Riyal' },
  { code: 'EUR', name: 'Euro' },
  { code: 'PKR', name: 'Pakistani Rupee' },
]

// ── Load all ──────────────────────────────────────────────
async function loadAll() {
  try {
    const [b, i, c, t] = await Promise.all([
      internationalApi.getBalance(),
      internationalApi.getIncoming(),
      internationalApi.getContracts(),
      internationalApi.getTransfers(),
    ])
    balance.value   = b.data.data
    incoming.value  = i.data.data
    contracts.value = c.data.data
    transfers.value = t.data.data
  } catch { notify('error', 'Failed to load international data.') }
}

// ── Incoming Payments ─────────────────────────────────────
const showIncomingForm  = ref(false)
const editingIncomingId = ref(null)
const savingIncoming    = ref(false)
const incomingForm = ref({
  groupName:'', country:'', pilgrimCount:0,
  amountExpected:'', amountReceived:'0', currency:'GBP',
  receivedDate:'', contactName:'', contactPhone:'', notes:''
})

function openAddIncoming() {
  editingIncomingId.value = null
  incomingForm.value = { groupName:'', country:'', pilgrimCount:0, amountExpected:'', amountReceived:'0', currency:'GBP', receivedDate:'', contactName:'', contactPhone:'', notes:'' }
  showIncomingForm.value = true
}

function openEditIncoming(x) {
  editingIncomingId.value = x.id
  incomingForm.value = {
    groupName: x.groupName, country: x.country, pilgrimCount: x.pilgrimCount,
    amountExpected: x.amountExpected, amountReceived: x.amountReceived,
    currency: x.currency, receivedDate: x.receivedDate?.slice(0,10) || '',
    contactName: x.contactName || '', contactPhone: x.contactPhone || '', notes: x.notes || ''
  }
  showIncomingForm.value = true
}

async function saveIncoming() {
  if (!incomingForm.value.groupName || !incomingForm.value.amountExpected) return notify('error', 'Group name and expected amount required.')
  savingIncoming.value = true
  try {
    const p = { ...incomingForm.value,
      amountExpected: parseFloat(incomingForm.value.amountExpected),
      amountReceived: parseFloat(incomingForm.value.amountReceived) || 0,
      pilgrimCount: parseInt(incomingForm.value.pilgrimCount) || 0,
      receivedDate: incomingForm.value.receivedDate ? new Date(incomingForm.value.receivedDate).toISOString() : null,
    }
    if (editingIncomingId.value) await internationalApi.updateIncoming(editingIncomingId.value, p)
    else await internationalApi.addIncoming(p)
    notify('success', editingIncomingId.value ? 'Updated.' : 'Payment record added.')
    showIncomingForm.value = false
    await loadAll()
  } catch { notify('error', 'Failed.')
  } finally { savingIncoming.value = false }
}

async function deleteIncoming(id) {
  if (!confirm('Delete?')) return
  try { await internationalApi.deleteIncoming(id); notify('success', 'Deleted.'); await loadAll() }
  catch { notify('error', 'Delete failed.') }
}

// ── Vendor Contracts ──────────────────────────────────────
const showContractForm  = ref(false)
const editingContractId = ref(null)
const savingContract    = ref(false)
const contractForm = ref({
  vendorName:'', country:'Iraq', serviceType:'Hotel', description:'',
  totalAmount:'', paidAmount:'0', currency:'USD',
  contractDate: new Date().toISOString().slice(0,10),
  status:'Active', contactName:'', contactPhone:'', notes:''
})

const serviceTypes = ['Hotel','Transport','Visa','Food','Medical','Guide','Other']
const contractStatuses = ['Active','Completed','Cancelled']

function openAddContract() {
  editingContractId.value = null
  contractForm.value = { vendorName:'', country:'Iraq', serviceType:'Hotel', description:'', totalAmount:'', paidAmount:'0', currency:'USD', contractDate: new Date().toISOString().slice(0,10), status:'Active', contactName:'', contactPhone:'', notes:'' }
  showContractForm.value = true
}

function openEditContract(x) {
  editingContractId.value = x.id
  contractForm.value = {
    vendorName: x.vendorName, country: x.country, serviceType: x.serviceType,
    description: x.description || '', totalAmount: x.totalAmount, paidAmount: x.paidAmount,
    currency: x.currency, contractDate: x.contractDate?.slice(0,10) || new Date().toISOString().slice(0,10),
    status: x.status, contactName: x.contactName || '', contactPhone: x.contactPhone || '', notes: x.notes || ''
  }
  showContractForm.value = true
}

async function saveContract() {
  if (!contractForm.value.vendorName || !contractForm.value.totalAmount) return notify('error', 'Vendor name and total amount required.')
  savingContract.value = true
  try {
    const p = { ...contractForm.value,
      totalAmount: parseFloat(contractForm.value.totalAmount),
      paidAmount: parseFloat(contractForm.value.paidAmount) || 0,
      contractDate: new Date(contractForm.value.contractDate).toISOString(),
    }
    if (editingContractId.value) await internationalApi.updateContract(editingContractId.value, p)
    else await internationalApi.addContract(p)
    notify('success', editingContractId.value ? 'Updated.' : 'Contract added.')
    showContractForm.value = false
    await loadAll()
  } catch { notify('error', 'Failed.')
  } finally { savingContract.value = false }
}

async function deleteContract(id) {
  if (!confirm('Delete?')) return
  try { await internationalApi.deleteContract(id); notify('success', 'Deleted.'); await loadAll() }
  catch { notify('error', 'Delete failed.') }
}

// ── Money Transfers ───────────────────────────────────────
const showTransferForm  = ref(false)
const editingTransferId = ref(null)
const savingTransfer    = ref(false)
const transferForm = ref({
  sentFrom:'', sentTo:'', amountSent:'', sentCurrency:'GBP',
  amountReceived:'', receivedCurrency:'USD', rateUsed:'',
  method:'Hawala', transferDate: new Date().toISOString().slice(0,10),
  status:'Pending', referenceNumber:'', notes:''
})

const transferMethods  = ['Hawala','Wire Transfer','Cash','Crypto','Other']
const transferStatuses = ['Pending','Completed','Failed']

function openAddTransfer() {
  editingTransferId.value = null
  transferForm.value = { sentFrom:'', sentTo:'', amountSent:'', sentCurrency:'GBP', amountReceived:'', receivedCurrency:'USD', rateUsed:'', method:'Hawala', transferDate: new Date().toISOString().slice(0,10), status:'Pending', referenceNumber:'', notes:'' }
  showTransferForm.value = true
}

function openEditTransfer(x) {
  editingTransferId.value = x.id
  transferForm.value = {
    sentFrom: x.sentFrom, sentTo: x.sentTo,
    amountSent: x.amountSent, sentCurrency: x.sentCurrency,
    amountReceived: x.amountReceived, receivedCurrency: x.receivedCurrency,
    rateUsed: x.rateUsed, method: x.method,
    transferDate: x.transferDate?.slice(0,10) || new Date().toISOString().slice(0,10),
    status: x.status, referenceNumber: x.referenceNumber || '', notes: x.notes || ''
  }
  showTransferForm.value = true
}

async function saveTransfer() {
  if (!transferForm.value.sentFrom || !transferForm.value.amountSent) return notify('error', 'From and amount required.')
  savingTransfer.value = true
  try {
    const p = { ...transferForm.value,
      amountSent: parseFloat(transferForm.value.amountSent),
      amountReceived: parseFloat(transferForm.value.amountReceived) || 0,
      rateUsed: parseFloat(transferForm.value.rateUsed) || 0,
      transferDate: new Date(transferForm.value.transferDate).toISOString(),
    }
    if (editingTransferId.value) await internationalApi.updateTransfer(editingTransferId.value, p)
    else await internationalApi.addTransfer(p)
    notify('success', editingTransferId.value ? 'Updated.' : 'Transfer added.')
    showTransferForm.value = false
    await loadAll()
  } catch { notify('error', 'Failed.')
  } finally { savingTransfer.value = false }
}

async function deleteTransfer(id) {
  if (!confirm('Delete?')) return
  try { await internationalApi.deleteTransfer(id); notify('success', 'Deleted.'); await loadAll() }
  catch { notify('error', 'Delete failed.') }
}

onMounted(loadAll)
</script>

<template>
  <div class="space-y-6">

    <!-- Header -->
    <div class="flex items-center justify-between flex-wrap gap-3">
      <div>
        <h1 class="text-2xl font-bold text-gray-900">🌍 International Dealings</h1>
        <p class="text-gray-700 text-sm mt-0.5">Pilgrim payments · Vendor contracts · Money transfers · Currency balance</p>
      </div>
    </div>

    <!-- Alert -->
    <div v-if="msg.text"
         :class="msg.type==='success' ? 'bg-green-100 border-green-300 text-green-700' : 'bg-red-100 border-red-300 text-red-600'"
         class="border rounded-lg px-4 py-3 text-sm flex justify-between">
      {{ msg.text }}<button @click="msg.text=''" class="opacity-60 hover:opacity-100">✕</button>
    </div>

    <!-- Tabs -->
    <div class="flex gap-1 bg-gray-100 p-1 rounded-xl w-fit flex-wrap">
      <button v-for="t in [{k:'balance',l:'⚖️ Balance Sheet'},{k:'incoming',l:'💵 Incoming'},{k:'contracts',l:'📄 Contracts'},{k:'transfers',l:'🔁 Transfers'}]"
              :key="t.k" @click="activeTab=t.k"
              :class="['px-4 py-2 rounded-lg text-sm font-semibold transition-all', activeTab===t.k ? 'bg-amber-600 text-white' : 'text-gray-700 hover:text-gray-900']">
        {{ t.l }}
      </button>
    </div>

    <!-- ── BALANCE SHEET ──────────────────────────────────── -->
    <template v-if="activeTab==='balance'">
      <div v-if="balance">
        <div class="card border border-gray-200 mb-4 flex items-center justify-between flex-wrap gap-2 px-6 py-4">
          <div>
            <p class="text-gray-600 text-xs uppercase tracking-wider">Grand Net Position (all currencies in PKR)</p>
            <p class="text-3xl font-bold mt-1" :class="balance.grandNetPkr >= 0 ? 'text-amber-700' : 'text-red-500'">
              {{ fmt(Math.abs(balance.grandNetPkr)) }} PKR
            </p>
            <p class="text-gray-600 text-xs mt-0.5">{{ balance.grandNetPkr >= 0 ? 'Net positive — more received than spent' : 'Net negative — more spent than received' }}</p>
          </div>
          <div class="text-5xl">{{ balance.grandNetPkr >= 0 ? '📈' : '📉' }}</div>
        </div>

        <div v-if="!balance.balance?.length" class="card text-center py-10 text-gray-700">
          No international transactions yet. Add incoming payments, contracts or transfers to see balance.
        </div>

        <div v-else class="overflow-x-auto rounded-xl border border-gray-200">
          <table class="w-full text-sm">
            <thead>
              <tr class="border-b border-gray-200 bg-gray-50">
                <th class="text-left px-4 py-3 text-amber-700 font-semibold text-xs uppercase">Currency</th>
                <th class="text-right px-4 py-3 text-amber-700 font-semibold text-xs uppercase">Total In</th>
                <th class="text-right px-4 py-3 text-amber-700 font-semibold text-xs uppercase">Total Out</th>
                <th class="text-right px-4 py-3 text-amber-700 font-semibold text-xs uppercase">Net Balance</th>
                <th class="text-right px-4 py-3 text-amber-700 font-semibold text-xs uppercase">≈ PKR</th>
                <th class="text-right px-4 py-3 text-amber-700 font-semibold text-xs uppercase">Rate / PKR</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="b in balance.balance" :key="b.currency"
                  class="border-b border-gray-200 hover:bg-gray-50">
                <td class="px-4 py-3">
                  <span class="text-gray-900 font-bold text-base">{{ b.currency }}</span>
                </td>
                <td class="px-4 py-3 text-right text-amber-700 font-semibold">{{ fmt(b.totalIn) }}</td>
                <td class="px-4 py-3 text-right text-red-500 font-semibold">{{ fmt(b.totalOut) }}</td>
                <td class="px-4 py-3 text-right font-bold text-lg" :class="b.net >= 0 ? 'text-amber-700' : 'text-orange-600'">
                  {{ b.net >= 0 ? '+' : '' }}{{ fmt(b.net) }}
                </td>
                <td class="px-4 py-3 text-right" :class="b.netPkr >= 0 ? 'text-amber-600' : 'text-orange-600'">
                  {{ fmt(b.netPkr) }}
                </td>
                <td class="px-4 py-3 text-right text-gray-600 text-xs">
                  {{ b.currency === 'PKR' ? '1.00' : (b.rate ? fmt(b.rate) : '—') }}
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>
    </template>

    <!-- ── INCOMING PAYMENTS ──────────────────────────────── -->
    <template v-if="activeTab==='incoming'">
      <div v-if="incoming" class="grid grid-cols-1 sm:grid-cols-3 gap-4">
        <div class="card border border-blue-200 text-center py-4">
          <p class="text-gray-600 text-xs uppercase mb-1">Expected Total</p>
          <p class="text-2xl font-bold text-blue-600">{{ fmt(incoming.totalExpectedPkr) }}</p>
          <p class="text-gray-600 text-xs">PKR equiv.</p>
        </div>
        <div class="card border border-emerald-200 text-center py-4">
          <p class="text-gray-600 text-xs uppercase mb-1">✅ Received</p>
          <p class="text-2xl font-bold text-amber-700">{{ fmt(incoming.totalReceivedPkr) }}</p>
          <p class="text-gray-600 text-xs">PKR equiv.</p>
        </div>
        <div class="card text-center py-4" :class="incoming.totalPendingPkr > 0 ? 'border border-orange-200' : 'border border-gray-200'">
          <p class="text-gray-600 text-xs uppercase mb-1">⏳ Still Pending</p>
          <p class="text-2xl font-bold" :class="incoming.totalPendingPkr > 0 ? 'text-orange-600' : 'text-gray-600'">{{ fmt(incoming.totalPendingPkr) }}</p>
          <p class="text-gray-600 text-xs">PKR equiv.</p>
        </div>
      </div>

      <div class="flex justify-end">
        <button @click="openAddIncoming" class="flex items-center gap-2 px-4 py-2 rounded-xl text-sm font-semibold text-gray-900" style="background:rgba(180,83,9,0.7);border:1px solid rgba(180,83,9,0.5);">
          + Add Group Payment
        </button>
      </div>

      <div v-if="!incoming?.payments?.length" class="card text-center py-12">
        <div class="text-5xl mb-3">💵</div>
        <p class="text-gray-900 font-semibold">No incoming payments yet</p>
        <p class="text-gray-700 text-sm mt-1">Track UK groups (GBP), Pakistani groups (PKR) etc.</p>
      </div>

      <div v-else class="overflow-x-auto rounded-xl border border-gray-200">
        <table class="w-full text-sm">
          <thead>
            <tr class="border-b border-gray-200 bg-gray-50">
              <th class="text-left px-4 py-3 text-amber-700 font-semibold text-xs uppercase">Group / Country</th>
              <th class="text-right px-4 py-3 text-amber-700 font-semibold text-xs uppercase">Expected</th>
              <th class="text-right px-4 py-3 text-amber-700 font-semibold text-xs uppercase">Received</th>
              <th class="text-right px-4 py-3 text-amber-700 font-semibold text-xs uppercase">Remaining</th>
              <th class="text-left px-4 py-3 text-amber-700 font-semibold text-xs uppercase">Status</th>
              <th class="px-4 py-3"></th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="x in incoming.payments" :key="x.id" class="border-b border-gray-200 hover:bg-gray-50">
              <td class="px-4 py-3">
                <p class="text-gray-900 font-medium">{{ x.groupName }}</p>
                <p class="text-gray-600 text-xs">🌍 {{ x.country }} · {{ x.pilgrimCount }} pilgrims</p>
                <p v-if="x.contactName" class="text-gray-600 text-xs">{{ x.contactName }}</p>
                <a v-if="x.contactPhone" :href="`https://wa.me/${x.contactPhone.replace(/[^\d]/g,'')}`" target="_blank" class="text-green-600 text-xs hover:underline">💬 {{ x.contactPhone }}</a>
              </td>
              <td class="px-4 py-3 text-right">
                <p class="text-blue-600 font-semibold">{{ fmt(x.amountExpected) }} {{ x.currency }}</p>
                <p class="text-gray-600 text-xs">≈ {{ fmt(x.amountExpectedPkr) }} PKR</p>
              </td>
              <td class="px-4 py-3 text-right">
                <p class="text-amber-700 font-semibold">{{ fmt(x.amountReceived) }} {{ x.currency }}</p>
                <p class="text-gray-600 text-xs">≈ {{ fmt(x.amountReceivedPkr) }} PKR</p>
              </td>
              <td class="px-4 py-3 text-right">
                <p :class="x.remaining > 0 ? 'text-orange-600' : 'text-amber-700'" class="font-semibold">{{ fmt(x.remaining) }} {{ x.currency }}</p>
              </td>
              <td class="px-4 py-3">
                <span class="text-xs px-2 py-1 rounded-lg"
                      :class="{ 'bg-green-100 text-green-700': x.status==='Full', 'bg-orange-100 text-orange-600': x.status==='Partial', 'bg-gray-100 text-gray-700': x.status==='Pending' }">
                  {{ x.status==='Full'?'✅ Full':x.status==='Partial'?'⚡ Partial':'⏳ Pending' }}
                </span>
              </td>
              <td class="px-4 py-3">
                <div class="flex gap-1">
                  <button @click="openEditIncoming(x)" class="text-xs px-2 py-1 rounded border border-blue-300 text-blue-600 hover:bg-blue-50">✏️</button>
                  <button @click="deleteIncoming(x.id)" class="text-xs px-2 py-1 rounded border border-red-300 text-red-600 hover:bg-red-50">🗑</button>
                </div>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </template>

    <!-- ── VENDOR CONTRACTS ───────────────────────────────── -->
    <template v-if="activeTab==='contracts'">
      <div v-if="contracts" class="grid grid-cols-1 sm:grid-cols-3 gap-4">
        <div class="card border border-gray-200 text-center py-4">
          <p class="text-gray-600 text-xs uppercase mb-1">Total Contract Value</p>
          <p class="text-2xl font-bold text-gray-900">{{ fmt(contracts.totalValuePkr) }}</p>
          <p class="text-gray-600 text-xs">PKR equiv.</p>
        </div>
        <div class="card border border-emerald-200 text-center py-4">
          <p class="text-gray-600 text-xs uppercase mb-1">✅ Paid</p>
          <p class="text-2xl font-bold text-amber-700">{{ fmt(contracts.totalPaidPkr) }}</p>
          <p class="text-gray-600 text-xs">PKR equiv.</p>
        </div>
        <div class="card text-center py-4" :class="contracts.totalDuePkr > 0 ? 'border border-orange-200' : 'border border-gray-200'">
          <p class="text-gray-600 text-xs uppercase mb-1">⏳ Still Due</p>
          <p class="text-2xl font-bold" :class="contracts.totalDuePkr > 0 ? 'text-orange-600' : 'text-gray-600'">{{ fmt(contracts.totalDuePkr) }}</p>
          <p class="text-gray-600 text-xs">PKR equiv.</p>
        </div>
      </div>

      <div class="flex justify-end">
        <button @click="openAddContract" class="flex items-center gap-2 px-4 py-2 rounded-xl text-sm font-semibold text-gray-900" style="background:rgba(180,83,9,0.7);border:1px solid rgba(180,83,9,0.5);">
          + Add Contract
        </button>
      </div>

      <div v-if="!contracts?.contracts?.length" class="card text-center py-12">
        <div class="text-5xl mb-3">📄</div>
        <p class="text-gray-900 font-semibold">No contracts yet</p>
        <p class="text-gray-700 text-sm mt-1">Add Iraqi hotel deals, transport company contracts etc.</p>
      </div>

      <div v-else class="overflow-x-auto rounded-xl border border-gray-200">
        <table class="w-full text-sm">
          <thead>
            <tr class="border-b border-gray-200 bg-gray-50">
              <th class="text-left px-4 py-3 text-amber-700 font-semibold text-xs uppercase">Vendor</th>
              <th class="text-left px-4 py-3 text-amber-700 font-semibold text-xs uppercase">Service</th>
              <th class="text-right px-4 py-3 text-amber-700 font-semibold text-xs uppercase">Total</th>
              <th class="text-right px-4 py-3 text-amber-700 font-semibold text-xs uppercase">Paid</th>
              <th class="text-right px-4 py-3 text-amber-700 font-semibold text-xs uppercase">Due</th>
              <th class="text-left px-4 py-3 text-amber-700 font-semibold text-xs uppercase">Status</th>
              <th class="px-4 py-3"></th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="x in contracts.contracts" :key="x.id" class="border-b border-gray-200 hover:bg-gray-50">
              <td class="px-4 py-3">
                <p class="text-gray-900 font-medium">{{ x.vendorName }}</p>
                <p class="text-gray-600 text-xs">🌍 {{ x.country }}</p>
                <a v-if="x.contactPhone" :href="`https://wa.me/${x.contactPhone.replace(/[^\d]/g,'')}`" target="_blank" class="text-green-600 text-xs hover:underline">💬 {{ x.contactPhone }}</a>
              </td>
              <td class="px-4 py-3">
                <span class="text-xs px-2 py-1 rounded bg-gray-100 text-gray-600">{{ x.serviceType }}</span>
                <p v-if="x.description" class="text-gray-600 text-xs mt-1 truncate max-w-36">{{ x.description }}</p>
              </td>
              <td class="px-4 py-3 text-right">
                <p class="text-gray-900 font-semibold">{{ fmt(x.totalAmount) }} {{ x.currency }}</p>
                <p class="text-gray-600 text-xs">≈ {{ fmt(x.totalAmountPkr) }} PKR</p>
              </td>
              <td class="px-4 py-3 text-right text-amber-700 font-semibold">{{ fmt(x.paidAmount) }}</td>
              <td class="px-4 py-3 text-right" :class="x.remaining > 0 ? 'text-orange-600' : 'text-amber-700'">{{ fmt(x.remaining) }}</td>
              <td class="px-4 py-3">
                <span class="text-xs px-2 py-1 rounded-lg"
                      :class="{ 'bg-green-100 text-green-700': x.status==='Completed', 'bg-blue-100 text-blue-700': x.status==='Active', 'bg-red-100 text-red-600': x.status==='Cancelled' }">
                  {{ x.status }}
                </span>
              </td>
              <td class="px-4 py-3">
                <div class="flex gap-1">
                  <button @click="openEditContract(x)" class="text-xs px-2 py-1 rounded border border-blue-300 text-blue-600 hover:bg-blue-50">✏️</button>
                  <button @click="deleteContract(x.id)" class="text-xs px-2 py-1 rounded border border-red-300 text-red-600 hover:bg-red-50">🗑</button>
                </div>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </template>

    <!-- ── MONEY TRANSFERS ────────────────────────────────── -->
    <template v-if="activeTab==='transfers'">
      <div v-if="transfers" class="grid grid-cols-2 sm:grid-cols-4 gap-4">
        <div class="card border border-gray-200 text-center py-4">
          <p class="text-gray-600 text-xs uppercase mb-1">Total Sent</p>
          <p class="text-xl font-bold text-gray-900">{{ fmt(transfers.totalSentPkr) }}</p>
          <p class="text-gray-600 text-xs">PKR</p>
        </div>
        <div class="card border border-gray-200 text-center py-4">
          <p class="text-gray-600 text-xs uppercase mb-1">Total</p>
          <p class="text-xl font-bold text-gray-900">{{ transfers.totalCount }}</p>
          <p class="text-gray-600 text-xs">transfers</p>
        </div>
        <div class="card border border-emerald-200 text-center py-4">
          <p class="text-gray-600 text-xs uppercase mb-1">✅ Completed</p>
          <p class="text-xl font-bold text-amber-700">{{ transfers.completedCount }}</p>
        </div>
        <div class="card text-center py-4" :class="transfers.pendingCount > 0 ? 'border border-orange-200' : 'border border-gray-200'">
          <p class="text-gray-600 text-xs uppercase mb-1">⏳ Pending</p>
          <p class="text-xl font-bold" :class="transfers.pendingCount > 0 ? 'text-orange-600' : 'text-gray-600'">{{ transfers.pendingCount }}</p>
        </div>
      </div>

      <div class="flex justify-end">
        <button @click="openAddTransfer" class="flex items-center gap-2 px-4 py-2 rounded-xl text-sm font-semibold text-gray-900" style="background:rgba(180,83,9,0.7);border:1px solid rgba(180,83,9,0.5);">
          + Add Transfer
        </button>
      </div>

      <div v-if="!transfers?.transfers?.length" class="card text-center py-12">
        <div class="text-5xl mb-3">🔁</div>
        <p class="text-gray-900 font-semibold">No transfers yet</p>
        <p class="text-gray-700 text-sm mt-1">Track hawala, wire transfers, cash movements between countries</p>
      </div>

      <div v-else class="overflow-x-auto rounded-xl border border-gray-200">
        <table class="w-full text-sm">
          <thead>
            <tr class="border-b border-gray-200 bg-gray-50">
              <th class="text-left px-4 py-3 text-amber-700 font-semibold text-xs uppercase">From → To</th>
              <th class="text-left px-4 py-3 text-amber-700 font-semibold text-xs uppercase">Method</th>
              <th class="text-right px-4 py-3 text-amber-700 font-semibold text-xs uppercase">Sent</th>
              <th class="text-right px-4 py-3 text-amber-700 font-semibold text-xs uppercase">Received</th>
              <th class="text-right px-4 py-3 text-amber-700 font-semibold text-xs uppercase">Rate</th>
              <th class="text-left px-4 py-3 text-amber-700 font-semibold text-xs uppercase">Date</th>
              <th class="text-left px-4 py-3 text-amber-700 font-semibold text-xs uppercase">Status</th>
              <th class="px-4 py-3"></th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="x in transfers.transfers" :key="x.id" class="border-b border-gray-200 hover:bg-gray-50">
              <td class="px-4 py-3">
                <p class="text-gray-900 text-xs font-medium">{{ x.sentFrom }}</p>
                <p class="text-gray-600 text-xs">→ {{ x.sentTo }}</p>
                <p v-if="x.referenceNumber" class="text-gray-600 text-xs">Ref: {{ x.referenceNumber }}</p>
              </td>
              <td class="px-4 py-3"><span class="text-xs px-2 py-1 rounded bg-gray-100 text-gray-600">{{ x.method }}</span></td>
              <td class="px-4 py-3 text-right font-semibold text-red-500">{{ fmt(x.amountSent) }} <span class="text-xs font-normal text-gray-600">{{ x.sentCurrency }}</span></td>
              <td class="px-4 py-3 text-right font-semibold text-amber-700">{{ fmt(x.amountReceived) }} <span class="text-xs font-normal text-gray-600">{{ x.receivedCurrency }}</span></td>
              <td class="px-4 py-3 text-right text-gray-600 text-xs">{{ x.rateUsed ? fmt(x.rateUsed) : '—' }}</td>
              <td class="px-4 py-3 text-gray-600 text-xs whitespace-nowrap">{{ new Date(x.transferDate).toLocaleDateString('en-GB') }}</td>
              <td class="px-4 py-3">
                <span class="text-xs px-2 py-1 rounded-lg"
                      :class="{ 'bg-green-100 text-green-700': x.status==='Completed', 'bg-orange-100 text-orange-600': x.status==='Pending', 'bg-red-100 text-red-600': x.status==='Failed' }">
                  {{ x.status }}
                </span>
              </td>
              <td class="px-4 py-3">
                <div class="flex gap-1">
                  <button @click="openEditTransfer(x)" class="text-xs px-2 py-1 rounded border border-blue-300 text-blue-600 hover:bg-blue-50">✏️</button>
                  <button @click="deleteTransfer(x.id)" class="text-xs px-2 py-1 rounded border border-red-300 text-red-600 hover:bg-red-50">🗑</button>
                </div>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </template>

    <!-- ── Incoming Payment Modal ──────────────────────── -->
    <Teleport to="body">
      <div v-if="showIncomingForm" class="fixed inset-0 z-50 flex items-center justify-center p-4" style="background:rgba(0,0,0,0.85);" @click.self="showIncomingForm=false">
        <div class="w-full max-w-lg rounded-2xl overflow-hidden shadow-2xl" style="background:#0f172a;border:1px solid rgba(180,83,9,0.35);">
          <div class="h-0.5 w-full" style="background:linear-gradient(90deg,transparent,#d97706,transparent)"></div>
          <div class="px-6 py-4 border-b border-slate-700 flex items-center justify-between">
            <h3 class="text-white font-bold text-lg">💵 {{ editingIncomingId ? 'Edit' : 'Add' }} Incoming Payment</h3>
            <button @click="showIncomingForm=false" class="text-slate-400 hover:text-white text-2xl">✕</button>
          </div>
          <div class="p-6 space-y-4">
            <div class="grid grid-cols-2 gap-3">
              <div><label class="form-label">Group Name *</label><input v-model="incomingForm.groupName" class="input w-full" placeholder="e.g. London Shia Group" /></div>
              <div><label class="form-label">Country *</label><input v-model="incomingForm.country" class="input w-full" placeholder="e.g. UK, Pakistan" /></div>
            </div>
            <div class="grid grid-cols-2 gap-3">
              <div><label class="form-label">Pilgrim Count</label><input v-model="incomingForm.pilgrimCount" type="number" min="0" class="input w-full" /></div>
              <div><label class="form-label">Currency</label>
                <select v-model="incomingForm.currency" class="input w-full">
                  <option v-for="c in currencies" :key="c.code" :value="c.code">{{ c.code }} — {{ c.name }}</option>
                </select>
              </div>
            </div>
            <div class="grid grid-cols-2 gap-3">
              <div><label class="form-label">Amount Expected *</label><input v-model="incomingForm.amountExpected" type="number" min="0" class="input w-full" placeholder="Total amount" /></div>
              <div><label class="form-label">Amount Received</label><input v-model="incomingForm.amountReceived" type="number" min="0" class="input w-full" placeholder="0 if not yet" /></div>
            </div>
            <div class="grid grid-cols-2 gap-3">
              <div><label class="form-label">Contact Name</label><input v-model="incomingForm.contactName" class="input w-full" placeholder="Group coordinator" /></div>
              <div><label class="form-label">WhatsApp</label><input v-model="incomingForm.contactPhone" class="input w-full" placeholder="+44..." /></div>
            </div>
            <div><label class="form-label">Date Received</label><input v-model="incomingForm.receivedDate" type="date" class="input w-full" /></div>
            <div><label class="form-label">Notes</label><textarea v-model="incomingForm.notes" class="input w-full" rows="2"></textarea></div>
            <div class="flex gap-3">
              <button @click="saveIncoming" :disabled="savingIncoming" class="flex-1 py-2.5 rounded-xl text-sm font-semibold text-gray-900 disabled:opacity-40" style="background:rgba(180,83,9,0.7);">{{ savingIncoming ? 'Saving...' : 'Save' }}</button>
              <button @click="showIncomingForm=false" class="btn-outline px-5">Cancel</button>
            </div>
          </div>
        </div>
      </div>
    </Teleport>

    <!-- ── Vendor Contract Modal ───────────────────────── -->
    <Teleport to="body">
      <div v-if="showContractForm" class="fixed inset-0 z-50 flex items-center justify-center p-4" style="background:rgba(0,0,0,0.85);" @click.self="showContractForm=false">
        <div class="w-full max-w-lg rounded-2xl overflow-hidden shadow-2xl" style="background:#0f172a;border:1px solid rgba(59,130,246,0.35);">
          <div class="h-0.5 w-full" style="background:linear-gradient(90deg,transparent,#3b82f6,transparent)"></div>
          <div class="px-6 py-4 border-b border-slate-700 flex items-center justify-between">
            <h3 class="text-white font-bold text-lg">📄 {{ editingContractId ? 'Edit' : 'Add' }} Vendor Contract</h3>
            <button @click="showContractForm=false" class="text-slate-400 hover:text-white text-2xl">✕</button>
          </div>
          <div class="p-6 space-y-4">
            <div class="grid grid-cols-2 gap-3">
              <div><label class="form-label">Vendor Name *</label><input v-model="contractForm.vendorName" class="input w-full" placeholder="e.g. Hotel Al-Kawthar" /></div>
              <div><label class="form-label">Country</label><input v-model="contractForm.country" class="input w-full" placeholder="e.g. Iraq, Saudi Arabia" /></div>
            </div>
            <div class="grid grid-cols-2 gap-3">
              <div><label class="form-label">Service Type</label>
                <select v-model="contractForm.serviceType" class="input w-full">
                  <option v-for="s in serviceTypes" :key="s">{{ s }}</option>
                </select>
              </div>
              <div><label class="form-label">Status</label>
                <select v-model="contractForm.status" class="input w-full">
                  <option v-for="s in contractStatuses" :key="s">{{ s }}</option>
                </select>
              </div>
            </div>
            <div><label class="form-label">Description</label><input v-model="contractForm.description" class="input w-full" placeholder="e.g. 50 rooms × $60/night × 7 nights" /></div>
            <div class="grid grid-cols-3 gap-3">
              <div><label class="form-label">Total Amount *</label><input v-model="contractForm.totalAmount" type="number" min="0" class="input w-full" /></div>
              <div><label class="form-label">Paid So Far</label><input v-model="contractForm.paidAmount" type="number" min="0" class="input w-full" /></div>
              <div><label class="form-label">Currency</label>
                <select v-model="contractForm.currency" class="input w-full">
                  <option v-for="c in currencies" :key="c.code" :value="c.code">{{ c.code }}</option>
                </select>
              </div>
            </div>
            <div class="grid grid-cols-2 gap-3">
              <div><label class="form-label">Contact Name</label><input v-model="contractForm.contactName" class="input w-full" /></div>
              <div><label class="form-label">WhatsApp</label><input v-model="contractForm.contactPhone" class="input w-full" placeholder="+964..." /></div>
            </div>
            <div><label class="form-label">Contract Date</label><input v-model="contractForm.contractDate" type="date" class="input w-full" /></div>
            <div><label class="form-label">Notes</label><textarea v-model="contractForm.notes" class="input w-full" rows="2"></textarea></div>
            <div class="flex gap-3">
              <button @click="saveContract" :disabled="savingContract" class="flex-1 py-2.5 rounded-xl text-sm font-semibold text-gray-900 bg-blue-700 hover:bg-blue-600 disabled:opacity-40">{{ savingContract ? 'Saving...' : 'Save' }}</button>
              <button @click="showContractForm=false" class="btn-outline px-5">Cancel</button>
            </div>
          </div>
        </div>
      </div>
    </Teleport>

    <!-- ── Money Transfer Modal ────────────────────────── -->
    <Teleport to="body">
      <div v-if="showTransferForm" class="fixed inset-0 z-50 flex items-center justify-center p-4" style="background:rgba(0,0,0,0.85);" @click.self="showTransferForm=false">
        <div class="w-full max-w-lg rounded-2xl overflow-hidden shadow-2xl" style="background:#0f172a;border:1px solid rgba(212,168,0,0.35);">
          <div class="h-0.5 w-full" style="background:linear-gradient(90deg,transparent,#D4A800,transparent)"></div>
          <div class="px-6 py-4 border-b border-slate-700 flex items-center justify-between">
            <h3 class="text-white font-bold text-lg">🔁 {{ editingTransferId ? 'Edit' : 'Add' }} Money Transfer</h3>
            <button @click="showTransferForm=false" class="text-slate-400 hover:text-white text-2xl">✕</button>
          </div>
          <div class="p-6 space-y-4">
            <div class="grid grid-cols-2 gap-3">
              <div><label class="form-label">Sent From *</label><input v-model="transferForm.sentFrom" class="input w-full" placeholder="e.g. UK - Barclays" /></div>
              <div><label class="form-label">Sent To *</label><input v-model="transferForm.sentTo" class="input w-full" placeholder="e.g. Iraq - Najaf Agent" /></div>
            </div>
            <div class="grid grid-cols-3 gap-3">
              <div><label class="form-label">Amount Sent *</label><input v-model="transferForm.amountSent" type="number" min="0" class="input w-full" /></div>
              <div><label class="form-label">Currency</label>
                <select v-model="transferForm.sentCurrency" class="input w-full">
                  <option v-for="c in currencies" :key="c.code" :value="c.code">{{ c.code }}</option>
                </select>
              </div>
              <div><label class="form-label">Rate Used</label><input v-model="transferForm.rateUsed" type="number" min="0" step="0.0001" class="input w-full" placeholder="1 GBP = ? USD" /></div>
            </div>
            <div class="grid grid-cols-2 gap-3">
              <div><label class="form-label">Amount Received</label><input v-model="transferForm.amountReceived" type="number" min="0" class="input w-full" /></div>
              <div><label class="form-label">Received Currency</label>
                <select v-model="transferForm.receivedCurrency" class="input w-full">
                  <option v-for="c in currencies" :key="c.code" :value="c.code">{{ c.code }}</option>
                </select>
              </div>
            </div>
            <div class="grid grid-cols-3 gap-3">
              <div><label class="form-label">Method</label>
                <select v-model="transferForm.method" class="input w-full">
                  <option v-for="m in transferMethods" :key="m">{{ m }}</option>
                </select>
              </div>
              <div><label class="form-label">Status</label>
                <select v-model="transferForm.status" class="input w-full">
                  <option v-for="s in transferStatuses" :key="s">{{ s }}</option>
                </select>
              </div>
              <div><label class="form-label">Date</label><input v-model="transferForm.transferDate" type="date" class="input w-full" /></div>
            </div>
            <div><label class="form-label">Reference No.</label><input v-model="transferForm.referenceNumber" class="input w-full" placeholder="Transaction / hawala ref" /></div>
            <div><label class="form-label">Notes</label><textarea v-model="transferForm.notes" class="input w-full" rows="2"></textarea></div>
            <div class="flex gap-3">
              <button @click="saveTransfer" :disabled="savingTransfer" class="flex-1 py-2.5 rounded-xl text-sm font-semibold text-gray-900 bg-emerald-700 hover:bg-emerald-600 disabled:opacity-40">{{ savingTransfer ? 'Saving...' : 'Save' }}</button>
              <button @click="showTransferForm=false" class="btn-outline px-5">Cancel</button>
            </div>
          </div>
        </div>
      </div>
    </Teleport>

  </div>
</template>
