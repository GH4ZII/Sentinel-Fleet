import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Link, useParams } from 'react-router-dom'
import {
  getGeofence,
  linkGeofenceAsset,
  listAssets,
  listGeofenceAssets,
  unlinkGeofenceAsset,
} from '../../lib/api'
import { GeofenceMap } from './GeofenceMap'
import { useState } from 'react'

export function GeofenceDetailPage() {
  const { geofenceId = '' } = useParams()
  const queryClient = useQueryClient()
  const [assetId, setAssetId] = useState('')
  const [error, setError] = useState<string | null>(null)

  const geofenceQuery = useQuery({
    queryKey: ['geofences', geofenceId],
    queryFn: () => getGeofence(geofenceId),
    enabled: Boolean(geofenceId),
  })

  const linksQuery = useQuery({
    queryKey: ['geofences', geofenceId, 'assets'],
    queryFn: () => listGeofenceAssets(geofenceId),
    enabled: Boolean(geofenceId),
  })

  const assetsQuery = useQuery({
    queryKey: ['assets'],
    queryFn: listAssets,
  })

  const linkMutation = useMutation({
    mutationFn: () =>
      linkGeofenceAsset(geofenceId, { assetId, ruleType: 'Both' }),
    onSuccess: async () => {
      setAssetId('')
      setError(null)
      await queryClient.invalidateQueries({ queryKey: ['geofences', geofenceId, 'assets'] })
    },
    onError: (err: Error) => setError(err.message),
  })

  const unlinkMutation = useMutation({
    mutationFn: (id: string) => unlinkGeofenceAsset(geofenceId, id),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ['geofences', geofenceId, 'assets'] })
    },
  })

  if (geofenceQuery.isLoading) {
    return <p className="text-[var(--sf-muted)]">Loading…</p>
  }

  if (geofenceQuery.isError || !geofenceQuery.data) {
    return (
      <p className="text-[var(--sf-danger)]">
        {geofenceQuery.error instanceof Error
          ? geofenceQuery.error.message
          : 'Geofence not found'}
      </p>
    )
  }

  const geofence = geofenceQuery.data
  const linkedIds = new Set((linksQuery.data ?? []).map((l) => l.assetId))
  const availableAssets = (assetsQuery.data ?? []).filter((a) => !linkedIds.has(a.id))

  return (
    <div className="space-y-8">
      <div>
        <Link to="/geofences" className="text-sm text-[var(--sf-muted)] hover:text-[var(--sf-ink)]">
          ← Geofences
        </Link>
        <h1 className="mt-2 text-3xl font-semibold tracking-tight">{geofence.name}</h1>
        <p className="mt-1 text-[var(--sf-muted)]">
          {geofence.geofenceType}
          {geofence.description ? ` · ${geofence.description}` : ''}
        </p>
      </div>

      <GeofenceMap geofences={[geofence]} />

      <section className="space-y-4 rounded-2xl border border-black/5 bg-white/70 p-4">
        <h2 className="text-lg font-semibold">Linked assets</h2>
        <div className="flex flex-wrap items-end gap-3">
          <label className="block text-sm">
            <span className="text-[var(--sf-muted)]">Asset</span>
            <select
              className="mt-1 block min-w-56 rounded-lg border border-black/10 bg-white px-3 py-2"
              value={assetId}
              onChange={(e) => setAssetId(e.target.value)}
            >
              <option value="">Select asset…</option>
              {availableAssets.map((a) => (
                <option key={a.id} value={a.id}>
                  {a.name}
                </option>
              ))}
            </select>
          </label>
          <button
            type="button"
            className="rounded-xl bg-[var(--sf-accent)] px-4 py-2 text-sm font-medium text-white disabled:opacity-60"
            disabled={!assetId || linkMutation.isPending}
            onClick={() => linkMutation.mutate()}
          >
            Link asset
          </button>
        </div>
        {error && <p className="text-sm text-[var(--sf-danger)]">{error}</p>}

        <ul className="divide-y divide-black/5 text-sm">
          {(linksQuery.data ?? []).length === 0 && (
            <li className="py-4 text-[var(--sf-muted)]">No assets linked yet.</li>
          )}
          {(linksQuery.data ?? []).map((link) => {
            const asset = assetsQuery.data?.find((a) => a.id === link.assetId)
            return (
              <li key={link.id} className="flex items-center justify-between py-3">
                <span>
                  {asset?.name ?? link.assetId}
                  <span className="ml-2 text-[var(--sf-muted)]">({link.ruleType})</span>
                </span>
                <button
                  type="button"
                  className="text-[var(--sf-danger)] hover:underline"
                  onClick={() => unlinkMutation.mutate(link.assetId)}
                >
                  Unlink
                </button>
              </li>
            )
          })}
        </ul>
      </section>
    </div>
  )
}
