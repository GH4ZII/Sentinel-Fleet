import { FormEvent, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { createAsset } from '../../lib/api'

export function NewAssetPage() {
  const navigate = useNavigate()
  const [name, setName] = useState('')
  const [registrationNumber, setRegistrationNumber] = useState('')
  const [manufacturer, setManufacturer] = useState('')
  const [model, setModel] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [pending, setPending] = useState(false)
  const [deviceKey, setDeviceKey] = useState<string | null>(null)

  async function onSubmit(event: FormEvent) {
    event.preventDefault()
    setPending(true)
    setError(null)
    try {
      const result = await createAsset({
        name,
        registrationNumber: registrationNumber || undefined,
        manufacturer: manufacturer || undefined,
        model: model || undefined,
      })
      if (result.deviceApiKey) {
        setDeviceKey(result.deviceApiKey)
      }
      navigate(`/assets/${result.asset.id}`, {
        state: { deviceApiKey: result.deviceApiKey },
      })
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Could not create asset')
      setPending(false)
    }
  }

  return (
    <div className="mx-auto max-w-xl space-y-6">
      <div>
        <h1 className="text-3xl font-semibold tracking-tight">Register vehicle</h1>
        <p className="mt-1 text-[var(--sf-muted)]">
          Creates an asset and a GPS device key for later telemetry.
        </p>
      </div>

      <form
        onSubmit={(e) => void onSubmit(e)}
        className="space-y-4 rounded-2xl border border-black/5 bg-white/70 p-6"
      >
        <Field label="Name" value={name} onChange={setName} required placeholder="Varebil 12" />
        <Field
          label="Registration number"
          value={registrationNumber}
          onChange={setRegistrationNumber}
          placeholder="AB12345"
        />
        <Field label="Manufacturer" value={manufacturer} onChange={setManufacturer} />
        <Field label="Model" value={model} onChange={setModel} />
        {error && <p className="text-sm text-[var(--sf-danger)]">{error}</p>}
        {deviceKey && (
          <p className="rounded-lg bg-[var(--sf-accent-soft)] p-3 text-sm">
            Device API key (save now): <code>{deviceKey}</code>
          </p>
        )}
        <button
          type="submit"
          disabled={pending}
          className="rounded-xl bg-[var(--sf-accent)] px-4 py-2.5 font-medium text-white disabled:opacity-60"
        >
          {pending ? 'Saving…' : 'Create asset'}
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
