<script setup>
import { ref, onMounted } from 'vue'
import { noticeApi } from '@/api/notices'

const props = defineProps({
  compact: { type: Boolean, default: false }
})

const notices = ref([])
const loading = ref(true)

const categoryConfig = {
  General:       { icon: '📢', color: 'border-slate-600 bg-dark-800',     badge: 'text-slate-300 bg-slate-700' },
  Food:          { icon: '🍽️', color: 'border-green-800 bg-green-950/40', badge: 'text-green-300 bg-green-900/60' },
  Transport:     { icon: '🚌', color: 'border-blue-800 bg-blue-950/40',   badge: 'text-blue-300 bg-blue-900/60' },
  Prayer:        { icon: '🕌', color: 'border-primary-800 bg-primary-950/40', badge: 'text-primary-300 bg-primary-900/60' },
  Ziyarat:       { icon: '🌙', color: 'border-gold-800 bg-gold-950/40',   badge: 'text-gold-300 bg-gold-900/60' },
  Emergency:     { icon: '🚨', color: 'border-red-700 bg-red-950/50',     badge: 'text-red-300 bg-red-900/60' },
  Accommodation: { icon: '🏨', color: 'border-purple-800 bg-purple-950/40', badge: 'text-purple-300 bg-purple-900/60' },
  Medical:       { icon: '🏥', color: 'border-pink-800 bg-pink-950/40',   badge: 'text-pink-300 bg-pink-900/60' }
}

const priorityLabel = { Normal: null, Important: '⭐ Important', Urgent: '🔥 Urgent' }
const priorityBorder = { Normal: '', Important: 'ring-1 ring-gold-700', Urgent: 'ring-2 ring-red-600' }

function timeAgo(date) {
  const diff = Date.now() - new Date(date).getTime()
  const m = Math.floor(diff / 60000)
  if (m < 1) return 'just now'
  if (m < 60) return `${m}m ago`
  const h = Math.floor(m / 60)
  if (h < 24) return `${h}h ago`
  return `${Math.floor(h / 24)}d ago`
}

async function load() {
  loading.value = true
  try {
    const res = await noticeApi.getAll()
    notices.value = res.data.data || []
  } catch {} finally { loading.value = false }
}

onMounted(load)
</script>

<template>
  <div class="space-y-3">

    <div v-if="loading" class="text-center py-6 text-slate-500 text-sm">Loading notices...</div>

    <div v-else-if="!notices.length" class="text-center py-8 text-slate-600">
      <div class="text-3xl mb-2">📋</div>
      <p class="text-sm">No notices at this time.</p>
    </div>

    <div v-else>
      <div v-for="notice in notices" :key="notice.id"
        :class="['rounded-xl border p-4 transition-all', categoryConfig[notice.category]?.color || categoryConfig.General.color, priorityBorder[notice.priority] || '']">

        <div class="flex items-start gap-3">
          <!-- Icon -->
          <div class="text-2xl flex-shrink-0 mt-0.5">
            {{ categoryConfig[notice.category]?.icon || '📢' }}
          </div>

          <div class="flex-1 min-w-0">
            <!-- Header row -->
            <div class="flex items-start justify-between gap-2 flex-wrap">
              <div class="flex items-center gap-2 flex-wrap">
                <h3 class="text-white font-semibold text-sm">{{ notice.title }}</h3>
                <span v-if="notice.isPinned" class="text-xs text-gold-400">📌 Pinned</span>
                <span v-if="priorityLabel[notice.priority]"
                  class="text-xs px-1.5 py-0.5 rounded font-medium"
                  :class="notice.priority === 'Urgent' ? 'bg-red-900/70 text-red-300' : 'bg-gold-900/70 text-gold-300'">
                  {{ priorityLabel[notice.priority] }}
                </span>
              </div>
              <div class="flex items-center gap-2 flex-shrink-0">
                <span :class="['text-xs px-2 py-0.5 rounded font-medium', categoryConfig[notice.category]?.badge]">
                  {{ notice.category }}
                </span>
                <span class="text-slate-600 text-xs">{{ timeAgo(notice.createdAt) }}</span>
              </div>
            </div>

            <!-- Content -->
            <p class="text-slate-300 text-sm mt-1.5 leading-relaxed">{{ notice.content }}</p>

            <!-- Urdu/Arabic content -->
            <p v-if="notice.urduContent" class="text-gold-400 text-sm mt-2 text-right font-arabic leading-relaxed">
              {{ notice.urduContent }}
            </p>
            <p v-if="notice.arabicContent" class="text-primary-400 text-sm mt-1 text-right font-arabic leading-relaxed">
              {{ notice.arabicContent }}
            </p>

            <!-- Schedule/Expiry -->
            <div v-if="notice.scheduledAt || notice.expiresAt" class="flex gap-4 mt-2 text-xs text-slate-500">
              <span v-if="notice.scheduledAt">🕐 {{ new Date(notice.scheduledAt).toLocaleString('en-GB', {dateStyle:'short', timeStyle:'short'}) }}</span>
              <span v-if="notice.expiresAt">⏰ Expires: {{ new Date(notice.expiresAt).toLocaleDateString('en-GB') }}</span>
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>
