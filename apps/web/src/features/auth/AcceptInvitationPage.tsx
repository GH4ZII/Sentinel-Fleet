import { FormEvent, useState } from 'react'
import { Link, Navigate, useNavigate, useSearchParams } from 'react-router-dom'
import { useQuery } from '@tanstack/react-query'
import { acceptInvitation, getInvitation, isAuthenticated } from '../../lib/api'

export function AcceptInvitationPage() {
  const [searchParams] = useSearchParams()
  const token = searchParams.get('token') ?? ''
  const navigate = useNavigate()

  const inviteQuery = useQuery({
    queryKey: ['invitation', token],
    queryFn: () => getInvitation(token),
    enabled: Boolean(token),
    retry: false,
  })

  const [firstName, setFirstName] = useState('')
  const [lastName, setLastName] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [pending, setPending] = useState(false)

  if (isAuthenticated() && !pending) {
    return <Navigate to="/assets" replace />
  }

  async function onSubmit(event: FormEvent) {
    event.preventDefault()
    setPending(true)
    setError(null)
    try {
      await acceptInvitation(token, { firstName, lastName, password })
      navigate('/assets')
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Could not accept invitation')
      setPending(false)
    }
  }

  if (!token) {
    return (
      <AuthShell title="Invalid invitation" subtitle="This link is missing an invitation token.">
        <p className="text-sm text-[var(--sf-muted)]">
          Ask your organization owner for a new invite link.{' '}
          <Link to="/login" className="font-medium text-[var(--sf-accent)]">
            Log in
          </Link>
        </p>
      </AuthShell>
    )
  }

  if (inviteQuery.isLoading) {
    return (
      <AuthShell title="Accept invitation" subtitle="Loading invitation…">
        <p className="text-[var(--sf-muted)]">Please wait…</p>
      </AuthShell>
    )
  }

  if (inviteQuery.isError || !inviteQuery.data) {
    return (
      <AuthShell title="Invitation unavailable" subtitle="This invite may be expired or already used.">
        <p className="text-sm text-[var(--sf-danger)]">
          {inviteQuery.error instanceof Error
            ? inviteQuery.error.message
            : 'Invitation not found'}
        </p>
        <p className="mt-4 text-sm text-[var(--sf-muted)]">
          <Link to="/login" className="font-medium text-[var(--sf-accent)]">
            Log in
          </Link>
        </p>
      </AuthShell>
    )
  }

  const invite = inviteQuery.data

  return (
    <AuthShell
      title="Join organization"
      subtitle={`You were invited to ${invite.organizationName} as ${invite.role}.`}
    >
      <p className="mb-4 text-sm text-[var(--sf-muted)]">
        Invited email: <span className="font-medium text-[var(--sf-ink)]">{invite.emailMasked}</span>
        <br />
        Expires {new Date(invite.expiresAt).toLocaleString()}
      </p>
      <form className="space-y-4" onSubmit={(e) => void onSubmit(e)}>
        <div className="grid gap-4 sm:grid-cols-2">
          <Field label="First name" value={firstName} onChange={setFirstName} required />
          <Field label="Last name" value={lastName} onChange={setLastName} required />
        </div>
        <Field
          label="Password"
          type="password"
          value={password}
          onChange={setPassword}
          required
        />
        {error && <p className="text-sm text-[var(--sf-danger)]">{error}</p>}
        <button
          type="submit"
          disabled={pending}
          className="w-full rounded-xl bg-[var(--sf-accent)] px-4 py-2.5 font-medium text-white disabled:opacity-60"
        >
          {pending ? 'Joining…' : 'Accept invitation'}
        </button>
      </form>
      <p className="mt-6 text-sm text-[var(--sf-muted)]">
        Already have an account?{' '}
        <Link to="/login" className="font-medium text-[var(--sf-accent)]">
          Log in
        </Link>
      </p>
    </AuthShell>
  )
}

function AuthShell({
  title,
  subtitle,
  children,
}: {
  title: string
  subtitle: string
  children: React.ReactNode
}) {
  return (
    <div className="mx-auto flex min-h-screen max-w-md flex-col justify-center px-6 py-12">
      <p className="mb-2 text-sm font-semibold tracking-[0.2em] text-[var(--sf-accent)] uppercase">
        Sentinel Fleet
      </p>
      <h1 className="mb-2 text-3xl font-semibold tracking-tight">{title}</h1>
      <p className="mb-8 text-[var(--sf-muted)]">{subtitle}</p>
      <div className="rounded-2xl border border-black/5 bg-white/70 p-6 shadow-sm backdrop-blur">
        {children}
      </div>
    </div>
  )
}

function Field({
  label,
  value,
  onChange,
  type = 'text',
  required,
}: {
  label: string
  value: string
  onChange: (value: string) => void
  type?: string
  required?: boolean
}) {
  return (
    <label className="block text-sm">
      <span className="mb-1.5 block font-medium text-[var(--sf-muted)]">{label}</span>
      <input
        type={type}
        value={value}
        required={required}
        onChange={(e) => onChange(e.target.value)}
        className="w-full rounded-xl border border-black/10 bg-white px-3 py-2 outline-none ring-[var(--sf-accent)] focus:ring-2"
      />
    </label>
  )
}
