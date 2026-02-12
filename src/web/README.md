# Honeydew Web (Vue)

Vue 3 + TypeScript + Vite frontend. Black/white notepad-style UI with a line-art / doodle sketch feel.

## Setup

```bash
npm install
```

## Dev

```bash
npm run dev
```

Runs at http://localhost:5173. API requests to `/api` are proxied to the backend (http://localhost:5050 per launchSettings). Start the Honeydew API separately.

## Build

```bash
npm run build
```

Output in `dist/`.

## Routes

- `/login` — Sign in
- `/register` — Sign up (create tenant + owner)
- `/app` — Post-login placeholder (after signin/signup)

Token is stored in `localStorage` under `honeydew_token`. Protected routes (e.g. `/app/*`) use a router guard that requires a valid token.
