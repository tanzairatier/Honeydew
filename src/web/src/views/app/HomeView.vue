<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { RouterLink } from 'vue-router'
import { getAssignedToMe, toggleTodoVote, getTodo } from '@/api/todosService'
import type { TodoItem } from '@/api/todosService'
import TodoCard from '@/components/TodoCard.vue'
import TodoDetailModal from '@/components/TodoDetailModal.vue'
import { useAuth } from '@/composables/useAuth'
import { useCurrentUser } from '@/composables/useCurrentUser'

const { userId } = useAuth()
const { currentUser } = useCurrentUser()
const assignedItems = ref<TodoItem[]>([])
const loading = ref(true)
const selectedTodo = ref<TodoItem | null>(null)
const modalVisible = ref(false)

function canEditTodo(todo: TodoItem | null): boolean {
  if (!todo || !userId.value) return false
  const u = currentUser.value
  if (!u) return false
  if (u.canEditAllTodos || u.role === 'Owner') return true
  return todo.createdByUserId === userId.value || todo.assignedToUserId === userId.value
}

onMounted(async () => {
  try {
    assignedItems.value = await getAssignedToMe(3)
  } catch {
    assignedItems.value = []
  } finally {
    loading.value = false
  }
})

function openCard(todo: TodoItem) {
  selectedTodo.value = todo
  modalVisible.value = true
}

function closeModal() {
  modalVisible.value = false
  selectedTodo.value = null
}

function onTodoUpdated(updated: TodoItem) {
  const i = assignedItems.value.findIndex((t) => t.id === updated.id)
  if (i >= 0) assignedItems.value[i] = updated
  selectedTodo.value = updated
}

async function onVote(todo: TodoItem) {
  try {
    await toggleTodoVote(todo.id)
    const updated = await getTodo(todo.id)
    const i = assignedItems.value.findIndex((t) => t.id === todo.id)
    if (i >= 0) {
      assignedItems.value = assignedItems.value.slice(0, i).concat(updated, assignedItems.value.slice(i + 1))
    }
    if (selectedTodo.value?.id === todo.id)
      selectedTodo.value = updated
  } catch {
    // ignore
  }
}
</script>

<template>
  <div class="dashboard">
    <h2 class="sketch-title">Welcome</h2>
    <p class="intro">Your household todo notepad.</p>

    <section class="sketch-box card">
      <h3 class="section-title">Assigned to you</h3>
      <p v-if="loading" class="count-note">Loading…</p>
      <p v-else-if="assignedItems.length === 0" class="count-note">You have no items assigned to you yet.</p>
      <div v-else class="home-todos">
        <TodoCard v-for="todo in assignedItems" :key="todo.id" :todo="todo" @click="openCard" @vote="onVote" />
      </div>
      <span v-if="false">’t have any todos yet. When you do, we’ll show a count and your most pressing items </span>
      <RouterLink to="/app/items" class="sketch-link">Go to Items →</RouterLink>
    </section>
    <TodoDetailModal :todo="selectedTodo" :visible="modalVisible" :can-edit="canEditTodo(selectedTodo)" @close="closeModal" @updated="onTodoUpdated" />
  </div>
</template>

<style scoped>
.dashboard {
  max-width: 32rem;
}

.intro {
  margin: 0 0 1rem;
  color: var(--pencil);
  font-size: 0.9rem;
}

.card {
  padding: 1rem;
}

.section-title {
  font-size: 1rem;
  margin: 0 0 0.5rem;
  font-weight: 500;
}

.count-note {
  margin: 0 0 0.75rem;
  font-size: 0.85rem;
  color: var(--pencil);
}

.home-todos {
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
  margin-bottom: 0.75rem;
}
</style>
