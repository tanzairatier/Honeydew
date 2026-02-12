<script setup lang="ts">
import { ref, watch, onMounted } from 'vue'
import { listTodos, toggleTodoVote } from '@/api/todosService'
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

function canEditTodo(todo: TodoItem | null): boolean {
  if (!todo || !userId.value) return false
  const u = currentUser.value
  if (!u) return false
  if (u.canEditAllTodos || u.role === 'Owner') return true
  return todo.createdByUserId === userId.value || todo.assignedToUserId === userId.value
}

const searchQuery = ref('')
const includeCompleted = ref(true)
const sortBy = ref('createdat')
const sortDesc = ref(true)
const filterUserIds = ref<string[]>([])
const tenantUsers = ref<UserSummary[]>([])
const items = ref<TodoItem[]>([])
const totalCount = ref(0)
const page = ref(1)
const loading = ref(false)
const error = ref('')
const selectedTodo = ref<TodoItem | null>(null)
const modalVisible = ref(false)

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
      pageSize: itemsPerPage.value,
      onlyMine: false,
      includeCompleted: includeCompleted.value,
      search: searchQuery.value.trim() || undefined,
      sortBy: sortBy.value,
      sortDesc: sortDesc.value,
      assignedToUserIds: filterUserIds.value.length ? filterUserIds.value : undefined,
    })
    items.value = data.items
    totalCount.value = data.totalCount
  } catch (e: unknown) {
    error.value = e instanceof Error ? e.message : 'Failed to search'
  } finally {
    loading.value = false
  }
}

function toggleFilterUser(id: string) {
  const i = filterUserIds.value.indexOf(id)
  if (i >= 0) filterUserIds.value = filterUserIds.value.filter((x) => x !== id)
  else filterUserIds.value = [...filterUserIds.value, id]
}

watch([page, includeCompleted, sortBy, sortDesc, filterUserIds], load, { immediate: true })
watch(searchQuery, () => {
  page.value = 1
  load()
})

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
    const { Voted } = await toggleTodoVote(todo.id)
    const i = items.value.findIndex((t) => t.id === todo.id)
    if (i >= 0) {
      items.value[i] = {
        ...items.value[i],
        voteCount: items.value[i].voteCount + (Voted ? 1 : -1),
        currentUserVoted: Voted,
      }
    }
  } catch {
    // ignore
  }
}
</script>

<template>
  <div class="search-page">
    <h2 class="sketch-title">Search</h2>
    <div class="search-toolbar">
      <input
        v-model="searchQuery"
        type="search"
        class="sketch-input search-input"
        placeholder="Search todos…"
        aria-label="Search"
      />
      <label class="include-completed">
        <input v-model="includeCompleted" type="checkbox" />
        Include completed
      </label>
      <div class="sort-row">
        <label class="sort-label">
          Sort by
          <select v-model="sortBy" class="sketch-input sort-select">
            <option value="createdat">Created</option>
            <option value="duedate">Due date</option>
            <option value="completedat">Done date</option>
          </select>
        </label>
        <label class="sort-dir">
          <input v-model="sortDesc" type="checkbox" />
          Descending
        </label>
      </div>
      <div v-if="tenantUsers.length" class="filter-users">
        <span class="filter-label">Filter by assignee:</span>
        <label v-for="u in tenantUsers" :key="u.id" class="filter-user-chk">
          <input
            type="checkbox"
            :checked="filterUserIds.includes(u.id)"
            @change="toggleFilterUser(u.id)"
          />
          {{ u.displayName }}
        </label>
      </div>
    </div>

    <div v-if="error" class="sketch-error" role="alert">{{ error }}</div>
    <div v-else-if="loading" class="loading">Loading…</div>
    <div v-else-if="items.length === 0" class="empty">No todos match.</div>
    <div v-else class="sticky-grid">
      <TodoCard v-for="todo in items" :key="todo.id" :todo="todo" @click="openCard" @vote="onVote" />
    </div>

    <PaginationNav
      v-if="totalCount > 0"
      :page="page"
      :page-size="itemsPerPage"
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
.search-page {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}
.search-toolbar {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 1rem;
}
.search-input {
  flex: 1;
  min-width: 12rem;
}
.include-completed {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  font-size: 0.9rem;
  color: var(--pencil);
  cursor: pointer;
}
.sort-row {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  flex-wrap: wrap;
}
.sort-label,
.sort-dir {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  font-size: 0.9rem;
  color: var(--pencil);
  cursor: pointer;
}
.sort-select {
  width: auto;
  min-width: 8rem;
}
.filter-users {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 0.5rem 1rem;
  font-size: 0.9rem;
  color: var(--pencil);
}
.filter-label {
  font-weight: 500;
}
.filter-user-chk {
  display: flex;
  align-items: center;
  gap: 0.35rem;
  cursor: pointer;
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
.loading,
.empty {
  color: var(--pencil);
  font-size: 0.9rem;
  padding: 1rem 0;
}
</style>
