import { lazy, Suspense, useMemo, useState, type FormEvent } from 'react'
import { Link, useParams } from 'react-router-dom'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import {
  addIncidentComment,
  getAccessToken,
  getIncident,
  getIncidentGraph,
  getIncidentPositions,
  incidentAttachmentUrl,
  listAssets,
  resolveIncident,
  updateIncident,
  uploadIncidentAttachment,
} from '../../lib/api'
import { ErrorBoundary } from '../../components/ErrorBoundary'
import { IncidentAnalysisPanel } from './IncidentAnalysisPanel'
import { IncidentRelationshipGraph } from './IncidentRelationshipGraph'

const IncidentPlaybackMap = lazy(async () => {
  const mod = await import('./IncidentPlaybackMap')
  return { default: mod.IncidentPlaybackMap }
})

type TabId =
  | 'overview'
  | 'timeline'
  | 'playback'
  | 'relationships'
  | 'analysis'
  | 'attachments'
  | 'audit'

const tabs: { id: TabId; label: string }[] = [
  { id: 'overview', label: 'Overview' },
  { id: 'timeline', label: 'Timeline' },
  { id: 'playback', label: 'Map playback' },
  { id: 'relationships', label: 'Relationships' },
  { id: 'analysis', label: 'AI analysis' },
  { id: 'attachments', label: 'Attachments' },
  { id: 'audit', label: 'Audit' },
]

