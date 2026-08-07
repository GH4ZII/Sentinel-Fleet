import { useCallback, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { createGeofence, type Coordinate } from '../../lib/api'
import { GeofenceMap } from './GeofenceMap'

export function NewGeofencePage() {
  const navigate = useNavigate()
  const [name, setName] = useState('')
  const [description, setDescription] = useState('')
  const [geofenceType, setGeofenceType] = useState('Allowed')
  const [coordinates, setCoordinates] = useState<Coordinate[]>([])
  const [error, setError] = useState<string | null>(null)
  const [saving, setSaving] = useState(false)

  const onMapClick = useCallback((coord: Coordinate) => {
    setCoordinates((prev) => [...prev, coord])
  }, [])

  async function handleSave() {
    setError(null)
    if (!name.trim()) {
      setError('Name is required.')
      return
    }
    if (coordinates.length < 3) {
      setError('Click the map at least 3 times to draw a polygon.')
      return
    }

    setSaving(true)
    try {
      const created = await createGeofence({
        name: name.trim(),
        description: description.trim() || undefined,
        geofenceType,
        coordinates,
      })
      navigate(`/geofences/${created.id}`)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to create geofence')
    } finally {
      setSaving(false)
    }
  }

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-3xl font-semibold tracking-tight">New geofence</h1>
        <p className="mt-1 text-[var(--sf-muted)]">
          Click the map to add polygon corners (minimum 3), then save.
        </p>
      </div>

      <div className="grid gap-6 lg:grid-cols-[1fr_280px]">
        <GeofenceMap
          draftCoordinates={coordinates}
          onMapClick={onMapClick}
          interactiveDraw
          className="h-[28rem] w-full overflow-hidden rounded-2xl border border-black/5"
        />

        <div className="space-y-4 rounded-2xl border border-black/5 bg-white/70 p-4">
          <label className="block text-sm">
            <span className="text-[var(--sf-muted)]">Name</span>
            <input
              className="mt-1 w-full rounded-lg border border-black/10 bg-white px-3 py-2"
              value={name}
              onChange={(e) => setName(e.target.value)}
            />
          </label>
          <label className="block text-sm">
            <span className="text-[var(--sf-muted)]">Description</span>
            <textarea
              className="mt-1 w-full rounded-lg border border-black/10 bg-white px-3 py-2"
              rows={3}
              value={description}
              onChange={(e) => setDescription(e.target.value)}
            />
          </label>
          <label className="block text-sm">
            <span className="text-[var(--sf-muted)]">Type</span>
            <select
              className="mt-1 w-full rounded-lg border border-black/10 bg-white px-3 py-2"
              value={geofenceType}
              onChange={(e) => setGeofenceType(e.target.value)}
            >
              <option value="Allowed">Allowed</option>
              <option value="Restricted">Restricted</option>
            </select>
          </label>

          <p className="text-sm text-[var(--sf-muted)]">
            Vertices: {coordinates.length}
          </p>
          <div className="flex flex-wrap gap-2">
            <button
              type="button"
              className="rounded-lg border border-black/10 px-3 py-2 text-sm"
              onClick={() => setCoordinates((prev) => prev.slice(0, -1))}
              disabled={coordinates.length === 0}
            >
              Undo point
            </button>
            <button
              type="button"
              className="rounded-lg border border-black/10 px-3 py-2 text-sm"
              onClick={() => setCoordinates([])}
              disabled={coordinates.length === 0}
            >
              Clear
            </button>
          </div>

          {error && <p className="text-sm text-[var(--sf-danger)]">{error}</p>}

          <button
            type="button"
            className="w-full rounded-xl bg-[var(--sf-accent)] px-4 py-2.5 text-sm font-medium text-white disabled:opacity-60"
            onClick={() => void handleSave()}
            disabled={saving}
          >
            {saving ? 'Saving…' : 'Save geofence'}
          </button>
        </div>
      </div>
    </div>
  )
}
