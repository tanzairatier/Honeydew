# detailed-design.md

## Overview

This document describes the detailed design for the **Honey‑do / Honeydew** multi‑tenant todo web application. The app targets small households and small teams that want a simple, shared, black‑and‑white notepad style task manager with basic auditing and reminders.

The system consists of:
- A **Vue 3** frontend.
- A **C# ASP.NET Core Web API** backend.
- A **SQLite** database (initially single instance, multi‑tenant via row‑level scoping).
- A simple **JWT‑based authentication and authorization** mechanism.
- A basic **background process** or scheduled job for email reminders and digests.

---

## Core Concepts and Domain Model

### Tenants (Orgs / Households / Companies)

- Each **tenant** represents a single household or small company.
- A tenant owns its own:
  - Users
  - Lists/projects
  - Todo items
  - Notification preferences and activity logs
- All tenants share the same physical database, with tenant isolation enforced via `TenantId` on all tenant‑scoped entities.

**Key fields (Tenant)**:
- `Id` (guid or int)
- `Name` (e.g., "The Smith Household", "ABC Plumbing")
- `CreatedAt`
- `IsActive`
- `SettingsJson` (optional, for flexible, per‑tenant configuration)

### Users

- A **user** belongs to exactly one tenant in v1.
- A user can be:
  - `Owner` (tenant admin / primary purchaser)
  - `Member` (normal user)
- Permissions are a mix of role and explicit flags:
  - `CanViewAllTodos`
  - `CanEditAllTodos`
- All users can always see and edit their own tasks.

**Key fields (User)**:
- `Id`
- `TenantId`
- `Email` (unique within tenant)
- `DisplayName`
- `PasswordHash`
- `PasswordSalt`
- `Role` (enum: Owner, Member)
- `CanViewAllTodos` (bool)
- `CanEditAllTodos` (bool)
- `IsActive` (bool)
- `CreatedAt`
- `LastLoginAt` (nullable)

### Todo Lists / Projects

- A **list** is a grouping of tasks, such as:
  - "Kitchen Remodel"
  - "Weekend Chores"
  - "Job: 123 Main St"
- Lists are optional but recommended for organization and for contractor‑style workflows (jobs).

**Key fields (List)**:
- `Id`
- `TenantId`
- `Name`
- `Description`
- `Type` (enum/string: "HomeProject", "Chores", "Job", etc.)
- `IsArchived` (bool)
- `CreatedAt`
- `CreatedByUserId`

### Todo Items

A **todo item** is the central entity.

**Key fields (TodoItem)**:
- `Id`
- `TenantId`
- `ListId` (nullable; for ungrouped tasks)
- `Title`
- `Description`
- `AssigneeUserId` (nullable; tasks may be unassigned)
- `DueDate` (nullable)
- `CompletedDate` (nullable)
- `CompletedByUserId` (nullable)
- `CreatedAt`
- `CreatedByUserId`
- `UpdatedAt` (nullable)
- `UpdatedByUserId` (nullable)
- `IsDeleted` (soft delete)
- `RecurrenceRule` (nullable, simple string later if recurring tasks are added)

### Activity / Audit Log

To support an audit trail and historical view, an **Activity** table captures important events:

**Key fields (Activity)**:
- `Id`
- `TenantId`
- `UserId` (who performed the action)
- `TodoItemId` (nullable, for task events)
- `Type` (enum/string, e.g., "TodoCreated", "TodoUpdated", "TodoCompleted", "TodoReopened", "UserCreated", "UserRoleChanged", etc.)
- `PayloadJson` (small JSON describing changes: old/new values, etc.)
- `CreatedAt`

### Notification Preferences and Events

**UserNotificationPreferences**:
- `UserId`
- `TenantId`
- `ReceiveDueReminders` (bool)
- `ReceiveOverdueReminders` (bool)
- `ReceiveDailyDigest` (bool)
- `PreferredReminderTimeUtc` (time or datetime)
- `PreferredEmail` (nullable; fallback is user email)

**NotificationEvent** (optional for future observability):
- `Id`
- `TenantId`
- `UserId`
- `Type` (e.g., "DueReminder", "DailyDigest")
- `Status` (e.g., "Pending", "Sent", "Failed")
- `CreatedAt`
- `SentAt` (nullable)
- `ErrorMessage` (nullable)

