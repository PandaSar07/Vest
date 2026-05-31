import { useState } from 'react'
import type { Trade } from '@/types'
import { updateTradeNote } from '@/api/portfolio'
import { fmtMoney, currencySymbol } from '@/lib/format'
import { cn } from '@/lib/utils'

const NOTE_MAX = 500

type TradesTableProps = {
  trades: Trade[]
  onNoteUpdated?: (tradeId: number, note: string | null) => void
}

export function TradesTable({ trades, onNoteUpdated }: TradesTableProps) {
  const [editingId, setEditingId] = useState<number | null>(null)
  const [draft, setDraft] = useState('')
  const [savingId, setSavingId] = useState<number | null>(null)
  const [error, setError] = useState<string | null>(null)

  if (!trades.length) {
    return (
      <p className="rounded-xl border border-dashed border-white/10 bg-white/[0.02] py-14 text-center text-sm text-[var(--text-secondary,#94a3b8)]">
        No trades yet.
      </p>
    )
  }

  function startEdit(t: Trade) {
    if (t.id == null) return
    setEditingId(t.id)
    setDraft(t.note ?? '')
    setError(null)
  }

  function cancelEdit() {
    setEditingId(null)
    setDraft('')
    setError(null)
  }

  async function saveNote(tradeId: number) {
    const trimmed = draft.trim()
    if (trimmed.length > NOTE_MAX) {
      setError(`Note must be ${NOTE_MAX} characters or fewer.`)
      return
    }
    setSavingId(tradeId)
    setError(null)
    const res = await updateTradeNote(tradeId, trimmed || null)
    setSavingId(null)
    if (!res.ok) {
      setError(res.error ?? 'Could not save note.')
      return
    }
    onNoteUpdated?.(tradeId, res.note ?? null)
    setEditingId(null)
    setDraft('')
  }

  return (
    <div className="overflow-x-auto rounded-xl border border-white/[0.06]">
      <table className="w-full min-w-[640px] border-collapse text-left text-sm">
        <thead>
          <tr className="border-b border-white/[0.06] text-[11px] font-semibold uppercase tracking-wider text-[var(--text-secondary,#94a3b8)]">
            <th className="px-4 py-3 font-semibold">Symbol</th>
            <th className="px-4 py-3 font-semibold">Side</th>
            <th className="px-4 py-3 font-semibold">Price</th>
            <th className="px-4 py-3 font-semibold">Exit</th>
            <th className="px-4 py-3 font-semibold">Date</th>
            <th className="px-4 py-3 font-semibold">Note</th>
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
            const isEditing = t.id != null && editingId === t.id
            const canEdit = t.id != null

            return (
              <tr
                key={t.id != null ? String(t.id) : `${t.symbol}-${t.tradedAt}-${t.price}`}
                className="border-b border-white/[0.04] align-top transition-colors hover:bg-white/[0.035]"
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
                <td className="px-4 py-3 min-w-[200px] max-w-[280px]">
                  {!canEdit ? (
                    <span className="text-[var(--text-secondary,#94a3b8)]">—</span>
                  ) : isEditing ? (
                    <div className="space-y-2">
                      <textarea
                        value={draft}
                        onChange={(e) => setDraft(e.target.value)}
                        maxLength={NOTE_MAX}
                        rows={3}
                        placeholder="Why you bought, target exit, thesis…"
                        className="w-full resize-y rounded-lg border border-white/10 bg-black/30 px-2.5 py-2 text-xs text-[var(--text-primary,#f1f5f9)] placeholder:text-[var(--text-secondary,#64748b)] focus:border-[var(--accent-color,#00c2ff)]/50 focus:outline-none"
                        autoFocus
                      />
                      <div className="flex flex-wrap items-center gap-2">
                        <button
                          type="button"
                          disabled={savingId === t.id}
                          onClick={() => void saveNote(t.id!)}
                          className="rounded-md bg-[var(--accent-color,#00c2ff)] px-2.5 py-1 text-[11px] font-semibold text-[#0a0e1a] disabled:opacity-50"
                        >
                          {savingId === t.id ? 'Saving…' : 'Save'}
                        </button>
                        <button
                          type="button"
                          onClick={cancelEdit}
                          className="rounded-md border border-white/10 px-2.5 py-1 text-[11px] text-[var(--text-secondary,#94a3b8)] hover:bg-white/5"
                        >
                          Cancel
                        </button>
                        <span className="text-[10px] text-[var(--text-secondary,#64748b)]">
                          {draft.length}/{NOTE_MAX}
                        </span>
                      </div>
                      {error && isEditing && (
                        <p className="text-[11px] text-[var(--danger-color,#ef4444)]">{error}</p>
                      )}
                    </div>
                  ) : t.note ? (
                    <div className="group">
                      <p className="text-xs leading-relaxed text-[var(--text-primary,#f1f5f9)] whitespace-pre-wrap break-words">
                        {t.note}
                      </p>
                      <button
                        type="button"
                        onClick={() => startEdit(t)}
                        className="mt-1 text-[11px] font-medium text-[var(--accent-color,#00c2ff)] opacity-70 hover:opacity-100"
                      >
                        Edit
                      </button>
                    </div>
                  ) : (
                    <button
                      type="button"
                      onClick={() => startEdit(t)}
                      className="text-[11px] font-medium text-[var(--accent-color,#00c2ff)] hover:underline"
                    >
                      Add note
                    </button>
                  )}
                </td>
              </tr>
            )
          })}
        </tbody>
      </table>
    </div>
  )
}
