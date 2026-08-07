import { useState, type ReactNode } from 'react'
import { useMutation } from '@tanstack/react-query'
import {
  analyzeIncidentSummary,
  explainIncidentRisk,
  generateIncidentReport,
  getIncidentMissingData,
  getSimilarIncidents,
  type Citation,
  type IncidentReport,
  type IncidentSummaryAnalysis,
  type MissingDataAnalysis,
  type RiskExplanation,
  type SimilarIncidentsAnalysis,
} from '../../lib/api'

type Props = {
  incidentId: string
}

type Panel = 'summary' | 'risk' | 'gaps' | 'similar' | 'report'

export function IncidentAnalysisPanel({ incidentId }: Props) {
  const [panel, setPanel] = useState<Panel>('summary')
  const [summary, setSummary] = useState<IncidentSummaryAnalysis | null>(null)
  const [risk, setRisk] = useState<RiskExplanation | null>(null)
  const [gaps, setGaps] = useState<MissingDataAnalysis | null>(null)
  const [similar, setSimilar] = useState<SimilarIncidentsAnalysis | null>(null)
  const [report, setReport] = useState<IncidentReport | null>(null)

  const summaryMutation = useMutation({
    mutationFn: () => analyzeIncidentSummary(incidentId),
    onSuccess: (data) => {
      setSummary(data)
      setPanel('summary')
    },
  })

  const riskMutation = useMutation({
    mutationFn: () => explainIncidentRisk(incidentId),
    onSuccess: (data) => {
      setRisk(data)
      setPanel('risk')
    },
  })

  const gapsMutation = useMutation({
    mutationFn: () => getIncidentMissingData(incidentId),
    onSuccess: (data) => {
      setGaps(data)
      setPanel('gaps')
    },
  })

  const similarMutation = useMutation({
    mutationFn: () => getSimilarIncidents(incidentId),
    onSuccess: (data) => {
      setSimilar(data)
      setPanel('similar')
    },
  })

  const reportMutation = useMutation({
    mutationFn: () => generateIncidentReport(incidentId),
    onSuccess: (data) => {
      setReport(data)
      setSummary(data.analysis)
      setRisk(data.risk)
      setGaps(data.gaps)
      setSimilar({
        incidents: data.similarIncidents,
        citations: data.allCitations,
        analystVersion: data.analystVersion,
      })
      setPanel('report')
    },
  })

  const busy =
    summaryMutation.isPending ||
    riskMutation.isPending ||
    gapsMutation.isPending ||
    similarMutation.isPending ||
    reportMutation.isPending

  const error =
    summaryMutation.error ??
    riskMutation.error ??
    gapsMutation.error ??
    similarMutation.error ??
    reportMutation.error

  return (
    <div className="space-y-4">
      <div className="rounded-2xl border border-black/5 bg-white/70 p-5">
        <h2 className="text-lg font-semibold">Incident analyst</h2>
        <p className="mt-1 text-sm text-[var(--sf-muted)]">
          Controlled tools only — every claim is tied to system sources. Suspicions and assumptions
          are labelled separately from facts.
        </p>
        <div className="mt-4 flex flex-wrap gap-2">
          <ActionButton disabled={busy} onClick={() => summaryMutation.mutate()}>
            Summarize
          </ActionButton>
          <ActionButton disabled={busy} onClick={() => riskMutation.mutate()}>
            Explain risk
          </ActionButton>
          <ActionButton disabled={busy} onClick={() => gapsMutation.mutate()}>
            Missing data
          </ActionButton>
          <ActionButton disabled={busy} onClick={() => similarMutation.mutate()}>
            Similar incidents
          </ActionButton>
          <ActionButton
            disabled={busy}
            primary
            onClick={() => reportMutation.mutate()}
          >
            Generate report
          </ActionButton>
        </div>
        {busy && <p className="mt-3 text-sm text-[var(--sf-muted)]">Running analyst tools…</p>}
        {error && (
          <p className="mt-3 text-sm text-[var(--sf-danger)]">
            {error instanceof Error ? error.message : 'Analysis failed'}
          </p>
        )}
      </div>

      {(summary || risk || gaps || similar || report) && (
        <div className="flex flex-wrap gap-2">
          {(
            [
              ['summary', 'Summary', summary],
              ['risk', 'Risk', risk],
              ['gaps', 'Gaps', gaps],
              ['similar', 'Similar', similar],
              ['report', 'Report', report],
            ] as const
          ).map(([id, label, data]) =>
            data ? (
              <button
                key={id}
                type="button"
                onClick={() => setPanel(id)}
                className={`rounded-lg px-3 py-1.5 text-sm ${
                  panel === id
                    ? 'bg-[var(--sf-ink)] text-white'
                    : 'border border-black/10 text-[var(--sf-muted)]'
                }`}
              >
                {label}
              </button>
            ) : null,
          )}
        </div>
      )}

      {panel === 'summary' && summary && <SummaryView data={summary} />}
      {panel === 'risk' && risk && <RiskView data={risk} />}
      {panel === 'gaps' && gaps && <GapsView data={gaps} />}
      {panel === 'similar' && similar && <SimilarView data={similar} />}
      {panel === 'report' && report && <ReportView data={report} />}
    </div>
  )
}

