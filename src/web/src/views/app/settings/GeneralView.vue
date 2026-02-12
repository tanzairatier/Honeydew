<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { getTenant, updateTenantName } from '@/api/tenantService'
import { useAuth } from '@/composables/useAuth'

const { tenantName, setTenantName } = useAuth()
const name = ref('')
const saving = ref(false)
const error = ref('')
const message = ref('')

onMounted(async () => {
  try {
    const t = await getTenant()
    name.value = t.name
  } catch {
    error.value = 'Could not load household name'
  }
})

async function save() {
  const trimmed = name.value.trim()
  if (!trimmed) {
    error.value = 'Name is required'
    return
  }
  error.value = ''
  message.value = ''
  saving.value = true
  try {
    await updateTenantName(trimmed)
    setTenantName(trimmed)
    message.value = 'Saved.'
  } catch (e: unknown) {
    error.value = e instanceof Error ? e.message : 'Failed to save'
  } finally {
    saving.value = false
  }
}
</script>

<template>
  <div class="general-page">
    <h2 class="sketch-title">General</h2>
    <p class="sketch-label">Household name (shown at the top)</p>
    <input v-model="name" type="text" class="sketch-input" placeholder="e.g. Smith Household" />
    <div v-if="error" class="sketch-error" role="alert">{{ error }}</div>
    <div v-if="message" class="sketch-message">{{ message }}</div>
    <button type="button" class="sketch-btn primary" :disabled="saving" @click="save">
      {{ saving ? '…' : 'Save' }}
    </button>
  </div>
</template>

<style scoped>
.general-page {
  max-width: 28rem;
}
.general-page .sketch-label {
  margin-bottom: 0.5rem;
}
.general-page .sketch-input {
  margin-bottom: 1rem;
}
.sketch-message {
  font-size: 0.9rem;
  color: var(--pencil);
  margin-top: 0.5rem;
}
</style>
