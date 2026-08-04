import { FormEvent, useState } from 'react'
import { Link, Navigate, useNavigate } from 'react-router-dom'
import { isAuthenticated, login, register } from '../../lib/api'

export function LoginPage() {
  const navigate = useNavigate()
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [pending, setPending] = useState(false)

  if (isAuthenticated()) {
    return <Navigate to="/assets" replace />
  }

  async function onSubmit(event: FormEvent) {
    event.preventDefault()
    setPending(true)
    setError(null)
    try {
      await login({ email, password })
      navigate('/assets')
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Login failed')
    } finally {
      setPending(false)
    }
  }

  return (
    <AuthCard title="Log in" subtitle="Access your fleet workspace.">
      <form className="space-y-4" onSubmit={(e) => void onSubmit(e)}>
        <Field label="Email" type="email" value={email} onChange={setEmail} required />
        <Field label="Password" type="password" value={password} onChange={setPassword} required />
        {error && <p className="text-sm text-[var(--sf-danger)]">{error}</p>}
        <button
          type="submit"
          disabled={pending}
          className="w-full rounded-xl bg-[var(--sf-accent)] px-4 py-2.5 font-medium text-white disabled:opacity-60"
        >
          {pending ? 'Signing in…' : 'Sign in'}
        </button>
      </form>
      <p className="mt-6 text-sm text-[var(--sf-muted)]">
        No account?{' '}
        <Link to="/register" className="font-medium text-[var(--sf-accent)]">
          Register
        </Link>
      </p>
    </AuthCard>
  )
}

export function RegisterPage() {
  const navigate = useNavigate()
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [firstName, setFirstName] = useState('')
  const [lastName, setLastName] = useState('')
  const [organizationName, setOrganizationName] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [pending, setPending] = useState(false)

  if (isAuthenticated()) {
    return <Navigate to="/assets" replace />
  }

  async function onSubmit(event: FormEvent) {
    event.preventDefault()
    setPending(true)
    setError(null)
    try {
      await register({
        email,
        password,
        firstName,
        lastName,
        organizationName: organizationName || undefined,
      })
      navigate('/assets')
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Registration failed')
    } finally {
      setPending(false)
    }
  }

  return (
    <AuthCard title="Create account" subtitle="Registers you as owner of a new organization.">
      <form className="space-y-4" onSubmit={(e) => void onSubmit(e)}>
        <div className="grid gap-4 sm:grid-cols-2">
          <Field label="First name" value={firstName} onChange={setFirstName} required />
          <Field label="Last name" value={lastName} onChange={setLastName} required />
        </div>
        <Field label="Email" type="email" value={email} onChange={setEmail} required />
        <Field label="Password" type="password" value={password} onChange={setPassword} required />
        <Field
          label="Organization name"
          value={organizationName}
          onChange={setOrganizationName}
          placeholder="Optional"
        />
        {error && <p className="text-sm text-[var(--sf-danger)]">{error}</p>}
        <button
          type="submit"
          disabled={pending}
          className="w-full rounded-xl bg-[var(--sf-accent)] px-4 py-2.5 font-medium text-white disabled:opacity-60"
        >
          {pending ? 'Creating…' : 'Create account'}
        </button>
      </form>
      <p className="mt-6 text-sm text-[var(--sf-muted)]">
        Already registered?{' '}
        <Link to="/login" className="font-medium text-[var(--sf-accent)]">
          Log in
        </Link>
      </p>
    </AuthCard>
  )
}

function AuthCard({
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
  placeholder,
}: {
  label: string
  value: string
  onChange: (value: string) => void
  type?: string
  required?: boolean
  placeholder?: string
}) {
  return (
    <label className="block text-sm">
      <span className="mb-1.5 block font-medium text-[var(--sf-muted)]">{label}</span>
      <input
        type={type}
        value={value}
        required={required}
        placeholder={placeholder}
        onChange={(e) => onChange(e.target.value)}
        className="w-full rounded-xl border border-black/10 bg-white px-3 py-2 outline-none ring-[var(--sf-accent)] focus:ring-2"
      />
    </label>
  )
}
