import { FormEvent, useState } from 'react'
import { Link, Navigate, useNavigate } from 'react-router-dom'
import { useQuery } from '@tanstack/react-query'
import { canMutate, createDevice, getMe, listAssets } from '../../lib/api'

export function NewDevicePage() {
  const navigate = useNavigate()
  const meQuery = useQuery({ queryKey: ['me'], queryFn: getMe })
  const assetsQuery = useQuery({ queryKey: ['assets'], queryFn: listAssets })

  const [externalDeviceId, setExternalDeviceId] = useState('')
  const [deviceType, setDeviceType] = useState('GpsTracker')
  const [assetId, setAssetId] = useState('')
  const [firmwareVersion, setFirmwareVersion] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [pending, setPending] = useState(false)

  if (meQuery.isSuccess && !canMutate(meQuery.data.organizationRole)) {
    return <Navigate to="/devices" replace />
  }

  async function onSubmit(event: FormEvent) {
    event.preventDefault()
    setPending(true)
    setError(null)
    try {
      const result = await createDevice({
        externalDeviceId,
        deviceType,
        assetId: assetId || null,
        firmwareVersion: firmwareVersion || undefined,
      })
      navigate(`/devices/${result.device.id}`, {
        state: { apiKey: result.apiKey },
      })
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Could not create device')
      setPending(false)
    }
  }

  return (
    <div className="mx-auto max-w-xl space-y-6">
      <div>
        <Link to="/devices" className="text-sm text-[var(--sf-muted)] hover:text-[var(--sf-ink)]">
          ← Back to devices
        </Link>
        <h1 className="mt-2 text-3xl font-semibold tracking-tight">New device</h1>
        <p className="mt-1 text-[var(--sf-muted)]">
          Registers a telemetry device and issues a one-time API key.
        </p>
      </div>

      <form
        onSubmit={(e) => void onSubmit(e)}
        className="space-y-4 rounded-2xl border border-black/5 bg-white/70 p-6"
      >
        <Field
          label="External device ID"
          value={externalDeviceId}
          onChange={setExternalDeviceId}
          required
          placeholder="sim-truck-01"
        />
        <label className="block text-sm">
          <span className="mb-1.5 block font-medium text-[var(--sf-muted)]">Device type</span>
          <select
            value={deviceType}
            onChange={(e) => setDeviceType(e.target.value)}
            className="w-full rounded-xl border border-black/10 bg-white px-3 py-2 outline-none ring-[var(--sf-accent)] focus:ring-2"
          >
            <option value="GpsTracker">GpsTracker</option>
            <option value="Obd">Obd</option>
            <option value="Simulator">Simulator</option>
            <option value="Other">Other</option>
          </select>
        </label>
        <label className="block text-sm">
          <span className="mb-1.5 block font-medium text-[var(--sf-muted)]">Asset (optional)</span>
          <select
            value={assetId}
            onChange={(e) => setAssetId(e.target.value)}
            className="w-full rounded-xl border border-black/10 bg-white px-3 py-2 outline-none ring-[var(--sf-accent)] focus:ring-2"
          >
            <option value="">Unassigned</option>
            {(assetsQuery.data ?? []).map((asset) => (
              <option key={asset.id} value={asset.id}>
                {asset.name}
              </option>
            ))}
          </select>
        </label>
        <Field
          label="Firmware version"
          value={firmwareVersion}
          onChange={setFirmwareVersion}
          placeholder="1.0.0"
        />
        {error && <p className="text-sm text-[var(--sf-danger)]">{error}</p>}
        <button
          type="submit"
          disabled={pending}
          className="rounded-xl bg-[var(--sf-accent)] px-4 py-2.5 font-medium text-white disabled:opacity-60"
        >
          {pending ? 'Saving…' : 'Create device'}
        </button>
      </form>
    </div>
  )
}

function Field({
  label,
  value,
  onChange,
  required,
  placeholder,
}: {
  label: string
  value: string
  onChange: (value: string) => void
  required?: boolean
  placeholder?: string
}) {
  return (
    <label className="block text-sm">
      <span className="mb-1.5 block font-medium text-[var(--sf-muted)]">{label}</span>
      <input
        value={value}
        required={required}
        placeholder={placeholder}
        onChange={(e) => onChange(e.target.value)}
        className="w-full rounded-xl border border-black/10 bg-white px-3 py-2 outline-none ring-[var(--sf-accent)] focus:ring-2"
      />
    </label>
  )
}
