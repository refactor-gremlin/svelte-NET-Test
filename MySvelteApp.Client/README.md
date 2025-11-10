# MySvelteApp.Client

SvelteKit frontend for the MySvelteApp solution.

## Setup

```bash
npm install
```

## Development

```bash
npm run dev
```

Starts dev server at `http://localhost:5173` (or next available port).

## Build

```bash
npm run build
npm run preview  # Preview production build
```

## Project Structure

```
src/
├── routes/          # SvelteKit routes
├── lib/             # Shared components and utilities
└── app.html         # HTML template
```

## API Integration

Backend API runs at `http://localhost:5000` (configured in `vite.config.ts` or environment variables).

**API Routes**:
- `POST /auth/register` - User registration
- `POST /auth/login` - User login
- `GET /auth/me` - Get current user
- `GET /auth/test` - Test authentication
- `GET /pokemon/random` - Get random Pokemon
