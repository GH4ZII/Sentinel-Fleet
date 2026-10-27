import { FormEvent, useEffect, useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import {
  canMutate,
  getCurrentOrganization,
  getMe,
  updateOrganization,
} from '../../lib/api'
import { SettingsNav } from './SettingsNav'

export function OrganizationSettingsPage() {
  const queryClient = useQueryClient()
  const meQuery = useQuery({ queryKey: ['me'], queryFn: getMe })
  const orgQuery = useQuery({ queryKey: ['organization'], queryFn: getCurrentOrganization })
  const allowEdit = canMutate(meQuery.data?.organizationRole)

  const [name, setName] = useState('')
  const [organizationNumber, setOrganizationNumber] = useState('')
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    if (!orgQuery.data) return
    setName(orgQuery.data.name)
    setOrganizationNumber(orgQuery.data.organizationNumber ?? '')
  }, [orgQuery.data])

  const saveMutation = useMutation({
    mutationFn: () =>
      updateOrganization({
        name,
        organizationNumber: organizationNumber || null,
      }),
    onSuccess: (org) => {
      queryClient.setQueryData(['organization'], org)
      setError(null)
    },
    onError: (err: Error) => setError(err.message),
  })

  function onSubmit(event: FormEvent) {
    event.preventDefault()
    saveMutation.mutate()
  }

  return (
    <div className="space-y-8">
      <div>
        <h1 className="text-3xl font-semibold tracking-tight">Settings</h1>
        <p className="mt-1 text-[var(--sf-muted)]">Organization profile and membership.</p>
      </div>

      <SettingsNav />

      {orgQuery.isLoading && <p className="text-[var(--sf-muted)]">Loading organization…</p>}
      {orgQuery.isError && (
        <p className="text-[var(--sf-danger)]">
          {orgQuery.error instanceof Error
            ? orgQuery.error.message
            : 'Failed to load organization'}
        </p>
      )}

      {orgQuery.data && (
        <form
          onSubmit={(e) => void onSubmit(e)}
          className="mx-auto max-w-xl space-y-4 rounded-2xl border border-black/5 bg-white/70 p-6"
        >
          <h2 className="text-lg font-semibold">Organization</h2>
          <label className="block text-sm">
            <span className="mb-1.5 block font-medium text-[var(--sf-muted)]">Name</span>
            <input
              value={name}
              onChange={(e) => setName(e.target.value)}
              disabled={!allowEdit}
              required
              className="w-full rounded-xl border border-black/10 bg-white px-3 py-2 outline-none ring-[var(--sf-accent)] focus:ring-2 disabled:opacity-60"
            />
          </label>
          <label className="block text-sm">
            <span className="mb-1.5 block font-medium text-[var(--sf-muted)]">
              Organization number
            </span>
            <input
              value={organizationNumber}
              onChange={(e) => setOrganizationNumber(e.target.value)}
              disabled={!allowEdit}
              className="w-full rounded-xl border border-black/10 bg-white px-3 py-2 outline-none ring-[var(--sf-accent)] focus:ring-2 disabled:opacity-60"
            />
          </label>
          <p className="text-xs text-[var(--sf-muted)]">
            Created {new Date(orgQuery.data.createdAt).toLocaleString()}
          </p>
          {error && <p className="text-sm text-[var(--sf-danger)]">{error}</p>}
          {saveMutation.isSuccess && !error && (
            <p className="text-sm text-[var(--sf-muted)]">Saved.</p>
          )}
          {allowEdit ? (
            <button
              type="submit"
              disabled={saveMutation.isPending}
              className="rounded-xl bg-[var(--sf-accent)] px-4 py-2.5 text-sm font-medium text-white disabled:opacity-60"
            >
              {saveMutation.isPending ? 'Saving…' : 'Save changes'}
            </button>
          ) : (
            <p className="text-sm text-[var(--sf-muted)]">
              Only owners and security managers can edit organization details.
            </p>
          )}
        </form>
      )}
    </div>
  )
}
