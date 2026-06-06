<script setup>
import { ref, onMounted } from 'vue'
import { reportsApi } from '@/api'
import * as XLSX from 'xlsx'
import jsPDF from 'jspdf'
import autoTable from 'jspdf-autotable'

const summary  = ref(null)
const loading  = ref(true)
const error    = ref('')
const exporting = ref('')

async function load() {
  try {
    const res = await reportsApi.getSummary()
    summary.value = res.data.data
  } catch {
    error.value = 'Failed to load report data.'
  } finally { loading.value = false }
}

// ─── Excel Export ─────────────────────────────────────────────
async function exportExcel(type) {
  exporting.value = type + '_excel'
  try {
    const res = type === 'pilgrims'
      ? await reportsApi.exportPilgrims()
      : await reportsApi.exportVolunteers()

    const rows = res.data.data
    const ws = XLSX.utils.json_to_sheet(rows)

    // Auto column widths
    const cols = Object.keys(rows[0] || {}).map(k => ({ wch: Math.max(k.length, 14) }))
    ws['!cols'] = cols

    const wb = XLSX.utils.book_new()
    XLSX.utils.book_append_sheet(wb, ws, type === 'pilgrims' ? 'Pilgrims' : 'Volunteers')

    const today = new Date().toISOString().slice(0, 10)
    XLSX.writeFile(wb, `Misbahuda_${type}_${today}.xlsx`)
  } catch {
    error.value = 'Excel export failed.'
  } finally { exporting.value = '' }
}

// ─── PDF Export ───────────────────────────────────────────────
async function exportPdf(type) {
  exporting.value = type + '_pdf'
  try {
    const res = type === 'pilgrims'
      ? await reportsApi.exportPilgrims()
      : await reportsApi.exportVolunteers()

    const rows = res.data.data
    const today = new Date().toLocaleDateString('en-GB', { day: '2-digit', month: 'short', year: 'numeric' })

    const doc = new jsPDF({ orientation: 'landscape', unit: 'mm', format: 'a4' })

    // Header
    doc.setFillColor(15, 23, 42)
    doc.rect(0, 0, 297, 22, 'F')
    doc.setTextColor(217, 119, 6)
    doc.setFontSize(14)
    doc.setFont('helvetica', 'bold')
    doc.text('Misbah ul Hoda — Arbaeen 2026', 14, 10)
    doc.setFontSize(9)
    doc.setTextColor(148, 163, 184)
    doc.text(`${type === 'pilgrims' ? 'Pilgrims' : 'Volunteers'} Report  |  Generated: ${today}`, 14, 17)
    doc.setTextColor(217, 119, 6)
    doc.text(`Total: ${rows.length}`, 260, 17)

    const columns = type === 'pilgrims'
      ? ['No','Full Name','Country','Passport','Status','Family','Arrival','Departure','Room','Bus','Registered']
      : ['No','Full Name','Phone','Status','Checked In','Area','Tasks Done','Last Check-in','Registered']

    const dataRows = type === 'pilgrims'
      ? rows.map(r => [r.No, r.FullName, r.Country, r.Passport, r.Status, r.Family, r.Arrival, r.Departure, r.Room, r.Bus, r.Registered])
      : rows.map(r => [r.No, r.FullName, r.Phone, r.Status, r.IsCheckedIn, r.Area, r.TasksCompleted, r.LastCheckIn, r.Registered])

    const statusColors = {
      'Approved':    [16, 185, 129],
      'Rejected':    [239, 68, 68],
      'Pending':     [217, 119, 6],
      'UnderReview': [59, 130, 246],
      'Cancelled':   [100, 116, 139],
      'Available':   [16, 185, 129],
      'Busy':        [245, 158, 11],
      'Offline':     [100, 116, 139],
    }

    autoTable(doc, {
      startY: 25,
      head: [columns],
      body: dataRows,
      theme: 'grid',
      styles: {
        fontSize: 7,
        cellPadding: 2,
        textColor: [30, 41, 59],
        lineColor: [203, 213, 225],
        lineWidth: 0.1,
      },
      headStyles: {
        fillColor: [15, 23, 42],
        textColor: [217, 119, 6],
        fontStyle: 'bold',
        fontSize: 7.5,
      },
      alternateRowStyles: { fillColor: [248, 250, 252] },
      didParseCell(data) {
        if (data.section === 'body') {
          const statusCol = type === 'pilgrims' ? 4 : 3
          if (data.column.index === statusCol) {
            const color = statusColors[data.cell.raw] || [30, 41, 59]
            data.cell.styles.textColor = color
            data.cell.styles.fontStyle = 'bold'
          }
        }
      },
      margin: { left: 14, right: 14 },
    })

    // Footer
    const pageCount = doc.internal.getNumberOfPages()
    for (let i = 1; i <= pageCount; i++) {
      doc.setPage(i)
      doc.setFontSize(7)
      doc.setTextColor(148, 163, 184)
      doc.text(`Page ${i} of ${pageCount}  |  Misbahuda System — Confidential`, 14, doc.internal.pageSize.height - 6)
    }

    const filename = `Misbahuda_${type}_${new Date().toISOString().slice(0, 10)}.pdf`
    doc.save(filename)
  } catch (e) {
    console.error(e)
    error.value = 'PDF export failed.'
  } finally { exporting.value = '' }
}

