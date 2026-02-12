import { apiFetch, apiJson } from './apiClientService'

export interface UserSummary {
  id: string
  email: string
  displayName: string
  role: string
  canViewAllTodos: boolean
  canEditAllTodos: boolean
  canCreateUser: boolean
  isActive: boolean
  createdAt: string
}

export function listUsers(activeOnly = false, forAssignmentOnly = false): Promise<UserSummary[]> {
  const params = new URLSearchParams()
  if (activeOnly) params.set('activeOnly', 'true')
  if (forAssignmentOnly) params.set('forAssignmentOnly', 'true')
  const q = params.toString() ? `?${params.toString()}` : ''
  return apiJson<UserSummary[]>(`/api/users${q}`)
}

export function getCurrentUser(): Promise<UserSummary> {
  return apiJson<UserSummary>('/api/users/me')
}

export function updateCurrentUser(payload: { displayName?: string; email?: string }): Promise<UserSummary> {
  return apiJson<UserSummary>('/api/users/me', {
    method: 'PATCH',
    body: JSON.stringify(payload),
  })
}

export function getMyPreferences(): Promise<{ itemsPerPage: number }> {
  return apiJson<{ itemsPerPage: number }>('/api/users/me/preferences')
}

export function updateMyPreferences(payload: { itemsPerPage: number }): Promise<{ itemsPerPage: number }> {
  return apiJson<{ itemsPerPage: number }>('/api/users/me/preferences', {
    method: 'PUT',
    body: JSON.stringify(payload),
  })
}

export interface CreateUserPayload {
  email: string
  displayName: string
  password: string
  role: string
  canViewAllTodos: boolean
  canEditAllTodos: boolean
  canCreateUser: boolean
}

export function createUser(payload: CreateUserPayload): Promise<UserSummary> {
  return apiJson<UserSummary>('/api/users', {
    method: 'POST',
    body: JSON.stringify(payload),
  })
}

export interface UpdateUserPayload {
  displayName?: string
  role?: string
  canViewAllTodos?: boolean
  canEditAllTodos?: boolean
  canCreateUser?: boolean
  isActive?: boolean
}

export function updateUser(id: string, payload: UpdateUserPayload): Promise<UserSummary> {
  return apiJson<UserSummary>(`/api/users/${id}`, {
    method: 'PUT',
    body: JSON.stringify(payload),
  })
}

export async function deleteUser(id: string): Promise<void> {
  const res = await apiFetch(`/api/users/${id}`, { method: 'DELETE' })
  if (!res.ok) {
    const data = await res.json().catch(() => ({}))
    throw new Error((data as { error?: string }).error ?? `Delete failed: ${res.status}`)
  }
}
