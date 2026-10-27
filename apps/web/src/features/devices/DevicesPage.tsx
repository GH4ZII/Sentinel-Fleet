import { useQuery } from '@tanstack/react-query'
import { Link } from 'react-router-dom'
import { canMutate, getMe, listAssets, listDevices } from '../../lib/api'

export function DevicesPage() {
  const meQuery = useQuery({ queryKey: ['me'], queryFn: getMe })
  const devicesQuery = useQuery({ queryKey: ['devices'], queryFn: listDevices })
  const assetsQuery = useQuery({ queryKey: ['assets'], queryFn: listAssets })
  const allowCreate = canMutate(meQuery.data?.organizationRole)

  const assetNameById = new Map((assetsQuery.data ?? []).map((a) => [a.id, a.name]))

  return (
    <div className="space-y-8">
      <div className="flex flex-wrap items-end justify-between gap-4">
        <div>
          <h1 className="text-3xl font-semibold tracking-tight">Devices</h1>
          <p className="mt-1 text-[var(--sf-muted)]">
            GPS and sensor units that send telemetry for your assets.
          </p>
        </div>
        {allowCreate && (
          <Link
            to="/devices/new"
            className="rounded-xl bg-[var(--sf-accent)] px-4 py-2.5 text-sm font-medium text-white"
          >
            New device
          </Link>
        )}
      </div>

      {devicesQuery.isLoading && <p className="text-[var(--sf-muted)]">Loading devices…</p>}
      {devicesQuery.isError && (
        <p className="text-[var(--sf-danger)]">
          {devicesQuery.error instanceof Error
            ? devicesQuery.error.message
            : 'Failed to load devices'}
        </p>
      )}

      {devicesQuery.data && (
        <div className="overflow-hidden rounded-2xl border border-black/5 bg-white/70">
          <table className="w-full text-left text-sm">
            <thead className="border-b border-black/5 text-xs tracking-wide text-[var(--sf-muted)] uppercase">
              <tr>
                <th className="px-4 py-3 font-medium">External ID</th>
                <th className="px-4 py-3 font-medium">Type</th>
                <th className="px-4 py-3 font-medium">Status</th>
                <th className="px-4 py-3 font-medium">Asset</th>
                <th className="px-4 py-3 font-medium">Last seen</th>
              </tr>
            </thead>
            <tbody>
              {devicesQuery.data.length === 0 && (
                <tr>
                  <td colSpan={5} className="px-4 py-8 text-[var(--sf-muted)]">
                    No devices yet.
                    {allowCreate ? ' Create one or register an asset with a device.' : ''}
                  </td>
                </tr>
              )}
              {devicesQuery.data.map((device) => (
                <tr key={device.id} className="border-t border-black/5 hover:bg-black/[0.02]">
                  <td className="px-4 py-3">
                    <Link
                      to={`/devices/${device.id}`}
                      className="font-medium text-[var(--sf-accent)] hover:underline"
                    >
                      {device.externalDeviceId}
                    </Link>
                  </td>
                  <td className="px-4 py-3">{device.deviceType}</td>
                  <td className="px-4 py-3">{device.status}</td>
                  <td className="px-4 py-3">
                    {device.assetId ? (
                      <Link
                        to={`/assets/${device.assetId}`}
                        className="text-[var(--sf-accent)] hover:underline"
                      >
                        {assetNameById.get(device.assetId) ?? device.assetId.slice(0, 8)}
                      </Link>
                    ) : (
                      '—'
                    )}
                  </td>
                  <td className="px-4 py-3 text-[var(--sf-muted)]">
                    {device.lastSeenAt
                      ? new Date(device.lastSeenAt).toLocaleString()
                      : 'Never'}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  )
}