// ─── Summary PDF ──────────────────────────────────────────────
function exportSummaryPdf() {
  if (!summary.value) return
  exporting.value = 'summary_pdf'
  try {
    const s = summary.value
    const today = new Date().toLocaleDateString('en-GB', { day: '2-digit', month: 'short', year: 'numeric' })

    const doc = new jsPDF({ orientation: 'portrait', unit: 'mm', format: 'a4' })

    doc.setFillColor(15, 23, 42)
    doc.rect(0, 0, 210, 28, 'F')
    doc.setTextColor(217, 119, 6)
    doc.setFontSize(16)
    doc.setFont('helvetica', 'bold')
    doc.text('Misbah ul Hoda', 14, 12)
    doc.setFontSize(10)
    doc.setTextColor(148, 163, 184)
    doc.text('Arbaeen 2026 — System Summary Report', 14, 20)
    doc.setTextColor(100, 116, 139)
    doc.setFontSize(8)
    doc.text(`Generated: ${today}`, 150, 20)

    let y = 36

    const section = (title, rows) => {
      doc.setFontSize(10)
      doc.setFont('helvetica', 'bold')
      doc.setTextColor(217, 119, 6)
      doc.text(title, 14, y)
      y += 3
      autoTable(doc, {
        startY: y,
        head: [['Metric', 'Value']],
        body: rows,
        theme: 'grid',
        styles: { fontSize: 9, cellPadding: 3, textColor: [30, 41, 59] },
        headStyles: { fillColor: [30, 41, 59], textColor: [217, 119, 6], fontStyle: 'bold' },
        alternateRowStyles: { fillColor: [248, 250, 252] },
        columnStyles: { 0: { cellWidth: 100 }, 1: { cellWidth: 60, fontStyle: 'bold' } },
        margin: { left: 14, right: 14 },
      })
      y = doc.lastAutoTable.finalY + 10
    }

    section('Pilgrim Applications', [
      ['Total Registered', s.pilgrims.total],
      ['Pending Review', s.pilgrims.pending],
      ['Under Review', s.pilgrims.underReview],
      ['Approved', s.pilgrims.approved],
      ['Rejected', s.pilgrims.rejected],
      ['Cancelled', s.pilgrims.cancelled],
      ['Room Allocated', s.pilgrims.withRoom],
      ['Awaiting Room', s.pilgrims.withoutRoom],
      ['Bus Allocated', s.pilgrims.withBus],
      ['Awaiting Bus', s.pilgrims.withoutBus],
    ])

    section('Volunteers', [
      ['Total Volunteers', s.volunteers.total],
      ['Available', s.volunteers.available],
      ['Busy', s.volunteers.busy],
      ['Offline', s.volunteers.offline],
      ['Emergency Assigned', s.volunteers.emergencyAssigned],
      ['Currently Checked In', s.volunteers.checkedIn],
    ])

    section('Accommodation', [
      ['Total Rooms', s.accommodation.totalRooms],
      ['Available Rooms', s.accommodation.availableRooms],
      ['Occupied Rooms', s.accommodation.occupiedRooms],
      ['Total Beds', s.accommodation.totalBeds],
      ['Occupied Beds', s.accommodation.occupiedBeds],
      ['Occupancy Rate', `${s.accommodation.occupancyRate}%`],
    ])

    section('Transport', [
      ['Total Buses', s.transport.totalBuses],
      ['Active Buses', s.transport.activeBuses],
      ['Total Capacity', s.transport.totalCapacity],
      ['Current Passengers', s.transport.totalPassengers],
    ])

    section('System Users', [
      ['Total Users', s.users.total],
      ['Active Users', s.users.active],
      ['Inactive Users', s.users.inactive],
      ['Pilgrims', s.users.byRole.pilgrim],
      ['Volunteers', s.users.byRole.volunteer],
      ['Volunteer Managers', s.users.byRole.volunteerManager],
      ['Admins', s.users.byRole.admin],
      ['Drivers', s.users.byRole.driver],
    ])

    const pageCount = doc.internal.getNumberOfPages()
    for (let i = 1; i <= pageCount; i++) {
      doc.setPage(i)
      doc.setFontSize(7)
      doc.setTextColor(148, 163, 184)
      doc.text(`Page ${i} of ${pageCount}  |  Misbahuda System — Confidential`, 14, doc.internal.pageSize.height - 6)
    }

    doc.save(`Misbahuda_Summary_${new Date().toISOString().slice(0, 10)}.pdf`)
  } catch (e) {
    console.error(e)
    error.value = 'Summary PDF export failed.'
  } finally { exporting.value = '' }
}

