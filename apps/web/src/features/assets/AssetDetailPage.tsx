import { useQuery } from '@tanstack/react-query'
import { Link, useLocation, useParams } from 'react-router-dom'
import { getAsset } from '../../lib/api'
import { useFleetPositions } from '../../lib/fleetHub'
import { AssetsMap } from './AssetsMap'

export function AssetDetailPage() {
  const { assetId } = useParams<{ assetId: string }>()
  const location = useLocation()
  const deviceApiKey = (location.state as { deviceApiKey?: string } | null)?.deviceApiKey
  const { positions, connected } = useFleetPositions()

  const query = useQuery({
    queryKey: ['assets', assetId],
    queryFn: () => getAsset(assetId!),
    enabled: Boolean(assetId),
    refetchInterval: 30_000,
  })

  if (query.isLoading) {
    return <p className="text-[var(--sf-muted)]">Loading asset…</p>
  }

  if (query.isError || !query.data) {
    return (
      <p className="text-[var(--sf-danger)]">
        {query.error instanceof Error ? query.error.message : 'Asset not found'}
      </p>
    )
  }

  const asset = query.data
  const live = assetId ? positions[assetId] : undefined

  return (
    <div className="space-y-8">
      <div>
        <Link to="/assets" className="text-sm text-[var(--sf-muted)] hover:text-[var(--sf-ink)]">
          ← Back to assets
        </Link>
        <h1 className="mt-2 text-3xl font-semibold tracking-tight">{asset.name}</h1>
        <p className="mt-1 text-[var(--sf-muted)]">
          {asset.assetTypeName} · {asset.status}
          {connected ? ' · Live' : ''}
        </p>
      </div>

      {deviceApiKey && (
        <div className="rounded-xl border border-[var(--sf-accent)]/20 bg-[var(--sf-accent-soft)] p-4 text-sm">
          <p className="font-medium">Device API key (shown once)</p>
          <code className="mt-1 block break-all">{deviceApiKey}</code>
        </div>
      )}

      <dl className="grid gap-4 rounded-2xl border border-black/5 bg-white/70 p-6 sm:grid-cols-2">
        <Item label="Registration" value={asset.registrationNumber} />
        <Item label="Asset number" value={asset.assetNumber} />
        <Item label="Manufacturer" value={asset.manufacturer} />
        <Item label="Model" value={asset.model} />
        <Item label="Criticality" value={asset.criticality} />
        <Item label="Serial number" value={asset.serialNumber} />
        <Item
          label="Speed"
          value={live?.speedKph != null ? `${live.speedKph.toFixed(1)} km/h` : null}
        />
        <Item
          label="Last position"
          value={
            live
              ? `${live.latitude.toFixed(5)}, ${live.longitude.toFixed(5)}`
              : asset.mapLatitude != null && asset.mapLongitude != null
                ? `${asset.mapLatitude.toFixed(5)}, ${asset.mapLongitude.toFixed(5)}`
                : null
          }
        />
      </dl>

      <div>
        <h2 className="mb-3 text-lg font-semibold">Live map</h2>
        <AssetsMap
          assets={[asset]}
          livePositions={positions}
          className="h-96 w-full overflow-hidden rounded-2xl border border-black/5"
        />
        <p className="mt-2 text-sm text-[var(--sf-muted)]">
          {connected
            ? 'Receiving live positions via SignalR.'
            : 'Connecting to live updates… positions fall back to last stored location.'}
        </p>
      </div>
    </div>
  )
}

function Item({ label, value }: { label: string; value: string | null }) {
  return (
    <div>
      <dt className="text-xs tracking-wide text-[var(--sf-muted)] uppercase">{label}</dt>
      <dd className="mt-1 font-medium">{value ?? '—'}</dd>
    </div>
  )
}