export function IncidentDetailPage() {
  const { incidentId = '' } = useParams()
  const queryClient = useQueryClient()
  const [tab, setTab] = useState<TabId>('overview')
  const [timelineFilter, setTimelineFilter] = useState<string>('all')
  const [comment, setComment] = useState('')
  const [relationFilter, setRelationFilter] = useState('all')
  const [graphLevels, setGraphLevels] = useState(2)

  const detailQuery = useQuery({
    queryKey: ['incident', incidentId],
    queryFn: () => getIncident(incidentId),
    enabled: Boolean(incidentId),
    refetchInterval: 15_000,
  })

  const positionsQuery = useQuery({
    queryKey: ['incident-positions', incidentId],
    queryFn: () => getIncidentPositions(incidentId),
    enabled: Boolean(incidentId) && tab === 'playback',
  })

  const graphQuery = useQuery({
    queryKey: ['incident-graph', incidentId, graphLevels],
    queryFn: () => getIncidentGraph(incidentId, { maxLevels: graphLevels }),
    enabled: Boolean(incidentId) && tab === 'relationships',
  })

  const assetsQuery = useQuery({ queryKey: ['assets'], queryFn: listAssets })

  const invalidate = () => {
    void queryClient.invalidateQueries({ queryKey: ['incident', incidentId] })
    void queryClient.invalidateQueries({ queryKey: ['incidents'] })
  }

  const resolveMutation = useMutation({
    mutationFn: () => resolveIncident(incidentId),
    onSuccess: invalidate,
  })

  const dismissMutation = useMutation({
    mutationFn: () => updateIncident(incidentId, { status: 'Dismissed' }),
    onSuccess: invalidate,
  })

  const investigateMutation = useMutation({
    mutationFn: () => updateIncident(incidentId, { status: 'Investigating' }),
    onSuccess: invalidate,
  })

  const commentMutation = useMutation({
    mutationFn: (content: string) => addIncidentComment(incidentId, content),
    onSuccess: () => {
      setComment('')
      invalidate()
    },
  })

  const uploadMutation = useMutation({
    mutationFn: (file: File) => uploadIncidentAttachment(incidentId, file),
    onSuccess: invalidate,
  })

  const detail = detailQuery.data
  const incident = detail?.incident
  const assetName =
    assetsQuery.data?.find((a) => a.id === incident?.primaryAssetId)?.name ??
    incident?.primaryAssetId?.slice(0, 8)

  const factors = useMemo(() => {
    const raw = incident?.latestRisk?.factors
    if (!raw) return [] as { code: string; label: string; points: number; explanation: string }[]
    try {
      const parsed = JSON.parse(raw) as unknown
      return Array.isArray(parsed) ? parsed : []
    } catch {
      return []
    }
  }, [incident?.latestRisk?.factors])

  const timeline = useMemo(() => {
    const entries = detail?.timeline ?? []
    if (timelineFilter === 'all') return entries
    return entries.filter((e) => e.entryType === timelineFilter)
  }, [detail?.timeline, timelineFilter])

  async function onComment(e: FormEvent) {
    e.preventDefault()
    if (!comment.trim()) return
    await commentMutation.mutateAsync(comment.trim())
  }

  if (detailQuery.isLoading) {
    return <p className="text-[var(--sf-muted)]">Loading incident…</p>
  }

  if (detailQuery.isError || !detail || !incident) {
    return (
      <p className="text-[var(--sf-danger)]">
        {detailQuery.error instanceof Error ? detailQuery.error.message : 'Incident not found'}
      </p>
    )
  }

  return (
    <div className="space-y-6">
      <div className="flex flex-wrap items-start justify-between gap-4">
        <div>
          <p className="text-sm text-[var(--sf-muted)]">
            <Link to="/incidents" className="hover:underline">
              Incidents
            </Link>{' '}
            / {incident.incidentType}
          </p>
          <h1 className="mt-1 text-3xl font-semibold tracking-tight">{incident.title}</h1>
          <p className="mt-2 text-[var(--sf-muted)]">
            {assetName} · {incident.status} · {incident.severity} · risk {incident.riskScore}
          </p>
        </div>
        <div className="flex flex-wrap gap-2">
          {incident.status === 'Open' && (
            <button
              type="button"
              className="rounded-lg border border-black/10 px-3 py-1.5 text-sm"
              onClick={() => investigateMutation.mutate()}
            >
              Start investigation
            </button>
          )}
          {incident.status !== 'Resolved' && (
            <button
              type="button"
              className="rounded-lg bg-[var(--sf-accent)] px-3 py-1.5 text-sm font-medium text-white"
              onClick={() => resolveMutation.mutate()}
            >
              Resolve
            </button>
          )}
          {incident.status !== 'Dismissed' && incident.status !== 'Resolved' && (
            <button
              type="button"
              className="rounded-lg border border-black/10 px-3 py-1.5 text-sm"
              onClick={() => dismissMutation.mutate()}
            >
              Dismiss
            </button>
          )}
        </div>
      </div>

      <div className="flex flex-wrap gap-2 border-b border-black/5 pb-2">
        {tabs.map((t) => (
          <button
            key={t.id}
            type="button"
            onClick={() => setTab(t.id)}
            className={`rounded-lg px-3 py-1.5 text-sm ${
              tab === t.id
                ? 'bg-[var(--sf-ink)] text-white'
                : 'text-[var(--sf-muted)] hover:text-[var(--sf-ink)]'
            }`}
          >
            {t.label}
          </button>
        ))}
      </div>

      {tab === 'overview' && (
        <div className="grid gap-6 lg:grid-cols-2">
          <section className="space-y-3 rounded-2xl border border-black/5 bg-white/70 p-5">
            <h2 className="text-lg font-semibold">Summary</h2>
            <p className="text-sm text-[var(--sf-muted)]">
              {incident.description ?? 'No description'}
            </p>
            <dl className="grid grid-cols-2 gap-3 text-sm">
              <div>
                <dt className="text-[var(--sf-muted)]">Started</dt>
                <dd>{new Date(incident.startedAt).toLocaleString()}</dd>
              </div>
              <div>
                <dt className="text-[var(--sf-muted)]">Detected</dt>
                <dd>{new Date(incident.detectedAt).toLocaleString()}</dd>
              </div>
              <div>
                <dt className="text-[var(--sf-muted)]">Ended</dt>
                <dd>{incident.endedAt ? new Date(incident.endedAt).toLocaleString() : '—'}</dd>
              </div>
              <div>
                <dt className="text-[var(--sf-muted)]">Confidence</dt>
                <dd>{(incident.confidence * 100).toFixed(0)}%</dd>
              </div>
            </dl>
          </section>

          <section className="space-y-3 rounded-2xl border border-black/5 bg-white/70 p-5">
            <h2 className="text-lg font-semibold">Risk score {incident.riskScore}</h2>
            <p className="text-sm text-[var(--sf-muted)]">
              Level: {incident.latestRisk?.riskLevel ?? '—'} · model{' '}
              {incident.latestRisk?.modelVersion ?? 'n/a'}
            </p>
            <ul className="space-y-2 text-sm">
              {factors.map((f) => (
                <li key={`${f.code}-${f.label}`} className="flex justify-between gap-3">
                  <span>
                    {f.label}
                    <span className="mt-0.5 block text-xs text-[var(--sf-muted)]">
                      {f.explanation}
                    </span>
                  </span>
                  <span className="font-medium">+{f.points}</span>
                </li>
              ))}
              {factors.length === 0 && (
                <li className="text-[var(--sf-muted)]">No risk factors recorded yet.</li>
              )}
            </ul>
          </section>

          <section className="space-y-3 rounded-2xl border border-black/5 bg-white/70 p-5 lg:col-span-2">
            <h2 className="text-lg font-semibold">Detections</h2>
            <div className="overflow-hidden rounded-xl border border-black/5">
              <table className="w-full text-left text-sm">
                <thead className="border-b border-black/5 text-xs uppercase text-[var(--sf-muted)]">
                  <tr>
                    <th className="px-3 py-2">Time</th>
                    <th className="px-3 py-2">Title</th>
                    <th className="px-3 py-2">Type</th>
                    <th className="px-3 py-2">Severity</th>
                    <th className="px-3 py-2">Contribution</th>
                  </tr>
                </thead>
                <tbody>
                  {detail.detections.map((d) => (
                    <tr key={d.id} className="border-t border-black/5">
                      <td className="px-3 py-2 text-[var(--sf-muted)]">
                        {new Date(d.triggeredAt).toLocaleString()}
                      </td>
                      <td className="px-3 py-2">{d.title}</td>
                      <td className="px-3 py-2">{d.detectionType}</td>
                      <td className="px-3 py-2">{d.severity}</td>
                      <td className="px-3 py-2">+{d.riskContribution}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </section>

          <section className="space-y-3 rounded-2xl border border-black/5 bg-white/70 p-5 lg:col-span-2">
            <h2 className="text-lg font-semibold">Add comment</h2>
            <form onSubmit={onComment} className="flex flex-col gap-3 sm:flex-row">
              <input
                value={comment}
                onChange={(e) => setComment(e.target.value)}
                placeholder="What did you observe?"
                className="flex-1 rounded-lg border border-black/10 bg-white px-3 py-2 text-sm"
              />
              <button
                type="submit"
                className="rounded-lg bg-[var(--sf-accent)] px-4 py-2 text-sm font-medium text-white"
                disabled={commentMutation.isPending}
              >
                Post
              </button>
            </form>
            <ul className="space-y-2 text-sm">
              {detail.comments.map((c) => (
                <li key={c.id} className="rounded-lg bg-black/[0.02] px-3 py-2">
                  <p>{c.content}</p>
                  <p className="mt-1 text-xs text-[var(--sf-muted)]">
                    {new Date(c.createdAt).toLocaleString()}
                  </p>
                </li>
              ))}
            </ul>
          </section>
        </div>
      )}

      {tab === 'timeline' && (
        <div className="space-y-4">
          <div className="flex flex-wrap gap-2">
            {['all', 'Detection', 'Comment', 'System', 'Risk', 'Attachment'].map((f) => (
              <button
                key={f}
                type="button"
                onClick={() => setTimelineFilter(f)}
                className={`rounded-lg px-3 py-1 text-sm ${
                  timelineFilter === f
                    ? 'bg-[var(--sf-ink)] text-white'
                    : 'border border-black/10 text-[var(--sf-muted)]'
                }`}
              >
                {f}
              </button>
            ))}
          </div>
          <ol className="space-y-3">
            {timeline.map((entry) => (
              <li
                key={entry.id}
                className="rounded-2xl border border-black/5 bg-white/70 px-4 py-3"
              >
                <div className="flex flex-wrap items-baseline justify-between gap-2">
                  <p className="font-medium">
                    <span className="mr-2 text-xs uppercase tracking-wide text-[var(--sf-muted)]">
                      {entry.entryType}
                    </span>
                    {entry.title}
                  </p>
                  <time className="text-xs text-[var(--sf-muted)]">
                    {new Date(entry.timestamp).toLocaleString()}
                  </time>
                </div>
                {entry.description && (
                  <p className="mt-1 text-sm text-[var(--sf-muted)]">{entry.description}</p>
                )}
              </li>
            ))}
            {timeline.length === 0 && (
              <p className="text-[var(--sf-muted)]">No timeline entries for this filter.</p>
            )}
          </ol>
        </div>
      )}

      {tab === 'playback' && (
        <div className="space-y-3">
          {positionsQuery.isLoading && (
            <p className="text-[var(--sf-muted)]">Loading positions…</p>
          )}
          {positionsQuery.isError && (
            <p className="text-[var(--sf-danger)]">
              {positionsQuery.error instanceof Error
                ? positionsQuery.error.message
                : 'Failed to load positions'}
            </p>
          )}
          {positionsQuery.data && (
            <ErrorBoundary fallbackTitle="Map playback failed">
              <Suspense fallback={<p className="text-[var(--sf-muted)]">Loading map…</p>}>
                <IncidentPlaybackMap
                  key={incidentId}
                  positions={positionsQuery.data}
                  timeline={detail.timeline}
                />
              </Suspense>
            </ErrorBoundary>
          )}
        </div>
      )}

      {tab === 'relationships' && (
        <div className="space-y-4">
          <div className="flex flex-wrap items-center gap-2">
            <label className="text-sm text-[var(--sf-muted)]">
              Levels
              <select
                className="ml-2 rounded-lg border border-black/10 bg-white px-2 py-1"
                value={graphLevels}
                onChange={(e) => setGraphLevels(Number(e.target.value))}
              >
                <option value={1}>1</option>
                <option value={2}>2</option>
                <option value={3}>3</option>
              </select>
            </label>
          </div>
          {graphQuery.isLoading && <p className="text-[var(--sf-muted)]">Loading graph…</p>}
          {graphQuery.isError && (
            <p className="text-[var(--sf-danger)]">
              {graphQuery.error instanceof Error
                ? graphQuery.error.message
                : 'Failed to load relationship graph'}
            </p>
          )}
          {graphQuery.data && (
            <IncidentRelationshipGraph
              graph={graphQuery.data}
              relationshipFilter={relationFilter}
              onFilterChange={setRelationFilter}
            />
          )}

          <div className="overflow-hidden rounded-2xl border border-black/5 bg-white/70">
            <table className="w-full text-left text-sm">
              <thead className="border-b border-black/5 text-xs uppercase text-[var(--sf-muted)]">
                <tr>
                  <th className="px-4 py-3">Type</th>
                  <th className="px-4 py-3">Entity</th>
                  <th className="px-4 py-3">Relationship</th>
                  <th className="px-4 py-3">First seen</th>
                  <th className="px-4 py-3">Last seen</th>
                </tr>
              </thead>
              <tbody>
                {detail.relationships.map((r) => (
                  <tr key={r.id} className="border-t border-black/5">
                    <td className="px-4 py-3">{r.entityType}</td>
                    <td className="px-4 py-3 font-mono text-xs">{r.entityId}</td>
                    <td className="px-4 py-3">{r.relationshipType}</td>
                    <td className="px-4 py-3 text-[var(--sf-muted)]">
                      {new Date(r.firstObservedAt).toLocaleString()}
                    </td>
                    <td className="px-4 py-3 text-[var(--sf-muted)]">
                      {new Date(r.lastObservedAt).toLocaleString()}
                    </td>
                  </tr>
                ))}
                {detail.relationships.length === 0 && (
                  <tr>
                    <td colSpan={5} className="px-4 py-6 text-[var(--sf-muted)]">
                      No related entities yet.
                    </td>
                  </tr>
                )}
              </tbody>
            </table>
          </div>
        </div>
      )}

      {tab === 'analysis' && <IncidentAnalysisPanel incidentId={incidentId} />}

      {tab === 'attachments' && (
        <div className="space-y-4">
          <label className="inline-flex cursor-pointer items-center gap-2 rounded-lg border border-black/10 px-3 py-2 text-sm">
            Upload file
            <input
              type="file"
              className="hidden"
              onChange={(e) => {
                const file = e.target.files?.[0]
                if (file) uploadMutation.mutate(file)
                e.target.value = ''
              }}
            />
          </label>
          <ul className="space-y-2">
            {detail.attachments.map((a) => (
              <li
                key={a.id}
                className="flex items-center justify-between rounded-xl border border-black/5 bg-white/70 px-4 py-3 text-sm"
              >
                <div>
                  <p className="font-medium">{a.name}</p>
                  <p className="text-xs text-[var(--sf-muted)]">
                    {(a.size / 1024).toFixed(1)} KB · {new Date(a.createdAt).toLocaleString()}
                  </p>
                </div>
                <a
                  className="text-[var(--sf-accent)] hover:underline"
                  href={incidentAttachmentUrl(incident.id, a.id)}
                  onClick={(e) => {
                    e.preventDefault()
                    const token = getAccessToken()
                    void fetch(incidentAttachmentUrl(incident.id, a.id), {
                      headers: token ? { Authorization: `Bearer ${token}` } : {},
                    })
                      .then(async (res) => {
                        const blob = await res.blob()
                        const url = URL.createObjectURL(blob)
                        const link = document.createElement('a')
                        link.href = url
                        link.download = a.name
                        link.click()
                        URL.revokeObjectURL(url)
                      })
                      .catch(console.error)
                  }}
                >
                  Download
                </a>
              </li>
            ))}
            {detail.attachments.length === 0 && (
              <p className="text-[var(--sf-muted)]">No attachments uploaded.</p>
            )}
          </ul>
        </div>
      )}

      {tab === 'audit' && (
        <div className="overflow-hidden rounded-2xl border border-black/5 bg-white/70">
          <table className="w-full text-left text-sm">
            <thead className="border-b border-black/5 text-xs uppercase text-[var(--sf-muted)]">
              <tr>
                <th className="px-4 py-3">When</th>
                <th className="px-4 py-3">Action</th>
                <th className="px-4 py-3">User</th>
                <th className="px-4 py-3">Details</th>
              </tr>
            </thead>
            <tbody>
              {detail.audit.map((a) => (
                <tr key={a.id} className="border-t border-black/5 align-top">
                  <td className="px-4 py-3 text-[var(--sf-muted)]">
                    {new Date(a.createdAt).toLocaleString()}
                  </td>
                  <td className="px-4 py-3">{a.action}</td>
                  <td className="px-4 py-3 font-mono text-xs">{a.userId?.slice(0, 8) ?? '—'}</td>
                  <td className="px-4 py-3 text-xs text-[var(--sf-muted)]">
                    {a.newValues ?? a.oldValues ?? '—'}
                  </td>
                </tr>
              ))}
              {detail.audit.length === 0 && (
                <tr>
                  <td colSpan={4} className="px-4 py-6 text-[var(--sf-muted)]">
                    No audit entries yet.
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
      )}
    </div>
  )
}
