<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { listBillingPlans } from '@/api/billingPlansService'
import type { BillingPlan } from '@/api/billingPlansService'
import { getTenant, setBillingPlan } from '@/api/tenantService'
import type { Tenant } from '@/api/tenantService'

const plans = ref<BillingPlan[]>([])
const tenant = ref<Tenant | null>(null)
const loading = ref(true)
const error = ref('')
const savingPlanId = ref<string | null>(null)

onMounted(async () => {
  loading.value = true
  error.value = ''
  try {
    const [plansList, tenantData] = await Promise.all([listBillingPlans(), getTenant()])
    plans.value = plansList
    tenant.value = tenantData
  } catch (e: unknown) {
    error.value = e instanceof Error ? e.message : 'Failed to load'
  } finally {
    loading.value = false
  }
})

function discountedPrice(plan: BillingPlan): number | null {
  if (plan.promotionPercent <= 0)
    return null
  return Math.round((plan.pricePerMonth * (100 - plan.promotionPercent) / 100) * 100) / 100
}

function isPlanSelected(plan: BillingPlan): boolean {
  if (!tenant.value)
    return false
  if (tenant.value.billingPlanId === plan.id)
    return true
  if (tenant.value.billingPlanId == null && (plan.code === 'Free' || plan.pricePerMonth === 0))
    return true
  return false
}

async function selectPlan(planId: string) {
  if (!tenant.value)
    return
  savingPlanId.value = planId
  try {
    tenant.value = await setBillingPlan(planId)
  } catch (e: unknown) {
    error.value = e instanceof Error ? e.message : 'Failed to update plan'
  } finally {
    savingPlanId.value = null
  }
}

const userCountText = computed(() => {
  const t = tenant.value
  if (!t || t.billingPlanMaxUsers == null)
    return null
  return `${t.userCount}/${t.billingPlanMaxUsers} Users`
})
</script>

<template>
  <div class="billing-page">
    <h2 class="sketch-title">Billing</h2>
    <p class="intro">Choose your plan. No payment collected yet — this just sets your household plan.</p>

    <div v-if="userCountText" class="user-count-note">{{ userCountText }}</div>

    <div v-if="error" class="sketch-error" role="alert">{{ error }}</div>

    <div v-if="loading" class="loading">Loading…</div>
    <div v-else class="plans">
      <div
        v-for="plan in plans"
        :key="plan.id"
        class="plan-card sketch-box"
        :class="{ selected: isPlanSelected(plan) }"
      >
        <h3 class="plan-name">{{ plan.name }}</h3>
        <p class="plan-users">Up to {{ plan.maxUsers }} Members</p>
        <div class="plan-price">
          <template v-if="discountedPrice(plan) != null">
            <span class="price-original">${{ plan.pricePerMonth.toFixed(2) }}/mo</span>
            <span class="price-sale">${{ discountedPrice(plan)!.toFixed(2) }}/mo</span>
            <span class="sale-badge">On sale ({{ plan.promotionPercent }}% off)</span>
          </template>
          <template v-else>
            <span class="price-current">${{ plan.pricePerMonth.toFixed(2) }}/mo</span>
          </template>
        </div>
        <button
          type="button"
          class="sketch-btn primary plan-select-btn"
          :disabled="savingPlanId !== null || isPlanSelected(plan)"
          @click="selectPlan(plan.id)"
        >
          {{ savingPlanId === plan.id ? '…' : isPlanSelected(plan) ? 'Current plan' : 'Select' }}
        </button>
      </div>
    </div>
  </div>
</template>

<style scoped>
.billing-page {
  max-width: 36rem;
}

.intro {
  margin: 0 0 1rem;
  color: var(--pencil);
  font-size: 0.9rem;
}

.user-count-note {
  margin-bottom: 1rem;
  font-size: 0.9rem;
  color: var(--pencil);
}

.loading {
  color: var(--pencil);
  font-size: 0.9rem;
}

.plans {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(13rem, 1fr));
  gap: 1rem;
}

.plan-card {
  padding: 1.25rem;
  text-align: center;
  display: flex;
  flex-direction: column;
  align-items: stretch;
}

.plan-card.selected {
  border-color: var(--ink);
  box-shadow: 0 0 0 2px var(--line);
}

.plan-name {
  font-size: 1.25rem;
  margin: 0 0 0.25rem;
  font-weight: 600;
}

.plan-users {
  margin: 0 0 0.75rem;
  font-size: 0.85rem;
  color: var(--pencil);
}

.plan-price {
  margin-bottom: 1rem;
  min-height: 4.25rem;
  display: flex;
  flex-direction: column;
  justify-content: flex-start;
}

.plan-select-btn {
  margin-top: auto;
  width: 100%;
}

.price-original {
  display: block;
  text-decoration: line-through;
  color: var(--pencil);
  font-size: 0.9rem;
}

.price-sale {
  font-size: 1.25rem;
  font-weight: 600;
  color: var(--ink);
}

.sale-badge {
  display: inline-block;
  margin-top: 0.25rem;
  font-size: 0.75rem;
  color: var(--pencil);
  background: rgba(0, 0, 0, 0.06);
  padding: 0.15rem 0.4rem;
  border-radius: var(--radius);
}

.price-current {
  font-size: 1.25rem;
  font-weight: 600;
}
</style>
