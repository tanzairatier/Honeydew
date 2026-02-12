<script setup lang="ts">
import { ref, onMounted } from 'vue'
import {
  listSupportTickets,
  createSupportTicket,
  getSupportTicket,
  addSupportTicketReply,
  updateSupportTicketStatus,
} from '@/api/supportTicketsService'
import type { SupportTicket, SupportTicketWithReplies } from '@/api/supportTicketsService'

const tickets = ref<SupportTicket[]>([])
const loading = ref(true)
const error = ref('')
const showCreate = ref(false)
const createSubject = ref('')
const createBody = ref('')
const createSaving = ref(false)
const createError = ref('')

const selectedTicket = ref<SupportTicketWithReplies | null>(null)
const ticketLoading = ref(false)
const replyBody = ref('')
const replySaving = ref(false)
const replyError = ref('')
const statusSaving = ref(false)

async function load() {
  loading.value = true
  error.value = ''
  try {
    tickets.value = await listSupportTickets()
  } catch (e: unknown) {
    error.value = e instanceof Error ? e.message : 'Failed to load'
  } finally {
    loading.value = false
  }
}

onMounted(load)

function openCreate() {
  createSubject.value = ''
  createBody.value = ''
  createError.value = ''
  showCreate.value = true
}

function closeCreate() {
  showCreate.value = false
}

async function openTicket(id: string) {
  selectedTicket.value = null
  replyBody.value = ''
  replyError.value = ''
  ticketLoading.value = true
  try {
    selectedTicket.value = await getSupportTicket(id)
  } catch (e: unknown) {
    replyError.value = e instanceof Error ? e.message : 'Failed to load ticket'
  } finally {
    ticketLoading.value = false
  }
}

function closeTicketModal() {
  selectedTicket.value = null
  load()
}

async function submitReply() {
  if (!selectedTicket.value) return
  const body = replyBody.value.trim()
  if (!body) return
  replyError.value = ''
  replySaving.value = true
  try {
    const reply = await addSupportTicketReply(selectedTicket.value.id, body)
    selectedTicket.value = {
      ...selectedTicket.value,
      replies: [...selectedTicket.value.replies, reply],
    }
    replyBody.value = ''
  } catch (e: unknown) {
    replyError.value = e instanceof Error ? e.message : 'Failed to add reply'
  } finally {
    replySaving.value = false
  }
}

async function setStatus(newStatus: string) {
  if (!selectedTicket.value) return
  statusSaving.value = true
  try {
    await updateSupportTicketStatus(selectedTicket.value.id, newStatus)
    selectedTicket.value = { ...selectedTicket.value, status: newStatus }
    const idx = tickets.value.findIndex((t) => t.id === selectedTicket.value!.id)
    if (idx >= 0) {
      tickets.value = tickets.value.slice(0, idx).concat(
        [{ ...tickets.value[idx], status: newStatus }],
        tickets.value.slice(idx + 1),
      )
    }
  } finally {
    statusSaving.value = false
  }
}

async function submitCreate() {
  const subject = createSubject.value.trim()
  if (!subject) {
    createError.value = 'Subject is required'
    return
  }
  createError.value = ''
  createSaving.value = true
  try {
    await createSupportTicket({ subject, body: createBody.value.trim() })
    closeCreate()
    load()
  } catch (e: unknown) {
    createError.value = e instanceof Error ? e.message : 'Failed to create'
  } finally {
    createSaving.value = false
  }
}
</script>

