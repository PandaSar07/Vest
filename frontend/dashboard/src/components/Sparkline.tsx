import {
  Area,
  AreaChart,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from 'recharts'
import type { ChartPoint } from '@/lib/chartData'
import { fmtMoney, currencySymbol } from '@/lib/format'

type SparklineProps = {
  data: ChartPoint[]
  positive: boolean
}

export function Sparkline({ data, positive }: SparklineProps) {
  const stroke = positive ? 'var(--success-color,#10b981)' : 'var(--danger-color,#ef4444)'
  const fillId = positive ? 'dashSparkPos' : 'dashSparkNeg'

  if (data.length < 2) {
    return (
      <div className="flex h-full min-h-[120px] items-center justify-center rounded-xl border border-dashed border-white/10 bg-white/[0.02] text-center text-sm text-[var(--text-secondary,#94a3b8)]">
        Visit regularly to build performance history.
      </div>
    )
  }

  return (
    <div className="h-[140px] w-full sm:h-[160px]">
      <ResponsiveContainer width="100%" height="100%">
        <AreaChart data={data} margin={{ top: 6, right: 6, left: -18, bottom: 0 }}>
          <defs>
            <linearGradient id={fillId} x1="0" y1="0" x2="0" y2="1">
              <stop offset="0%" stopColor={stroke} stopOpacity={0.35} />
              <stop offset="100%" stopColor={stroke} stopOpacity={0} />
            </linearGradient>
          </defs>
          <XAxis dataKey="label" hide />
          <YAxis hide domain={['dataMin', 'dataMax']} />
          <Tooltip
            cursor={{ stroke: 'rgba(148,163,184,0.25)', strokeWidth: 1 }}
            contentStyle={{
              background: 'rgba(10,14,26,0.95)',
              border: '1px solid rgba(255,255,255,0.08)',
              borderRadius: '12px',
              fontSize: '12px',
            }}
            labelStyle={{ color: '#94a3b8' }}
            formatter={(v: number) => [`${currencySymbol}${fmtMoney(v)}`, 'Value']}
          />
          <Area
            type="monotone"
            dataKey="value"
            stroke={stroke}
            strokeWidth={2}
            fill={`url(#${fillId})`}
            dot={false}
            activeDot={{ r: 3, strokeWidth: 0 }}
            isAnimationActive
          />
        </AreaChart>
      </ResponsiveContainer>
    </div>
  )
}
