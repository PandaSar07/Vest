import type { Holding } from '@/types'
import { displaySymbol, fmtMoney } from '@/lib/format'
import { cn } from '@/lib/utils'

function InsightCard({
  label,
  symbol,
  pct,
  positive,
}: {
  label: string
  symbol: string
  pct: number
  positive: boolean
}) {
  return (
    <div className="rounded-xl border border-white/[0.06] bg-black/15 px-4 py-3">
      <p className="text-[11px] font-semibold uppercase tracking-wider text-[var(--text-secondary,#94a3b8)]">{label}</p>
      <p className="mt-2 truncate text-sm font-bold text-[var(--text-primary,#f1f5f9)]">{symbol}</p>
      <p className={cn('mt-1 text-lg font-bold tabular-nums', positive ? 'text-emerald-400' : 'text-rose-400')}>
        {pct >= 0 ? '+' : ''}
        {fmtMoney(pct)}%
      </p>
      <p className="mt-1 text-xs text-[var(--text-secondary,#94a3b8)]">Based on position return vs cost basis.</p>
    </div>
  )
}

export function InsightsStrip({ holdings }: { holdings: Holding[] }) {
  if (holdings.length < 2) return null

  const sorted = [...holdings].sort((a, b) => b.gainLossPct - a.gainLossPct)
  const top = sorted[0]
  const bottom = sorted[sorted.length - 1]

  const bestToday = [...holdings].sort((a, b) => b.gainLoss - a.gainLoss)[0]

  return (
    <div className="grid gap-4 sm:grid-cols-3">
      <InsightCard label="Top gainer" symbol={displaySymbol(top.symbol)} pct={top.gainLossPct} positive />
      <InsightCard label="Top loser" symbol={displaySymbol(bottom.symbol)} pct={bottom.gainLossPct} positive={bottom.gainLossPct >= 0} />
      <div className="rounded-xl border border-white/[0.06] bg-black/15 px-4 py-3">
        <p className="text-[11px] font-semibold uppercase tracking-wider text-[var(--text-secondary,#94a3b8)]">
          Best performer ($ P/L)
        </p>
        <p className="mt-2 truncate text-sm font-bold text-[var(--text-primary,#f1f5f9)]">
          {displaySymbol(bestToday.symbol)}
        </p>
        <p
          className={cn(
            'mt-1 text-lg font-bold tabular-nums',
            bestToday.gainLoss >= 0 ? 'text-emerald-400' : 'text-rose-400',
          )}
        >
          {bestToday.gainLoss >= 0 ? '+' : '-'}${fmtMoney(Math.abs(bestToday.gainLoss))}
        </p>
        <p className="mt-1 text-xs text-[var(--text-secondary,#94a3b8)]">Largest dollar move in your book.</p>
      </div>
    </div>
  )
}