<template>
  <div class="support-page">
    <h2 class="sketch-title">Support</h2>
    <p class="intro">Submit a ticket for help. Your household ID is stored so we can assist you.</p>

    <div v-if="error" class="sketch-error" role="alert">{{ error }}</div>

    <div class="toolbar">
      <button type="button" class="sketch-btn primary" @click="openCreate">New ticket</button>
    </div>

    <div v-if="showCreate" class="sketch-box create-form">
      <h3 class="sketch-title">New support ticket</h3>
      <label>
        <span class="sketch-label">Subject</span>
        <input v-model="createSubject" type="text" class="sketch-input" placeholder="Brief summary" />
      </label>
      <label>
        <span class="sketch-label">Details</span>
        <textarea v-model="createBody" class="sketch-input" rows="4" placeholder="Describe your issue…"></textarea>
      </label>
      <div v-if="createError" class="sketch-error" role="alert">{{ createError }}</div>
      <div class="form-actions">
        <button type="button" class="sketch-btn" @click="closeCreate">Cancel</button>
        <button type="button" class="sketch-btn primary" :disabled="createSaving" @click="submitCreate">
          {{ createSaving ? '…' : 'Submit' }}
        </button>
      </div>
    </div>

    <div v-if="loading" class="loading">Loading…</div>
    <div v-else-if="tickets.length === 0" class="empty-state sketch-box">
      <p class="empty-state-text">No support tickets yet.</p>
      <p class="empty-state-hint">Open a ticket if you need help.</p>
      <button type="button" class="sketch-btn primary" @click="openCreate">New ticket</button>
    </div>
    <div v-else class="ticket-grid">
      <button
        v-for="t in tickets"
        :key="t.id"
        type="button"
        class="support-card sketch-box support-card-btn"
        @click="openTicket(t.id)"
      >
        <div class="support-card-header">
          <span class="support-card-subject">{{ t.subject }}</span>
          <span class="support-card-status">{{ t.status }}</span>
        </div>
        <p v-if="t.body" class="support-card-body">{{ t.body }}</p>
        <div class="support-card-meta">
          {{ new Date(t.createdAt).toLocaleString() }}
        </div>
      </button>
    </div>

    <!-- Ticket detail modal (thread) -->
    <Teleport to="body">
      <div v-if="selectedTicket !== null" class="modal-backdrop" @click.self="closeTicketModal">
        <div class="modal-content sketch-box ticket-modal">
          <div class="ticket-modal-header">
            <h3 class="sketch-title">{{ selectedTicket.subject }}</h3>
            <span class="ticket-modal-status" :class="selectedTicket.status.toLowerCase()">{{ selectedTicket.status }}</span>
            <button type="button" class="modal-close" aria-label="Close" @click="closeTicketModal">×</button>
          </div>
          <div v-if="ticketLoading" class="loading">Loading…</div>
          <template v-else>
            <div class="thread">
              <div class="thread-item user-message">
                <div class="thread-meta">{{ new Date(selectedTicket.createdAt).toLocaleString() }} · You</div>
                <div class="thread-body">{{ selectedTicket.body || '—' }}</div>
              </div>
              <div v-for="r in selectedTicket.replies" :key="r.id" class="thread-item" :class="r.isFromStaff ? 'staff-message' : 'user-message'">
                <div class="thread-meta">
                  {{ new Date(r.createdAt).toLocaleString() }} · {{ r.isFromStaff ? 'Support' : 'You' }}
                </div>
                <div class="thread-body">{{ r.body }}</div>
              </div>
            </div>
            <div v-if="selectedTicket.status === 'Open'" class="reply-section">
              <label>
                <span class="sketch-label">Add a reply</span>
                <textarea v-model="replyBody" class="sketch-input" rows="3" placeholder="Type your message…"></textarea>
              </label>
              <div v-if="replyError" class="sketch-error" role="alert">{{ replyError }}</div>
              <div class="form-actions">
                <button type="button" class="sketch-btn primary" :disabled="replySaving || !replyBody.trim()" @click="submitReply">
                  {{ replySaving ? '…' : 'Send reply' }}
                </button>
              </div>
            </div>
            <div class="ticket-modal-actions">
              <button
                v-if="selectedTicket.status === 'Open'"
                type="button"
                class="sketch-btn"
                :disabled="statusSaving"
                @click="setStatus('Closed')"
              >
                {{ statusSaving ? '…' : 'Close ticket' }}
              </button>
              <button v-else type="button" class="sketch-btn" :disabled="statusSaving" @click="setStatus('Open')">
                {{ statusSaving ? '…' : 'Reopen ticket' }}
              </button>
            </div>
          </template>
        </div>
      </div>
    </Teleport>
  </div>
</template>

<style scoped>
.support-page {
  max-width: 40rem;
}

.intro {
  margin: 0 0 1rem;
  color: var(--pencil);
  font-size: 0.9rem;
}

