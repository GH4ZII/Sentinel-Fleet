import { useEffect, useMemo, useRef, useState, type PointerEvent as ReactPointerEvent } from 'react'
import type { GraphEdge, GraphNode, IncidentGraph } from '../../lib/api'

type Props = {
  graph: IncidentGraph
  relationshipFilter: string
  onFilterChange: (value: string) => void
}

type LayoutNode = GraphNode & { x: number; y: number }

const TYPE_COLORS: Record<string, string> = {
  Incident: '#1f3a5f',
  Asset: '#0f766e',
  User: '#9a3412',
  Geofence: '#6d28d9',
  Detection: '#b45309',
}

export function IncidentRelationshipGraph({ graph, relationshipFilter, onFilterChange }: Props) {
  const svgRef = useRef<SVGSVGElement>(null)
  const [selectedId, setSelectedId] = useState<string | null>(null)
  const [dragId, setDragId] = useState<string | null>(null)
  const [positions, setPositions] = useState<Record<string, { x: number; y: number }>>({})
  const [scale, setScale] = useState(1)

  const filteredEdges = useMemo(() => {
    if (relationshipFilter === 'all') return graph.edges
    return graph.edges.filter((e) => e.relationshipType === relationshipFilter)
  }, [graph.edges, relationshipFilter])

  const visibleNodeIds = useMemo(() => {
    const ids = new Set<string>()
    for (const e of filteredEdges) {
      ids.add(e.sourceId)
      ids.add(e.targetId)
    }
    // Always keep incident node if present.
    const incident = graph.nodes.find((n) => n.entityType === 'Incident')
    if (incident) ids.add(incident.id)
    return ids
  }, [filteredEdges, graph.nodes])

  const nodes = useMemo(
    () => graph.nodes.filter((n) => visibleNodeIds.has(n.id)),
    [graph.nodes, visibleNodeIds],
  )

  useEffect(() => {
    // Initial radial layout by level.
    const width = 720
    const height = 420
    const cx = width / 2
    const cy = height / 2
    const next: Record<string, { x: number; y: number }> = {}
    const byLevel = new Map<number, GraphNode[]>()
    for (const n of graph.nodes) {
      const list = byLevel.get(n.level) ?? []
      list.push(n)
      byLevel.set(n.level, list)
    }

    for (const [level, list] of byLevel) {
      const radius = level === 0 ? 0 : 90 + level * 95
      list.forEach((n, i) => {
        if (level === 0) {
          next[n.id] = { x: cx, y: cy }
          return
        }
        const angle = (i / Math.max(list.length, 1)) * Math.PI * 2 - Math.PI / 2
        next[n.id] = {
          x: cx + Math.cos(angle) * radius,
          y: cy + Math.sin(angle) * radius,
        }
      })
    }
    setPositions(next)
    setSelectedId(null)
    setScale(1)
  }, [graph])

  const layoutNodes: LayoutNode[] = nodes.map((n) => ({
    ...n,
    x: positions[n.id]?.x ?? 360,
    y: positions[n.id]?.y ?? 210,
  }))

  const selected = layoutNodes.find((n) => n.id === selectedId) ?? null

  function onPointerDown(nodeId: string, e: ReactPointerEvent) {
    e.currentTarget.setPointerCapture(e.pointerId)
    setDragId(nodeId)
    setSelectedId(nodeId)
  }

  function onPointerMove(e: ReactPointerEvent) {
    if (!dragId || !svgRef.current) return
    const rect = svgRef.current.getBoundingClientRect()
    const x = (e.clientX - rect.left) / scale
    const y = (e.clientY - rect.top) / scale
    setPositions((prev) => ({ ...prev, [dragId]: { x, y } }))
  }

  function onPointerUp() {
    setDragId(null)
  }

  return (
    <div className="space-y-3">
      <div className="flex flex-wrap items-center gap-2">
        <label className="text-sm text-[var(--sf-muted)]">
          Relation
          <select
            className="ml-2 rounded-lg border border-black/10 bg-white px-2 py-1 text-[var(--sf-ink)]"
            value={relationshipFilter}
            onChange={(e) => onFilterChange(e.target.value)}
          >
            <option value="all">All</option>
            {graph.relationshipTypes.map((t) => (
              <option key={t} value={t}>
                {t}
              </option>
            ))}
          </select>
        </label>
        <button
          type="button"
          className="rounded-lg border border-black/10 px-2 py-1 text-sm"
          onClick={() => setScale((s) => Math.min(1.8, s + 0.15))}
        >
          Zoom in
        </button>
        <button
          type="button"
          className="rounded-lg border border-black/10 px-2 py-1 text-sm"
          onClick={() => setScale((s) => Math.max(0.6, s - 0.15))}
        >
          Zoom out
        </button>
      </div>

      <div className="overflow-hidden rounded-2xl border border-black/5 bg-[linear-gradient(180deg,#f7f4ef,#eef2f6)]">
        <svg
          ref={svgRef}
          viewBox="0 0 720 420"
          className="h-[420px] w-full touch-none"
          onPointerMove={onPointerMove}
          onPointerUp={onPointerUp}
          onPointerLeave={onPointerUp}
        >
          <g transform={`scale(${scale})`} style={{ transformOrigin: '360px 210px' }}>
            {filteredEdges.map((edge) => (
              <EdgeLine key={edge.id} edge={edge} positions={positions} />
            ))}
            {layoutNodes.map((node) => {
              const color = TYPE_COLORS[node.entityType] ?? '#334155'
              const active = selectedId === node.id
              return (
                <g
                  key={node.id}
                  transform={`translate(${node.x}, ${node.y})`}
                  style={{ cursor: 'grab' }}
                  onPointerDown={(e) => onPointerDown(node.id, e)}
                >
                  <circle
                    r={active ? 28 : 24}
                    fill={color}
                    opacity={0.92}
                    stroke={active ? '#111' : 'white'}
                    strokeWidth={active ? 3 : 2}
                  />
                  <text
                    textAnchor="middle"
                    y={4}
                    fill="white"
                    fontSize="10"
                    fontWeight="600"
                    style={{ pointerEvents: 'none' }}
                  >
                    {node.entityType.slice(0, 3).toUpperCase()}
                  </text>
                  <text
                    textAnchor="middle"
                    y={42}
                    fill="#1f2937"
                    fontSize="11"
                    fontWeight="600"
                    style={{ pointerEvents: 'none' }}
                  >
                    {truncate(node.label, 22)}
                  </text>
                </g>
              )
            })}
          </g>
        </svg>
      </div>

      {selected && (
        <div className="rounded-xl border border-black/5 bg-white/80 px-4 py-3 text-sm">
          <p className="font-medium">
            {selected.label}{' '}
            <span className="text-xs uppercase text-[var(--sf-muted)]">{selected.entityType}</span>
          </p>
          {selected.subtitle && (
            <p className="mt-1 text-[var(--sf-muted)]">{selected.subtitle}</p>
          )}
          <p className="mt-1 font-mono text-xs text-[var(--sf-muted)]">{selected.entityId}</p>
        </div>
      )}

      {nodes.length === 0 && (
        <p className="text-[var(--sf-muted)]">No graph nodes for this filter.</p>
      )}
    </div>
  )
}

function EdgeLine({
  edge,
  positions,
}: {
  edge: GraphEdge
  positions: Record<string, { x: number; y: number }>
}) {
  const a = positions[edge.sourceId]
  const b = positions[edge.targetId]
  if (!a || !b) return null
  const mx = (a.x + b.x) / 2
  const my = (a.y + b.y) / 2
  return (
    <g>
      <line
        x1={a.x}
        y1={a.y}
        x2={b.x}
        y2={b.y}
        stroke="#94a3b8"
        strokeWidth={1.5}
        opacity={0.8}
      />
      <text x={mx} y={my - 6} textAnchor="middle" fill="#64748b" fontSize="9">
        {edge.relationshipType}
      </text>
    </g>
  )
}

function truncate(value: string, max: number) {
  return value.length <= max ? value : `${value.slice(0, max - 1)}…`
}
