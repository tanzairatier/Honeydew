<script setup lang="ts">
import { ref, computed, watch, onMounted } from 'vue'
import type { TodoItem } from '@/api/todosService'
import { updateTodo } from '@/api/todosService'
import { listUsers } from '@/api/usersService'
import type { UserSummary } from '@/api/usersService'

const props = defineProps<{
  todo: TodoItem | null
  visible: boolean
  canEdit?: boolean
}>()

const emit = defineEmits<{
  close: []
  updated: [todo: TodoItem]
}>()

const isDone = ref(false)
const editAssignedTo = ref('')
const editDueDate = ref('')
const saving = ref(false)
const error = ref('')
const users = ref<UserSummary[]>([])

onMounted(async () => {
  try {
    users.value = await listUsers(true, true)
  } catch {
    users.value = []
  }
})

watch(
  () => props.todo,
  (t) => {
    isDone.value = t?.isDone ?? false
    editAssignedTo.value = t?.assignedToUserId ?? ''
    editDueDate.value = t?.dueDate ? t.dueDate.slice(0, 10) : ''
    error.value = ''
  },
  { immediate: true }
)

async function toggleDone() {
  if (!props.todo) return
  saving.value = true
  error.value = ''
  try {
    const newDone = !props.todo.isDone
    const updated = await updateTodo(props.todo.id, {
      isDone: newDone,
      completedAt: newDone ? new Date().toISOString() : null,
    })
    isDone.value = updated.isDone
    emit('updated', updated)
  } catch (e: unknown) {
    error.value = e instanceof Error ? e.message : 'Failed to update'
    isDone.value = props.todo?.isDone ?? false
  } finally {
    saving.value = false
  }
}

async function saveAssignedAndDue() {
  if (!props.todo) return
  saving.value = true
  error.value = ''
  try {
    const updated = await updateTodo(props.todo.id, {
      assignedToUserId: editAssignedTo.value || null,
      dueDate: editDueDate.value || null,
    })
    emit('updated', updated)
  } catch (e: unknown) {
    error.value = e instanceof Error ? e.message : 'Failed to update'
  } finally {
    saving.value = false
  }
}

const canEdit = computed(() => props.canEdit !== false)

function close() {
  emit('close')
}

async function saveAndClose() {
  if (props.todo && canEdit.value && (editAssignedTo.value !== (props.todo.assignedToUserId ?? '') || editDueDate.value !== (props.todo.dueDate ? props.todo.dueDate.slice(0, 10) : ''))) {
    await saveAssignedAndDue()
  }
  close()
}

function setDueShortcut(days: number) {
  const d = new Date()
  d.setDate(d.getDate() + days)
  editDueDate.value = d.toISOString().slice(0, 10)
}
</script>

