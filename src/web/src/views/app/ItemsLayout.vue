<script setup lang="ts">
import { useRoute } from 'vue-router'
</script>

<template>
  <div class="items-layout">
    <nav class="items-subnav">
      <router-link to="/app/items/your-items" class="items-tab" active-class="items-tab-active">Your Items</router-link>
      <router-link to="/app/items/search" class="items-tab" active-class="items-tab-active">Search</router-link>
      <router-link to="/app/items/export" class="items-tab" active-class="items-tab-active">Export</router-link>
    </nav>
    <div class="items-content">
      <RouterView />
    </div>
  </div>
</template>

<style scoped>
.items-layout {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}
.items-subnav {
  display: flex;
  gap: 0.25rem;
  flex-wrap: wrap;
}
.items-tab {
  padding: 0.4rem 0.75rem;
  font-size: 0.85rem;
  color: var(--pencil);
  text-decoration: none;
  border: var(--border-width) solid transparent;
  border-radius: var(--radius);
  background: rgba(0, 0, 0, 0.04);
}
.items-tab:hover {
  color: var(--ink);
}
.items-tab-active {
  color: var(--ink);
  background: var(--paper);
  border-color: var(--line);
  box-shadow: 1px 1px 0 var(--line);
}
.items-content {
  min-height: 0;
}
</style>
