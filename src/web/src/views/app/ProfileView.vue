<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { getCurrentUser, updateCurrentUser } from '@/api/usersService'
import type { UserSummary } from '@/api/usersService'

const user = ref<UserSummary | null>(null)
const displayName = ref('')
const email = ref('')
const saving = ref(false)
const error = ref('')
const message = ref('')

onMounted(async () => {
  try {
    user.value = await getCurrentUser()
    if (user.value) {
      displayName.value = user.value.displayName
      email.value = user.value.email
    }
  } catch {
    error.value = 'Failed to load profile'
  }
})

async function save() {
  error.value = ''
  message.value = ''
  const d = displayName.value.trim()
  const e = email.value.trim().toLowerCase()
  if (!e) {
    error.value = 'Email is required'
    return
  }
  saving.value = true
  try {
    user.value = await updateCurrentUser({ displayName: d || undefined, email: e })
    displayName.value = user.value.displayName
    email.value = user.value.email
    message.value = 'Saved.'
  } catch (err: unknown) {
    error.value = err instanceof Error ? err.message : 'Failed to save'
  } finally {
    saving.value = false
  }
}
</script>

<template>
  <div class="profile-page">
    <h2 class="sketch-title">My Profile</h2>
    <p class="intro">Update your display name and email.</p>
    <form v-if="user" @submit.prevent="save" class="profile-form">
      <label>
        <span class="sketch-label">Display name</span>
        <input v-model="displayName" type="text" class="sketch-input" />
      </label>
      <label>
        <span class="sketch-label">Email</span>
        <input v-model="email" type="email" class="sketch-input" required />
      </label>
      <div v-if="error" class="sketch-error" role="alert">{{ error }}</div>
      <div v-if="message" class="sketch-message">{{ message }}</div>
      <div class="form-actions">
        <button type="submit" class="sketch-btn primary" :disabled="saving">
          {{ saving ? 'Saving…' : 'Save' }}
        </button>
      </div>
    </form>
    <p v-else-if="!user && !error" class="loading">Loading…</p>
  </div>
</template>

<style scoped>
.profile-page {
  max-width: 28rem;
}
.intro {
  margin: 0 0 1rem;
  color: var(--pencil);
  font-size: 0.9rem;
}
.profile-form label {
  display: block;
  margin-bottom: 1rem;
}
.form-actions {
  margin-top: 1rem;
}
.sketch-message {
  color: var(--pencil);
  font-size: 0.9rem;
  margin-top: 0.5rem;
}
.loading {
  color: var(--pencil);
}
</style>
