<script setup>
import { computed } from 'vue'

const props = defineProps({
  page:      { type: Number, required: true },
  total:     { type: Number, required: true },
  pageSize:  { type: Number, default: 20 },
})
const emit = defineEmits(['update:page'])

const totalPages = computed(() => Math.ceil(props.total / props.pageSize))
const from = computed(() => ((props.page - 1) * props.pageSize) + 1)
const to   = computed(() => Math.min(props.page * props.pageSize, props.total))

const pages = computed(() => {
  const p = totalPages.value
  const cur = props.page
  if (p <= 7) return Array.from({ length: p }, (_, i) => i + 1)
  if (cur <= 4)  return [1, 2, 3, 4, 5, '...', p]
  if (cur >= p - 3) return [1, '...', p-4, p-3, p-2, p-1, p]
  return [1, '...', cur-1, cur, cur+1, '...', p]
})

function go(n) {
  if (n === '...' || n === props.page) return
  if (n >= 1 && n <= totalPages.value) emit('update:page', n)
}
</script>

<template>
  <div v-if="total > pageSize" class="flex items-center justify-between gap-4 mt-4 pt-4"
    style="border-top: 1px solid rgba(180,83,9,0.15);">

    <!-- Record count -->
    <p class="text-slate-500 text-sm hidden sm:block">
      Showing <span class="text-slate-300">{{ from }}–{{ to }}</span> of
      <span class="text-slate-300">{{ total }}</span> records
    </p>

    <!-- Buttons -->
    <div class="flex items-center gap-1">
      <!-- Prev -->
      <button @click="go(page - 1)" :disabled="page === 1"
        class="px-3 py-1.5 rounded-lg text-sm transition-colors disabled:opacity-30 disabled:cursor-not-allowed"
        style="background:rgba(255,255,255,0.05);color:#94a3b8;"
        onmouseover="this.style.background='rgba(180,83,9,0.15)'"
        onmouseout="this.style.background='rgba(255,255,255,0.05)'">
        ← Prev
      </button>

      <!-- Page numbers -->
      <template v-for="p in pages" :key="p">
        <button v-if="p !== '...'" @click="go(p)"
          :class="['min-w-[34px] py-1.5 rounded-lg text-sm font-medium transition-colors',
            p === page
              ? 'bg-gold-700 text-white'
              : 'text-slate-400 hover:text-white']"
          :style="p !== page ? 'background:rgba(255,255,255,0.05)' : ''">
          {{ p }}
        </button>
        <span v-else class="px-1 text-slate-600 text-sm select-none">···</span>
      </template>

      <!-- Next -->
      <button @click="go(page + 1)" :disabled="page >= totalPages"
        class="px-3 py-1.5 rounded-lg text-sm transition-colors disabled:opacity-30 disabled:cursor-not-allowed"
        style="background:rgba(255,255,255,0.05);color:#94a3b8;"
        onmouseover="this.style.background='rgba(180,83,9,0.15)'"
        onmouseout="this.style.background='rgba(255,255,255,0.05)'">
        Next →
      </button>
    </div>
  </div>
</template>
