import { ref, computed } from 'vue'
import { getTenant } from '@/api/tenantService'
import { TOKEN_KEY, setAuthToken } from '@/api/apiClientService'

const token = ref<string | null>(typeof localStorage !== 'undefined' ? localStorage.getItem(TOKEN_KEY) : null)
const tenantName = ref<string>('')

function parseUserIdFromToken(t: string | null): string | null {
  if (!t) return null
  try {
    const payload = t.split('.')[1]
    if (!payload) return null
    const decoded = JSON.parse(atob(payload.replace(/-/g, '+').replace(/_/g, '/')))
    return decoded.sub ?? null
  } catch {
    return null
  }
}

// Keep API client in-memory token in sync so requests get the token immediately after sign-in
if (typeof localStorage !== 'undefined') setAuthToken(token.value)

export function useAuth() {
  const isAuthenticated = computed(() => !!token.value)
  const userId = computed(() => parseUserIdFromToken(token.value))

  async function loadTenant() {
    if (!token.value) return
    try {
      const t = await getTenant()
      tenantName.value = t.name
    } catch {
      tenantName.value = ''
    }
  }

  function setTenantName(name: string) {
    tenantName.value = name
  }

  function setToken(newToken: string | null) {
    token.value = newToken
    setAuthToken(newToken)
    if (typeof localStorage !== 'undefined') {
      if (newToken) localStorage.setItem(TOKEN_KEY, newToken)
      else localStorage.removeItem(TOKEN_KEY)
    }
    tenantName.value = ''
  }

  return { token, tenantName, userId, isAuthenticated, loadTenant, setToken, setTenantName }
}