---

## High‑Level Architecture

### Frontend

- **Framework**: Vue 3
- **Language**: TypeScript
- **Structure**: Single File Components (SFCs) with `<script setup>` syntax.
- **State Management**: A lightweight store (Pinia or composables) for auth, current tenant, and todos.
- **Routing**: Vue Router for public vs authenticated views.

### Backend

- **Framework**: ASP.NET Core Web API
- **Language**: C#
- **Authentication**: JWT bearer tokens using ASP.NET Core authentication middleware.
- **Authorization**: Claims + policy‑based authorization for:
  - Tenant scoping
  - `Owner` vs `Member`
  - `CanViewAllTodos`, `CanEditAllTodos`

### Persistence

- **Database**: SQLite
- **Data Access**: EF Core or Dapper (EF Core is convenient for prototyping and migrations).
- **Multi‑tenancy**: Row‑level filtering by `TenantId` on all tenant entities. A per‑request tenant context is resolved from the JWT.

### Background Processing

- **Short term**: ASP.NET Core hosted service / background worker triggered by an internal timer for daily and periodic tasks.
- **Later**: Replace or augment with a proper scheduler (cron in container, Azure WebJob, etc.) without changing domain logic.

---

## Authentication and Authorization Design

### Authentication Flow

1. User submits credentials to `/api/auth/login` along with tenant identifier (e.g., email + tenant name, or email alone if unique across all tenants).
2. Backend:
   - Looks up user by email+tenant (or email alone if global unique).
   - Verifies the password using stored `PasswordHash` and `PasswordSalt`.
   - If successful, issues a JWT with claims:
     - `sub` = user id
     - `tenant` = tenant id
     - `email`
     - `role` = "Owner" / "Member"
     - `can_view_all_todos` (bool)
     - `can_edit_all_todos` (bool)
3. Frontend stores the access token (e.g., in memory and fallback in localStorage) and sends it in `Authorization: Bearer <token>` header.

### JWT Configuration

- Signing key: Symmetric key, stored in configuration (to be moved to a secret store later).
- Token lifetime: 15–60 minutes for access tokens.
- Optional: implement refresh tokens via a DB table or skip for v1.

### Authorization Policies

Define policies in ASP.NET Core to avoid hard‑coding logic in controllers:

- `"TenantUser"`:
  - Requires authenticated user and a valid `tenant` claim.
- `"Owner"`:
  - Requires `role == "Owner"`.
- `"ViewAllTodos"`:
  - Requires `role == "Owner"` or `can_view_all_todos == true`.
- `"EditAllTodos"`:
  - Requires `role == "Owner"` or `can_edit_all_todos == true`.

Resource‑level checks (e.g., "self" vs "all") are enforced by:
- Comparing `UserId` from claims with the resource’s `AssigneeUserId` or `CreatedByUserId`.
- Using policies plus custom authorization handlers where necessary.

---

## API Design

Base URL: `/api`

### AuthController

`/api/auth`

- `POST /register-tenant`
  - Description: Register a new tenant and its owner user.
  - Input: Tenant name, owner email, password, owner display name.
  - Output: Owner user info + JWT tokens.

- `POST /login`
  - Description: Authenticate a user and issue JWT.
  - Input: Email, password (and optionally tenant name/identifier).
  - Output: Access token (and optionally refresh token).

- `POST /refresh`
  - Description: Exchange refresh token for new access token (optional for v1).

- `POST /logout`
  - Description: Invalidate refresh token/session if implemented.

### TenantController

`/api/tenant`

- `GET /`
  - Description: Get current tenant details.
  - Auth: Tenant user.
  - Returns: `Id`, `Name`, `CreatedAt`, basic settings.

- `PUT /`
  - Description: Update tenant name and high‑level settings.
  - Auth: Owner.

- `GET /settings`
  - Description: Get tenant feature/config settings.

- `PUT /settings`
  - Description: Update tenant settings (e.g., default reminder times, enabling recurring tasks).

### UsersController

`/api/users`

- `GET /`
  - Description: List users in the current tenant.
  - Auth: Owner or a policy that allows view‑all users.
  - Query parameters:
    - `activeOnly` (bool)
    - `role` (filter)
    - `search` (email/displayName substring).