onMounted(load)
</script>

<template>
  <div class="space-y-6">

    <!-- Header -->
    <div class="flex items-center justify-between flex-wrap gap-3">
      <div>
        <h1 class="text-2xl font-bold text-gray-900">Reports</h1>
        <p class="text-gray-700 text-sm mt-0.5">System statistics and data exports</p>
      </div>
      <div class="flex gap-2 flex-wrap">
        <button @click="exportSummaryPdf" :disabled="!summary || exporting === 'summary_pdf'"
                class="flex items-center gap-1.5 px-3 py-2 rounded-xl text-xs font-semibold border border-red-300 text-red-600 hover:bg-red-50 transition-colors disabled:opacity-50">
          {{ exporting === 'summary_pdf' ? '⏳ Generating...' : '📄 Summary PDF' }}
        </button>
        <button @click="exportExcel('pilgrims')" :disabled="exporting === 'pilgrims_excel'"
                class="flex items-center gap-1.5 px-3 py-2 rounded-xl text-xs font-semibold border border-emerald-300 text-amber-700 hover:bg-amber-50 transition-colors disabled:opacity-50">
          {{ exporting === 'pilgrims_excel' ? '⏳ Exporting...' : '📊 Pilgrims Excel' }}
        </button>
        <button @click="exportPdf('pilgrims')" :disabled="exporting === 'pilgrims_pdf'"
                class="flex items-center gap-1.5 px-3 py-2 rounded-xl text-xs font-semibold border border-red-300 text-red-600 hover:bg-red-50 transition-colors disabled:opacity-50">
          {{ exporting === 'pilgrims_pdf' ? '⏳ Generating...' : '📄 Pilgrims PDF' }}
        </button>
        <button @click="exportExcel('volunteers')" :disabled="exporting === 'volunteers_excel'"
                class="flex items-center gap-1.5 px-3 py-2 rounded-xl text-xs font-semibold border border-emerald-300 text-amber-700 hover:bg-amber-50 transition-colors disabled:opacity-50">
          {{ exporting === 'volunteers_excel' ? '⏳ Exporting...' : '📊 Volunteers Excel' }}
        </button>
        <button @click="exportPdf('volunteers')" :disabled="exporting === 'volunteers_pdf'"
                class="flex items-center gap-1.5 px-3 py-2 rounded-xl text-xs font-semibold border border-red-300 text-red-600 hover:bg-red-50 transition-colors disabled:opacity-50">
          {{ exporting === 'volunteers_pdf' ? '⏳ Generating...' : '📄 Volunteers PDF' }}
        </button>
      </div>
    </div>

    <div v-if="error" class="bg-red-100 border border-red-300 text-red-600 rounded-lg px-4 py-3 text-sm flex justify-between">
      {{ error }}<button @click="error=''" class="opacity-60 hover:opacity-100">✕</button>
    </div>

    <div v-if="loading" class="card text-center py-16 text-gray-700">Loading report data...</div>

    <template v-else-if="summary">

      <!-- ── Pilgrim Stats ───────────────────────────── -->
      <div>
        <h2 class="text-sm font-semibold text-gold-500 uppercase tracking-wider mb-3">Pilgrim Applications</h2>
        <div class="grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-5 gap-3">
          <div class="card text-center py-4 border border-gray-200">
            <div class="text-3xl font-bold text-gray-900">{{ summary.pilgrims.total }}</div>
            <div class="text-xs text-gray-600 mt-1">Total Registered</div>
          </div>
          <div class="card text-center py-4 border border-amber-200">
            <div class="text-3xl font-bold text-amber-600">{{ summary.pilgrims.pending }}</div>
            <div class="text-xs text-gray-600 mt-1">⏳ Pending</div>
          </div>
          <div class="card text-center py-4 border border-blue-200">
            <div class="text-3xl font-bold text-blue-600">{{ summary.pilgrims.underReview }}</div>
            <div class="text-xs text-gray-600 mt-1">🔍 Under Review</div>
          </div>
          <div class="card text-center py-4 border border-emerald-200">
            <div class="text-3xl font-bold text-amber-700">{{ summary.pilgrims.approved }}</div>
            <div class="text-xs text-gray-600 mt-1">✅ Approved</div>
          </div>
          <div class="card text-center py-4 border border-red-200">
            <div class="text-3xl font-bold text-red-500">{{ summary.pilgrims.rejected }}</div>
            <div class="text-xs text-gray-600 mt-1">❌ Rejected</div>
          </div>
        </div>
      </div>

      <!-- ── Room & Bus Allocation ───────────────────── -->
      <div class="grid sm:grid-cols-2 gap-4">
        <div class="card border border-gray-200">
          <h3 class="text-sm font-semibold text-gray-900 mb-4">🏨 Room Allocation</h3>
          <div class="space-y-3">
            <div class="flex items-center justify-between">
              <span class="text-gray-700 text-sm">Allocated</span>
              <span class="text-amber-700 font-bold text-lg">{{ summary.pilgrims.withRoom }}</span>
            </div>
            <div class="w-full bg-gray-200 rounded-full h-2.5">
              <div class="bg-emerald-500 h-2.5 rounded-full transition-all"
                   :style="`width: ${summary.pilgrims.total ? Math.round(summary.pilgrims.withRoom / summary.pilgrims.total * 100) : 0}%`"></div>
            </div>
            <div class="flex justify-between text-xs text-gray-600">
              <span>Without Room: {{ summary.pilgrims.withoutRoom }}</span>
              <span>{{ summary.pilgrims.total ? Math.round(summary.pilgrims.withRoom / summary.pilgrims.total * 100) : 0 }}%</span>
            </div>
            <div class="pt-2 border-t border-gray-200 grid grid-cols-3 gap-2 text-center">
              <div>
                <div class="text-gray-900 font-semibold">{{ summary.accommodation.totalRooms }}</div>
                <div class="text-xs text-gray-600">Total Rooms</div>
              </div>
              <div>
                <div class="text-amber-700 font-semibold">{{ summary.accommodation.availableRooms }}</div>
                <div class="text-xs text-gray-600">Available</div>
              </div>
              <div>
                <div class="text-red-500 font-semibold">{{ summary.accommodation.occupiedRooms }}</div>
                <div class="text-xs text-gray-600">Occupied</div>
              </div>
            </div>
          </div>
        </div>

        <div class="card border border-gray-200">
          <h3 class="text-sm font-semibold text-gray-900 mb-4">🚌 Bus Allocation</h3>
          <div class="space-y-3">
            <div class="flex items-center justify-between">
              <span class="text-gray-700 text-sm">Allocated</span>
              <span class="text-blue-600 font-bold text-lg">{{ summary.pilgrims.withBus }}</span>
            </div>
            <div class="w-full bg-gray-200 rounded-full h-2.5">
              <div class="bg-blue-500 h-2.5 rounded-full transition-all"
                   :style="`width: ${summary.pilgrims.total ? Math.round(summary.pilgrims.withBus / summary.pilgrims.total * 100) : 0}%`"></div>
            </div>
            <div class="flex justify-between text-xs text-gray-600">
              <span>Without Bus: {{ summary.pilgrims.withoutBus }}</span>
              <span>{{ summary.pilgrims.total ? Math.round(summary.pilgrims.withBus / summary.pilgrims.total * 100) : 0 }}%</span>
            </div>
            <div class="pt-2 border-t border-gray-200 grid grid-cols-3 gap-2 text-center">
              <div>
                <div class="text-gray-900 font-semibold">{{ summary.transport.totalBuses }}</div>
                <div class="text-xs text-gray-600">Total Buses</div>
              </div>
              <div>
                <div class="text-amber-700 font-semibold">{{ summary.transport.totalCapacity }}</div>
                <div class="text-xs text-gray-600">Capacity</div>
              </div>
              <div>
                <div class="text-amber-600 font-semibold">{{ summary.transport.totalPassengers }}</div>
                <div class="text-xs text-gray-600">Passengers</div>
              </div>
            </div>
          </div>
        </div>
      </div>

      <!-- ── Volunteer Stats ─────────────────────────── -->
      <div>
        <h2 class="text-sm font-semibold text-gold-500 uppercase tracking-wider mb-3">Volunteers</h2>
        <div class="grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-6 gap-3">
          <div class="card text-center py-4 border border-gray-200">
            <div class="text-3xl font-bold text-gray-900">{{ summary.volunteers.total }}</div>
            <div class="text-xs text-gray-600 mt-1">Total</div>
          </div>
          <div class="card text-center py-4 border border-emerald-200">
            <div class="text-3xl font-bold text-amber-700">{{ summary.volunteers.available }}</div>
            <div class="text-xs text-gray-600 mt-1">Available</div>
          </div>
          <div class="card text-center py-4 border border-amber-200">
            <div class="text-3xl font-bold text-amber-600">{{ summary.volunteers.busy }}</div>
            <div class="text-xs text-gray-600 mt-1">Busy</div>
          </div>
          <div class="card text-center py-4 border border-gray-200">
            <div class="text-3xl font-bold text-gray-600">{{ summary.volunteers.offline }}</div>
            <div class="text-xs text-gray-600 mt-1">Offline</div>
          </div>
          <div class="card text-center py-4 border border-red-200">
            <div class="text-3xl font-bold text-red-500">{{ summary.volunteers.emergencyAssigned }}</div>
            <div class="text-xs text-gray-600 mt-1">Emergency</div>
          </div>
          <div class="card text-center py-4 border border-blue-200">
            <div class="text-3xl font-bold text-blue-600">{{ summary.volunteers.checkedIn }}</div>
            <div class="text-xs text-gray-600 mt-1">Checked In</div>
          </div>
        </div>
      </div>

      <!-- ── Users by Role + Top Countries ─────────────── -->
      <div class="grid sm:grid-cols-2 gap-4">

        <div class="card border border-gray-200">
          <h3 class="text-sm font-semibold text-gray-900 mb-4">👥 Users by Role</h3>
          <div class="space-y-2">
            <div v-for="(count, role) in summary.users.byRole" :key="role"
                 class="flex items-center justify-between py-1.5 border-b border-gray-200 last:border-0">
              <span class="text-gray-600 text-sm capitalize">{{ role.replace(/([A-Z])/g, ' $1').trim() }}</span>
              <span class="font-bold text-gray-900 text-sm">{{ count }}</span>
            </div>
            <div class="flex items-center justify-between pt-2">
              <span class="text-amber-700 text-sm font-semibold">Total</span>
              <span class="font-bold text-amber-700">{{ summary.users.total }}</span>
            </div>
          </div>
        </div>

        <div class="card border border-gray-200">
          <h3 class="text-sm font-semibold text-gray-900 mb-4">🌍 Top Countries (Pilgrims)</h3>
          <div v-if="!summary.pilgrims.byCountry?.length" class="text-gray-700 text-sm text-center py-6">No data yet</div>
          <div class="space-y-2">
            <div v-for="item in summary.pilgrims.byCountry" :key="item.country"
                 class="flex items-center gap-3">
              <span class="text-gray-600 text-sm flex-1 truncate">{{ item.country }}</span>
              <div class="flex-1 bg-gray-200 rounded-full h-1.5">
                <div class="bg-amber-500 h-1.5 rounded-full"
                     :style="`width: ${summary.pilgrims.total ? Math.round(item.count / summary.pilgrims.total * 100) : 0}%`"></div>
              </div>
              <span class="text-gray-900 font-bold text-sm w-6 text-right">{{ item.count }}</span>
            </div>
          </div>
        </div>
      </div>

      <!-- ── Accommodation Summary ───────────────────── -->
      <div class="card border border-gray-200">
        <h3 class="text-sm font-semibold text-gray-900 mb-4">🏨 Accommodation Overview</h3>
        <div class="grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-6 gap-4 text-center">
          <div>
            <div class="text-2xl font-bold text-gray-900">{{ summary.accommodation.totalRooms }}</div>
            <div class="text-xs text-gray-600 mt-1">Total Rooms</div>
          </div>
          <div>
            <div class="text-2xl font-bold text-amber-700">{{ summary.accommodation.availableRooms }}</div>
            <div class="text-xs text-gray-600 mt-1">Available</div>
          </div>
          <div>
            <div class="text-2xl font-bold text-red-500">{{ summary.accommodation.occupiedRooms }}</div>
            <div class="text-xs text-gray-600 mt-1">Occupied</div>
          </div>
          <div>
            <div class="text-2xl font-bold text-blue-600">{{ summary.accommodation.totalBeds }}</div>
            <div class="text-xs text-gray-600 mt-1">Total Beds</div>
          </div>
          <div>
            <div class="text-2xl font-bold text-amber-600">{{ summary.accommodation.occupiedBeds }}</div>
            <div class="text-xs text-gray-600 mt-1">Occupied Beds</div>
          </div>
          <div>
            <div class="text-2xl font-bold text-purple-600">{{ summary.accommodation.occupancyRate }}%</div>
            <div class="text-xs text-gray-600 mt-1">Occupancy</div>
          </div>
        </div>
        <div class="mt-4">
          <div class="flex justify-between text-xs text-gray-600 mb-1">
            <span>Room Occupancy</span>
            <span>{{ summary.accommodation.occupancyRate }}%</span>
          </div>
          <div class="w-full bg-gray-200 rounded-full h-3">
            <div class="h-3 rounded-full transition-all"
                 :class="summary.accommodation.occupancyRate > 80 ? 'bg-red-500' : summary.accommodation.occupancyRate > 50 ? 'bg-amber-500' : 'bg-emerald-500'"
                 :style="`width: ${summary.accommodation.occupancyRate}%`"></div>
          </div>
        </div>
      </div>

      <!-- ── Generated At ────────────────────────────── -->
      <p class="text-xs text-gray-700 text-center">
        Report generated at {{ new Date(summary.generatedAt).toLocaleString('en-GB') }} UTC
      </p>

    </template>
  </div>
</template>
