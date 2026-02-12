import { ref, watch } from 'vue'
import { getMyPreferences, updateMyPreferences } from '@/api/usersService'
import { useAuth } from '@/composables/useAuth'

const DEFAULT = 9
const OPTIONS = [9, 12, 15, 18, 21, 24] as const

const itemsPerPage = ref(DEFAULT)

export function usePreferences() {
  const { userId } = useAuth()

  watch(
    userId,
    async (id) => {
      if (!id) return
      try {
        const prefs = await getMyPreferences()
        if (OPTIONS.includes(prefs.itemsPerPage as (typeof OPTIONS)[number]))
          itemsPerPage.value = prefs.itemsPerPage
      } catch {
        // keep current value
      }
    },
    { immediate: true }
  )

  return {
    itemsPerPage,
    itemsPerPageOptions: OPTIONS,
    async setItemsPerPage(v: number) {
      if (!OPTIONS.includes(v as (typeof OPTIONS)[number])) return
      itemsPerPage.value = v
      if (userId.value) {
        try {
          await updateMyPreferences({ itemsPerPage: v })
        } catch {
          // ignore
        }
      }
    },
  }
}
