<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { listUsers, createUser, updateUser, deleteUser } from '@/api/usersService'
import type { UserSummary, CreateUserPayload, UpdateUserPayload } from '@/api/usersService'
import { getTenant } from '@/api/tenantService'
import type { Tenant } from '@/api/tenantService'

const users = ref<UserSummary[]>([])
const tenant = ref<Tenant | null>(null)
const loading = ref(true)
const error = ref('')
const showAdd = ref(false)
const editingId = ref<string | null>(null)
const editForm = ref<UpdateUserPayload>({})
const deleteConfirmId = ref<string | null>(null)
const deleteSaving = ref(false)
const deleteError = ref('')
const showFullModal = ref(false)

const userCountText = computed(() => {
  const t = tenant.value
  if (!t || t.billingPlanMaxUsers == null)
    return null
  return `${t.userCount}/${t.billingPlanMaxUsers} Users`
})

const addForm = ref({
  email: '',
  displayName: '',
  password: '',
  role: 'Member',
  canViewAllTodos: false,
  canEditAllTodos: false,
  canCreateUser: false,
})
const addError = ref('')
const addSaving = ref(false)

async function load() {
  loading.value = true
  error.value = ''
  try {
    const [list, t] = await Promise.all([listUsers(false), getTenant()])
    users.value = list
    tenant.value = t
  } catch (e: Error) {
    error.value = e.message
  } finally {
    loading.value = false
  }
}

onMounted(load)

function openAdd() {
  const t = tenant.value
  const maxUsers = t?.billingPlanMaxUsers ?? null
  const atLimit = maxUsers != null && (t?.userCount ?? 0) >= maxUsers
  if (atLimit) {
    showFullModal.value = true
    return
  }
  addForm.value = {
    email: '',
    displayName: '',
    password: '',
    role: 'Member',
    canViewAllTodos: false,
    canEditAllTodos: false,
    canCreateUser: false,
  }
  addError.value = ''
  showAdd.value = true
}

function closeFullModal() {
  showFullModal.value = false
}

function closeAdd() {
  showAdd.value = false
}

async function submitAdd() {
  addError.value = ''
  addSaving.value = true
  try {
    const payload: CreateUserPayload = {
      email: addForm.value.email.trim(),
      displayName: addForm.value.displayName.trim(),
      password: addForm.value.password,
      role: addForm.value.role,
      canViewAllTodos: addForm.value.canViewAllTodos,
      canEditAllTodos: addForm.value.canEditAllTodos,
      canCreateUser: addForm.value.canCreateUser,
    }
    await createUser(payload)
    closeAdd()
    load()
  } catch (e: unknown) {
    addError.value = e instanceof Error ? e.message : 'Failed to add user'
  } finally {
    addSaving.value = false
  }
}

function startEdit(user: UserSummary) {
  editingId.value = user.id
  editForm.value = {
    role: user.role,
    canViewAllTodos: user.canViewAllTodos,
    canEditAllTodos: user.canEditAllTodos,
    canCreateUser: user.canCreateUser,
  }
}

function cancelEdit() {
  editingId.value = null
}

async function saveEdit() {
  const id = editingId.value
  if (!id)
    return
  try {
    await updateUser(id, editForm.value)
    editingId.value = null
    load()
  } catch (e: unknown) {
    error.value = e instanceof Error ? e.message : 'Failed to update user'
  }
}

function openDeleteConfirm(user: UserSummary) {
  deleteError.value = ''
  deleteConfirmId.value = user.id
}

function closeDeleteConfirm() {
  deleteConfirmId.value = null
}

async function confirmDelete() {
  const id = deleteConfirmId.value
  if (!id)
    return
  deleteError.value = ''
  deleteSaving.value = true
  try {
    await deleteUser(id)
    closeDeleteConfirm()
    load()
  } catch (e: unknown) {
    deleteError.value = e instanceof Error ? e.message : 'Failed to delete user'
  } finally {
    deleteSaving.value = false
  }
}
</script>

