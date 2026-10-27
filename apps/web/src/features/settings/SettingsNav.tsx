import { Link, useLocation } from 'react-router-dom'

const links = [
  { to: '/settings/organization', label: 'Organization' },
  { to: '/settings/users', label: 'Users' },
]

export function SettingsNav() {
  const location = useLocation()

  return (
    <nav className="flex gap-2 border-b border-black/5 pb-4">
      {links.map((link) => {
        const active = location.pathname === link.to
        return (
          <Link
            key={link.to}
            to={link.to}
            className={
              active
                ? 'rounded-lg bg-[var(--sf-accent)] px-3 py-1.5 text-sm font-medium text-white'
                : 'rounded-lg px-3 py-1.5 text-sm text-[var(--sf-muted)] hover:bg-black/[0.03] hover:text-[var(--sf-ink)]'
            }
          >
            {link.label}
          </Link>
        )
      })}
    </nav>
  )
}
