<script setup lang="ts">
import { ref } from 'vue'
import { exportTodosCsv } from '@/api/todosService'

const onlyMine = ref(true)
const exporting = ref(false)
const error = ref('')

async function exportCsv() {
  exporting.value = true
  error.value = ''
  try {
    const blob = await exportTodosCsv(onlyMine.value)
    const url = URL.createObjectURL(blob)
    const a = document.createElement('a')
    a.href = url
    a.download = 'todos.csv'
    a.click()
    URL.revokeObjectURL(url)
  } catch (e: unknown) {
    error.value = e instanceof Error ? e.message : 'Export failed'
  } finally {
    exporting.value = false
  }
}
</script>

<template>
  <div class="export-page">
    <h2 class="sketch-title">Export</h2>
    <p class="sketch-label">Export todos to CSV</p>
    <div class="export-options">
      <label class="radio-label">
        <input v-model="onlyMine" type="radio" :value="true" />
        Your todos only
      </label>
      <label class="radio-label">
        <input v-model="onlyMine" type="radio" :value="false" />
        All household todos
      </label>
    </div>
    <p class="hint">If you don’t have permission to view all todos, “All household todos” will be restricted.</p>
    <div v-if="error" class="sketch-error" role="alert">{{ error }}</div>
    <button type="button" class="sketch-btn primary" :disabled="exporting" @click="exportCsv">
      {{ exporting ? '…' : 'Download CSV' }}
    </button>
  </div>
</template>

<style scoped>
.export-page {
  max-width: 28rem;
  display: flex;
  flex-direction: column;
  gap: 1rem;
}
.export-options {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
}
.radio-label {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  font-size: 0.9rem;
  cursor: pointer;
}
.hint {
  font-size: 0.8rem;
  color: var(--pencil);
  margin: 0;
}
</style>
