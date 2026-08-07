import { Link, Navigate, Outlet, useNavigate } from 'react-router-dom'
import { isAuthenticated, logout } from '../lib/api'

export function RequireAuth() {
  if (!isAuthenticated()) {
    return <Navigate to="/login" replace />
  }

  return <Outlet />
}

export function AppShell() {
  const navigate = useNavigate()

  async function handleLogout() {
    await logout()
    navigate('/login')
  }

  return (
    <div className="min-h-screen">
      <header className="border-b border-black/5 bg-white/70 backdrop-blur">
        <div className="mx-auto flex max-w-6xl items-center justify-between px-6 py-4">
          <Link to="/assets" className="text-lg font-semibold tracking-tight text-[var(--sf-ink)]">
            Sentinel Fleet
          </Link>
          <nav className="flex items-center gap-4 text-sm">
            <Link to="/assets" className="text-[var(--sf-muted)] hover:text-[var(--sf-ink)]">
              Assets
            </Link>
            <Link to="/geofences" className="text-[var(--sf-muted)] hover:text-[var(--sf-ink)]">
              Geofences
            </Link>
            <Link to="/detections" className="text-[var(--sf-muted)] hover:text-[var(--sf-ink)]">
              Detections
            </Link>
            <Link to="/incidents" className="text-[var(--sf-muted)] hover:text-[var(--sf-ink)]">
              Incidents
            </Link>
            <Link
              to="/assets/new"
              className="rounded-lg bg-[var(--sf-accent)] px-3 py-1.5 font-medium text-white"
            >
              New asset
            </Link>
            <button
              type="button"
              onClick={() => void handleLogout()}
              className="text-[var(--sf-muted)] hover:text-[var(--sf-ink)]"
            >
              Log out
            </button>
          </nav>
        </div>
      </header>
      <main className="mx-auto max-w-6xl px-6 py-8">
        <Outlet />
      </main>
    </div>
  )
}
