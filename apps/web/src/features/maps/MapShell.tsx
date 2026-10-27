import { useEffect, useState, type FormEvent, type ReactNode, type Ref } from 'react'
import type { Map as MapLibreMap } from 'maplibre-gl'

type GeocodeResult = {
  place_id: number
  display_name: string
  lat: string
  lon: string
  boundingbox?: [string, string, string, string]
}

async function searchAddress(query: string): Promise<GeocodeResult[]> {
  const params = new URLSearchParams({
    format: 'json',
    q: query,
    limit: '5',
    addressdetails: '0',
  })
  const res = await fetch(`https://nominatim.openstreetmap.org/search?${params}`, {
    headers: { Accept: 'application/json' },
  })
  if (!res.ok) throw new Error('Address search failed')
  return (await res.json()) as GeocodeResult[]
}

type Props = {
  map: MapLibreMap | null
  mapContainerRef: Ref<HTMLDivElement>
  className?: string
  children?: ReactNode
}

export function MapShell({ map, mapContainerRef, className, children }: Props) {
  const [expanded, setExpanded] = useState(false)
  const [query, setQuery] = useState('')
  const [results, setResults] = useState<GeocodeResult[]>([])
  const [searching, setSearching] = useState(false)
  const [searchError, setSearchError] = useState<string | null>(null)
  const [resultsOpen, setResultsOpen] = useState(false)

  useEffect(() => {
    if (!map) return
    const id = window.setTimeout(() => map.resize(), 50)
    return () => window.clearTimeout(id)
  }, [expanded, map])

  useEffect(() => {
    if (!expanded) return
    const onKey = (e: KeyboardEvent) => {
      if (e.key === 'Escape') setExpanded(false)
    }
    window.addEventListener('keydown', onKey)
    const prev = document.body.style.overflow
    document.body.style.overflow = 'hidden'
    return () => {
      window.removeEventListener('keydown', onKey)
      document.body.style.overflow = prev
    }
  }, [expanded])

  async function handleSearch(e?: FormEvent) {
    e?.preventDefault()
    const q = query.trim()
    if (!q) return

    setSearching(true)
    setSearchError(null)
    try {
      const found = await searchAddress(q)
      setResults(found)
      setResultsOpen(true)
      if (found.length === 0) setSearchError('No addresses found.')
    } catch {
      setResults([])
      setSearchError('Could not search address.')
      setResultsOpen(true)
    } finally {
      setSearching(false)
    }
  }

  function goToResult(result: GeocodeResult) {
    if (!map) return

    const lon = Number(result.lon)
    const lat = Number(result.lat)
    if (!Number.isFinite(lon) || !Number.isFinite(lat)) return

    if (result.boundingbox && result.boundingbox.length === 4) {
      const [south, north, west, east] = result.boundingbox.map(Number)
      if ([south, north, west, east].every(Number.isFinite)) {
        map.fitBounds(
          [
            [west, south],
            [east, north],
          ],
          { padding: 48, maxZoom: 16, duration: 1200 },
        )
      } else {
        map.flyTo({ center: [lon, lat], zoom: 15, essential: true })
      }
    } else {
      map.flyTo({ center: [lon, lat], zoom: 15, essential: true })
    }

    setQuery(result.display_name.split(',').slice(0, 3).join(','))
    setResultsOpen(false)
  }

  const defaultClass = 'h-80 w-full rounded-2xl border border-black/5'
  const collapsedClass = `relative overflow-hidden ${className ?? defaultClass}`
  const shellClass = expanded
    ? 'fixed inset-3 z-50 overflow-hidden rounded-2xl border border-black/10 bg-white shadow-2xl'
    : collapsedClass

  return (
    <>
      {expanded && <div className={collapsedClass} aria-hidden />}
      {expanded && (
        <button
          type="button"
          aria-label="Close expanded map"
          className="fixed inset-0 z-40 bg-black/40"
          onClick={() => setExpanded(false)}
        />
      )}

      <div className={shellClass}>
        <div ref={mapContainerRef} className="absolute inset-0 h-full w-full" />
        {children}

        <div className="pointer-events-none absolute inset-x-0 top-0 z-10 flex items-start gap-2 p-3 pr-14">
          <form
            className="pointer-events-auto relative min-w-0 max-w-md flex-1"
            onSubmit={(e) => void handleSearch(e)}
          >
            <div className="flex overflow-hidden rounded-xl border border-black/10 bg-white/95 shadow-sm backdrop-blur">
              <input
                type="search"
                value={query}
                onChange={(e) => {
                  setQuery(e.target.value)
                  setSearchError(null)
                }}
                onFocus={() => {
                  if (results.length > 0) setResultsOpen(true)
                }}
                placeholder="Search address…"
                className="min-w-0 flex-1 bg-transparent px-3 py-2 text-sm outline-none"
                aria-label="Search address"
              />
              <button
                type="submit"
                className="border-l border-black/10 px-3 text-sm font-medium text-[var(--sf-accent)] disabled:opacity-60"
                disabled={searching || !query.trim()}
              >
                {searching ? '…' : 'Search'}
              </button>
            </div>

            {resultsOpen && (results.length > 0 || searchError) && (
              <ul className="mt-1 max-h-56 overflow-auto rounded-xl border border-black/10 bg-white/95 shadow-lg backdrop-blur">
                {searchError && (
                  <li className="px-3 py-2 text-sm text-[var(--sf-muted)]">{searchError}</li>
                )}
                {results.map((r) => (
                  <li key={r.place_id}>
                    <button
                      type="button"
                      className="w-full px-3 py-2 text-left text-sm hover:bg-black/[0.04]"
                      onClick={() => goToResult(r)}
                    >
                      {r.display_name}
                    </button>
                  </li>
                ))}
              </ul>
            )}
          </form>

          <button
            type="button"
            className="pointer-events-auto shrink-0 rounded-xl border border-black/10 bg-white/95 px-3 py-2 text-sm font-medium shadow-sm backdrop-blur hover:bg-white"
            onClick={() => setExpanded((v) => !v)}
            aria-pressed={expanded}
          >
            {expanded ? 'Close map' : 'Expand map'}
          </button>
        </div>
      </div>
    </>
  )
}
