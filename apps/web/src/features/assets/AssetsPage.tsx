import { useQuery } from '@tanstack/react-query'
import { Link } from 'react-router-dom'
import { listAssets } from '../../lib/api'
import { AssetsMap } from './AssetsMap'

export function AssetsPage() {
  const query = useQuery({
    queryKey: ['assets'],
    queryFn: listAssets,
  })

  return (
    <div className="space-y-8">
      <div className="flex flex-wrap items-end justify-between gap-4">
        <div>
          <h1 className="text-3xl font-semibold tracking-tight">Assets</h1>
          <p className="mt-1 text-[var(--sf-muted)]">
            Register and track vehicles in your organization.
          </p>
        </div>
        <Link
          to="/assets/new"
          className="rounded-xl bg-[var(--sf-accent)] px-4 py-2.5 text-sm font-medium text-white"
        >
          Register vehicle
        </Link>
      </div>

      {query.isLoading && <p className="text-[var(--sf-muted)]">Loading assets…</p>}
      {query.isError && (
        <p className="text-[var(--sf-danger)]">
          {query.error instanceof Error ? query.error.message : 'Failed to load assets'}
        </p>
      )}

      {query.data && (
        <>
          <AssetsMap assets={query.data} />
          <div className="overflow-hidden rounded-2xl border border-black/5 bg-white/70">
            <table className="w-full text-left text-sm">
              <thead className="border-b border-black/5 text-xs tracking-wide text-[var(--sf-muted)] uppercase">
                <tr>
                  <th className="px-4 py-3 font-medium">Name</th>
                  <th className="px-4 py-3 font-medium">Type</th>
                  <th className="px-4 py-3 font-medium">Registration</th>
                  <th className="px-4 py-3 font-medium">Status</th>
                </tr>
              </thead>
              <tbody>
                {query.data.length === 0 && (
                  <tr>
                    <td colSpan={4} className="px-4 py-8 text-[var(--sf-muted)]">
                      No assets yet. Register your first vehicle.
                    </td>
                  </tr>
                )}
                {query.data.map((asset) => (
                  <tr key={asset.id} className="border-t border-black/5 hover:bg-black/[0.02]">
                    <td className="px-4 py-3">
                      <Link
                        to={`/assets/${asset.id}`}
                        className="font-medium text-[var(--sf-accent)] hover:underline"
                      >
                        {asset.name}
                      </Link>
                    </td>
                    <td className="px-4 py-3 text-[var(--sf-muted)]">{asset.assetTypeName}</td>
                    <td className="px-4 py-3 text-[var(--sf-muted)]">
                      {asset.registrationNumber ?? '—'}
                    </td>
                    <td className="px-4 py-3">{asset.status}</td>
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