.toolbar {
  margin-bottom: 0.75rem;
}

.create-form {
  padding: 1rem;
  margin-bottom: 1rem;
}

.create-form label {
  display: block;
  margin-bottom: 1rem;
}

.form-actions {
  display: flex;
  gap: 0.5rem;
  margin-top: 0.5rem;
}

.loading {
  color: var(--pencil);
  font-size: 0.9rem;
  padding: 1rem 0;
}

.empty-state {
  padding: 2rem;
  text-align: center;
  margin-top: 0.5rem;
}
.empty-state-text {
  font-size: 1.1rem;
  color: var(--ink);
  margin: 0 0 0.25rem 0;
}
.empty-state-hint {
  color: var(--pencil);
  font-size: 0.9rem;
  margin: 0 0 1rem 0;
}
.empty-state .sketch-btn {
  margin: 0 auto;
}

.ticket-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(18rem, 1fr));
  gap: 1rem;
}

.support-card {
  padding: 1rem;
  display: flex;
  flex-direction: column;
  text-align: left;
  background: #fefce8;
}

.support-card-header {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  gap: 0.5rem;
  margin-bottom: 0.5rem;
}

.support-card-subject {
  font-weight: 600;
  word-break: break-word;
}

.support-card-status {
  font-size: 0.75rem;
  color: var(--pencil);
  flex-shrink: 0;
}

.support-card-body {
  margin: 0 0 0.5rem;
  font-size: 0.85rem;
  color: var(--pencil);
  line-height: 1.4;
  display: -webkit-box;
  -webkit-line-clamp: 3;
  -webkit-box-orient: vertical;
  overflow: hidden;
}

.support-card-meta {
  margin-top: auto;
  font-size: 0.75rem;
  color: var(--pencil);
}

.support-card-btn {
  cursor: pointer;
  border: 1px solid var(--border);
  text-align: left;
  width: 100%;
}
.support-card-btn:hover {
  border-color: var(--ink);
  background: #fef9c3;
}

/* Modal */
.modal-backdrop {
  position: fixed;
  inset: 0;
  background: rgba(0, 0, 0, 0.4);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 1000;
  padding: 1rem;
}

.modal-content {
  max-width: 32rem;
  width: 100%;
  max-height: 90vh;
  overflow: auto;
}

.ticket-modal {
  padding: 1rem;
}

.ticket-modal-header {
  display: flex;
  align-items: flex-start;
  gap: 0.5rem;
  margin-bottom: 1rem;
}

.ticket-modal-header .sketch-title {
  flex: 1;
  margin: 0;
}

.ticket-modal-status {
  font-size: 0.75rem;
  padding: 0.2rem 0.5rem;
  border-radius: 4px;
  flex-shrink: 0;
}
.ticket-modal-status.open {
  background: #dcfce7;
  color: #166534;
}
.ticket-modal-status.closed {
  background: #f3f4f6;
  color: #4b5563;
}

.modal-close {
  background: none;
  border: none;
  font-size: 1.5rem;
  line-height: 1;
  cursor: pointer;
  color: var(--pencil);
  padding: 0 0.25rem;
}
.modal-close:hover {
  color: var(--ink);
}

.thread {
  display: flex;
  flex-direction: column;
  gap: 1rem;
  margin-bottom: 1rem;
}

.thread-item {
  padding: 0.75rem;
  border-radius: 6px;
  border: 1px solid var(--border);
}

.thread-item.user-message {
  background: #fefce8;
  margin-right: 1rem;
}

.thread-item.staff-message {
  background: #eff6ff;
  margin-left: 1rem;
}

.thread-meta {
  font-size: 0.75rem;
  color: var(--pencil);
  margin-bottom: 0.25rem;
}

.thread-body {
  font-size: 0.9rem;
  white-space: pre-wrap;
  word-break: break-word;
}

.reply-section {
  margin-top: 1rem;
  padding-top: 1rem;
  border-top: 1px solid var(--border);
}

.reply-section label {
  display: block;
  margin-bottom: 0.5rem;
}

.ticket-modal-actions {
  margin-top: 1rem;
  padding-top: 0.5rem;
}
</style>
