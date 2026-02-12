const base = import.meta.env.VITE_API_BASE_URL ?? ''

export interface RegisterPayload {
  tenantName: string
  ownerEmail: string
  password: string
  ownerDisplayName: string
}

export interface LoginPayload {
  email: string
  password: string
  tenantId?: string
}

export interface AuthResponse {
  token?: string
  error?: string
}

export async function register(payload: RegisterPayload): Promise<AuthResponse> {
  const res = await fetch(`${base}/api/auth/register-tenant`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(payload),
  })
  const data = await res.json().catch(() => ({}))
  if (!res.ok) return { error: data.error ?? 'Sign up failed' }
  return { token: data.token }
}

export async function login(payload: LoginPayload): Promise<AuthResponse> {
  const body: Record<string, string> = {
    email: payload.email,
    password: payload.password,
  }
  if (payload.tenantId) body.tenantId = payload.tenantId
  const res = await fetch(`${base}/api/auth/login`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(body),
  })
  const data = await res.json().catch(() => ({}))
  if (!res.ok) return { error: data.error ?? 'Sign in failed' }
  return { token: data.token }
}