<template>
  <Teleport to="body">
    <div v-if="visible" class="modal-overlay" @click.self="close">
      <div class="modal sketch-box" role="dialog" aria-modal="true" aria-labelledby="todo-detail-title">
        <button type="button" class="modal-close-x" aria-label="Close" @click="close">×</button>
        <div class="modal-scroll-content">
          <h2 id="todo-detail-title" class="sketch-title">{{ todo?.title ?? '' }}</h2>
          <p v-if="todo?.notes" class="todo-notes">{{ todo.notes }}</p>
        </div>
        <div class="todo-meta">
          <label v-if="canEdit" class="todo-done-label">
            <input v-model="isDone" type="checkbox" :disabled="saving" @change="toggleDone" />
            Done
          </label>
          <template v-else>
            <span class="todo-done-readonly">Done: {{ todo?.isDone ? 'Yes' : 'No' }}</span>
          </template>
          <span v-if="todo?.completedAt" class="todo-completed">
            Completed {{ new Date(todo.completedAt).toLocaleString() }}
          </span>
          <span v-else class="todo-created">Created {{ todo ? new Date(todo.createdAt).toLocaleString() : '' }}</span>
        </div>
        <div v-if="canEdit && users.length" class="todo-edit-row">
          <label class="todo-edit-label">
            Assignee
            <select v-model="editAssignedTo" class="sketch-input" :disabled="saving">
              <option value="">—</option>
              <option v-for="u in users" :key="u.id" :value="u.id">{{ u.displayName }}</option>
            </select>
          </label>
          <label class="todo-edit-label">
            Due date
            <div class="due-shortcuts">
              <button type="button" class="sketch-btn small" :disabled="saving" @click="setDueShortcut(0)">Today</button>
              <button type="button" class="sketch-btn small" :disabled="saving" @click="setDueShortcut(1)">Tomorrow</button>
              <button type="button" class="sketch-btn small" :disabled="saving" @click="setDueShortcut(3)">+3 Days</button>
              <button type="button" class="sketch-btn small" :disabled="saving" @click="setDueShortcut(7)">+1 Week</button>
              <button type="button" class="sketch-btn small" :disabled="saving" @click="setDueShortcut(30)">+1 Month</button>
            </div>
            <input v-model="editDueDate" type="date" class="sketch-input due-date-input" :disabled="saving" />
          </label>
        </div>
        <div v-if="todo?.voteCount !== undefined" class="todo-vote-meta">
          👍 {{ todo.voteCount }}
          <span v-if="todo.currentUserVoted"> (you voted)</span>
        </div>
        <div v-if="error" class="sketch-error" role="alert">{{ error }}</div>
        <div class="modal-actions">
          <button type="button" class="sketch-btn primary" :disabled="saving" @click="canEdit ? saveAndClose() : close()">
            {{ saving ? '…' : canEdit ? 'Save & Close' : 'Close' }}
          </button>
        </div>
      </div>
    </div>
  </Teleport>
</template>

<style scoped>
.modal-overlay {
  position: fixed;
  inset: 0;
  background: rgba(0, 0, 0, 0.35);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 100;
  padding: 1rem;
}
.modal {
  position: relative;
  padding: 1.5rem;
  padding-top: 2.25rem;
  min-width: 20rem;
  max-width: 28rem;
  max-height: 85vh;
  display: flex;
  flex-direction: column;
}
.modal-close-x {
  position: absolute;
  top: 0.5rem;
  right: 0.5rem;
  background: none;
  border: none;
  font-size: 1.5rem;
  line-height: 1;
  cursor: pointer;
  color: var(--pencil);
  padding: 0.25rem;
}
.modal-close-x:hover {
  color: var(--ink);
}
.todo-done-readonly {
  font-size: 0.85rem;
  color: var(--pencil);
}
.modal-scroll-content {
  flex: 1 1 auto;
  min-height: 0;
  overflow-y: auto;
  margin-bottom: 1rem;
}
.todo-notes {
  margin: 0 0 1rem;
  font-size: 0.9rem;
  color: var(--pencil);
  white-space: pre-wrap;
  word-break: break-word;
}
.todo-meta {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 1rem;
  margin-bottom: 1rem;
  font-size: 0.85rem;
  color: var(--pencil);
}
.todo-done-label {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  cursor: pointer;
}
.todo-done-label input {
  width: 1rem;
  height: 1rem;
}
.todo-edit-row {
  display: flex;
  flex-wrap: wrap;
  align-items: flex-end;
  gap: 0.75rem;
  margin-bottom: 0.75rem;
}
.todo-edit-label {
  display: flex;
  flex-direction: column;
  gap: 0.25rem;
  font-size: 0.85rem;
  color: var(--pencil);
}
.todo-edit-label .sketch-input {
  width: auto;
  min-width: 10rem;
}
.due-shortcuts {
  display: flex;
  flex-wrap: wrap;
  gap: 0.35rem;
  margin-bottom: 0.5rem;
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
.todo-vote-meta {
  font-size: 0.85rem;
  color: var(--pencil);
  margin-bottom: 0.5rem;
}
.modal-actions {
  margin-top: 1rem;
}
</style>