<template>
  <div class="users-settings">
    <h2 class="sketch-title">Users</h2>
    <p class="intro">Manage household members and their access.</p>
    <p v-if="userCountText" class="user-count-note">{{ userCountText }}</p>

    <div v-if="error" class="sketch-error">{{ error }}</div>

    <div v-if="loading" class="loading">Loading…</div>

    <template v-else>
      <div class="toolbar">
        <button type="button" class="sketch-btn primary" @click="openAdd">Add user</button>
      </div>

      <div class="sketch-box table-wrap">
        <table class="users-table">
          <thead>
            <tr>
              <th>Name</th>
              <th>Email</th>
              <th>Role</th>
              <th>View all</th>
              <th>Edit all</th>
              <th>Invite users</th>
              <th>Active</th>
              <th></th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="u in users" :key="u.id">
              <td>{{ u.displayName }}</td>
              <td>{{ u.email }}</td>
              <td>{{ u.role }}</td>
              <td>{{ u.canViewAllTodos ? 'Yes' : 'No' }}</td>
              <td>{{ u.canEditAllTodos ? 'Yes' : 'No' }}</td>
              <td>{{ u.canCreateUser ? 'Yes' : 'No' }}</td>
              <td>{{ u.isActive ? 'Yes' : 'No' }}</td>
              <td class="actions-cell">
                <div class="actions-buttons">
                  <button
                    v-if="u.role !== 'Owner'"
                    type="button"
                    class="sketch-btn small"
                    @click="editingId === u.id ? cancelEdit() : startEdit(u)"
                  >
                    {{ editingId === u.id ? 'Cancel' : 'Edit' }}
                  </button>
                  <button
                    v-if="users.length > 1 && (u.role !== 'Owner' || users.filter(x => x.role === 'Owner').length > 1)"
                    type="button"
                    class="sketch-btn small danger"
                    @click="openDeleteConfirm(u)"
                  >
                    Delete
                  </button>
                </div>
              </td>
            </tr>
          </tbody>
        </table>
      </div>

      <!-- Inline edit (one at a time) -->
      <div v-if="editingId" class="sketch-box edit-card">
        <h4>Edit user</h4>
        <form @submit.prevent="saveEdit">
          <label>
            <span class="sketch-label">Role</span>
            <select v-model="editForm.role" class="sketch-input">
              <option value="Member">Member</option>
              <option value="Owner">Owner</option>
            </select>
          </label>
          <label class="checkbox-label">
            <input type="checkbox" v-model="editForm.canViewAllTodos" />
            View all todos
          </label>
          <label class="checkbox-label">
            <input type="checkbox" v-model="editForm.canEditAllTodos" />
            Edit all todos
          </label>
          <label class="checkbox-label">
            <input type="checkbox" v-model="editForm.canCreateUser" />
            Invite users
          </label>
          <div class="edit-actions">
            <button type="submit" class="sketch-btn primary">Save</button>
            <button type="button" class="sketch-btn" @click="cancelEdit">Cancel</button>
          </div>
        </form>
      </div>
    </template>

    <!-- Add user modal / panel -->
    <Teleport to="body">
      <div class="modal-overlay" v-if="showAdd" @click.self="closeAdd">
        <div class="sketch-box modal">
          <h3 class="sketch-title">Add user</h3>
          <form @submit.prevent="submitAdd">
            <label>
              <span class="sketch-label">Email</span>
              <input v-model="addForm.email" type="email" class="sketch-input" required />
            </label>
            <label>
              <span class="sketch-label">Display name</span>
              <input v-model="addForm.displayName" type="text" class="sketch-input" />
            </label>
            <label>
              <span class="sketch-label">Password</span>
              <input v-model="addForm.password" type="password" class="sketch-input" required />
            </label>
            <label>
              <span class="sketch-label">Role</span>
              <select v-model="addForm.role" class="sketch-input">
                <option value="Member">Member</option>
                <option value="Owner">Owner</option>
              </select>
            </label>
            <label class="checkbox-label">
              <input v-model="addForm.canViewAllTodos" type="checkbox" />
              View all todos
            </label>
            <label class="checkbox-label">
              <input v-model="addForm.canEditAllTodos" type="checkbox" />
              Edit all todos
            </label>
            <label class="checkbox-label">
              <input v-model="addForm.canCreateUser" type="checkbox" />
              Invite users
            </label>
            <div v-if="addError" class="sketch-error">{{ addError }}</div>
            <div class="form-actions">
              <button type="submit" class="sketch-btn primary" :disabled="addSaving">
                {{ addSaving ? 'Adding…' : 'Add user' }}
              </button>
              <button type="button" class="sketch-btn" @click="closeAdd">Cancel</button>
            </div>
          </form>
        </div>
      </div>
    </Teleport>

    <!-- Full on users modal -->
    <Teleport to="body">
      <div v-if="showFullModal" class="modal-overlay" @click.self="closeFullModal">
        <div class="sketch-box modal">
          <h3 class="sketch-title">Cannot add user</h3>
          <p>Sorry! You're full up on users for your current plan. Check out Billing to change your plan and add more members.</p>
          <div class="form-actions">
            <router-link to="/app/settings/billing" class="sketch-btn primary" @click="closeFullModal">Go to Billing</router-link>
            <button type="button" class="sketch-btn" @click="closeFullModal">Close</button>
          </div>
        </div>
      </div>
    </Teleport>

    <!-- Delete confirm modal -->
    <Teleport to="body">
      <div v-if="deleteConfirmId" class="modal-overlay" @click.self="closeDeleteConfirm">
        <div class="sketch-box modal">
          <h3 class="sketch-title">Delete user?</h3>
          <p>This cannot be undone. Are you sure?</p>
          <div v-if="deleteError" class="sketch-error">{{ deleteError }}</div>
          <div class="form-actions">
            <button type="button" class="sketch-btn" @click="closeDeleteConfirm">Cancel</button>
            <button type="button" class="sketch-btn danger" :disabled="deleteSaving" @click="confirmDelete">
              {{ deleteSaving ? '…' : 'Delete' }}
            </button>
          </div>
        </div>
      </div>
    </Teleport>
  </div>
