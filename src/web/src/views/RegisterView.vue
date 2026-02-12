<script setup lang="ts">
import { ref, computed } from 'vue'
import { useRouter } from 'vue-router'
import { register } from '@/api/authService'
import { useAuth } from '@/composables/useAuth'

const router = useRouter()
const { setToken } = useAuth()
const tenantName = ref('')
const ownerEmail = ref('')
const password = ref('')
const ownerDisplayName = ref('')
const error = ref('')
const loading = ref(false)

const canSubmit = computed(() =>
  tenantName.value.trim() !== '' &&
  ownerEmail.value.trim() !== '' &&
  password.value !== '' &&
  ownerDisplayName.value.trim() !== ''
)

async function onSubmit() {
  if (!canSubmit.value || loading.value) return
  error.value = ''
  loading.value = true
  try {
    const res = await register({
      tenantName: tenantName.value.trim(),
      ownerEmail: ownerEmail.value.trim(),
      password: password.value,
      ownerDisplayName: ownerDisplayName.value.trim(),
    })
    if (res.error) {
      error.value = res.error
      return
    }
    if (res.token) {
      setToken(res.token)
      router.push('/app/home')
    }
  } finally {
    loading.value = false
  }
}
</script>

<template>
  <div class="auth-page">
  <div class="sketch-box card">
    <h1 class="sketch-title">Sign up</h1>
    <form @submit.prevent="onSubmit" class="form">
      <label>
        <span class="sketch-label">Household / team name</span>
        <input
          v-model="tenantName"
          type="text"
          class="sketch-input"
          placeholder="e.g. Smith Household"
          autocomplete="organization"
        />
      </label>
      <label>
        <span class="sketch-label">Your name</span>
        <input
          v-model="ownerDisplayName"
          type="text"
          class="sketch-input"
          placeholder="Jane Smith"
          autocomplete="name"
        />
      </label>
      <label>
        <span class="sketch-label">Email</span>
        <input
          v-model="ownerEmail"
          type="email"
          class="sketch-input"
          placeholder="you@example.com"
          autocomplete="email"
        />
      </label>
      <label>
        <span class="sketch-label">Password</span>
        <input
          v-model="password"
          type="password"
          class="sketch-input"
          placeholder="••••••••"
          autocomplete="new-password"
        />
      </label>
      <div v-if="error" class="sketch-error" role="alert">{{ error }}</div>
      <button
        type="submit"
        class="sketch-btn primary submit"
        :disabled="!canSubmit || loading"
      >
        {{ loading ? '…' : 'Create account' }}
      </button>
    </form>
    <p class="footer">
      Already have an account?
      <RouterLink to="/login" class="sketch-link">Sign in</RouterLink>
    </p>
  </div>
  </div>
</template>

<style scoped>
.auth-page {
  min-height: 100vh;
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 1.5rem;
}
.card {
  padding: 1.5rem;
}

.form label {
  display: block;
  margin-bottom: 1rem;
}

.submit {
  width: 100%;
  margin-top: 0.5rem;
  padding: 0.6rem;
}

.footer {
  margin: 1rem 0 0;
  font-size: 0.9rem;
  color: var(--pencil);
}
</style>
