import { useEffect, useState } from 'react'

type ApiStatus = {
  service: string
  version: string
  environment: string
  utc: string
}

type LoadState =
  | { kind: 'loading' }
  | { kind: 'ok'; status: ApiStatus; healthOk: boolean }
  | { kind: 'error'; message: string }

const apiBase = import.meta.env.VITE_API_BASE_URL ?? ''

async function fetchJson<T>(path: string): Promise<T> {
  const response = await fetch(`${apiBase}${path}`)
  if (!response.ok) {
    throw new Error(`${path} returned ${response.status}`)
  }
  return response.json() as Promise<T>
}

export default function App() {
  const [state, setState] = useState<LoadState>({ kind: 'loading' })

  useEffect(() => {
    let cancelled = false

    async function load() {
      try {
        const [status, healthResponse] = await Promise.all([
          fetchJson<ApiStatus>('/api/v1/status'),
          fetch(`${apiBase}/health/ready`),
        ])

        if (cancelled) return

        setState({
          kind: 'ok',
          status,
          healthOk: healthResponse.ok,
        })
      } catch (error) {
        if (cancelled) return
        setState({
          kind: 'error',
          message: error instanceof Error ? error.message : 'Unknown error',
        })
      }
    }

    void load()
    const timer = window.setInterval(() => void load(), 10_000)
    return () => {
      cancelled = true
      window.clearInterval(timer)
    }
  }, [])

  return (
    <main className="mx-auto flex min-h-screen max-w-3xl flex-col justify-center px-6 py-16">
      <p className="mb-3 text-sm font-semibold tracking-[0.2em] text-[var(--sf-accent)] uppercase">
        Platform scaffold
      </p>
      <h1 className="mb-3 text-5xl font-semibold tracking-tight text-[var(--sf-ink)]">
        Sentinel Fleet
      </h1>
      <p className="mb-10 max-w-xl text-lg text-[var(--sf-muted)]">
        Week 1 foundation is online when the API reports healthy connectivity to
        PostgreSQL, Redis, and RabbitMQ.
      </p>

      <section className="rounded-2xl border border-black/5 bg-white/70 p-6 shadow-sm backdrop-blur">
        <h2 className="mb-4 text-sm font-semibold tracking-wide text-[var(--sf-muted)] uppercase">
          API connectivity
        </h2>

        {state.kind === 'loading' && (
          <p className="text-[var(--sf-muted)]">Checking API status…</p>
        )}

        {state.kind === 'error' && (
          <div>
            <p className="mb-2 font-medium text-[var(--sf-danger)]">API unreachable</p>
            <p className="text-sm text-[var(--sf-muted)]">{state.message}</p>
          </div>
        )}

        {state.kind === 'ok' && (
          <dl className="grid gap-3 sm:grid-cols-2">
            <div>
              <dt className="text-xs tracking-wide text-[var(--sf-muted)] uppercase">Service</dt>
              <dd className="font-medium">{state.status.service}</dd>
            </div>
            <div>
              <dt className="text-xs tracking-wide text-[var(--sf-muted)] uppercase">Version</dt>
              <dd className="font-medium">{state.status.version}</dd>
            </div>
            <div>
              <dt className="text-xs tracking-wide text-[var(--sf-muted)] uppercase">Environment</dt>
              <dd className="font-medium">{state.status.environment}</dd>
            </div>
            <div>
              <dt className="text-xs tracking-wide text-[var(--sf-muted)] uppercase">Readiness</dt>
              <dd
                className={`font-medium ${
                  state.healthOk ? 'text-[var(--sf-ok)]' : 'text-[var(--sf-danger)]'
                }`}
              >
                {state.healthOk ? 'Healthy' : 'Degraded'}
              </dd>
            </div>
            <div className="sm:col-span-2">
              <dt className="text-xs tracking-wide text-[var(--sf-muted)] uppercase">UTC</dt>
              <dd className="font-medium">{new Date(state.status.utc).toLocaleString()}</dd>
            </div>
          </dl>
        )}
      </section>
    </main>
  )
}
