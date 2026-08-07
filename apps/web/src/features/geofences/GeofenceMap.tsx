import { useEffect, useRef, useState } from 'react'
import * as maplibregl from 'maplibre-gl'
import 'maplibre-gl/dist/maplibre-gl.css'
import type { Coordinate, Geofence } from '../../lib/api'

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
  const [ready, setReady] = useState(false)

  useEffect(() => {
    if (!containerRef.current || mapRef.current) return

    const map = new maplibregl.Map({
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

    map.addControl(new maplibregl.NavigationControl(), 'top-right')

    const setupLayers = () => {
      if (map.getSource('geofences')) {
        setReady(true)
        map.resize()
        return
      }

      map.addSource('geofences', {
        type: 'geojson',
        data: { type: 'FeatureCollection', features: [] },
      })
      map.addLayer({
        id: 'geofences-fill',
        type: 'fill',
        source: 'geofences',
        paint: {
          'fill-color': [
            'match',
            ['get', 'geofenceType'],
            'Restricted',
            '#9b2c2c',
            '#1f6b4f',
          ],
          'fill-opacity': 0.35,
        },
      })
      map.addLayer({
        id: 'geofences-outline',
        type: 'line',
        source: 'geofences',
        paint: {
          'line-color': [
            'match',
            ['get', 'geofenceType'],
            'Restricted',
            '#9b2c2c',
            '#1f6b4f',
          ],
          'line-width': 3,
        },
      })

      map.addSource('draft', {
        type: 'geojson',
        data: { type: 'FeatureCollection', features: [] },
      })
      map.addLayer({
        id: 'draft-line',
        type: 'line',
        source: 'draft',
        paint: { 'line-color': '#0f1c18', 'line-width': 2, 'line-dasharray': [2, 1] },
      })
      map.addLayer({
        id: 'draft-points',
        type: 'circle',
        source: 'draft',
        filter: ['==', ['get', 'kind'], 'vertex'],
        paint: {
          'circle-radius': 5,
          'circle-color': '#1f6b4f',
          'circle-stroke-width': 2,
          'circle-stroke-color': '#ffffff',
        },
      })

      setReady(true)
      map.resize()
    }

    if (map.loaded()) setupLayers()
    else map.once('load', setupLayers)

    mapRef.current = map

    return () => {
      map.remove()
      mapRef.current = null
      setReady(false)
    }
  }, [])

  useEffect(() => {
    const map = mapRef.current
    if (!map || !ready || !interactiveDraw || !onMapClick) return

    const handler = (e: maplibregl.MapMouseEvent) => {
      onMapClick({ longitude: e.lngLat.lng, latitude: e.lngLat.lat })
    }
    map.on('click', handler)
    map.getCanvas().style.cursor = 'crosshair'
    return () => {
      map.off('click', handler)
      map.getCanvas().style.cursor = ''
    }
  }, [ready, interactiveDraw, onMapClick])

  useEffect(() => {
    const map = mapRef.current
    if (!map || !ready) return
    const source = map.getSource('geofences') as maplibregl.GeoJSONSource | undefined
    if (!source) return

    const features = geofences
      .filter((g) => g.geometry?.coordinates?.length)
      .map((g) => ({
        type: 'Feature' as const,
        properties: {
          id: g.id,
          name: g.name,
          geofenceType: g.geofenceType,
        },
        geometry: {
          type: 'Polygon' as const,
          coordinates: g.geometry.coordinates,
        },
      }))

    source.setData({ type: 'FeatureCollection', features })

    const coords = features.flatMap((f) =>
      (f.geometry.coordinates[0] ?? []).map((c) => [c[0], c[1]] as [number, number]),
    )
    if (coords.length > 0) {
      const bounds = new maplibregl.LngLatBounds(coords[0], coords[0])
      for (const c of coords) bounds.extend(c)
      map.fitBounds(bounds, { padding: 48, maxZoom: 14 })
    }
  }, [geofences, ready])

  useEffect(() => {
    const map = mapRef.current
    if (!map || !ready) return
    const source = map.getSource('draft') as maplibregl.GeoJSONSource | undefined
    if (!source) return

    const features: GeoJSON.Feature[] = []
    if (draftCoordinates.length >= 2) {
      features.push({
        type: 'Feature',
        properties: { kind: 'line' },
        geometry: {
          type: 'LineString',
          coordinates: draftCoordinates.map((c) => [c.longitude, c.latitude]),
        },
      })
    }
    for (const c of draftCoordinates) {
      features.push({
        type: 'Feature',
        properties: { kind: 'vertex' },
        geometry: {
          type: 'Point',
          coordinates: [c.longitude, c.latitude],
        },
      })
    }

    source.setData({ type: 'FeatureCollection', features })
  }, [draftCoordinates, ready])

  return (
    <div
      ref={containerRef}
      className={className ?? 'h-80 w-full overflow-hidden rounded-2xl border border-black/5'}
    />
  )
}
