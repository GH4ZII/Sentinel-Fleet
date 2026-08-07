import { useQuery } from '@tanstack/react-query'
import { Link } from 'react-router-dom'
import { listGeofences } from '../../lib/api'
import { GeofenceMap } from './GeofenceMap'

export function GeofencesPage() {
  const query = useQuery({
    queryKey: ['geofences'],
    queryFn: listGeofences,
  })

  return (
    <div className="space-y-8">
      <div className="flex flex-wrap items-end justify-between gap-4">
        <div>
          <h1 className="text-3xl font-semibold tracking-tight">Geofences</h1>
          <p className="mt-1 text-[var(--sf-muted)]">
            Define allowed and restricted areas on the map.
          </p>
        </div>
        <Link
          to="/geofences/new"
          className="rounded-xl bg-[var(--sf-accent)] px-4 py-2.5 text-sm font-medium text-white"
        >
          New geofence
        </Link>
      </div>

      {query.isLoading && <p className="text-[var(--sf-muted)]">Loading geofences…</p>}
      {query.isError && (
        <p className="text-[var(--sf-danger)]">
          {query.error instanceof Error ? query.error.message : 'Failed to load geofences'}
        </p>
      )}

      {query.data && (
        <>
          <GeofenceMap geofences={query.data} />
          <div className="overflow-hidden rounded-2xl border border-black/5 bg-white/70">
            <table className="w-full text-left text-sm">
              <thead className="border-b border-black/5 text-xs tracking-wide text-[var(--sf-muted)] uppercase">
                <tr>
                  <th className="px-4 py-3 font-medium">Name</th>
                  <th className="px-4 py-3 font-medium">Type</th>
                  <th className="px-4 py-3 font-medium">Active</th>
                </tr>
              </thead>
              <tbody>
                {query.data.length === 0 && (
                  <tr>
                    <td colSpan={3} className="px-4 py-8 text-[var(--sf-muted)]">
                      No geofences yet. Draw your first area.
                    </td>
                  </tr>
                )}
                {query.data.map((g) => (
                  <tr key={g.id} className="border-t border-black/5 hover:bg-black/[0.02]">
                    <td className="px-4 py-3">
                      <Link
                        to={`/geofences/${g.id}`}
                        className="font-medium text-[var(--sf-accent)] hover:underline"
                      >
                        {g.name}
                      </Link>
                    </td>
                    <td className="px-4 py-3 text-[var(--sf-muted)]">{g.geofenceType}</td>
                    <td className="px-4 py-3">{g.isActive ? 'Yes' : 'No'}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </>
      )}
    </div>
  )
}