function ActionButton({
  children,
  onClick,
  disabled,
  primary,
}: {
  children: ReactNode
  onClick: () => void
  disabled?: boolean
  primary?: boolean
}) {
  return (
    <button
      type="button"
      disabled={disabled}
      onClick={onClick}
      className={`rounded-lg px-3 py-1.5 text-sm disabled:opacity-50 ${
        primary
          ? 'bg-[var(--sf-accent)] font-medium text-white'
          : 'border border-black/10'
      }`}
    >
      {children}
    </button>
  )
}

function SummaryView({ data }: { data: IncidentSummaryAnalysis }) {
  return (
    <div className="space-y-4">
      <section className="rounded-2xl border border-black/5 bg-white/70 p-5">
        <h3 className="font-semibold">Summary</h3>
        <p className="mt-2 text-sm leading-relaxed">{data.summary}</p>
        <p className="mt-2 text-xs text-[var(--sf-muted)]">Analyst {data.analystVersion}</p>
      </section>
      <StatementGroup title="Facts" items={data.facts} tone="fact" />
      <StatementGroup title="Suspicions" items={data.suspicions} tone="suspicion" />
      <StatementGroup title="Assumptions" items={data.assumptions} tone="assumption" />
      <CitationList citations={data.citations} />
    </div>
  )
}

function RiskView({ data }: { data: RiskExplanation }) {
  return (
    <div className="space-y-4">
      <section className="rounded-2xl border border-black/5 bg-white/70 p-5">
        <h3 className="font-semibold">
          Risk {data.riskScore}/100 · {data.riskLevel}
        </h3>
        <p className="mt-2 text-sm leading-relaxed">{data.explanation}</p>
      </section>
      <StatementGroup title="Contributing factors" items={data.factors} tone="fact" />
      <CitationList citations={data.citations} />
    </div>
  )
}

function GapsView({ data }: { data: MissingDataAnalysis }) {
  return (
    <div className="grid gap-4 lg:grid-cols-2">
      <section className="rounded-2xl border border-black/5 bg-white/70 p-5">
        <h3 className="font-semibold">Missing data</h3>
        <ul className="mt-2 list-disc space-y-1 pl-5 text-sm">
          {data.missingData.map((m) => (
            <li key={m}>{m}</li>
          ))}
        </ul>
      </section>
      <section className="rounded-2xl border border-black/5 bg-white/70 p-5">
        <h3 className="font-semibold">Suggested actions</h3>
        <ul className="mt-2 list-disc space-y-1 pl-5 text-sm">
          {data.suggestedActions.map((m) => (
            <li key={m}>{m}</li>
          ))}
        </ul>
      </section>
    </div>
  )
}

