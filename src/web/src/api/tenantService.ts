import { apiJson } from './apiClientService'

export interface Tenant {
  id: string
  name: string
  createdAt: string
  billingPlanId: string | null
  billingPlanName: string | null
  billingPlanCode: string | null
  billingPlanMaxUsers: number | null
  userCount: number
}

export function getTenant(): Promise<Tenant> {
  return apiJson<Tenant>('/api/tenant')
}

export function updateTenantName(name: string): Promise<Tenant> {
  return apiJson<Tenant>('/api/tenant', {
    method: 'PATCH',
    body: JSON.stringify({ name }),
  })
}

export function setBillingPlan(billingPlanId: string | null): Promise<Tenant> {
  return apiJson<Tenant>('/api/tenant/billing-plan', {
    method: 'PUT',
    body: JSON.stringify({ billingPlanId }),
  })
}
