import { motion } from 'framer-motion'
import {
  Area,
  AreaChart,
  CartesianGrid,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from 'recharts'
import type { ChartPoint } from '@/lib/chartData'
import { filterChartPoints } from '@/lib/chartData'
import { fmtMoney, currencySymbol } from '@/lib/format'
import { cn } from '@/lib/utils'
import type { PerfRange } from '@/types'

const FILTERS: PerfRange[] = ['1D', '1W', '1M', '1Y', 'ALL']

type PerformanceChartProps = {
  points: ChartPoint[]
  range: PerfRange
  onRangeChange: (r: PerfRange) => void
}

export function PerformanceChart({ points, range, onRangeChange }: PerformanceChartProps) {
  const data = filterChartPoints(points, range)
  const up =
    data.length >= 2 ? data[data.length - 1].value >= data[0].value : true
  const stroke = up ? 'var(--success-color,#10b981)' : 'var(--danger-color,#ef4444)'
  const fillId = up ? 'perfPos' : 'perfNeg'

  return (
    <div>
      <div className="mb-5 flex flex-wrap items-center justify-between gap-3">
        <div>
          <h2 className="font-[family-name:var(--font-brand)] text-xs font-semibold uppercase tracking-[0.14em] text-[var(--text-secondary,#94a3b8)]">
            Portfolio performance
          </h2>
          <p className="mt-1 text-sm text-[var(--text-secondary,#94a3b8)]">
            Smooth historical curve from your snapshots.
          </p>
        </div>
        <div className="flex flex-wrap gap-1 rounded-xl border border-white/10 bg-black/20 p-1">
          {FILTERS.map((r) => (
            <button
              key={r}
              type="button"
              onClick={() => onRangeChange(r)}
              className={cn(
                'rounded-lg px-3 py-1.5 text-xs font-semibold transition-colors',
                range === r
                  ? 'bg-[var(--accent-color,#00c2ff)] text-[#0a0e1a]'
                  : 'text-[var(--text-secondary,#94a3b8)] hover:bg-white/5 hover:text-[var(--text-primary,#f1f5f9)]',
              )}
            >
              {r}
            </button>
          ))}
        </div>
      </div>

      {data.length < 2 ? (
        <p className="rounded-xl border border-dashed border-white/10 bg-white/[0.02] py-12 text-center text-sm text-[var(--text-secondary,#94a3b8)]">
          Visit this page over time to unlock your performance chart.
        </p>
      ) : (
        <motion.div
          key={range}
          initial={{ opacity: 0.85 }}
          animate={{ opacity: 1 }}
          transition={{ duration: 0.35 }}
          className="h-[300px] w-full sm:h-[340px]"
        >
          <ResponsiveContainer width="100%" height="100%">
            <AreaChart data={data} margin={{ top: 8, right: 12, left: 4, bottom: 4 }}>
              <defs>
                <linearGradient id={fillId} x1="0" y1="0" x2="0" y2="1">
                  <stop offset="0%" stopColor={stroke} stopOpacity={0.35} />
                  <stop offset="100%" stopColor={stroke} stopOpacity={0} />
                </linearGradient>
              </defs>
              <CartesianGrid vertical={false} stroke="rgba(148,163,184,0.08)" />
              <XAxis
                dataKey="label"
                tick={{ fill: '#64748b', fontSize: 11 }}
                tickLine={false}
                axisLine={{ stroke: 'rgba(148,163,184,0.12)' }}
                interval="preserveStartEnd"
                minTickGap={28}
              />
              <YAxis
                tick={{ fill: '#64748b', fontSize: 11 }}
                tickLine={false}
                axisLine={false}
                tickFormatter={(v) => `${currencySymbol}${fmtMoney(Number(v), 0)}`}
                width={56}
              />
              <Tooltip
                cursor={{ stroke: 'rgba(148,163,184,0.25)', strokeWidth: 1 }}
                contentStyle={{
                  background: 'rgba(10,14,26,0.96)',
                  border: '1px solid rgba(255,255,255,0.08)',
                  borderRadius: '12px',
                  fontSize: '12px',
                }}
                labelStyle={{ color: '#94a3b8' }}
                formatter={(v: number) => [`${currencySymbol}${fmtMoney(v)}`, 'Portfolio']}
              />
              <Area
                type="monotone"
                dataKey="value"
                stroke={stroke}
                strokeWidth={2.25}
                fill={`url(#${fillId})`}
                dot={false}
                activeDot={{ r: 4, strokeWidth: 0 }}
                isAnimationActive
              />
            </AreaChart>
          </ResponsiveContainer>
        </motion.div>
      )}
    </div>
  )
}