function SimilarView({ data }: { data: SimilarIncidentsAnalysis }) {
  return (
    <div className="overflow-hidden rounded-2xl border border-black/5 bg-white/70">
      <table className="w-full text-left text-sm">
        <thead className="border-b border-black/5 text-xs uppercase text-[var(--sf-muted)]">
          <tr>
            <th className="px-4 py-3">Title</th>
            <th className="px-4 py-3">Type</th>
            <th className="px-4 py-3">Risk</th>
            <th className="px-4 py-3">Similarity</th>
            <th className="px-4 py-3">Reason</th>
          </tr>
        </thead>
        <tbody>
          {data.incidents.map((i) => (
            <tr key={i.incidentId} className="border-t border-black/5">
              <td className="px-4 py-3">
                <a className="text-[var(--sf-accent)] hover:underline" href={`/incidents/${i.incidentId}`}>
                  {i.title}
                </a>
              </td>
              <td className="px-4 py-3">{i.incidentType}</td>
              <td className="px-4 py-3">{i.riskScore}</td>
              <td className="px-4 py-3">{i.similarity.toFixed(2)}</td>
              <td className="px-4 py-3 text-[var(--sf-muted)]">{i.reason}</td>
            </tr>
          ))}
          {data.incidents.length === 0 && (
            <tr>
              <td colSpan={5} className="px-4 py-6 text-[var(--sf-muted)]">
                No similar incidents found.
              </td>
            </tr>
          )}
        </tbody>
      </table>
    </div>
  )
}

function ReportView({ data }: { data: IncidentReport }) {
  return (
    <div className="space-y-4">
      <section className="rounded-2xl border border-black/5 bg-white/70 p-5">
        <h3 className="text-xl font-semibold">{data.title}</h3>
        <p className="mt-1 text-xs text-[var(--sf-muted)]">
          Generated {new Date(data.generatedAt).toLocaleString()} · {data.analystVersion}
        </p>
        <pre className="mt-4 whitespace-pre-wrap font-sans text-sm leading-relaxed">
          {data.narrative}
        </pre>
      </section>
      <CitationList citations={data.allCitations} />
    </div>
  )
}

function StatementGroup({
  title,
  items,
  tone,
}: {
  title: string
  items: { text: string; citations: Citation[] }[]
  tone: 'fact' | 'suspicion' | 'assumption'
}) {
  if (items.length === 0) return null
  const badge =
    tone === 'fact'
      ? 'bg-emerald-100 text-emerald-900'
      : tone === 'suspicion'
        ? 'bg-amber-100 text-amber-900'
        : 'bg-slate-100 text-slate-800'

  return (
    <section className="rounded-2xl border border-black/5 bg-white/70 p-5">
      <h3 className="font-semibold">{title}</h3>
      <ul className="mt-3 space-y-3">
        {items.map((item) => (
          <li key={item.text} className="text-sm">
            <span className={`mr-2 rounded px-1.5 py-0.5 text-[10px] uppercase ${badge}`}>
              {tone}
            </span>
            {item.text}
            {item.citations[0] && (
              <span className="mt-1 block text-xs text-[var(--sf-muted)]">
                Source: {item.citations[0].sourceType} {item.citations[0].sourceId}
              </span>
            )}
          </li>
        ))}
      </ul>
    </section>
  )
}

function CitationList({ citations }: { citations: Citation[] }) {
  if (citations.length === 0) return null
  return (
    <section className="rounded-2xl border border-black/5 bg-white/70 p-5">
      <h3 className="font-semibold">Citations</h3>
      <ul className="mt-3 space-y-2 text-sm">
        {citations.map((c) => (
          <li key={`${c.sourceType}-${c.sourceId}-${c.claim}`} className="border-t border-black/5 pt-2 first:border-0 first:pt-0">
            <p>{c.claim}</p>
            <p className="text-xs text-[var(--sf-muted)]">
              {c.sourceType} {c.sourceId}
              {c.detail ? ` · ${c.detail}` : ''}
            </p>
          </li>
        ))}
      </ul>
    </section>
  )
}
