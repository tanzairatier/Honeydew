<script setup lang="ts">
import { ref, computed } from 'vue'
import { useRouter } from 'vue-router'
import { login } from '@/api/authService'
import { useAuth } from '@/composables/useAuth'

const router = useRouter()
const { setToken } = useAuth()
const email = ref('')
const password = ref('')
const error = ref('')
const loading = ref(false)

const canSubmit = computed(() =>
  email.value.trim() !== '' && password.value !== ''
)

async function onSubmit() {
  if (!canSubmit.value || loading.value) return
  error.value = ''
  loading.value = true
  try {
    const res = await login({
      email: email.value.trim(),
      password: password.value,
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
    <h1 class="sketch-title">Sign in</h1>
    <form @submit.prevent="onSubmit" class="form">
      <label>
        <span class="sketch-label">Email</span>
        <input
          v-model="email"
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
          autocomplete="current-password"
        />
      </label>
      <div v-if="error" class="sketch-error" role="alert">{{ error }}</div>
      <button
        type="submit"
        class="sketch-btn primary submit"
        :disabled="!canSubmit || loading"
      >
        {{ loading ? '…' : 'Sign in' }}
      </button>
    </form>
    <p class="footer">
      No account?
      <RouterLink to="/register" class="sketch-link">Sign up</RouterLink>
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
