import { useEffect, useRef, useState } from 'react'
import * as maplibregl from 'maplibre-gl'
import 'maplibre-gl/dist/maplibre-gl.css'
import type { Coordinate, Geofence } from '../../lib/api'
import { MapPolygonOverlay } from '../maps/MapPolygonOverlay'

type Props = {
  geofences?: Geofence[]
  draftCoordinates?: Coordinate[]
  onMapClick?: (coord: Coordinate) => void
  className?: string
  interactiveDraw?: boolean
}

export function GeofenceMap({
  geofences = [],
  draftCoordinates = [],
  onMapClick,
  className,
  interactiveDraw = false,
}: Props) {
  const containerRef = useRef<HTMLDivElement | null>(null)
  const mapRef = useRef<maplibregl.Map | null>(null)
  const vertexMarkersRef = useRef<maplibregl.Marker[]>([])
  const onMapClickRef = useRef(onMapClick)
  const interactiveDrawRef = useRef(interactiveDraw)
  const [map, setMap] = useState<maplibregl.Map | null>(null)

  onMapClickRef.current = onMapClick
  interactiveDrawRef.current = interactiveDraw

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

    const onClick = (e: maplibregl.MapMouseEvent) => {
      if (!interactiveDrawRef.current || !onMapClickRef.current) return
      onMapClickRef.current({ longitude: e.lngLat.lng, latitude: e.lngLat.lat })
    }

    instance.on('click', onClick)
    instance.on('load', () => {
      instance.resize()
      setMap(instance)
    })

    mapRef.current = instance
    requestAnimationFrame(() => instance.resize())

    return () => {
      instance.off('click', onClick)
      for (const marker of vertexMarkersRef.current) marker.remove()
      vertexMarkersRef.current = []
      setMap(null)
      instance.remove()
      mapRef.current = null
    }
  }, [])

  useEffect(() => {
    const instance = mapRef.current
    if (!instance) return
    instance.getCanvas().style.cursor = interactiveDraw ? 'crosshair' : ''
  }, [interactiveDraw])

  useEffect(() => {
    const instance = mapRef.current
    if (!instance) return

    for (const marker of vertexMarkersRef.current) marker.remove()
    vertexMarkersRef.current = draftCoordinates.map((c, index) => {
      const el = document.createElement('div')
      el.style.width = '14px'
      el.style.height = '14px'
      el.style.borderRadius = '9999px'
      el.style.background = '#1f6b4f'
      el.style.border = '2px solid #ffffff'
      el.style.boxShadow = '0 1px 4px rgba(0,0,0,0.35)'
      el.title = `Vertex ${index + 1}`
      return new maplibregl.Marker({ element: el })
        .setLngLat([c.longitude, c.latitude])
        .addTo(instance)
    })
  }, [draftCoordinates])

  useEffect(() => {
    const instance = mapRef.current
    if (!instance || interactiveDraw || geofences.length === 0) return

    const coords: [number, number][] = []
    for (const g of geofences) {
      for (const c of g.geometry?.coordinates?.[0] ?? []) {
        coords.push([c[0], c[1]])
      }
    }
    if (coords.length === 0) return

    const bounds = new maplibregl.LngLatBounds(coords[0], coords[0])
    for (const c of coords) bounds.extend(c)
    instance.fitBounds(bounds, { padding: 48, maxZoom: 14 })
  }, [geofences, interactiveDraw])

  return (
    <div
      className={`relative overflow-hidden ${className ?? 'h-80 w-full rounded-2xl border border-black/5'}`}
    >
      <div ref={containerRef} className="absolute inset-0 h-full w-full" />
      <MapPolygonOverlay
        map={map}
        geofences={geofences}
        draftCoordinates={draftCoordinates}
      />
    </div>
  )
}
