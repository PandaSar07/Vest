import { motion } from 'framer-motion'
import type { PortfolioSummary, Snapshot } from '@/types'
import type { ChartPoint } from '@/lib/chartData'
import { estimateDailyChange, filterChartPoints } from '@/lib/chartData'
import { fmtMoney } from '@/lib/format'
import { cn } from '@/lib/utils'
import { Card } from '@/components/Card'
import { Sparkline } from '@/components/Sparkline'

type PortfolioHeroProps = {
  summary: PortfolioSummary
  chartPoints: ChartPoint[]
  snapshots: Snapshot[]
}

function Stat({ label, value }: { label: string; value: string }) {
  return (
    <div className="rounded-xl border border-white/[0.06] bg-black/20 px-3 py-3 sm:px-4">
      <p className="text-[10px] font-semibold uppercase tracking-wider text-[var(--text-secondary,#94a3b8)]">{label}</p>
      <p className="mt-1 text-sm font-bold tabular-nums text-[var(--text-primary,#f1f5f9)]">{value}</p>
    </div>
  )
}

export function PortfolioHero({ summary, chartPoints, snapshots }: PortfolioHeroProps) {
  const sparkData = filterChartPoints(chartPoints, '1M')
  const upSpark =
    sparkData.length >= 2 ? sparkData[sparkData.length - 1].value >= sparkData[0].value : true

  const daily = estimateDailyChange(summary.totalValue, snapshots)
  const dailyUp = daily ? daily.abs >= 0 : true

  const totalCost = summary.holdings.reduce((s, h) => s + h.shares * h.avgCost, 0)
  const totalReturn = summary.stockValue - totalCost
  const retUp = totalReturn >= 0

  const formattedTotal = `$${fmtMoney(summary.totalValue)}`

  return (
    <Card>
      <div className="grid gap-8 lg:grid-cols-2 lg:items-center">
        <div>
          <p className="text-xs font-semibold uppercase tracking-[0.14em] text-[var(--text-secondary,#94a3b8)]">
            Portfolio value
          </p>
          <motion.p
            key={formattedTotal}
            initial={{ opacity: 0.65, scale: 0.992 }}
            animate={{ opacity: 1, scale: 1 }}
            transition={{ duration: 0.35, ease: [0.22, 1, 0.36, 1] }}
            className="mt-3 font-[family-name:var(--font-brand)] text-4xl font-extrabold tracking-tight tabular-nums sm:text-5xl"
          >
            {formattedTotal}
          </motion.p>

          {daily ? (
            <p
              className={cn(
                'mt-3 flex flex-wrap items-baseline gap-x-2 text-lg font-semibold tabular-nums',
                dailyUp ? 'text-[var(--success-color,#10b981)]' : 'text-[var(--danger-color,#ef4444)]',
              )}
            >
              <span>
                {dailyUp ? '+' : '-'}${fmtMoney(Math.abs(daily.abs))} ({dailyUp ? '+' : ''}
                {fmtMoney(daily.pct)}%)
              </span>
              <span className="text-sm font-normal text-[var(--text-secondary,#94a3b8)]">
                vs prior day snapshot
              </span>
            </p>
          ) : (
            <p className="mt-3 text-sm text-[var(--text-secondary,#94a3b8)]">
              Add a few daily visits to unlock snapshot-based daily moves.
            </p>
          )}

          <div className="mt-8 grid grid-cols-2 gap-3 lg:grid-cols-4">
            <Stat label="Cash" value={`$${fmtMoney(summary.cash)}`} />
            <Stat label="Invested" value={`$${fmtMoney(summary.stockValue)}`} />
            <Stat label="Positions" value={String(summary.holdings.length)} />
            <div className="rounded-xl border border-white/[0.06] bg-black/20 px-3 py-3 sm:px-4">
              <p className="text-[10px] font-semibold uppercase tracking-wider text-[var(--text-secondary,#94a3b8)]">
                Equity P/L
              </p>
              <p
                className={cn(
                  'mt-1 text-sm font-bold tabular-nums',
                  retUp ? 'text-[var(--success-color,#10b981)]' : 'text-[var(--danger-color,#ef4444)]',
                )}
              >
                {retUp ? '+' : '-'}${fmtMoney(Math.abs(totalReturn))}
              </p>
            </div>
          </div>
        </div>

        <div className="rounded-xl border border-white/[0.06] bg-gradient-to-b from-white/[0.04] to-transparent p-4">
          <p className="text-xs font-semibold uppercase tracking-[0.14em] text-[var(--text-secondary,#94a3b8)]">
            Trend (1M snapshots)
          </p>
          <div className="mt-3">
            <Sparkline data={sparkData} positive={upSpark} />
          </div>
        </div>
      </div>
    </Card>
  )
}
