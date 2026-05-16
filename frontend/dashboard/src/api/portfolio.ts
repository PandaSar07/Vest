import type { LimitOrder, PortfolioSummary, Snapshot, Trade } from '@/types'

function handleUnauthorized(res: Response) {
  if (res.status === 401) window.location.assign('/Home/Log')
}

export async function fetchSummary(): Promise<PortfolioSummary | null> {
  const r = await fetch('/portfolio/summary', { credentials: 'same-origin' })
  handleUnauthorized(r)
  if (!r.ok) return null
  return r.json()
}

export async function fetchSnapshots(days: number): Promise<Snapshot[]> {
  const r = await fetch(`/portfolio/snapshots?days=${days}`, { credentials: 'same-origin' })
  handleUnauthorized(r)
  if (!r.ok) return []
  const raw: unknown = await r.json()
  if (!Array.isArray(raw)) return []
  return raw.map((x) => {
    const row = x as Record<string, unknown>
    return {
      value: Number(row.value ?? 0),
      snappedAt: String(row.snappedAt ?? row.snapped_at ?? ''),
    }
  })
}

export async function fetchTrades(limit: number): Promise<Trade[]> {
  const r = await fetch(`/portfolio/trades?limit=${limit}`, { credentials: 'same-origin' })
  handleUnauthorized(r)
  if (!r.ok) return []
  const raw: unknown = await r.json()
  if (!Array.isArray(raw)) return []
  return raw.map((x) => {
    const row = x as Record<string, unknown>
    return {
      id: row.id != null ? Number(row.id) : undefined,
      symbol: String(row.symbol ?? ''),
      action: String(row.action ?? ''),
      shares: Number(row.shares ?? 0),
      price: Number(row.price ?? 0),
      total: Number(row.total ?? 0),
      tradedAt: String(row.tradedAt ?? row.traded_at ?? ''),
      exitReason: row.exitReason != null ? String(row.exitReason) : row.exit_reason != null ? String(row.exit_reason) : null,
    }
  })
}

export async function fetchOrders(): Promise<LimitOrder[]> {
  const r = await fetch('/portfolio/orders', { credentials: 'same-origin' })
  handleUnauthorized(r)
  if (!r.ok) return []
  const raw: unknown = await r.json()
  if (!Array.isArray(raw)) return []
  return raw.map((x) => {
    const row = x as Record<string, unknown>
    return {
      id: Number(row.id ?? 0),
      symbol: String(row.symbol ?? ''),
      action: String(row.action ?? ''),
      shares: Number(row.shares ?? 0),
      limitPrice: Number(row.limitPrice ?? row.limit_price ?? 0),
      createdAt: String(row.createdAt ?? row.created_at ?? ''),
    }
  })
}

export async function cancelOrder(id: number): Promise<boolean> {
  const r = await fetch(`/portfolio/order/${id}`, { method: 'DELETE', credentials: 'same-origin' })
  return r.ok
}
