import { useEffect, useMemo, useRef, useState } from 'react'
import * as maplibregl from 'maplibre-gl'
import 'maplibre-gl/dist/maplibre-gl.css'
import type { IncidentPosition, IncidentTimelineEntry } from '../../lib/api'

type Props = {
  positions: IncidentPosition[]
  timeline: IncidentTimelineEntry[]
}

export function IncidentPlaybackMap({ positions, timeline }: Props) {
  const containerRef = useRef<HTMLDivElement | null>(null)
  const mapRef = useRef<maplibregl.Map | null>(null)
  const markerRef = useRef<maplibregl.Marker | null>(null)
  const eventMarkersRef = useRef<maplibregl.Marker[]>([])
  const [playing, setPlaying] = useState(false)
  const [speed, setSpeed] = useState(4)
  const [index, setIndex] = useState(0)

  const sorted = useMemo(
    () => [...positions].sort((a, b) => a.recordedAt.localeCompare(b.recordedAt)),
    [positions],
  )

  const eventPoints = useMemo(
    () =>
      timeline.filter(
        (e) => e.latitude != null && e.longitude != null && e.entryType === 'Detection',
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
    mapRef.current = instance
    requestAnimationFrame(() => instance.resize())

    return () => {
      markerRef.current?.remove()
      for (const m of eventMarkersRef.current) m.remove()
      eventMarkersRef.current = []
      instance.remove()
      mapRef.current = null
    }
  }, [])

  useEffect(() => {
    const map = mapRef.current
    if (!map || sorted.length === 0) return

    const drawRoute = () => {
      const coords = sorted.map((p) => [p.longitude, p.latitude] as [number, number])
      if (map.getSource('route')) {
        ;(map.getSource('route') as maplibregl.GeoJSONSource).setData({
          type: 'Feature',
          properties: {},
          geometry: { type: 'LineString', coordinates: coords },
        })
      } else {
        map.addSource('route', {
          type: 'geojson',
          data: {
            type: 'Feature',
            properties: {},
            geometry: { type: 'LineString', coordinates: coords },
          },
        })
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

      const bounds = coords.reduce(
        (b, c) => b.extend(c),
        new maplibregl.LngLatBounds(coords[0], coords[0]),
      )
      map.fitBounds(bounds, { padding: 48, maxZoom: 15 })
    }

    if (map.isStyleLoaded()) drawRoute()
    else map.once('load', drawRoute)
  }, [sorted])

  useEffect(() => {
    const map = mapRef.current
    if (!map) return

    for (const m of eventMarkersRef.current) m.remove()
    eventMarkersRef.current = []

    for (const event of eventPoints) {
      const el = document.createElement('div')
      el.className = 'h-3 w-3 rounded-full border-2 border-white bg-[var(--sf-danger)] shadow'
      el.title = event.title
      const marker = new maplibregl.Marker({ element: el })
        .setLngLat([event.longitude!, event.latitude!])
        .addTo(map)
      eventMarkersRef.current.push(marker)
    }
  }, [eventPoints])

  useEffect(() => {
    const map = mapRef.current
    if (!map || sorted.length === 0) return

    const point = sorted[Math.min(index, sorted.length - 1)]
    if (!markerRef.current) {
      const el = document.createElement('div')
      el.className = 'h-4 w-4 rounded-full border-2 border-white bg-[var(--sf-accent)] shadow-lg'
      markerRef.current = new maplibregl.Marker({ element: el }).addTo(map)
    }
    markerRef.current.setLngLat([point.longitude, point.latitude])
  }, [index, sorted])

  useEffect(() => {
    if (!playing || sorted.length === 0) return
    const id = window.setInterval(() => {
      setIndex((prev) => {
        if (prev >= sorted.length - 1) {
          setPlaying(false)
          return prev
        }
        return prev + 1
      })
    }, Math.max(50, 1000 / speed))
    return () => window.clearInterval(id)
  }, [playing, speed, sorted.length])

  const current = sorted[Math.min(index, Math.max(sorted.length - 1, 0))]

  return (
    <div className="space-y-4">
      <div ref={containerRef} className="h-[420px] w-full overflow-hidden rounded-2xl border border-black/5" />

      <div className="flex flex-wrap items-center gap-3">
        <button
          type="button"
          className="rounded-lg bg-[var(--sf-accent)] px-3 py-1.5 text-sm font-medium text-white"
          onClick={() => setPlaying((p) => !p)}
          disabled={sorted.length === 0}
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
          disabled={sorted.length === 0}
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
          value={index}
          onChange={(e) => {
            setPlaying(false)
            setIndex(Number(e.target.value))
          }}
          className="min-w-[180px] flex-1"
          disabled={sorted.length === 0}
        />
        <span className="text-sm text-[var(--sf-muted)]">
          {current ? new Date(current.recordedAt).toLocaleString() : 'No positions'}
          {current?.speedKph != null ? ` · ${current.speedKph.toFixed(0)} km/h` : ''}
        </span>
      </div>
    </div>
  )
}
