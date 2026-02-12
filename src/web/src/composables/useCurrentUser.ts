import { ref, watch } from 'vue'
import { getCurrentUser } from '@/api/usersService'
import type { UserSummary } from '@/api/usersService'
import { useAuth } from '@/composables/useAuth'

const currentUser = ref<UserSummary | null>(null)

export function useCurrentUser() {
  const { userId } = useAuth()

  watch(
    userId,
    async (id) => {
      if (!id) {
        currentUser.value = null
        return
      }
      try {
        currentUser.value = await getCurrentUser()
      } catch {
        currentUser.value = null
      }
    },
    { immediate: true }
  )

  return {
    currentUser,
    isOwner: () => currentUser.value?.role === 'Owner',
    canManageUsers: () => (currentUser.value?.role === 'Owner') || (currentUser.value?.canCreateUser === true),
    hasAnySettingsAccess: () => {
      const u = currentUser.value
      if (!u) return false
      if (u.role === 'Owner') return true
      if (u.canCreateUser) return true
      return false
    },
  }
}
