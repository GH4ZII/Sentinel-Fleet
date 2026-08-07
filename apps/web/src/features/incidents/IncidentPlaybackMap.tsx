import { useEffect, useMemo, useRef, useState } from 'react'
import * as maplibregl from 'maplibre-gl'
import 'maplibre-gl/dist/maplibre-gl.css'
import type { IncidentPosition, IncidentTimelineEntry } from '../../lib/api'

type Props = {
  positions: IncidentPosition[]
  timeline: IncidentTimelineEntry[]
}

const MAX_POINTS = 300

function samplePositions(positions: IncidentPosition[]): IncidentPosition[] {
  const points = [...positions].sort((a, b) => a.recordedAt.localeCompare(b.recordedAt))
  if (points.length <= MAX_POINTS) return points
  const step = (points.length - 1) / (MAX_POINTS - 1)
  const sampled: IncidentPosition[] = []
  for (let i = 0; i < MAX_POINTS; i++) {
    sampled.push(points[Math.round(i * step)]!)
  }
  return sampled
}

/**
 * Playback map — follows the same MapLibre init pattern as AssetsMap
 * (wait for style load via React state before mutating the map).
 */
export function IncidentPlaybackMap({ positions, timeline }: Props) {
  const containerRef = useRef<HTMLDivElement | null>(null)
  const mapRef = useRef<maplibregl.Map | null>(null)
  const playMarkerRef = useRef<maplibregl.Marker | null>(null)
  const eventMarkersRef = useRef<maplibregl.Marker[]>([])
  const fittedRef = useRef(false)

  const [map, setMap] = useState<maplibregl.Map | null>(null)
  const [playing, setPlaying] = useState(false)
  const [speed, setSpeed] = useState(4)
  const [index, setIndex] = useState(0)

  const sorted = useMemo(
    () => samplePositions(Array.isArray(positions) ? positions : []),
    [positions],
  )

  const eventPoints = useMemo(
    () =>
      (Array.isArray(timeline) ? timeline : []).filter(
        (e) =>
          e.entryType === 'Detection' &&
          typeof e.latitude === 'number' &&
          typeof e.longitude === 'number',
      ),
    [timeline],
  )

  useEffect(() => {
    if (!containerRef.current || mapRef.current) return

    const instance = new maplibregl.Map({
      container: containerRef.current,
      style: {
        version: 8,
        sources: {
          osm: {
            type: 'raster',
            tiles: ['https://tile.openstreetmap.org/{z}/{x}/{y}.png'],
            tileSize: 256,
            attribution: '© OpenStreetMap',
          },
        },
        layers: [{ id: 'osm', type: 'raster', source: 'osm' }],
      },
      center: [10.7522, 59.9139],
      zoom: 12,
    })

    instance.addControl(new maplibregl.NavigationControl(), 'top-right')
    instance.on('load', () => {
      instance.resize()
      setMap(instance)
    })

    mapRef.current = instance
    requestAnimationFrame(() => instance.resize())

    return () => {
      playMarkerRef.current?.remove()
      playMarkerRef.current = null
      for (const marker of eventMarkersRef.current) marker.remove()
      eventMarkersRef.current = []
      setMap(null)
      instance.remove()
      mapRef.current = null
      fittedRef.current = false
    }
  }, [])

  // Draw route line + fit bounds once map is ready.
  useEffect(() => {
    if (!map || sorted.length === 0) return

    const coords = sorted.map((p) => [p.longitude, p.latitude] as [number, number])
    const data: GeoJSON.Feature = {
      type: 'Feature',
      properties: {},
      geometry: { type: 'LineString', coordinates: coords },
    }

    const existing = map.getSource('route') as maplibregl.GeoJSONSource | undefined
    if (existing) {
      existing.setData(data)
    } else if (map.isStyleLoaded()) {
      map.addSource('route', { type: 'geojson', data })
      map.addLayer({
        id: 'route-line',
        type: 'line',
        source: 'route',
        paint: {
          'line-color': '#b45309',
          'line-width': 3,
          'line-opacity': 0.85,
        },
      })
    }

    if (!fittedRef.current) {
      if (coords.length === 1) {
        map.jumpTo({ center: coords[0], zoom: 14 })
      } else {
        const bounds = new maplibregl.LngLatBounds(coords[0], coords[0])
        for (const c of coords) bounds.extend(c)
        map.fitBounds(bounds, { padding: 48, maxZoom: 15 })
      }
      fittedRef.current = true
    }
  }, [map, sorted])

  // Playhead marker
  useEffect(() => {
    if (!map || sorted.length === 0) return
    const point = sorted[Math.min(index, sorted.length - 1)]
    if (!point) return
    if (!Number.isFinite(point.longitude) || !Number.isFinite(point.latitude)) return

    if (!playMarkerRef.current) {
      const el = document.createElement('div')
      el.className = 'h-4 w-4 rounded-full border-2 border-white bg-[var(--sf-accent)] shadow-lg'
      playMarkerRef.current = new maplibregl.Marker({ element: el })
        .setLngLat([point.longitude, point.latitude])
        .addTo(map)
    } else {
      playMarkerRef.current.setLngLat([point.longitude, point.latitude])
    }
  }, [map, sorted, index])

  // Detection markers
  useEffect(() => {
    if (!map) return

    for (const marker of eventMarkersRef.current) marker.remove()
    eventMarkersRef.current = []

    for (const event of eventPoints) {
      if (!Number.isFinite(event.longitude) || !Number.isFinite(event.latitude)) continue
      const el = document.createElement('div')
      el.className = 'h-3 w-3 rounded-full border-2 border-white bg-[var(--sf-danger)] shadow'
      el.title = event.title
      eventMarkersRef.current.push(
        new maplibregl.Marker({ element: el })
          .setLngLat([event.longitude!, event.latitude!])
          .addTo(map),
      )
    }
  }, [map, eventPoints])

  useEffect(() => {
    if (!playing || sorted.length < 2) return
    const timer = window.setInterval(() => {
      setIndex((prev) => {
        if (prev >= sorted.length - 1) {
          setPlaying(false)
          return prev
        }
        return prev + 1
      })
    }, Math.max(40, 800 / speed))
    return () => window.clearInterval(timer)
  }, [playing, speed, sorted.length])

  const current = sorted[Math.min(index, Math.max(sorted.length - 1, 0))]
  const speedLabel =
    current && typeof current.speedKph === 'number'
      ? ` · ${current.speedKph.toFixed(0)} km/h`
      : ''

  return (
    <div className="space-y-4">
      <div className="relative h-[420px] w-full overflow-hidden rounded-2xl border border-black/5">
        <div ref={containerRef} className="absolute inset-0 h-full w-full" />
        {!map && (
          <div className="absolute inset-0 flex items-center justify-center text-sm text-[var(--sf-muted)]">
            Loading map…
          </div>
        )}
      </div>

      {sorted.length === 0 ? (
        <p className="text-sm text-[var(--sf-muted)]">
          No telemetry positions in this incident window.
        </p>
      ) : (
        <div className="flex flex-wrap items-center gap-3">
          <button
            type="button"
            className="rounded-lg bg-[var(--sf-accent)] px-3 py-1.5 text-sm font-medium text-white"
            onClick={() => setPlaying((value) => !value)}
          >
            {playing ? 'Pause' : 'Play'}
          </button>
          <button
            type="button"
            className="rounded-lg border border-black/10 px-3 py-1.5 text-sm"
            onClick={() => {
              setPlaying(false)
              setIndex(0)
            }}
          >
            Restart
          </button>
          <label className="flex items-center gap-2 text-sm text-[var(--sf-muted)]">
            Speed
            <select
              className="rounded-md border border-black/10 bg-white px-2 py-1 text-[var(--sf-ink)]"
              value={speed}
              onChange={(e) => setSpeed(Number(e.target.value))}
            >
              <option value={1}>1x</option>
              <option value={2}>2x</option>
              <option value={4}>4x</option>
              <option value={8}>8x</option>
            </select>
          </label>
          <input
            type="range"
            min={0}
            max={Math.max(sorted.length - 1, 0)}
            value={Math.min(index, Math.max(sorted.length - 1, 0))}
            onChange={(e) => {
              setPlaying(false)
              setIndex(Number(e.target.value))
            }}
            className="min-w-[180px] flex-1"
          />
          <span className="text-sm text-[var(--sf-muted)]">
            {current ? new Date(current.recordedAt).toLocaleString() : ''}
            {speedLabel}
          </span>
        </div>
      )}
    </div>
  )
}
