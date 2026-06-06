<script setup>
import { ref } from 'vue'
import { imagesApi } from '@/api'

const props = defineProps({
  modelValue: { type: String, default: '' },
  placeholder: { type: String, default: 'Paste image URL here, or click Upload' },
  label: { type: String, default: '' },
  previewClass: { type: String, default: 'w-full max-h-48 object-cover object-top rounded-xl' },
})
const emit = defineEmits(['update:modelValue'])

const uploading = ref(false)
const error     = ref('')
const fileInput = ref(null)

function onUrl(e) {
  emit('update:modelValue', e.target.value)
  error.value = ''
}

function triggerFileInput() {
  fileInput.value?.click()
}

async function onFileChange(e) {
  const file = e.target.files?.[0]
  if (!file) return

  // Client-side size check (5 MB)
  if (file.size > 5 * 1024 * 1024) {
    error.value = 'File too large. Maximum allowed size is 5 MB.'
    e.target.value = ''
    return
  }

  uploading.value = true
  error.value = ''
  try {
    const res = await imagesApi.upload(file)
    emit('update:modelValue', res.data.data.url)
  } catch (err) {
    const status = err.response?.status
    const msg    = err.response?.data?.message

    if (status === 413)       error.value = 'File too large. Maximum allowed size is 5 MB.'
    else if (status === 401)  error.value = 'Not authorized. Please log in again.'
    else if (status === 403)  error.value = 'Access denied. Admin role required.'
    else if (status === 400)  error.value = msg || 'Invalid file. Only JPG, PNG, WEBP, GIF are allowed.'
    else if (!err.response)   error.value = 'Network error. Check your connection and try again.'
    else                       error.value = msg || `Upload failed (${status ?? 'unknown error'}).`
  } finally {
    uploading.value = false
    e.target.value = ''
  }
}
</script>

<template>
  <div class="space-y-2">
    <label v-if="label" class="form-label">{{ label }}</label>

    <!-- Input row: URL field + Upload button -->
    <div class="flex gap-2">
      <input
        :value="modelValue"
        @input="onUrl"
        class="input flex-1 text-sm"
        :placeholder="placeholder"
        style="min-height:40px;"
      />
      <button
        type="button"
        @click="triggerFileInput"
        :disabled="uploading"
        class="shrink-0 px-4 rounded-xl text-sm font-semibold text-white disabled:opacity-50 transition whitespace-nowrap"
        style="background:linear-gradient(135deg,#7c3aed,#5b21b6); min-height:40px;"
      >
        {{ uploading ? 'Uploading...' : '📤 Upload' }}
      </button>
      <input ref="fileInput" type="file" accept="image/jpeg,image/jpg,image/png,image/webp,image/gif"
             class="hidden" @change="onFileChange" />
    </div>

    <!-- Error message -->
    <p v-if="error" class="text-red-400 text-xs flex items-start gap-1">
      <span>⚠️</span> {{ error }}
    </p>

    <!-- Preview -->
    <div v-if="modelValue" class="rounded-xl overflow-hidden border border-purple-900/30">
      <img :src="modelValue" alt="Preview" :class="previewClass"
           @error="error = 'Image could not be loaded. Check the URL.'" />
    </div>
  </div>
</template>
