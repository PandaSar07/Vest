import type { Trade } from '@/types'
import { fmtMoney, currencySymbol } from '@/lib/format'
import { cn } from '@/lib/utils'

export function TradesTable({ trades }: { trades: Trade[] }) {
  if (!trades.length) {
    return (
      <p className="rounded-xl border border-dashed border-white/10 bg-white/[0.02] py-14 text-center text-sm text-[var(--text-secondary,#94a3b8)]">
        No trades yet.
      </p>
    )
  }

  return (
    <div className="overflow-x-auto rounded-xl border border-white/[0.06]">
      <table className="w-full min-w-[480px] border-collapse text-left text-sm">
        <thead>
          <tr className="border-b border-white/[0.06] text-[11px] font-semibold uppercase tracking-wider text-[var(--text-secondary,#94a3b8)]">
            <th className="px-4 py-3 font-semibold">Symbol</th>
            <th className="px-4 py-3 font-semibold">Side</th>
            <th className="px-4 py-3 font-semibold">Price</th>
            <th className="px-4 py-3 font-semibold">Exit</th>
            <th className="px-4 py-3 font-semibold">Date</th>
          </tr>
        </thead>
        <tbody>
          {trades.map((t) => {
            const buy = t.action === 'BUY'
            const exitLabel =
              t.exitReason === 'STOP_LOSS'
                ? 'Stop-loss'
                : t.exitReason === 'TAKE_PROFIT'
                  ? 'Take-profit'
                  : t.exitReason === 'LIMIT_FILL'
                    ? 'Limit'
                    : t.exitReason === 'MANUAL'
                      ? 'Manual'
                      : '—'
            const date = new Date(t.tradedAt).toLocaleString('en-US', {
              month: 'short',
              day: 'numeric',
              year: 'numeric',
              hour: 'numeric',
              minute: '2-digit',
            })
            return (
              <tr
                key={t.id != null ? String(t.id) : `${t.symbol}-${t.tradedAt}-${t.price}`}
                className="border-b border-white/[0.04] transition-colors hover:bg-white/[0.035]"
              >
                <td className="px-4 py-3 font-semibold">
                  <a
                    href={`/Stocks?symbol=${encodeURIComponent(t.symbol)}`}
                    className="text-[var(--accent-color,#00c2ff)] hover:text-[var(--accent-hover,#33d6ff)]"
                  >
                    {t.symbol}
                  </a>
                </td>
                <td className="px-4 py-3">
                  <span
                    className={cn(
                      'rounded-md px-2 py-0.5 text-[11px] font-bold uppercase tracking-wide',
                      buy
                        ? 'bg-emerald-500/15 text-emerald-400 ring-1 ring-emerald-500/25'
                        : 'bg-rose-500/15 text-rose-400 ring-1 ring-rose-500/25',
                    )}
                  >
                    {t.action}
                  </span>
                </td>
                <td className="px-4 py-3 tabular-nums">{currencySymbol}{fmtMoney(t.price)}</td>
                <td className="px-4 py-3 text-[var(--text-secondary,#94a3b8)]">
                  {!buy ? exitLabel : '—'}
                </td>
                <td className="px-4 py-3 text-[var(--text-secondary,#94a3b8)]">{date}</td>
              </tr>
            )
          })}
        </tbody>
      </table>
    </div>
  )
}