</template>

<style scoped>
.users-settings {
  max-width: 56rem;
}

.intro {
  margin: 0 0 0.5rem;
  color: var(--pencil);
  font-size: 0.9rem;
}

.user-count-note {
  margin: 0 0 1rem;
  font-size: 0.9rem;
  color: var(--pencil);
}

.sketch-btn.danger {
  background: rgba(180, 0, 0, 0.08);
  color: #a00;
}

.sketch-btn.danger:hover {
  background: rgba(180, 0, 0, 0.15);
}

.toolbar {
  margin-bottom: 0.75rem;
}

.table-wrap {
  overflow-x: auto;
  padding: 0;
}

.users-table {
  width: 100%;
  border-collapse: collapse;
  font-size: 0.85rem;
}

.users-table th,
.users-table td {
  padding: 0.5rem 0.75rem;
  text-align: left;
  border-bottom: 1px solid var(--line);
}

.users-table th {
  font-weight: 500;
  color: var(--pencil);
}

.sketch-btn.small {
  padding: 0.25rem 0.5rem;
  font-size: 0.8rem;
}

.actions-cell {
  white-space: nowrap;
}

.actions-buttons {
  display: flex;
  flex-direction: row;
  align-items: center;
  gap: 0.5rem;
}

.edit-card {
  margin-top: 1rem;
  padding: 1rem;
}

.edit-card h4 {
  margin: 0 0 0.75rem;
  font-size: 1rem;
}

.edit-card label {
  display: block;
  margin-bottom: 0.5rem;
}

.checkbox-label {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  margin-bottom: 0.5rem;
}

.checkbox-label input {
  width: auto;
}

.edit-actions,
.form-actions {
  margin-top: 1rem;
  display: flex;
  gap: 0.5rem;
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
  min-width: 20rem;
  max-width: 90vw;
}

.modal label {
  display: block;
  margin-bottom: 0.75rem;
}

.loading {
  color: var(--pencil);
  font-size: 0.9rem;
}
</style>
