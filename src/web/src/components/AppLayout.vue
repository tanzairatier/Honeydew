<script setup lang="ts">
import { ref, onMounted, computed } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { useAuth } from '@/composables/useAuth'
import { useCurrentUser } from '@/composables/useCurrentUser'

const router = useRouter()
const route = useRoute()
const { tenantName, setToken, loadTenant } = useAuth()
const { isOwner, canManageUsers, hasAnySettingsAccess } = useCurrentUser()
const showCredits = ref(false)

const isSettings = computed(() => route.path.startsWith('/app/settings'))
const isItems = computed(() => route.path.startsWith('/app/items'))

onMounted(() => {
  loadTenant()
})

function goHome() {
  router.push('/app/home')
}

function goSettings() {
  if (canManageUsers()) router.push('/app/settings/users')
  else if (isOwner()) router.push('/app/settings/general')
  else router.push('/app/home')
}

function signOut() {
  setToken(null)
  router.push('/login')
}
</script>

<template>
  <div class="notepad">
    <!-- Top margin: rolled-paper / branding -->
    <header class="notepad-header">
      <h1 class="notepad-title">{{ tenantName || 'Honeydew' }}</h1>
    </header>

    <!-- Tab bar: left tabs + cog right -->
    <nav class="notepad-tabs" v-if="!isSettings">
      <div class="tabs-left">
        <router-link to="/app/home" class="tab" active-class="tab-active">Home</router-link>
        <router-link to="/app/items/your-items" class="tab" :class="{ 'tab-active': isItems }">Items</router-link>
        <router-link to="/app/preferences" class="tab" active-class="tab-active">Preferences</router-link>
      </div>
      <div class="tabs-right">
        <router-link to="/app/profile" class="tab-icon" aria-label="My Profile" title="My Profile">
          <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
            <path d="M20 21v-2a4 4 0 0 0-4-4H8a4 4 0 0 0-4 4v2"/>
            <circle cx="12" cy="7" r="4"/>
          </svg>
        </router-link>
        <button v-if="hasAnySettingsAccess()" type="button" class="tab-icon" aria-label="Settings" @click="goSettings">
          <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
            <circle cx="12" cy="12" r="3"/>
            <path d="M19.4 15a1.65 1.65 0 0 0 .33 1.82l.06.06a2 2 0 0 1 0 2.83 2 2 0 0 1-2.83 0l-.06-.06a1.65 1.65 0 0 0-1.82-.33 1.65 1.65 0 0 0-1 1.51V21a2 2 0 0 1-2 2 2 2 0 0 1-2-2v-.09A1.65 1.65 0 0 0 9 19.4a1.65 1.65 0 0 0-1.82.33l-.06.06a2 2 0 0 1-2.83 0 2 2 0 0 1 0-2.83l.06-.06a1.65 1.65 0 0 0 .33-1.82 1.65 1.65 0 0 0-1.51-1H3a2 2 0 0 1-2-2 2 2 0 0 1 2-2h.09A1.65 1.65 0 0 0 4.6 9a1.65 1.65 0 0 0-.33-1.82l-.06-.06a2 2 0 0 1 0-2.83 2 2 0 0 1 2.83 0l.06.06a1.65 1.65 0 0 0 1.82.33H9a1.65 1.65 0 0 0 1-1.51V3a2 2 0 0 1 2-2 2 2 0 0 1 2 2v.09a1.65 1.65 0 0 0 1 1.51 1.65 1.65 0 0 0 1.82-.33l.06-.06a2 2 0 0 1 2.83 0 2 2 0 0 1 0 2.83l-.06.06a1.65 1.65 0 0 0-.33 1.82V9a1.65 1.65 0 0 0 1.51 1H21a2 2 0 0 1 2 2 2 2 0 0 1-2 2h-.09a1.65 1.65 0 0 0-1.51 1z"/>
          </svg>
        </button>
        <button type="button" class="sign-out-btn" @click="signOut">Sign out</button>
      </div>
    </nav>

    <!-- Settings: back + left nav -->
    <nav class="notepad-settings-nav" v-else>
      <button type="button" class="sketch-btn back-btn" @click="goHome">← Back</button>
      <div class="settings-tabs">
        <router-link v-if="canManageUsers()" to="/app/settings/users" class="settings-tab" active-class="settings-tab-active">Users</router-link>
        <router-link v-if="isOwner()" to="/app/settings/general" class="settings-tab" active-class="settings-tab-active">General</router-link>
        <router-link v-if="isOwner()" to="/app/settings/billing" class="settings-tab" active-class="settings-tab-active">Billing</router-link>
        <router-link v-if="isOwner()" to="/app/settings/support" class="settings-tab" active-class="settings-tab-active">Support</router-link>
      </div>
    </nav>

    <!-- Main content -->
    <main class="notepad-main">
      <RouterView />
    </main>

    <footer class="notepad-footer">
      <button type="button" class="powered-by" @click="showCredits = true">
        About
      </button>
    </footer>

    <!-- Credits modal -->
    <Teleport to="body">
      <div class="modal-overlay" v-if="showCredits" @click.self="showCredits = false">
        <div class="sketch-box modal">
          <p class="sketch-title">Honeydew</p>
          <p>Honeydew was made by Joe Krall.</p>
          <button type="button" class="sketch-btn primary" @click="showCredits = false">OK</button>
        </div>
      </div>
    </Teleport>
  </div>
