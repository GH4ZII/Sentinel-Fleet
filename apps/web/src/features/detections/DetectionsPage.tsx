import { useQuery, useQueryClient } from '@tanstack/react-query'
import { Link } from 'react-router-dom'
import { listAssets, listDetections } from '../../lib/api'
import { useLiveDetections } from '../../lib/fleetHub'
import { useEffect } from 'react'

export function DetectionsPage() {
  const queryClient = useQueryClient()
  const query = useQuery({
    queryKey: ['detections'],
    queryFn: () => listDetections({ limit: 100 }),
    refetchInterval: 15_000,
  })
  const assetsQuery = useQuery({ queryKey: ['assets'], queryFn: listAssets })
  const { detections: live, latest, connected, dismissLatest } = useLiveDetections()

  useEffect(() => {
    if (!latest) return
    void queryClient.invalidateQueries({ queryKey: ['detections'] })
  }, [latest, queryClient])

  const assetName = (assetId: string) =>
    assetsQuery.data?.find((a) => a.id === assetId)?.name ?? assetId.slice(0, 8)

  const rows = query.data ?? []

  return (
    <div className="space-y-8">
      <div>
        <h1 className="text-3xl font-semibold tracking-tight">Detections</h1>
        <p className="mt-1 text-[var(--sf-muted)]">
          Rule engine alerts for your fleet.
          {connected ? ' · Live' : ''}
        </p>
      </div>

      {latest && (
        <div className="flex items-start justify-between gap-4 rounded-2xl border border-[var(--sf-danger)]/30 bg-[#fdf2f2] px-4 py-3">
          <div>
            <p className="text-sm font-semibold text-[var(--sf-danger)]">New alert</p>
            <p className="mt-0.5 text-sm">
              {latest.title} · {assetName(latest.assetId)} · {latest.severity}
            </p>
          </div>
          <button
            type="button"
            className="text-sm text-[var(--sf-muted)] hover:text-[var(--sf-ink)]"
            onClick={dismissLatest}
          >
            Dismiss
          </button>
        </div>
      )}

      {live.length > 0 && (
        <p className="text-sm text-[var(--sf-muted)]">
          {live.length} live detection{live.length === 1 ? '' : 's'} this session
        </p>
      )}

      {query.isLoading && <p className="text-[var(--sf-muted)]">Loading detections…</p>}
      {query.isError && (
        <p className="text-[var(--sf-danger)]">
          {query.error instanceof Error ? query.error.message : 'Failed to load detections'}
        </p>
      )}

      {query.data && (
        <div className="overflow-hidden rounded-2xl border border-black/5 bg-white/70">
          <table className="w-full text-left text-sm">
            <thead className="border-b border-black/5 text-xs tracking-wide text-[var(--sf-muted)] uppercase">
              <tr>
                <th className="px-4 py-3 font-medium">Triggered</th>
                <th className="px-4 py-3 font-medium">Title</th>
                <th className="px-4 py-3 font-medium">Asset</th>
                <th className="px-4 py-3 font-medium">Type</th>
                <th className="px-4 py-3 font-medium">Severity</th>
              </tr>
            </thead>
            <tbody>
              {rows.length === 0 && (
                <tr>
                  <td colSpan={5} className="px-4 py-8 text-[var(--sf-muted)]">
                    No detections yet. Run the geofence exit scenario to trigger an alarm.
                  </td>
                </tr>
              )}
              {rows.map((d) => (
                <tr key={d.id} className="border-t border-black/5 hover:bg-black/[0.02]">
                  <td className="px-4 py-3 text-[var(--sf-muted)]">
                    {new Date(d.triggeredAt).toLocaleString()}
                  </td>
                  <td className="px-4 py-3 font-medium">{d.title}</td>
                  <td className="px-4 py-3">
                    <Link
                      to={`/assets/${d.assetId}`}
                      className="text-[var(--sf-accent)] hover:underline"
                    >
                      {assetName(d.assetId)}
                    </Link>
                  </td>
                  <td className="px-4 py-3 text-[var(--sf-muted)]">{d.detectionType}</td>
                  <td className="px-4 py-3">{d.severity}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  )
}
