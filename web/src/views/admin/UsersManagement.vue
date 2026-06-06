<script setup>
import { ref, onMounted } from 'vue'
import { adminApi } from '@/api'
import AppPagination from '@/components/AppPagination.vue'

const users = ref([])
const loading = ref(true)
const page = ref(1)
const total = ref(0)
const pageSize = 20
const search = ref('')

const selectedUser = ref(null)
const showDetail = ref(false)
const changingRole = ref(false)
const toggling = ref(null)
const msg = ref({ type: '', text: '' })

let searchTimer = null

const roleColors = {
  SuperAdmin: 'text-gold-300 bg-gold-900/50 border-gold-700',
  Admin:      'text-blue-300 bg-blue-900/50 border-blue-700',
  Pilgrim:    'text-primary-300 bg-primary-900/50 border-primary-700',
  Volunteer:  'text-green-300 bg-green-900/50 border-green-700',
  VolunteerManager: 'text-teal-300 bg-teal-900/50 border-teal-700',
  Driver:     'text-purple-300 bg-purple-900/50 border-purple-700',
}

const roleOptions = [
  { value: 5, label: '🕌 Pilgrim' },
  { value: 4, label: '🤝 Volunteer' },
  { value: 2, label: '🛡️ Admin' },
  { value: 3, label: '📋 Volunteer Manager' },
  { value: 6, label: '🚌 Driver' },
]

function notify(type, text) {
  msg.value = { type, text }
  setTimeout(() => msg.value = { type: '', text: '' }, 5000)
}

async function load() {
  loading.value = true
  try {
    const res = await adminApi.getUsers({ page: page.value, pageSize, search: search.value || undefined })
    users.value = res.data.data?.items || []
    total.value = res.data.data?.totalCount || 0
  } catch (e) {
    notify('error', e.response?.data?.message || 'Failed to load users.')
  } finally {
    loading.value = false
  }
}

function onSearchInput() {
  clearTimeout(searchTimer)
  searchTimer = setTimeout(() => { page.value = 1; load() }, 400)
}

function openDetail(user) {
  selectedUser.value = { ...user }
  showDetail.value = true
}

function closeDetail() {
  showDetail.value = false
  selectedUser.value = null
}

async function toggleActive(user) {
  if (toggling.value) return
  toggling.value = user.id
  try {
    await adminApi.toggleUserActive(user.id)
    // update locally
    const found = users.value.find(u => u.id === user.id)
    if (found) found.isActive = !found.isActive
    if (selectedUser.value?.id === user.id) selectedUser.value.isActive = !selectedUser.value.isActive
    notify('success', `${user.fullName} ${found?.isActive ? 'activated' : 'deactivated'}.`)
  } catch (e) {
    notify('error', e.response?.data?.message || 'Failed to update status.')
  } finally {
    toggling.value = null
  }
}

async function changeRole(roleValue) {
  if (!selectedUser.value || changingRole.value) return
  changingRole.value = true
  try {
    await adminApi.changeRole(selectedUser.value.id, roleValue)
    const label = roleOptions.find(r => r.value === roleValue)?.label?.replace(/.*\s/, '') || String(roleValue)
    notify('success', `${selectedUser.value.fullName} role changed to ${label}.`)
    closeDetail()
    await load()
  } catch (e) {
    notify('error', e.response?.data?.message || 'Role change failed.')
  } finally {
    changingRole.value = false
  }
}

onMounted(load)
</script>

