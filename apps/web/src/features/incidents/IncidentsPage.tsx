import { useEffect } from 'react'
import { useQuery, useQueryClient } from '@tanstack/react-query'
import { Link } from 'react-router-dom'
import { listAssets, listIncidents } from '../../lib/api'
import { useLiveIncidents } from '../../lib/fleetHub'

export function IncidentsPage() {
  const queryClient = useQueryClient()
  const query = useQuery({
    queryKey: ['incidents'],
    queryFn: () => listIncidents({ limit: 100 }),
    refetchInterval: 15_000,
  })
  const assetsQuery = useQuery({ queryKey: ['assets'], queryFn: listAssets })
  const { latest, connected, dismissLatest } = useLiveIncidents()

  useEffect(() => {
    if (!latest) return
    void queryClient.invalidateQueries({ queryKey: ['incidents'] })
  }, [latest, queryClient])

  const assetName = (assetId: string) =>
    assetsQuery.data?.find((a) => a.id === assetId)?.name ?? assetId.slice(0, 8)

  const rows = query.data ?? []

  return (
    <div className="space-y-8">
      <div>
        <h1 className="text-3xl font-semibold tracking-tight">Incidents</h1>
        <p className="mt-1 text-[var(--sf-muted)]">
          Correlated investigations from detections.
          {connected ? ' · Live' : ''}
        </p>
      </div>

      {latest && (
        <div className="flex items-start justify-between gap-4 rounded-2xl border border-[var(--sf-danger)]/30 bg-[#fdf2f2] px-4 py-3">
          <div>
            <p className="text-sm font-semibold text-[var(--sf-danger)]">Incident update</p>
            <p className="mt-0.5 text-sm">
              {latest.title} · {assetName(latest.assetId)} · risk {latest.riskScore}
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

      {query.isLoading && <p className="text-[var(--sf-muted)]">Loading incidents…</p>}
      {query.isError && (
        <p className="text-[var(--sf-danger)]">
          {query.error instanceof Error ? query.error.message : 'Failed to load incidents'}
        </p>
      )}

      {query.data && (
        <div className="overflow-hidden rounded-2xl border border-black/5 bg-white/70">
          <table className="w-full text-left text-sm">
            <thead className="border-b border-black/5 text-xs tracking-wide text-[var(--sf-muted)] uppercase">
              <tr>
                <th className="px-4 py-3 font-medium">Detected</th>
                <th className="px-4 py-3 font-medium">Title</th>
                <th className="px-4 py-3 font-medium">Asset</th>
                <th className="px-4 py-3 font-medium">Status</th>
                <th className="px-4 py-3 font-medium">Severity</th>
                <th className="px-4 py-3 font-medium">Risk</th>
                <th className="px-4 py-3 font-medium">Alerts</th>
              </tr>
            </thead>
            <tbody>
              {rows.length === 0 && (
                <tr>
                  <td colSpan={7} className="px-4 py-8 text-[var(--sf-muted)]">
                    No incidents yet. Trigger a geofence exit to open an investigation.
                  </td>
                </tr>
              )}
              {rows.map((incident) => (
                <tr key={incident.id} className="border-t border-black/5 hover:bg-black/[0.02]">
                  <td className="px-4 py-3 text-[var(--sf-muted)]">
                    {new Date(incident.detectedAt).toLocaleString()}
                  </td>
                  <td className="px-4 py-3 font-medium">
                    <Link
                      to={`/incidents/${incident.id}`}
                      className="text-[var(--sf-accent)] hover:underline"
                    >
                      {incident.title}
                    </Link>
                  </td>
                  <td className="px-4 py-3">
                    <Link
                      to={`/assets/${incident.primaryAssetId}`}
                      className="hover:underline"
                    >
                      {assetName(incident.primaryAssetId)}
                    </Link>
                  </td>
                  <td className="px-4 py-3">{incident.status}</td>
                  <td className="px-4 py-3">{incident.severity}</td>
                  <td className="px-4 py-3 font-semibold">{incident.riskScore}</td>
                  <td className="px-4 py-3 text-[var(--sf-muted)]">{incident.detectionCount}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  )
}