- `GET /{id}`
  - Description: Get user details by id.
  - Auth:
    - Owner can fetch any user.
    - User can fetch themselves.

- `POST /`
  - Description: Create new user in the tenant (owner invites).
  - Auth: Owner.
  - Input: Email, display name, role, flags for `CanViewAllTodos`, `CanEditAllTodos`.
  - Output: Created user.

- `PUT /{id}`
  - Description: Update user’s profile and role/flags.
  - Auth:
    - Owner can update any user, including role/flags.
    - User can update their own profile info (but not role/flags).

- `DELETE /{id}`
  - Description: Soft‑delete or deactivate user.
  - Auth: Owner.

- `GET /me`
  - Description: Get current authenticated user’s profile.
  - Auth: Tenant user.

- `PUT /me`
  - Description: Update current user’s own profile (display name, email, password change).

### ListsController

`/api/lists`

- `GET /`
  - Description: Get all lists for tenant (optionally filter archived).
  - Auth: Tenant user.

- `GET /{id}`
  - Description: Get single list details and summarized stats (e.g., open/closed counts).

- `POST /`
  - Description: Create a new list.
  - Auth: Tenant user.
  - Input: Name, description, type.

- `PUT /{id}`
  - Description: Update list basic info.
  - Auth: Tenant user (optionally restrict some actions to Owner).

- `DELETE /{id}`
  - Description: Archive/remove list.
  - Auth: Tenant user with permission (likely Owner or `EditAllTodos`).

### TodoItemsController

`/api/todos`

- `GET /`
  - Description: List todo items for the current tenant with filters.
  - Auth: Tenant user.
  - Query parameters:
    - `assigneeId` (optional)
    - `status` (e.g., "Open", "Completed")
    - `dueBefore`, `dueAfter`
    - `includeCompleted` (bool)
    - `listId`
    - `search` (title/description)
  - Behavior:
    - If user has `ViewAllTodos`, they can see all tenant tasks.
    - Otherwise, they see tasks assigned to themselves (and optionally unassigned).

- `GET /{id}`
  - Description: Get one todo item.
  - Auth:
    - Allowed if user is assigned, created the todo, or has `ViewAllTodos`.

- `POST /`
  - Description: Create a new todo item.
  - Auth: Tenant user.
  - Input: Title, description, assigneeId, dueDate, listId.
  - Behavior:
    - `TenantId` from token.
    - `CreatedByUserId` from token.
    - Activity entry created ("TodoCreated").

- `PUT /{id}`
  - Description: Update an existing todo item (title, description, due date, assignee, list).
  - Auth:
    - If user has `EditAllTodos`, they can edit any task in tenant.
    - Else, they can edit only tasks they own (assignee or creator).
  - Behavior:
    - Update `UpdatedAt`, `UpdatedByUserId`.
    - Activity entry ("TodoUpdated").

- `PATCH /{id}/complete`
  - Description: Mark a todo as completed.
  - Auth: Same as for editing; plus, you may allow the assignee to complete even if they cannot edit all fields.
  - Behavior:
    - Set `CompletedDate` (now).
    - Set `CompletedByUserId` from token.
    - Activity entry ("TodoCompleted").

- `PATCH /{id}/reopen`
  - Description: Reopen a completed todo item.
  - Auth: Same pattern as complete.

- `DELETE /{id}`
  - Description: Soft delete a todo.
  - Auth:
    - `EditAllTodos` or owner/creator depending on your policy.
  - Behavior:
    - Set `IsDeleted = true`.
    - Activity entry ("TodoDeleted").

### NotificationsController

`/api/notifications`

- `GET /preferences`
  - Description: Get current user notification settings.
  - Auth: Tenant user.

- `PUT /preferences`
  - Description: Update current user notification preferences.

- `POST /test`
  - Description: Send a test email to current user (for debugging and UX).

Internal/background endpoints (if triggered via HTTP):

- `POST /internal/notifications/due-reminders`
  - Description: Finds tasks due/overdue and sends reminders per user preferences.
  - Auth: Internal/system only (e.g., an API key or restricted network).

- `POST /internal/notifications/daily-digest`
  - Description: Sends daily digest emails to users who opted in.

### ActivityController

`/api/activity`

