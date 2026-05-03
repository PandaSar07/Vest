import { motion } from 'framer-motion'
import type { Holding } from '@/types'
import { displaySymbol, fmtMoney, qtyLabel } from '@/lib/format'
import { cn } from '@/lib/utils'

export function HoldingsTable({ holdings }: { holdings: Holding[] }) {
  if (!holdings.length) {
    return (
      <p className="rounded-xl border border-dashed border-white/10 bg-white/[0.02] py-14 text-center text-sm text-[var(--text-secondary,#94a3b8)]">
        No positions yet —{' '}
        <a href="/Stocks" className="font-semibold text-[var(--accent-color,#00c2ff)] hover:underline">
          start trading
        </a>
        .
      </p>
    )
  }

  return (
    <div className="overflow-x-auto rounded-xl border border-white/[0.06]">
      <table className="w-full min-w-[640px] border-collapse text-left text-sm">
        <thead>
          <tr className="border-b border-white/[0.06] text-[11px] font-semibold uppercase tracking-wider text-[var(--text-secondary,#94a3b8)]">
            <th className="px-4 py-3 font-semibold">Symbol</th>
            <th className="px-4 py-3 font-semibold">Shares</th>
            <th className="px-4 py-3 font-semibold">Avg price</th>
            <th className="px-4 py-3 font-semibold">Current</th>
            <th className="px-4 py-3 text-right font-semibold">P/L</th>
          </tr>
        </thead>
        <tbody>
          {holdings.map((h, i) => {
            const sym = displaySymbol(h.symbol)
            const positive = h.gainLoss >= 0
            return (
              <motion.tr
                key={h.symbol}
                initial={{ opacity: 0, y: 6 }}
                animate={{ opacity: 1, y: 0 }}
                transition={{ duration: 0.22, delay: Math.min(i * 0.03, 0.24) }}
                className="border-b border-white/[0.04] transition-colors hover:bg-white/[0.035]"
              >
                <td className="px-4 py-3 font-semibold tabular-nums">
                  <a
                    href={`/Stocks?symbol=${encodeURIComponent(h.symbol)}`}
                    className="text-[var(--accent-color,#00c2ff)] hover:text-[var(--accent-hover,#33d6ff)]"
                  >
                    {sym}
                  </a>
                </td>
                <td className="px-4 py-3 tabular-nums text-[var(--text-secondary,#94a3b8)]">
                  {qtyLabel(h.shares)}
                </td>
                <td className="px-4 py-3 tabular-nums">${fmtMoney(h.avgCost)}</td>
                <td className="px-4 py-3 tabular-nums">${fmtMoney(h.livePrice)}</td>
                <td
                  className={cn(
                    'px-4 py-3 text-right font-semibold tabular-nums',
                    positive ? 'text-[var(--success-color,#10b981)]' : 'text-[var(--danger-color,#ef4444)]',
                  )}
                >
                  {positive ? '+' : '-'}${fmtMoney(Math.abs(h.gainLoss))}{' '}
                  <span className="text-xs font-medium opacity-90">
                    ({positive ? '+' : ''}
                    {fmtMoney(h.gainLossPct)}%)
                  </span>
                </td>
              </motion.tr>
            )
          })}
        </tbody>
      </table>
    </div>
  )
}
