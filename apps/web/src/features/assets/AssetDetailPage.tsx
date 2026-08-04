import { useQuery } from '@tanstack/react-query'
import { Link, useLocation, useParams } from 'react-router-dom'
import { getAsset } from '../../lib/api'
import { AssetsMap } from './AssetsMap'

export function AssetDetailPage() {
  const { assetId } = useParams<{ assetId: string }>()
  const location = useLocation()
  const deviceApiKey = (location.state as { deviceApiKey?: string } | null)?.deviceApiKey

  const query = useQuery({
    queryKey: ['assets', assetId],
    queryFn: () => getAsset(assetId!),
    enabled: Boolean(assetId),
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

  return (
    <div className="space-y-8">
      <div>
        <Link to="/assets" className="text-sm text-[var(--sf-muted)] hover:text-[var(--sf-ink)]">
          ← Back to assets
        </Link>
        <h1 className="mt-2 text-3xl font-semibold tracking-tight">{asset.name}</h1>
        <p className="mt-1 text-[var(--sf-muted)]">
          {asset.assetTypeName} · {asset.status}
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
      </dl>

      <div>
        <h2 className="mb-3 text-lg font-semibold">Map</h2>
        <AssetsMap assets={[asset]} className="h-96 w-full overflow-hidden rounded-2xl border border-black/5" />
        <p className="mt-2 text-sm text-[var(--sf-muted)]">
          Placeholder marker until live telemetry arrives in week 3.
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
