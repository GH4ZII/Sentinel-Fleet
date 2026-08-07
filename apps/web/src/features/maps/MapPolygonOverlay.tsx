import { useEffect, useRef } from 'react'
import type { Map as MapLibreMap } from 'maplibre-gl'
import type { Coordinate, Geofence } from '../../lib/api'

type PolygonSpec = {
  id: string
  ring: Coordinate[]
  fill: string
  stroke: string
  opacity?: number
}

type Props = {
  map: MapLibreMap | null
  geofences?: Geofence[]
  draftCoordinates?: Coordinate[]
}

function geofenceToSpec(g: Geofence): PolygonSpec | null {
  const ringCoords = g.geometry?.coordinates?.[0]
  if (!Array.isArray(ringCoords) || ringCoords.length < 4) return null

  const restricted = g.geofenceType === 'Restricted'
  return {
    id: g.id,
    ring: ringCoords.map((c) => ({ longitude: c[0], latitude: c[1] })),
    fill: restricted ? '#9b2c2c' : '#1f6b4f',
    stroke: restricted ? '#9b2c2c' : '#1f6b4f',
    opacity: 0.35,
  }
}

function draftToSpec(draft: Coordinate[]): PolygonSpec | null {
  if (draft.length < 2) return null
  const ring = [...draft]
  if (draft.length >= 3) {
    const first = draft[0]
    const last = draft[draft.length - 1]
    if (first.longitude !== last.longitude || first.latitude !== last.latitude) {
      ring.push(first)
    }
  }
  return {
    id: 'draft',
    ring,
    fill: '#1f6b4f',
    stroke: '#0f1c18',
    opacity: draft.length >= 3 ? 0.3 : 0,
  }
}

/** Renders polygons as SVG on top of MapLibre — reliable across MapLibre versions. */
export function MapPolygonOverlay({ map, geofences = [], draftCoordinates = [] }: Props) {
  const svgRef = useRef<SVGSVGElement | null>(null)

  useEffect(() => {
    if (!map) return

    const render = () => {
      const svg = svgRef.current
      if (!svg) return

      const container = map.getContainer()
      const width = container.clientWidth
      const height = container.clientHeight
      if (width <= 0 || height <= 0) return

      svg.setAttribute('viewBox', `0 0 ${width} ${height}`)
      svg.setAttribute('width', String(width))
      svg.setAttribute('height', String(height))

      const specs: PolygonSpec[] = []
      for (const g of geofences) {
        const spec = geofenceToSpec(g)
        if (spec) specs.push(spec)
      }
      const draft = draftToSpec(draftCoordinates)
      if (draft) specs.push(draft)

      while (svg.firstChild) svg.removeChild(svg.firstChild)

      for (const spec of specs) {
        const points = spec.ring
          .map((c) => {
            const p = map.project([c.longitude, c.latitude])
            return `${p.x},${p.y}`
          })
          .join(' ')

        if (spec.ring.length >= 3 && (spec.opacity ?? 0) > 0) {
          const polygon = document.createElementNS('http://www.w3.org/2000/svg', 'polygon')
          polygon.setAttribute('points', points)
          polygon.setAttribute('fill', spec.fill)
          polygon.setAttribute('fill-opacity', String(spec.opacity ?? 0.35))
          polygon.setAttribute('stroke', 'none')
          svg.appendChild(polygon)
        }

        const polyline = document.createElementNS('http://www.w3.org/2000/svg', 'polyline')
        polyline.setAttribute('points', points)
        polyline.setAttribute('fill', 'none')
        polyline.setAttribute('stroke', spec.stroke)
        polyline.setAttribute('stroke-width', '3')
        polyline.setAttribute('stroke-linejoin', 'round')
        polyline.setAttribute('stroke-linecap', 'round')
        svg.appendChild(polyline)
      }
    }

    render()
    map.on('render', render)
    map.on('resize', render)

    return () => {
      map.off('render', render)
      map.off('resize', render)
    }
  }, [map, geofences, draftCoordinates])

  return (
    <svg
      ref={svgRef}
      className="pointer-events-none absolute inset-0 z-10 h-full w-full"
      aria-hidden
    />
  )
}
