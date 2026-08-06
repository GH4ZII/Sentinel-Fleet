import { useEffect, useRef } from 'react'
import * as maplibregl from 'maplibre-gl'
import 'maplibre-gl/dist/maplibre-gl.css'
import type { Asset } from '../../lib/api'
import type { LivePosition } from '../../lib/fleetHub'

type Props = {
  assets: Asset[]
  livePositions?: Record<string, LivePosition>
  className?: string
  followLive?: boolean
}

export function AssetsMap({ assets, livePositions = {}, className, followLive = true }: Props) {
  const containerRef = useRef<HTMLDivElement | null>(null)
  const mapRef = useRef<maplibregl.Map | null>(null)
  const markersRef = useRef<Map<string, maplibregl.Marker>>(new Map())
  const fittedRef = useRef(false)

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
      zoom: 11,
    })

    map.addControl(new maplibregl.NavigationControl(), 'top-right')
    mapRef.current = map

    return () => {
      for (const marker of markersRef.current.values()) marker.remove()
      markersRef.current.clear()
      map.remove()
      mapRef.current = null
    }
  }, [])

  useEffect(() => {
    const map = mapRef.current
    if (!map) return

    const seen = new Set<string>()

    for (const asset of assets) {
      const live = livePositions[asset.id]
      const lat = live?.latitude ?? asset.mapLatitude
      const lng = live?.longitude ?? asset.mapLongitude
      if (lat == null || lng == null) continue

      seen.add(asset.id)
      let marker = markersRef.current.get(asset.id)
      if (!marker) {
        const el = document.createElement('div')
        el.className =
          'h-3 w-3 rounded-full border-2 border-white bg-[var(--sf-accent)] shadow'
        el.title = asset.name
        marker = new maplibregl.Marker({ element: el })
          .setLngLat([lng, lat])
          .setPopup(
            new maplibregl.Popup({ offset: 12 }).setHTML(
              `<strong>${escapeHtml(asset.name)}</strong><br/><span>${escapeHtml(asset.status)}</span>`,
            ),
          )
          .addTo(map)
        markersRef.current.set(asset.id, marker)
      } else {
        marker.setLngLat([lng, lat])
      }
    }

    for (const [id, marker] of markersRef.current) {
      if (!seen.has(id)) {
        marker.remove()
        markersRef.current.delete(id)
      }
    }

    if (!fittedRef.current && seen.size > 0) {
      const coords: [number, number][] = []
      for (const asset of assets) {
        const live = livePositions[asset.id]
        const lat = live?.latitude ?? asset.mapLatitude
        const lng = live?.longitude ?? asset.mapLongitude
        if (lat != null && lng != null) coords.push([lng, lat])
      }

      if (coords.length === 1) {
        map.flyTo({ center: coords[0], zoom: 13 })
      } else if (coords.length > 1) {
        const bounds = new maplibregl.LngLatBounds(coords[0], coords[0])
        for (const c of coords) bounds.extend(c)
        map.fitBounds(bounds, { padding: 48, maxZoom: 13 })
      }
      fittedRef.current = true
    } else if (followLive && seen.size === 1) {
      const onlyId = [...seen][0]
      const live = livePositions[onlyId]
      if (live) {
        map.easeTo({
          center: [live.longitude, live.latitude],
          duration: 500,
        })
      }
    }
  }, [assets, livePositions, followLive])

  return (
    <div
      ref={containerRef}
      className={className ?? 'h-80 w-full overflow-hidden rounded-2xl border border-black/5'}
    />
  )
}

function escapeHtml(value: string) {
  return value
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
    .replace(/"/g, '&quot;')
}
