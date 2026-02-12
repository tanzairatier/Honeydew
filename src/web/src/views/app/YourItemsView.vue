<script setup lang="ts">
import { ref, computed, watch, onMounted } from 'vue'
import { listTodos, createTodo, toggleTodoVote, getTodo } from '@/api/todosService'
import type { TodoItem } from '@/api/todosService'
import { listUsers } from '@/api/usersService'
import type { UserSummary } from '@/api/usersService'
import TodoCard from '@/components/TodoCard.vue'
import PaginationNav from '@/components/PaginationNav.vue'
import TodoDetailModal from '@/components/TodoDetailModal.vue'
import { usePreferences } from '@/composables/usePreferences'
import { useAuth } from '@/composables/useAuth'
import { useCurrentUser } from '@/composables/useCurrentUser'

const { itemsPerPage } = usePreferences()
const { userId } = useAuth()
const { currentUser } = useCurrentUser()

const items = ref<TodoItem[]>([])
const totalCount = ref(0)
const page = ref(1)
const loading = ref(false)
const error = ref('')
const showCreate = ref(false)
const createTitle = ref('')
const createNotes = ref('')
const createAssignedTo = ref('')
const createDueDate = ref('')
const createSaving = ref(false)
const createError = ref('')
const includeCompleted = ref(false)
const tenantUsers = ref<UserSummary[]>([])
const selectedTodo = ref<TodoItem | null>(null)
const modalVisible = ref(false)

const pageSize = computed(() => itemsPerPage.value)

onMounted(async () => {
  try {
    tenantUsers.value = await listUsers(true, true)
  } catch {
    tenantUsers.value = []
  }
})

async function load() {
  loading.value = true
  error.value = ''
  try {
    const data = await listTodos({
      page: page.value,
      pageSize: pageSize.value,
      onlyMine: false,
      assignedToUserIds: userId.value ? [userId.value] : undefined,
      includeCompleted: includeCompleted.value,
    })
    items.value = data.items
    totalCount.value = data.totalCount
  } catch (e: unknown) {
    error.value = e instanceof Error ? e.message : 'Failed to load'
  } finally {
    loading.value = false
  }
}

watch([page, pageSize, includeCompleted], load, { immediate: true })

function goPage(p: number) {
  page.value = p
}

function openCard(todo: TodoItem) {
  selectedTodo.value = todo
  modalVisible.value = true
}

function closeModal() {
  modalVisible.value = false
  selectedTodo.value = null
}

function onTodoUpdated(updated: TodoItem) {
  const i = items.value.findIndex((t) => t.id === updated.id)
  if (i >= 0) items.value[i] = updated
  selectedTodo.value = updated
}

async function onVote(todo: TodoItem) {
  try {
    await toggleTodoVote(todo.id)
    const updated = await getTodo(todo.id)
    const i = items.value.findIndex((t) => t.id === todo.id)
    if (i >= 0) {
      items.value = items.value.slice(0, i).concat(updated, items.value.slice(i + 1))
    }
    if (selectedTodo.value?.id === todo.id)
      selectedTodo.value = updated
  } catch {
    // ignore
  }
}

function toDateString(d: Date): string {
  return d.toISOString().slice(0, 10)
}

function setDueShortcut(days: number) {
  const d = new Date()
  d.setDate(d.getDate() + days)
  createDueDate.value = toDateString(d)
}

function dueShortcutDate(days: number): string {
  const d = new Date()
  d.setDate(d.getDate() + days)
  return toDateString(d)
}

function isDueShortcutSelected(days: number): boolean {
  if (!createDueDate.value) return false
  return createDueDate.value === dueShortcutDate(days)
}

function canEditTodo(todo: TodoItem | null): boolean {
  if (!todo || !userId.value) return false
  const u = currentUser.value
  if (!u) return false
  if (u.canEditAllTodos || u.role === 'Owner') return true
  return todo.createdByUserId === userId.value || todo.assignedToUserId === userId.value
}

function openCreate() {
  createTitle.value = ''
  createNotes.value = ''
  createDueDate.value = ''
  createAssignedTo.value = userId.value ?? tenantUsers.value[0]?.id ?? ''
  createError.value = ''
  showCreate.value = true
}

function closeCreate() {
  showCreate.value = false
}

async function submitCreate() {
  const title = createTitle.value.trim()
  if (!title) {
    createError.value = 'Title is required'
    return
  }
  if (!createAssignedTo.value) {
    createError.value = 'Please assign to a user'
    return
  }
  createError.value = ''
  createSaving.value = true
  try {
    await createTodo({
      title,
      notes: createNotes.value.trim() || undefined,
      assignedToUserId: createAssignedTo.value,
      dueDate: createDueDate.value || undefined,
    })
    closeCreate()
    load()
  } catch (e: unknown) {
    createError.value = e instanceof Error ? e.message : 'Failed to create'
  } finally {
    createSaving.value = false
  }
}
</script>

