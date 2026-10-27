import { FormEvent, useEffect, useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Link, useLocation, useParams } from 'react-router-dom'
import {
  canMutate,
  getDevice,
  getMe,
  listAssets,
  rotateDeviceKey,
  updateDevice,
} from '../../lib/api'

const STATUSES = ['Active', 'Inactive', 'Offline', 'Revoked'] as const

export function DeviceDetailPage() {
  const { deviceId } = useParams<{ deviceId: string }>()
  const location = useLocation()
  const queryClient = useQueryClient()
  const initialApiKey = (location.state as { apiKey?: string } | null)?.apiKey

  const meQuery = useQuery({ queryKey: ['me'], queryFn: getMe })
  const allowMutate = canMutate(meQuery.data?.organizationRole)
  const assetsQuery = useQuery({ queryKey: ['assets'], queryFn: listAssets })
  const deviceQuery = useQuery({
    queryKey: ['devices', deviceId],
    queryFn: () => getDevice(deviceId!),
    enabled: Boolean(deviceId),
  })

  const [deviceType, setDeviceType] = useState('')
  const [status, setStatus] = useState('')
  const [assetId, setAssetId] = useState('')
  const [firmwareVersion, setFirmwareVersion] = useState('')
  const [apiKey, setApiKey] = useState<string | null>(initialApiKey ?? null)
  const [formError, setFormError] = useState<string | null>(null)

  useEffect(() => {
    if (!deviceQuery.data) return
    setDeviceType(deviceQuery.data.deviceType)
    setStatus(deviceQuery.data.status)
    setAssetId(deviceQuery.data.assetId ?? '')
    setFirmwareVersion(deviceQuery.data.firmwareVersion ?? '')
  }, [deviceQuery.data])

  const saveMutation = useMutation({
    mutationFn: () =>
      updateDevice(deviceId!, {
        deviceType,
        status,
        assetId: assetId || null,
        firmwareVersion: firmwareVersion || null,
      }),
    onSuccess: (device) => {
      void queryClient.invalidateQueries({ queryKey: ['devices'] })
      queryClient.setQueryData(['devices', deviceId], device)
      setFormError(null)
    },
    onError: (err: Error) => setFormError(err.message),
  })

  const rotateMutation = useMutation({
    mutationFn: () => rotateDeviceKey(deviceId!),
    onSuccess: (result) => {
      setApiKey(result.apiKey)
      void queryClient.invalidateQueries({ queryKey: ['devices', deviceId] })
    },
  })

  if (deviceQuery.isLoading) {
    return <p className="text-[var(--sf-muted)]">Loading device…</p>
  }

  if (deviceQuery.isError || !deviceQuery.data) {
    return (
      <p className="text-[var(--sf-danger)]">
        {deviceQuery.error instanceof Error ? deviceQuery.error.message : 'Device not found'}
      </p>
    )
  }

  const device = deviceQuery.data

  function onSave(event: FormEvent) {
    event.preventDefault()
    saveMutation.mutate()
  }

  function onRotate() {
    if (!window.confirm('Rotate API key? The previous key will stop working immediately.')) {
      return
    }
    rotateMutation.mutate()
  }

  return (
    <div className="space-y-8">
      <div>
        <Link to="/devices" className="text-sm text-[var(--sf-muted)] hover:text-[var(--sf-ink)]">
          ← Back to devices
        </Link>
        <h1 className="mt-2 text-3xl font-semibold tracking-tight">{device.externalDeviceId}</h1>
        <p className="mt-1 text-[var(--sf-muted)]">
          {device.deviceType} · {device.status}
        </p>
      </div>

      {apiKey && (
        <div className="rounded-xl border border-[var(--sf-accent)]/20 bg-[var(--sf-accent-soft)] p-4 text-sm">
          <p className="font-medium">Device API key (shown once)</p>
          <code className="mt-1 block break-all">{apiKey}</code>
        </div>
      )}

      <dl className="grid gap-4 rounded-2xl border border-black/5 bg-white/70 p-6 sm:grid-cols-2">
        <Item label="External ID" value={device.externalDeviceId} />
        <Item
          label="Last seen"
          value={device.lastSeenAt ? new Date(device.lastSeenAt).toLocaleString() : null}
        />
        <Item label="Created" value={new Date(device.createdAt).toLocaleString()} />
        <Item
          label="Linked asset"
          value={
            device.assetId ? (
              <Link
                to={`/assets/${device.assetId}`}
                className="text-[var(--sf-accent)] hover:underline"
              >
                View asset
              </Link>
            ) : null
          }
        />
      </dl>

      {allowMutate && (
        <form
          onSubmit={(e) => void onSave(e)}
          className="space-y-4 rounded-2xl border border-black/5 bg-white/70 p-6"
        >
          <h2 className="text-lg font-semibold">Edit device</h2>
          <label className="block text-sm">
            <span className="mb-1.5 block font-medium text-[var(--sf-muted)]">Device type</span>
            <input
              value={deviceType}
              onChange={(e) => setDeviceType(e.target.value)}
              className="w-full rounded-xl border border-black/10 bg-white px-3 py-2 outline-none ring-[var(--sf-accent)] focus:ring-2"
            />
          </label>
          <label className="block text-sm">
            <span className="mb-1.5 block font-medium text-[var(--sf-muted)]">Status</span>
            <select
              value={status}
              onChange={(e) => setStatus(e.target.value)}
              className="w-full rounded-xl border border-black/10 bg-white px-3 py-2 outline-none ring-[var(--sf-accent)] focus:ring-2"
            >
              {STATUSES.map((s) => (
                <option key={s} value={s}>
                  {s}
                </option>
              ))}
            </select>
          </label>
          <label className="block text-sm">
            <span className="mb-1.5 block font-medium text-[var(--sf-muted)]">Asset</span>
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
          <label className="block text-sm">
            <span className="mb-1.5 block font-medium text-[var(--sf-muted)]">Firmware</span>
            <input
              value={firmwareVersion}
              onChange={(e) => setFirmwareVersion(e.target.value)}
              className="w-full rounded-xl border border-black/10 bg-white px-3 py-2 outline-none ring-[var(--sf-accent)] focus:ring-2"
            />
          </label>
          {formError && <p className="text-sm text-[var(--sf-danger)]">{formError}</p>}
          {saveMutation.isSuccess && !formError && (
            <p className="text-sm text-[var(--sf-muted)]">Saved.</p>
          )}
          <div className="flex flex-wrap gap-3">
            <button
              type="submit"
              disabled={saveMutation.isPending}
              className="rounded-xl bg-[var(--sf-accent)] px-4 py-2.5 text-sm font-medium text-white disabled:opacity-60"
            >
              {saveMutation.isPending ? 'Saving…' : 'Save changes'}
            </button>
            <button
              type="button"
              onClick={onRotate}
              disabled={rotateMutation.isPending}
              className="rounded-xl border border-black/10 px-4 py-2.5 text-sm font-medium text-[var(--sf-ink)] hover:bg-black/[0.02] disabled:opacity-60"
            >
              {rotateMutation.isPending ? 'Rotating…' : 'Rotate API key'}
            </button>
          </div>
          {rotateMutation.isError && (
            <p className="text-sm text-[var(--sf-danger)]">
              {rotateMutation.error instanceof Error
                ? rotateMutation.error.message
                : 'Could not rotate key'}
            </p>
          )}
        </form>
      )}
    </div>
  )
}

function Item({ label, value }: { label: string; value: React.ReactNode }) {
  return (
    <div>
      <dt className="text-xs tracking-wide text-[var(--sf-muted)] uppercase">{label}</dt>
      <dd className="mt-1 font-medium">{value ?? '—'}</dd>
    </div>
  )
}
