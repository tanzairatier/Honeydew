<script setup lang="ts">
import { usePreferences } from '@/composables/usePreferences'

const { itemsPerPage, itemsPerPageOptions, setItemsPerPage } = usePreferences()
</script>

<template>
  <div class="preferences-page">
    <h2 class="sketch-title">Preferences</h2>
    <label class="pref-label">
      <span class="sketch-label">Items per page (sticky notes)</span>
      <select
        :value="itemsPerPage"
        class="sketch-input pref-select"
        @change="setItemsPerPage(Number(($event.target as HTMLSelectElement).value))"
      >
        <option v-for="n in itemsPerPageOptions" :key="n" :value="n">{{ n }}</option>
      </select>
    </label>
    <p class="hint">Used on Your Items and Search. Multiples of 3 for a neat grid.</p>
  </div>
</template>

<style scoped>
.preferences-page {
  max-width: 28rem;
}
.pref-label {
  display: block;
  margin-bottom: 0.5rem;
}
.pref-label .sketch-label {
  margin-bottom: 0.25rem;
}
.pref-select {
  max-width: 6rem;
  padding: 0.5rem 0.6rem;
  cursor: pointer;
}
.hint {
  font-size: 0.8rem;
  color: var(--pencil);
  margin: 0.5rem 0 0;
}
</style>