<template>
  <div class="space-y-5">

    <!-- Header -->
    <div class="flex items-center justify-between">
      <div>
        <h1 class="text-2xl font-bold text-white">Users Management</h1>
        <p class="text-slate-400 text-sm mt-0.5">{{ total }} total users</p>
      </div>
    </div>

    <!-- Alert -->
    <div v-if="msg.text"
         :class="msg.type === 'success' ? 'bg-emerald-900/50 border-emerald-700 text-emerald-300' : 'bg-red-900/50 border-red-700 text-red-300'"
         class="border rounded-lg px-4 py-3 text-sm flex justify-between items-center">
      {{ msg.text }}
      <button @click="msg.text = ''" class="ml-4 opacity-60 hover:opacity-100">✕</button>
    </div>

    <!-- Search -->
    <div class="flex gap-3">
      <input v-model="search" @input="onSearchInput" type="text" class="input w-72"
             placeholder="Search name, email, phone..." />
      <button @click="load" class="btn-outline px-4">↻ Refresh</button>
    </div>

    <!-- Table -->
    <div class="rounded-xl border border-dark-700 overflow-hidden">
      <div v-if="loading" class="py-12 text-center text-slate-400">Loading users...</div>
      <div v-else-if="!users.length" class="py-12 text-center text-slate-500">No users found.</div>

      <table v-else class="w-full text-sm">
        <thead style="background: rgba(180,83,9,0.08); border-bottom: 1px solid rgba(180,83,9,0.2);">
          <tr class="text-slate-400 text-xs uppercase tracking-wider">
            <th class="text-left px-4 py-3 font-medium">Name</th>
            <th class="text-left px-4 py-3 font-medium hidden md:table-cell">Email</th>
            <th class="text-left px-4 py-3 font-medium hidden lg:table-cell">Phone</th>
            <th class="text-left px-4 py-3 font-medium">Role</th>
            <th class="text-left px-4 py-3 font-medium">Status</th>
            <th class="text-left px-4 py-3 font-medium hidden lg:table-cell">Joined</th>
            <th class="px-4 py-3"></th>
          </tr>
        </thead>
        <tbody class="divide-y divide-dark-700">
          <tr v-for="user in users" :key="user.id" class="hover:bg-dark-800/40 transition-colors">

            <!-- Name -->
            <td class="px-4 py-3">
              <div class="flex items-center gap-2.5">
                <div class="w-8 h-8 rounded-full flex items-center justify-center text-xs font-bold text-white flex-shrink-0"
                     style="background: linear-gradient(135deg, #92400e, #451a03);">
                  {{ user.fullName?.[0]?.toUpperCase() }}
                </div>
                <div>
                  <p class="text-white font-medium">{{ user.fullName }}</p>
                  <p class="text-slate-500 text-xs md:hidden">{{ user.email }}</p>
                </div>
              </div>
            </td>

            <td class="px-4 py-3 text-slate-400 hidden md:table-cell">{{ user.email }}</td>
            <td class="px-4 py-3 text-slate-400 hidden lg:table-cell">{{ user.phoneNumber || '—' }}</td>

            <!-- Role -->
            <td class="px-4 py-3">
              <span :class="['text-xs px-2 py-0.5 rounded-full border font-medium', roleColors[user.role] || 'text-slate-400 bg-slate-800 border-slate-600']">
                {{ user.role }}
              </span>
            </td>

            <!-- Status -->
            <td class="px-4 py-3">
              <span :class="['text-xs px-2 py-0.5 rounded-full font-medium', user.isActive ? 'text-emerald-400 bg-emerald-900/40' : 'text-red-400 bg-red-900/40']">
                {{ user.isActive ? '● Active' : '○ Inactive' }}
              </span>
            </td>

            <td class="px-4 py-3 text-slate-500 text-xs hidden lg:table-cell">
              {{ new Date(user.createdAt).toLocaleDateString('en-GB') }}
            </td>

            <!-- Actions — each button stops its own propagation -->
            <td class="px-4 py-3">
              <div class="flex items-center justify-end gap-2">
                <button
                  @click.stop="openDetail(user)"
                  type="button"
                  class="text-xs px-3 py-1.5 rounded-lg border border-gold-800/60 text-gold-400 hover:bg-gold-900/30 transition-colors font-medium">
                  📋 Details
                </button>
                <button
                  @click.stop="toggleActive(user)"
                  type="button"
                  :disabled="toggling === user.id"
                  :class="['text-xs px-3 py-1.5 rounded-lg border transition-colors font-medium disabled:opacity-50',
                    user.isActive
                      ? 'border-red-800/60 text-red-400 hover:bg-red-950/40'
                      : 'border-emerald-800/60 text-emerald-400 hover:bg-emerald-950/40']">
                  {{ toggling === user.id ? '...' : user.isActive ? 'Deactivate' : 'Activate' }}
                </button>
              </div>
            </td>
          </tr>
        </tbody>
      </table>
    </div>

    <!-- Pagination -->
    <AppPagination :page="page" :total="total" :pageSize="pageSize"
      @update:page="p => { page = p; load() }" />

    <!-- ── Detail Modal ── teleported to body so no overflow clipping -->
    <Teleport to="body">
      <div v-if="showDetail && selectedUser"
           class="fixed inset-0 z-[9999] flex items-center justify-center p-4"
           style="background: rgba(0,0,0,0.80);"
           @click.self="closeDetail">

        <div class="w-full max-w-lg rounded-2xl overflow-hidden shadow-2xl"
             style="background: #0f172a; border: 1px solid rgba(180,83,9,0.35);">

          <!-- Top accent line -->
          <div class="h-0.5 w-full" style="background: linear-gradient(90deg, transparent, #d97706, transparent);"></div>

          <!-- Header -->
          <div class="flex items-center justify-between px-6 py-4 border-b" style="border-color: rgba(180,83,9,0.2);">
            <h3 class="text-white font-bold text-lg">User Details</h3>
            <button @click="closeDetail" type="button" class="text-slate-400 hover:text-white text-2xl leading-none">✕</button>
          </div>

          <div class="p-6 space-y-5">

            <!-- Avatar + Name -->
            <div class="flex items-center gap-4">
              <div class="w-16 h-16 rounded-full flex items-center justify-center text-2xl font-bold text-white flex-shrink-0"
                   style="background: linear-gradient(135deg, #92400e, #451a03); box-shadow: 0 0 20px rgba(180,83,9,0.3);">
                {{ selectedUser.fullName?.[0]?.toUpperCase() }}
              </div>
              <div>
                <p class="text-white font-bold text-xl">{{ selectedUser.fullName }}</p>
                <span :class="['text-xs px-2.5 py-0.5 rounded-full border font-medium mt-1 inline-block', roleColors[selectedUser.role] || 'text-slate-400 bg-slate-800 border-slate-600']">
                  {{ selectedUser.role }}
                </span>
              </div>
            </div>

            <!-- Info grid -->
            <div class="grid grid-cols-2 gap-3">
              <div class="bg-dark-800 rounded-xl p-3 col-span-2">
                <p class="text-slate-500 text-xs uppercase mb-1">Email</p>
                <p class="text-white text-sm break-all">{{ selectedUser.email }}</p>
              </div>
              <div class="bg-dark-800 rounded-xl p-3">
                <p class="text-slate-500 text-xs uppercase mb-1">Phone</p>
                <p class="text-white text-sm">{{ selectedUser.phoneNumber || '—' }}</p>
                <p v-if="selectedUser.whatsAppNumber" class="text-green-400 text-xs mt-1">📱 {{ selectedUser.whatsAppNumber }}</p>
              </div>
              <div class="bg-dark-800 rounded-xl p-3">
                <p class="text-slate-500 text-xs uppercase mb-1">Joined</p>
                <p class="text-white text-sm">{{ new Date(selectedUser.createdAt).toLocaleDateString('en-GB', { day:'2-digit', month:'short', year:'numeric' }) }}</p>
              </div>
              <div class="bg-dark-800 rounded-xl p-3 col-span-2 flex items-center justify-between">
                <div>
                  <p class="text-slate-500 text-xs uppercase mb-1">Account Status</p>
                  <p :class="selectedUser.isActive ? 'text-emerald-400' : 'text-red-400'" class="text-sm font-semibold">
                    {{ selectedUser.isActive ? '● Active' : '○ Inactive' }}
                  </p>
                </div>
                <button
                  @click="toggleActive(selectedUser)"
                  type="button"
                  :disabled="toggling === selectedUser.id"
                  :class="['text-sm px-4 py-2 rounded-lg border font-medium transition-colors disabled:opacity-50',
                    selectedUser.isActive
                      ? 'border-red-700 text-red-400 hover:bg-red-950/50'
                      : 'border-emerald-700 text-emerald-400 hover:bg-emerald-950/50']">
                  {{ toggling === selectedUser.id ? 'Updating...' : selectedUser.isActive ? '🔒 Deactivate' : '🔓 Activate' }}
                </button>
              </div>
            </div>

            <!-- Change Role -->
            <div class="rounded-xl p-4" style="background: rgba(180,83,9,0.08); border: 1px solid rgba(180,83,9,0.2);">
              <p class="text-gold-400 text-xs font-semibold uppercase tracking-wider mb-3">Change Role</p>
              <div class="grid grid-cols-2 gap-2 sm:grid-cols-3">
                <button v-for="r in roleOptions" :key="r.value"
                  @click="changeRole(r.value)"
                  type="button"
                  :disabled="changingRole"
                  :class="['text-sm px-3 py-2.5 rounded-lg border transition-all font-medium text-left',
                    selectedUser.role?.toLowerCase() === r.label.replace(/.*\s/, '').toLowerCase()
                      ? 'border-gold-600 bg-gold-900/50 text-gold-300'
                      : 'border-dark-600 bg-dark-800 text-slate-300 hover:border-gold-700 hover:bg-dark-700 hover:text-gold-300']">
                  {{ changingRole ? '...' : r.label }}
                </button>
              </div>
            </div>

            <button @click="closeDetail" type="button" class="w-full py-2.5 rounded-xl border border-dark-600 text-slate-400 hover:text-white hover:border-dark-500 text-sm transition-colors">
              Close
            </button>
          </div>
        </div>

      </div>
    </Teleport>

  </div>
</template>
