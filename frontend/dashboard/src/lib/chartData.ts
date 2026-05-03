import { parseApiDate } from '@/lib/format'
import type { PerfRange, Snapshot } from '@/types'

export type ChartPoint = {
  t: number
  date: Date
  value: number
  label: string
}

export function normalizeSnapshots(snaps: Snapshot[]): ChartPoint[] {
  const normalized = snaps
    .map((s) => ({
      value: Number(s.value),
      date: parseApiDate(s.snappedAt),
    }))
    .filter((x): x is { value: number; date: Date } => Number.isFinite(x.value) && x.date !== null)
    .sort((a, b) => a.date.getTime() - b.date.getTime())

  return normalized.map((s) => ({
    t: s.date.getTime(),
    date: s.date,
    value: s.value,
    label: s.date.toLocaleDateString('en-US', { month: 'short', day: 'numeric' }),
  }))
}

const RANGE_MS: Record<Exclude<PerfRange, 'ALL'>, number> = {
  '1D': 24 * 60 * 60 * 1000,
  '1W': 7 * 24 * 60 * 60 * 1000,
  '1M': 30 * 24 * 60 * 60 * 1000,
  '1Y': 365 * 24 * 60 * 60 * 1000,
}

/** Portfolio snapshots are coarse (often daily). We clamp ranges but keep at least two points when possible. */
export function filterChartPoints(points: ChartPoint[], range: PerfRange): ChartPoint[] {
  if (range === 'ALL' || points.length === 0) return points
  const cutoff = Date.now() - RANGE_MS[range]
  const filtered = points.filter((p) => p.t >= cutoff)
  if (filtered.length >= 2) return filtered
  const tail = points.slice(-Math.max(2, Math.min(12, points.length)))
  return tail.length >= 2 ? tail : points
}

export function estimateDailyChange(totalValue: number, snaps: Snapshot[]): { abs: number; pct: number } | null {
  if (!snaps.length) return null
  const sorted = [...snaps].sort(
    (a, b) => (parseApiDate(a.snappedAt)?.getTime() ?? 0) - (parseApiDate(b.snappedAt)?.getTime() ?? 0),
  )
  const today = new Date().toISOString().slice(0, 10)
  let prev: Snapshot | undefined
  for (let i = sorted.length - 1; i >= 0; i--) {
    const d = parseApiDate(sorted[i].snappedAt)
    const day = d ? d.toISOString().slice(0, 10) : sorted[i].snappedAt.slice(0, 10)
    if (day < today) {
      prev = sorted[i]
      break
    }
  }
  if (!prev && sorted.length >= 2) prev = sorted[sorted.length - 2]
  if (!prev) return null
  const abs = totalValue - Number(prev.value)
  const base = Number(prev.value)
  const pct = base !== 0 ? (abs / base) * 100 : 0
  return { abs, pct }
}
