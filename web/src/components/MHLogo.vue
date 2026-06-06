<script setup>
defineProps({
  size: { type: String, default: 'md' } // sm, md, lg, xl
})
</script>

<template>
  <div :class="{
    'w-8 h-8': size === 'sm',
    'w-10 h-10': size === 'md',
    'w-16 h-16': size === 'lg',
    'w-20 h-20': size === 'xl',
    'w-24 h-24': size === '2xl',
    'w-28 h-28': size === '3xl',
  }"
  class="rounded-full flex items-center justify-center overflow-hidden border border-gold-700/40 flex-shrink-0"
  style="background: radial-gradient(circle, #1a1200 0%, #020617 100%); box-shadow: 0 0 16px rgba(180,83,9,0.25);">
    <img
      src="https://www.misbahulhoda.org/wp-content/uploads/2023/10/Logo-white-512-512.png"
      alt="Misbah ul Hoda"
      class="w-full h-full object-contain p-1"
      @error="e => e.target.style.display='none'"
    />
  </div>
</template>
