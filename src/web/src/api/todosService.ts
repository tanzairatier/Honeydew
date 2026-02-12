import { apiFetch, apiJson } from './apiClientService'

export interface TodoItem {
  id: string
  title: string
  notes: string | null
  isDone: boolean
  completedAt: string | null
  dueDate: string | null
  createdAt: string
  createdByUserId: string
  assignedToUserId: string | null
  voteCount: number
  currentUserVoted: boolean
}

export interface TodosPage {
  items: TodoItem[]
  totalCount: number
  page: number
  pageSize: number
}

export function listTodos(params: {
  page?: number
  pageSize?: number
  onlyMine?: boolean
  includeCompleted?: boolean
  search?: string
  sortBy?: string
  sortDesc?: boolean
  assignedToUserIds?: string[]
}): Promise<TodosPage> {
  const sp = new URLSearchParams()
  if (params.page != null) sp.set('page', String(params.page))
  if (params.pageSize != null) sp.set('pageSize', String(params.pageSize))
  if (params.onlyMine != null) sp.set('onlyMine', String(params.onlyMine))
  if (params.includeCompleted != null) sp.set('includeCompleted', String(params.includeCompleted))
  if (params.search) sp.set('search', params.search)
  if (params.sortBy) sp.set('sortBy', params.sortBy)
  if (params.sortDesc != null) sp.set('sortDesc', String(params.sortDesc))
  if (params.assignedToUserIds?.length) params.assignedToUserIds.forEach((id) => sp.append('assignedToUserIds', id))
  const q = sp.toString()
  return apiJson<TodosPage>(`/api/todos${q ? `?${q}` : ''}`)
}

export function getAssignedToMe(take = 3): Promise<TodoItem[]> {
  return apiJson<TodoItem[]>(`/api/todos/assigned-to-me?take=${take}`)
}

export function toggleTodoVote(id: string): Promise<{ Voted: boolean }> {
  return apiJson<{ Voted: boolean }>(`/api/todos/${id}/vote`, { method: 'POST' })
}

export function createTodo(payload: {
  title: string
  notes?: string
  assignedToUserId: string
  dueDate?: string | null
}): Promise<TodoItem> {
  return apiJson<TodoItem>('/api/todos', {
    method: 'POST',
    body: JSON.stringify(payload),
  })
}

export function updateTodo(
  id: string,
  payload: {
    title?: string
    notes?: string
    assignedToUserId?: string | null
    dueDate?: string | null
    isDone?: boolean
    completedAt?: string | null
  }
): Promise<TodoItem> {
  return apiJson<TodoItem>(`/api/todos/${id}`, {
    method: 'PUT',
    body: JSON.stringify(payload),
  })
}

export function getTodo(id: string): Promise<TodoItem> {
  return apiJson<TodoItem>(`/api/todos/${id}`)
}

export async function exportTodosCsv(onlyMine: boolean): Promise<Blob> {
  const q = onlyMine ? '?onlyMine=true' : '?onlyMine=false'
  const res = await apiFetch(`/api/todos/export${q}`)
  if (!res.ok) {
    const data = await res.json().catch(() => ({}))
    throw new Error((data as { error?: string }).error ?? `Export failed: ${res.status}`)
  }
  return res.blob()
}
