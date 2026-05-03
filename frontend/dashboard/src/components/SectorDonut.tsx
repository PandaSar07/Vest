import { Cell, Legend, Pie, PieChart, ResponsiveContainer, Tooltip } from 'recharts'
import type { Holding } from '@/types'
import { fmtMoney } from '@/lib/format'

const PALETTE = [
  '#38bdf8',
  '#10b981',
  '#f59e0b',
  '#ef4444',
  '#8b5cf6',
  '#ec4899',
  '#f97316',
  '#06b6d4',
]

type SectorRow = { name: string; value: number; pct: number }

export function SectorDonut({ holdings }: { holdings: Holding[] }) {
  const sectorMap: Record<string, number> = {}
  holdings.forEach((h) => {
    const s = h.sector || 'Other'
    sectorMap[s] = (sectorMap[s] ?? 0) + h.marketValue
  })
  const rows: SectorRow[] = Object.entries(sectorMap).map(([name, value]) => ({ name, value, pct: 0 }))
  const total = rows.reduce((s, r) => s + r.value, 0)

  const data = rows.map((r) => ({
    ...r,
    pct: total > 0 ? (r.value / total) * 100 : 0,
  }))

  if (!holdings.length || total <= 0) {
    return (
      <p className="rounded-xl border border-dashed border-white/10 bg-white/[0.02] py-16 text-center text-sm text-[var(--text-secondary,#94a3b8)]">
        No holdings yet — sector mix will appear after your first buy.
      </p>
    )
  }

  return (
    <div className="h-[280px] w-full sm:h-[300px]">
      <ResponsiveContainer width="100%" height="100%">
        <PieChart>
          <Pie
            data={data}
            dataKey="value"
            nameKey="name"
            cx="50%"
            cy="48%"
            innerRadius={56}
            outerRadius={88}
            paddingAngle={2}
            stroke="rgba(0,0,0,0.25)"
            strokeWidth={2}
          >
            {data.map((_, i) => (
              <Cell key={i} fill={PALETTE[i % PALETTE.length]} />
            ))}
          </Pie>
          <Tooltip
            content={({ active, payload }) => {
              if (!active || !payload?.length) return null
              const p = payload[0].payload as SectorRow
              return (
                <div className="rounded-xl border border-white/10 bg-[rgba(10,14,26,0.96)] px-3 py-2 text-xs text-[var(--text-primary,#f1f5f9)] shadow-xl backdrop-blur-md">
                  <div className="font-semibold">{p.name}</div>
                  <div className="mt-0.5 tabular-nums text-[var(--text-secondary,#94a3b8)]">${fmtMoney(p.value)}</div>
                  <div className="tabular-nums text-[var(--text-secondary,#94a3b8)]">{fmtMoney(p.pct, 1)}% of book</div>
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
  )
}