- `GET /`
  - Description: Query activity/audit events within tenant.
  - Auth:
    - Users with `ViewAllTodos` or Owner role see all.
    - Non‑privileged users may see only their own activity and activity on tasks they are involved in.
  - Query parameters:
    - `userId`
    - `todoId`
    - `from`
    - `to`
    - `type`

---

## Frontend Design

### Pages and Routes

- `/` (Landing)
  - Marketing page: what the app is for, high‑level demo.

- `/login`
  - Login form, tenant‑agnostic or tenant‑aware depending on UX.

- `/register`
  - Sign up flow creating a new tenant and owner user.

- `/app`
  - Authenticated shell:
    - Header: tenant name, user menu (profile, logout).
    - Sidebar: navigation (Dashboard, My Tasks, All Tasks, Lists, Activity, Settings).

- `/app/dashboard`
  - Summary:
    - "Today’s tasks for you"
    - Overdue tasks
    - Simple charts or counts (optional).

- `/app/my-todos`
  - List view of tasks filtered to the current user.

- `/app/all-todos`
  - For users with `ViewAllTodos`.

- `/app/lists`
  - List of lists/projects and their key stats.

- `/app/lists/:id`
  - Tasks for a specific list.

- `/app/activity`
  - Activity / audit log view (filters by time range, type, user).

- `/app/settings/profile`
  - Profile and notification preferences.

- `/app/settings/users`
  - User management (Owner only).

- `/app/settings/tenant`
  - Tenant settings.

### Vue Component Structure (Examples)

- `views/`
  - `DashboardView.vue`
  - `MyTodosView.vue`
  - `AllTodosView.vue`
  - `ListsView.vue`
  - `ListDetailView.vue`
  - `ActivityView.vue`
  - `SettingsProfileView.vue`
  - `SettingsUsersView.vue`
  - `SettingsTenantView.vue`

- `components/`
  - `TodoListTable.vue` or `TodoListView.vue`
  - `TodoItemRow.vue`
  - `TodoFilterBar.vue`
  - `TodoEditDialog.vue`
  - `UserTable.vue`
  - `UserEditDialog.vue`
  - `ListCard.vue`
  - `ActivityTable.vue`
  - `NotificationPreferencesForm.vue`

### State Management and Composables

Use composables (and/or a store) to keep components lean:

- `useAuth()`
  - Holds current user, tenant, JWT token.
  - Exposes login, logout, register methods.

- `useTodos()`
  - Manages fetching, caching, and updating todo items.
  - Filter parameters (assignee, status, list, search).

- `useLists()`
  - Manages lists/projects.

- `useUsers()`
  - For user management (Owner‑only operations).

- `useNotifications()`
  - Wraps API calls for notification preferences and maybe test emails.

---

## Multi‑Tenancy Implementation Details

- Tenant identification per request:
  - Extract `TenantId` from JWT claim.
- Data access:
  - All queries must filter by `TenantId`.
  - Common patterns:
    - A repository or EF Core `DbContext` that automatically injects `TenantId` filter using query filters.
    - Guard rails on write operations to set `TenantId` from the current context and never from the request body.
- Cross‑tenant leakage prevention:
  - Forbidden to pass `TenantId` from client.
  - Always enforce tenant checks in controllers and data layer.

---

## Security Considerations

- Hash passwords using a standard algorithm (e.g., ASP.NET Core `PasswordHasher<TUser>`).
- Validate all authorization decisions:
  - Check tenant membership for every tenant‑scoped operation.
  - Check role/claims for user and todo operations.
- Input validation:
  - Ensure IDs and payloads are validated before use.
- Rate limiting and lockouts:
  - Optional in v1 but recommended later:
    - Basic login throttling.
    - Lock out accounts after several failed attempts.

---

## Future Enhancements (Beyond Initial Scope)

- Recurring tasks:
  - Extend TodoItem with simple recurrence rules and generate instances ahead of time via a background job.
- Templates:
  - Add `TaskTemplate` and `ListTemplate` entities for common workflows (e.g., "Move‑in checklist").
- Multi‑tenant user accounts:
  - Allow a single email/user to belong to multiple tenants.
- Advanced reporting:
  - Per‑tenant metrics on tasks created/completed, average completion time, etc.
- External identity provider:
  - Swap internal JWT issuance with Azure AD B2C or another provider while keeping claims‑based authorization logic intact.

---
