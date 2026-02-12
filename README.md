# Honeydew

**Simple, notepad-style task management for households and small teams.**

Honeydew is a “honey-do” list app: lightweight, focused, and built for real-world use. It targets **families** and **small outfits** (e.g. rural contractors, micro businesses) that need shared task lists without enterprise tooling. The UI is deliberately minimal and simplistic to allow users to get straight to it, much like a family might with notepads on a fridge.

---

## What it is

Honeydew utilizes a multi-tenant setup, with JWT auth, a REST backend API and a Vue frontend.

- **Multi-tenant:** Each “tenant” is a household or small company. You register once (tenant + owner), then invite or add members.
- **JWT auth:** Real token-based login. Passwords and client secrets are hashed (no plaintext); JWTs are validated on every request.
- **REST API + Vue SPA:** .NET 8 API with SQLite and EF Core migrations; Vue 3 + TypeScript + Vite frontend. API is the single source of truth; the UI is a thin client.

---

## How to run

Clone this repository. The solution contains the backend API (`src/Honeydew`) and the Vue frontend (`src/web`).

**Backend (API)**

```bash
cd src/Honeydew
dotnet run
```

- Uses SQLite by default (`Data Source=honeydew.db`). DB and tables are created on first run via EF Core migrations.
- Requires `Jwt:SigningKey` in config (see `appsettings.json`; default is fine for local dev).
- API runs at the URL in launchSettings (e.g. `http://localhost:5050`). Swagger at `/swagger`.

**Frontend**

```bash
cd src/web
npm install
npm run dev
```

- Dev server at `http://localhost:5173`. Requests to `/api` are proxied to the backend (configure in Vite if your API port differs).

**First use:** Open the app → Register (create a tenant + owner) → Log in → Create todos and explore settings.

**Testing the API:** Swagger UI at `http://localhost:5050/swagger` (with the API running). Optional: use the `Honeydew.http` file in `src/Honeydew` (VS Code / Rider) to send requests once you have a token.

---

## Assumptions and trade-offs

- **Assumptions:** One tenant per household/team; users have a single tenant for normal login (multi-tenant users pass `tenantId` at login). SQLite and file-based DB are acceptable for MVP. Reviewers run .NET 8 and Node for local dev.
- **Trade-offs:** No Vue Query yet—data is fetched in components/composables with explicit loading and error state; we’d add it for caching and invalidation if the app grew. Controllers are not unit-tested; behavior is covered via service and data-access tests. Frontend and backend share no generated types (DTOs are defined in both); OpenAPI codegen could be a future step.

---

## Workflows

| Flow | What you do |
|------|-------------|
| **Onboard** | Register (tenant name + owner email/password) → log in. Optional: add users (Settings → Users), set household name (Settings → General). |
| **Tasks** | **Home:** “Assigned to me” and quick access to a todo. **Items → Your items:** paginated list, filter by assigned-to-me, include completed, search, sort. Create, edit, set due date, assign to a household member, mark done. Thumbs-up vote on items. **Items → Export:** CSV export (yours or all, depending on permission). |
| **Settings** | **Profile:** display name, email. **Preferences:** items per page. **Users:** (owner or CanCreateUser) list, add, edit, deactivate, delete. **General:** (owner) household name. **Billing:** (owner) view plans, set plan (e.g. Free). **Support:** list tickets, open a ticket, add replies, set status. |

Role-based access: owners see all settings; members see profile and preferences; user management is owner or CanCreateUser.

---

## Main features

- **Auth:** Register tenant + owner, login (with optional tenantId for multi-tenant users), JWT in header; password hashing (PBKDF2), client-secret hashing for API clients.
- **Tenants & users:** One tenant per household/company; users with roles (Owner, Member), flags (CanViewAllTodos, CanEditAllTodos, CanCreateUser), active/inactive.
- **Todos:** CRUD, pagination, search, sort (due date, completed, created); assign to user; due date; mark done; thumbs-up vote; CSV export.
- **Settings:** Household name, billing plan (e.g. Free), support tickets (create, reply, Open/Closed), user management, profile and preferences.
- **API clients:** Create client credentials (owner) for programmatic access; token endpoint for client JWT.

---

## Tech stack

| Layer | Choices |
|-------|---------|
| Backend | .NET 8, ASP.NET Core, EF Core, SQLite, JWT Bearer auth |
| Frontend | Vue 3, TypeScript, Vite |
| Data | SQLite file (`honeydew.db`), migrations on startup |

---

## Unit tests

**How to run**

```bash
cd src   # or open the solution in your IDE
dotnet test Honeydew.sln
```

Or from the repo root: `dotnet test src/Honeydew.sln`. The test project is `Honeydew.Tests`; no separate test run is needed for the frontend.

**Ethos**

- **Only the type under test is real.** Services and data access are tested in isolation; all dependencies (other services, `HoneydewDbContext`, etc.) are mocked. That keeps tests fast, deterministic, and focused on one class’s behavior.
- **Arrange, Act, Assert.** Each test is structured so the setup (Arrange), the single call under test (Act), and the expectations (Assert) are clear. Boilerplate is moved into `[TestInitialize]` where it helps readability.
- **MSTest.** We use `[TestClass]`, `[TestMethod]`, and `[TestInitialize]`. Tests prove that the service and data layers behave as specified (auth, tenants, users, todos, support tickets, preferences) so that refactors and new features don’t break existing behavior.

---

## POC vs production

**Where this stands today (proof-of-concept / MVP):**

- **Real security baseline:** Token-based auth, password and client-secret hashing, JWT validation, no sensitive data in logs. CORS is explicit (localhost dev origins).
- **Structured app:** Controllers → services → data access; DTOs for API; only TS `*Service` modules talk to the API. Unit tests cover services and data access.
- **API errors and validation:** Global exception handler returns a single JSON error shape (`error`, optional `code`). Request DTOs use data annotations (required, lengths, email); invalid model state returns the same error shape. Frontend throws `ApiError` with message and code for consistent handling.
- **Production-minded but not hardened:** Migrations and config are in place; SigningKey and DB path are configurable. Remaining gaps: CORS and config tuned for a real production host, and optional hardening (rate limiting, audit logging).

**Future work (to cross the line into production):**

- **Backend:** Optional rate limiting and audit logging.
- **Frontend:** Optional Vue Query (or similar) for caching and invalidation; further polish of loading/error states where needed.
- **Ops:** Configure CORS and Jwt:SigningKey for production; backups and migration strategy; health checks and logging for deploy environments.

So: the *security and architecture* are built to be production-capable; the *operational and UX polish* are at “ship internally or as a small beta” level. Honeydew is intended to feel like something you could hand to a family or a small crew today, with a clear path to harden it further.
