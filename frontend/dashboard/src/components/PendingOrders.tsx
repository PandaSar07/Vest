import { useState } from 'react'
import type { LimitOrder } from '@/types'
import { fmtMoney, currencySymbol } from '@/lib/format'
import { cancelOrder } from '@/api/portfolio'
import { cn } from '@/lib/utils'

export function PendingOrders({ orders, onCancelled }: { orders: LimitOrder[]; onCancelled: () => void }) {
  const [busyId, setBusyId] = useState<number | null>(null)

  if (!orders.length) return null

  async function onCancel(id: number) {
    setBusyId(id)
    const ok = await cancelOrder(id)
    setBusyId(null)
    if (ok) onCancelled()
    else alert('Failed to cancel order.')
  }

  return (
    <div className="overflow-x-auto rounded-xl border border-white/[0.06]">
      <table className="w-full min-w-[560px] border-collapse text-left text-sm">
        <thead>
          <tr className="border-b border-white/[0.06] text-[11px] font-semibold uppercase tracking-wider text-[var(--text-secondary,#94a3b8)]">
            <th className="px-4 py-3 font-semibold">Symbol</th>
            <th className="px-4 py-3 font-semibold">Action</th>
            <th className="px-4 py-3 font-semibold">Shares</th>
            <th className="px-4 py-3 font-semibold">Limit</th>
            <th className="px-4 py-3 font-semibold">Placed</th>
            <th className="px-4 py-3" />
          </tr>
        </thead>
        <tbody>
          {orders.map((o) => {
            const buy = o.action === 'BUY'
            const placed = new Date(o.createdAt).toLocaleDateString()
            return (
              <tr key={o.id} className="border-b border-white/[0.04] transition-colors hover:bg-white/[0.035]">
                <td className="px-4 py-3 font-semibold">
                  <a
                    href={`/Stocks?symbol=${encodeURIComponent(o.symbol)}`}
                    className="text-[var(--accent-color,#00c2ff)] hover:text-[var(--accent-hover,#33d6ff)]"
                  >
                    {o.symbol}
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
                    {o.action}
                  </span>
                </td>
                <td className="px-4 py-3 tabular-nums">{fmtMoney(o.shares, 4)}</td>
                <td className="px-4 py-3 tabular-nums">{currencySymbol}{fmtMoney(o.limitPrice)}</td>
                <td className="px-4 py-3 text-[var(--text-secondary,#94a3b8)]">{placed}</td>
                <td className="px-4 py-3 text-right">
                  <button
                    type="button"
                    disabled={busyId === o.id}
                    onClick={() => onCancel(o.id)}
                    className="rounded-lg border border-rose-500/35 px-3 py-1 text-[11px] font-semibold text-rose-400 transition-colors hover:bg-rose-500/10 disabled:opacity-50"
                  >
                    Cancel
                  </button>
                </td>
              </tr>
            )
          })}
        </tbody>
      </table>
    </div>
  )
}
