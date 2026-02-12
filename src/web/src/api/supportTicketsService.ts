import { apiJson } from './apiClientService'

export interface SupportTicket {
  id: string
  tenantId: string
  subject: string
  body: string
  status: string
  createdAt: string
  updatedAt: string | null
}

export interface SupportTicketReply {
  id: string
  supportTicketId: string
  body: string
  isFromStaff: boolean
  createdAt: string
}

export interface SupportTicketWithReplies extends SupportTicket {
  replies: SupportTicketReply[]
}

export function listSupportTickets(): Promise<SupportTicket[]> {
  return apiJson<SupportTicket[]>('/api/support-tickets')
}

export function getSupportTicket(id: string): Promise<SupportTicketWithReplies> {
  return apiJson<SupportTicketWithReplies>(`/api/support-tickets/${id}`)
}

export function createSupportTicket(payload: { subject: string; body: string }): Promise<SupportTicket> {
  return apiJson<SupportTicket>('/api/support-tickets', {
    method: 'POST',
    body: JSON.stringify(payload),
  })
}

export function addSupportTicketReply(id: string, body: string): Promise<SupportTicketReply> {
  return apiJson<SupportTicketReply>(`/api/support-tickets/${id}/replies`, {
    method: 'POST',
    body: JSON.stringify({ body }),
  })
}

export function updateSupportTicketStatus(id: string, status: string): Promise<void> {
  return apiJson<void>(`/api/support-tickets/${id}/status`, {
    method: 'PATCH',
    body: JSON.stringify({ status }),
  })
}
