import { apiJson } from './apiClientService'

export interface BillingPlan {
  id: string
  name: string
  code: string
  maxUsers: number
  pricePerMonth: number
  promotionPercent: number
}

export function listBillingPlans(): Promise<BillingPlan[]> {
  return apiJson<BillingPlan[]>('/api/billing-plans')
}