</template>

<style scoped>
.notepad {
  min-height: 100vh;
  display: flex;
  flex-direction: column;
  max-width: 56rem;
  margin: 0 auto;
  background: var(--paper);
}

.notepad-header {
  padding: 1rem 1.5rem 0.5rem;
  border-bottom: var(--border-width) solid var(--line);
  box-shadow: 0 2px 0 var(--line);
}

.notepad-title {
  font-family: 'Caveat', cursive;
  font-weight: 600;
  font-size: 1.75rem;
  margin: 0;
  color: var(--ink);
}

.notepad-tabs {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 0.25rem 1rem;
  border-bottom: var(--border-width) solid var(--line);
  gap: 0.5rem;
}

.tabs-left {
  display: flex;
  gap: 0.25rem;
}

.tab {
  padding: 0.4rem 0.75rem;
  font-size: 0.85rem;
  color: var(--pencil);
  text-decoration: none;
  border: var(--border-width) solid transparent;
  border-bottom: none;
  border-radius: var(--radius) var(--radius) 0 0;
  margin-bottom: -2px;
  background: rgba(0,0,0,0.04);
}

.tab:hover {
  color: var(--ink);
}

.tab-active {
  color: var(--ink);
  background: var(--paper);
  border-color: var(--line);
}

.tab-icon {
  padding: 0.35rem;
  background: none;
  border: none;
  cursor: pointer;
  color: var(--pencil);
  display: inline-flex;
  align-items: center;
  justify-content: center;
  text-decoration: none;
}
.tab-icon:hover {
  color: var(--ink);
}
.tab-cog {
  padding: 0.35rem;
  background: none;
  border: none;
  cursor: pointer;
  color: var(--pencil);
}

.tabs-right {
  display: flex;
  align-items: center;
  gap: 0.5rem;
}

.tab-cog:hover {
  color: var(--ink);
}

.sign-out-btn {
  font-size: 0.8rem;
  color: var(--pencil);
  background: none;
  border: none;
  cursor: pointer;
  text-decoration: underline;
}

.sign-out-btn:hover {
  color: var(--ink);
}

.notepad-settings-nav {
  display: flex;
  align-items: flex-start;
  gap: 1rem;
  padding: 0.75rem 1rem;
  border-bottom: var(--border-width) solid var(--line);
}

.back-btn {
  flex-shrink: 0;
}

.settings-tabs {
  display: flex;
  flex-direction: column;
  gap: 0.25rem;
}

.settings-tab {
  padding: 0.35rem 0.5rem;
  font-size: 0.85rem;
  color: var(--pencil);
  text-decoration: none;
  border-left: 2px solid transparent;
  padding-left: 0.75rem;
}

.settings-tab:hover:not(.disabled) {
  color: var(--ink);
}

.settings-tab-active {
  color: var(--ink);
  border-left-color: var(--line);
}

.settings-tab.disabled {
  opacity: 0.5;
  cursor: default;
}

.notepad-main {
  flex: 1;
  padding: 1rem 1.5rem 1.5rem;
}

.notepad-footer {
  padding: 0.5rem 1.5rem 1rem;
  border-top: var(--border-width) solid var(--line);
}

.powered-by {
  font-size: 0.75rem;
  color: var(--pencil);
  background: none;
  border: none;
  cursor: pointer;
  text-decoration: underline;
  text-underline-offset: 2px;
}

.powered-by:hover {
  color: var(--ink);
}

.modal-overlay {
  position: fixed;
  inset: 0;
  background: rgba(0,0,0,0.3);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 100;
}

.modal {
  padding: 1.5rem;
  min-width: 16rem;
}

.modal p {
  margin: 0 0 1rem;
  font-size: 0.9rem;
}
</style>
