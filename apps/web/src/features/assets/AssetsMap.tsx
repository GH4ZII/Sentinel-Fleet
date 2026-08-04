import { useEffect, useRef } from 'react'
import * as maplibregl from 'maplibre-gl'
import 'maplibre-gl/dist/maplibre-gl.css'
import type { Asset } from '../../lib/api'

type Props = {
  assets: Asset[]
  className?: string
}

export function AssetsMap({ assets, className }: Props) {
  const containerRef = useRef<HTMLDivElement | null>(null)
  const mapRef = useRef<maplibregl.Map | null>(null)

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
      map.remove()
      mapRef.current = null
    }
  }, [])

  useEffect(() => {
    const map = mapRef.current
    if (!map) return

    const markers: maplibregl.Marker[] = []
    const withCoords = assets.filter(
      (a) => a.mapLatitude != null && a.mapLongitude != null,
    )

    for (const asset of withCoords) {
      const el = document.createElement('div')
      el.className =
        'h-3 w-3 rounded-full border-2 border-white bg-[var(--sf-accent)] shadow'
      el.title = asset.name

      const marker = new maplibregl.Marker({ element: el })
        .setLngLat([asset.mapLongitude!, asset.mapLatitude!])
        .setPopup(
          new maplibregl.Popup({ offset: 12 }).setHTML(
            `<strong>${escapeHtml(asset.name)}</strong><br/><span>${escapeHtml(asset.status)}</span>`,
          ),
        )
        .addTo(map)

      markers.push(marker)
    }

    if (withCoords.length === 1) {
      map.flyTo({
        center: [withCoords[0].mapLongitude!, withCoords[0].mapLatitude!],
        zoom: 13,
      })
    } else if (withCoords.length > 1) {
      const bounds = new maplibregl.LngLatBounds()
      for (const asset of withCoords) {
        bounds.extend([asset.mapLongitude!, asset.mapLatitude!])
      }
      map.fitBounds(bounds, { padding: 48, maxZoom: 13 })
    }

    return () => {
      for (const marker of markers) marker.remove()
    }
  }, [assets])

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