<template>
  <div class="your-items-page">
    <div class="page-header">
      <h2 class="sketch-title">Your Items</h2>
      <label class="include-completed">
        <input v-model="includeCompleted" type="checkbox" />
        Include completed
      </label>
      <button type="button" class="sketch-btn primary" @click="openCreate">+ New todo</button>
    </div>

    <div v-if="showCreate" class="sketch-box create-form">
      <h3 class="sketch-title">New todo</h3>
      <label>
        <span class="sketch-label">Title</span>
        <input v-model="createTitle" type="text" class="sketch-input" placeholder="What to do?" />
      </label>
      <label>
        <span class="sketch-label">Assign to</span>
        <select v-model="createAssignedTo" class="sketch-input" required>
          <option v-for="u in tenantUsers" :key="u.id" :value="u.id">{{ u.displayName }}</option>
        </select>
      </label>
      <label>
        <span class="sketch-label">Due date (optional)</span>
        <div class="due-shortcuts">
          <button type="button" class="sketch-btn small" :class="{ selected: isDueShortcutSelected(0) }" @click="setDueShortcut(0)">Today</button>
          <button type="button" class="sketch-btn small" :class="{ selected: isDueShortcutSelected(1) }" @click="setDueShortcut(1)">Tomorrow</button>
          <button type="button" class="sketch-btn small" :class="{ selected: isDueShortcutSelected(3) }" @click="setDueShortcut(3)">+3 Days</button>
          <button type="button" class="sketch-btn small" :class="{ selected: isDueShortcutSelected(7) }" @click="setDueShortcut(7)">+1 Week</button>
          <button type="button" class="sketch-btn small" :class="{ selected: isDueShortcutSelected(30) }" @click="setDueShortcut(30)">+1 Month</button>
        </div>
        <input v-model="createDueDate" type="date" class="sketch-input due-date-input" />
      </label>
      <label>
        <span class="sketch-label">Notes (optional)</span>
        <textarea v-model="createNotes" class="sketch-input" rows="2" placeholder="Details…"></textarea>
      </label>
      <div v-if="createError" class="sketch-error" role="alert">{{ createError }}</div>
      <div class="form-actions">
        <button type="button" class="sketch-btn" @click="closeCreate">Cancel</button>
        <button type="button" class="sketch-btn primary" :disabled="createSaving" @click="submitCreate">
          {{ createSaving ? '…' : 'Add' }}
        </button>
      </div>
    </div>

    <div v-if="error" class="sketch-error" role="alert">{{ error }}</div>
    <div v-else-if="loading" class="loading">Loading…</div>
    <div v-else-if="items.length === 0" class="empty-state sketch-box">
      <p class="empty-state-text">No tasks yet.</p>
      <p class="empty-state-hint">Add your first task above, or click below.</p>
      <button type="button" class="sketch-btn primary" @click="showCreate = true">Add task</button>
    </div>
    <div v-else class="sticky-grid">
      <TodoCard v-for="todo in items" :key="todo.id" :todo="todo" @click="openCard" @vote="onVote" />
    </div>

    <PaginationNav
      v-if="totalCount > 0"
      :page="page"
      :page-size="pageSize"
      :total-count="totalCount"
      @go="goPage"
    />

    <TodoDetailModal
      :todo="selectedTodo"
      :visible="modalVisible"
      :can-edit="canEditTodo(selectedTodo)"
      @close="closeModal"
      @updated="onTodoUpdated"
    />
  </div>
</template>

<style scoped>
.your-items-page {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}
.page-header {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  justify-content: space-between;
  gap: 0.75rem;
}
.include-completed {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  font-size: 0.9rem;
  color: var(--pencil);
  cursor: pointer;
}
.create-form {
  padding: 1rem;
}
.create-form label {
  display: block;
  margin-bottom: 1rem;
}
.create-form .sketch-label {
  margin-bottom: 0.25rem;
}
.due-shortcuts {
  display: flex;
  flex-wrap: wrap;
  gap: 0.35rem;
  margin-bottom: 0.5rem;
}

.due-shortcuts .sketch-btn.small.selected {
  border-color: var(--ink);
  background: var(--ink);
  color: var(--paper);
  font-weight: 500;
}
.due-shortcuts .sketch-btn.small.selected:hover {
  background: var(--ink);
  color: var(--paper);
}
.due-shortcuts .sketch-btn.small:not(.selected):hover {
  background: rgba(0, 0, 0, 0.06);
  color: var(--ink);
}

.sketch-btn.small {
  padding: 0.3rem 0.5rem;
  font-size: 0.8rem;
}
.due-date-input {
  max-width: 12rem;
}
.form-actions {
  display: flex;
  gap: 0.5rem;
  margin-top: 0.5rem;
}
.sticky-grid {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: 1rem;
}
@media (max-width: 640px) {
  .sticky-grid {
    grid-template-columns: 1fr;
  }
}
.loading {
  color: var(--pencil);
  font-size: 0.9rem;
  padding: 1rem 0;
}
.empty-state {
  padding: 2rem;
  text-align: center;
  margin-top: 0.5rem;
}
.empty-state-text {
  font-size: 1.1rem;
  color: var(--ink);
  margin: 0 0 0.25rem 0;
}
.empty-state-hint {
  color: var(--pencil);
  font-size: 0.9rem;
  margin: 0 0 1rem 0;
}
.empty-state .sketch-btn {
  margin: 0 auto;
}
</style>
