import { ApiError } from './apiError'

const base = (import.meta.env.VITE_API_BASE_URL ?? '').replace(/\/$/, '')

export const TOKEN_KEY = 'honeydew_token'
export { ApiError, getErrorMessage } from './apiError'

// In-memory token so the API client sees the token immediately after sign-in
// (avoids any timing gap with localStorage). Composable syncs this on setToken.
let inMemoryToken: string | null = null

/** Called by auth composable when token is set/cleared. Keeps client in sync. */
export function setAuthToken(token: string | null): void {
  inMemoryToken = token
}

function getToken(): string | null {
  return inMemoryToken ?? (typeof localStorage !== 'undefined' ? localStorage.getItem(TOKEN_KEY) : null)
}

function clearTokenAndRedirectToLogin(): void {
  inMemoryToken = null
  if (typeof localStorage !== 'undefined') localStorage.removeItem(TOKEN_KEY)
  if (typeof window !== 'undefined' && !window.location.pathname.startsWith('/login')) {
    window.location.href = `${window.location.origin}/login`
  }
}

function buildUrl(path: string): string {
  if (!base) return path
  const p = path.startsWith('/') ? path : `/${path}`
  return `${base}${p}`
}

export async function apiFetch(path: string, init: RequestInit = {}): Promise<Response> {
  const token = getToken()
  const headers = new Headers(init.headers)
  if (init.body !== undefined) {
    headers.set('Content-Type', 'application/json')
  }
  if (token) {
    headers.set('Authorization', `Bearer ${token}`)
  }
  const res = await fetch(buildUrl(path), { ...init, headers, credentials: 'omit' })
  if (res.status === 401) {
    clearTokenAndRedirectToLogin()
    const data = await res.json().catch(() => ({}))
    throw ApiError.fromResponse(data, 401)
  }
  return res
}

export async function apiJson<T>(path: string, init?: RequestInit): Promise<T> {
  const res = await apiFetch(path, init)
  const data = await res.json().catch(() => ({}))
  if (!res.ok) {
    throw ApiError.fromResponse(data, res.status)
  }
  return data as T
}
