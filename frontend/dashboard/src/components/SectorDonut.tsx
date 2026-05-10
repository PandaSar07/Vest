import { Cell, Legend, Pie, PieChart, ResponsiveContainer, Tooltip } from 'recharts'
import type { PortfolioSummary } from '@/types'
import { fmtMoney, currencySymbol } from '@/lib/format'

const PALETTE: { fill: string; dotClass: string }[] = [
  { fill: '#38bdf8', dotClass: 'bg-[#38bdf8]' },
  { fill: '#10b981', dotClass: 'bg-[#10b981]' },
  { fill: '#f59e0b', dotClass: 'bg-[#f59e0b]' },
  { fill: '#ef4444', dotClass: 'bg-[#ef4444]' },
  { fill: '#8b5cf6', dotClass: 'bg-[#8b5cf6]' },
  { fill: '#ec4899', dotClass: 'bg-[#ec4899]' },
  { fill: '#f97316', dotClass: 'bg-[#f97316]' },
  { fill: '#06b6d4', dotClass: 'bg-[#06b6d4]' },
]

type AllocationRow = { name: string; value: number; pct: number }

export function SectorDonut({ summary }: { summary: PortfolioSummary }) {
  const sectorMap: Record<string, number> = {}
  summary.holdings.forEach((h) => {
    const s = h.sector || 'Other'
    sectorMap[s] = (sectorMap[s] ?? 0) + h.marketValue
  })
  const rows: AllocationRow[] = [
    ...Object.entries(sectorMap).map(([name, value]) => ({ name, value, pct: 0 })),
    { name: 'Cash', value: summary.cash, pct: 0 },
  ]
    .filter((row) => row.value > 0)
    .sort((a, b) => b.value - a.value)
  const total = summary.totalValue

  const data = rows.map((r) => ({
    ...r,
    pct: total > 0 ? (r.value / total) * 100 : 0,
  }))

  if (!summary.holdings.length || total <= 0) {
    return (
      <p className="rounded-xl border border-dashed border-white/10 bg-white/[0.02] py-16 text-center text-sm text-[var(--text-secondary,#94a3b8)]">
        No holdings yet — your allocation mix will appear after your first buy.
      </p>
    )
  }

  return (
    <div className="space-y-4">
      <div className="h-[240px] w-full sm:h-[260px]">
        <ResponsiveContainer width="100%" height="100%">
          <PieChart>
            <Pie
              data={data}
              dataKey="value"
              nameKey="name"
              cx="50%"
              cy="48%"
              innerRadius={54}
              outerRadius={86}
              paddingAngle={2}
              stroke="rgba(0,0,0,0.25)"
              strokeWidth={2}
            >
              {data.map((_, i) => (
                <Cell key={i} fill={PALETTE[i % PALETTE.length].fill} />
              ))}
            </Pie>
            <Tooltip
              content={({ active, payload }) => {
                if (!active || !payload?.length) return null
                const p = payload[0].payload as AllocationRow
                return (
                  <div className="rounded-xl border border-white/10 bg-[rgba(10,14,26,0.96)] px-3 py-2 text-xs text-[var(--text-primary,#f1f5f9)] shadow-xl backdrop-blur-md">
                    <div className="font-semibold">{p.name}</div>
                    <div className="mt-0.5 tabular-nums text-[var(--text-secondary,#94a3b8)]">{currencySymbol}{fmtMoney(p.value)}</div>
                    <div className="tabular-nums text-[var(--text-secondary,#94a3b8)]">{fmtMoney(p.pct, 1)}% of portfolio</div>
                  </div>
                )
              }}
            />
            <Legend
              verticalAlign="bottom"
              wrapperStyle={{ fontSize: '11px', color: '#94a3b8', paddingTop: 8 }}
            />
          </PieChart>
        </ResponsiveContainer>
      </div>

      <div className="space-y-2">
        {data.map((row, i) => (
          <div
            key={row.name}
            className="flex items-center justify-between gap-3 rounded-xl border border-white/[0.06] bg-white/[0.02] px-3 py-2 text-sm"
          >
            <div className="flex min-w-0 items-center gap-2">
              <span
                className={`h-3 w-3 rounded-md ${PALETTE[i % PALETTE.length].dotClass}`}
                aria-hidden="true"
              />
              <span className="truncate font-semibold text-[var(--text-primary,#f1f5f9)]">{row.name}</span>
            </div>
            <div className="text-right tabular-nums">
              <div className="font-semibold text-[var(--text-primary,#f1f5f9)]">{currencySymbol}{fmtMoney(row.value)}</div>
              <div className="text-xs text-[var(--text-secondary,#94a3b8)]">{fmtMoney(row.pct, 1)}%</div>
            </div>
          </div>
        ))}
      </div>
    </div>
  )
}
