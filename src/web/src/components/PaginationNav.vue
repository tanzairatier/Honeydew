<script setup lang="ts">
import { computed } from 'vue'

const props = withDefaults(
  defineProps<{
    page: number
    pageSize: number
    totalCount: number
  }>(),
  {}
)

const totalPages = computed(() => Math.max(1, Math.ceil(props.totalCount / props.pageSize)))
const hasPrev = computed(() => props.page > 1)
const hasNext = computed(() => props.page < totalPages.value)

const emit = defineEmits<{
  go: [page: number]
}>()
</script>

<template>
  <nav class="pagination" aria-label="Pagination">
    <button type="button" class="pagination-btn" :disabled="!hasPrev" @click="emit('go', 1)">First</button>
    <button type="button" class="pagination-btn" :disabled="!hasPrev" @click="emit('go', page - 1)">Prev</button>
    <span class="pagination-info">
      Page {{ page }} of {{ totalPages }} ({{ totalCount }} items)
    </span>
    <button type="button" class="pagination-btn" :disabled="!hasNext" @click="emit('go', page + 1)">Next</button>
    <button type="button" class="pagination-btn" :disabled="!hasNext" @click="emit('go', totalPages)">Last</button>
  </nav>
</template>

<style scoped>
.pagination {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 0.5rem;
  margin-top: 1rem;
}
.pagination-btn {
  font-family: inherit;
  font-size: 0.8rem;
  padding: 0.4rem 0.6rem;
  border: var(--border-width) solid var(--line);
  border-radius: var(--radius);
  background: var(--paper);
  color: var(--ink);
  cursor: pointer;
}
.pagination-btn:hover:not(:disabled) {
  background: var(--ink);
  color: var(--paper);
}
.pagination-btn:disabled {
  opacity: 0.5;
  cursor: not-allowed;
}
.pagination-info {
  font-size: 0.8rem;
  color: var(--pencil);
  padding: 0 0.25rem;
}
</style>
