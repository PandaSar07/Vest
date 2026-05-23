import { useCallback, useEffect, useState } from 'react'
import { motion } from 'framer-motion'
import { fetchOrders, fetchSnapshots, fetchSummary, fetchTrades } from '@/api/portfolio'
import { normalizeSnapshots } from '@/lib/chartData'
import type { LimitOrder, PerfRange, PortfolioSummary, Snapshot, Trade } from '@/types'
import { Card } from '@/components/Card'
import { DashboardSkeleton } from '@/components/Skeleton'
import { PortfolioHero } from '@/components/PortfolioHero'
import { HoldingsTable } from '@/components/HoldingsTable'
import { SectorDonut } from '@/components/SectorDonut'
import { PerformanceChart } from '@/components/PerformanceChart'
import { TradesTable } from '@/components/TradesTable'
import { PendingOrders } from '@/components/PendingOrders'
import { InsightsStrip } from '@/components/InsightsStrip'

function readDashboardMeta() {
  const el = document.getElementById('dashboard-root')
  return {
    username: el?.dataset.username?.trim() || 'Trader',
    avatarUrl: el?.dataset.avatarUrl?.trim() || '',
    avatarInitials: el?.dataset.avatarInitials?.trim() || '?',
  }
}

function UserAvatar({
  url,
  initials,
  className,
}: {
  url: string
  initials: string
  className: string
}) {
  if (url) {
    return <img src={url} alt="" className={`${className} object-cover`} />
  }
  return (
    <span
      className={`${className} inline-flex items-center justify-center bg-gradient-to-br from-[var(--accent-color,#00c2ff)] to-[#7c3aed] font-extrabold text-[#0a0e1a]`}
      aria-hidden
    >
      {initials}
    </span>
  )
}

export default function App() {
  const { username, avatarUrl, avatarInitials } = readDashboardMeta()
  const [loading, setLoading] = useState(true)
  const [loadError, setLoadError] = useState<string | null>(null)
  const [summary, setSummary] = useState<PortfolioSummary | null>(null)
  const [snapshots, setSnapshots] = useState<Snapshot[]>([])
  const [trades, setTrades] = useState<Trade[]>([])
  const [orders, setOrders] = useState<LimitOrder[]>([])
  const [perfRange, setPerfRange] = useState<PerfRange>('1M')

  const refresh = useCallback(async () => {
    setLoadError(null)
    const [sum, snaps, tr, ord] = await Promise.allSettled([
      fetchSummary(),
      fetchSnapshots(730),
      fetchTrades(15),
      fetchOrders(),
    ])

    setSummary(sum.status === 'fulfilled' ? sum.value : null)
    setSnapshots(snaps.status === 'fulfilled' ? snaps.value : [])
    setTrades(tr.status === 'fulfilled' ? tr.value : [])
    setOrders(ord.status === 'fulfilled' ? ord.value : [])

    if (
      sum.status === 'rejected' ||
      snaps.status === 'rejected' ||
      tr.status === 'rejected' ||
      ord.status === 'rejected'
    ) {
      setLoadError('We hit a temporary issue loading your dashboard data.')
    }
  }, [])

  useEffect(() => {
    let cancelled = false
    ;(async () => {
      try {
        await refresh()
      } finally {
        if (!cancelled) setLoading(false)
      }
    })()
    return () => {
      cancelled = true
    }
  }, [refresh])

  const chartPoints = normalizeSnapshots(snapshots)

  async function onOrderCancelled() {
    try {
      const ord = await fetchOrders()
      setOrders(ord)
      await refresh()
    } catch {
      setLoadError('The order was updated, but the dashboard could not refresh right away.')
    }
  }

  if (loading) {
    return (
      <div className="py-2">
        <header className="mb-8">
          <SkeletonTitle />
        </header>
        <DashboardSkeleton />
      </div>
    )
  }

  if (!summary) {
    return (
      <Card>
        <div className="space-y-4 text-center">
          <div className="space-y-2">
            <h1 className="font-[family-name:var(--font-brand)] text-2xl font-bold tracking-tight sm:text-3xl">
              Dashboard
            </h1>
            <p className="text-sm text-[var(--text-secondary,#94a3b8)] sm:text-base">
              Welcome back, <span className="font-semibold text-[var(--text-primary,#f1f5f9)]">{username}</span>.
            </p>
          </div>
          <p className="text-sm text-[var(--text-secondary,#94a3b8)]">
            {loadError ?? 'We couldn&apos;t load your portfolio. Refresh and try again.'}
          </p>
          <div className="flex justify-center">
            <button
              type="button"
              onClick={() => {
                setLoading(true)
                void refresh().finally(() => setLoading(false))
              }}
              className="rounded-xl bg-[var(--accent-color,#00c2ff)] px-4 py-2 text-sm font-semibold text-[#0a0e1a] transition hover:brightness-110"
            >
              Retry dashboard
            </button>
          </div>
        </div>
      </Card>
    )
  }

  return (
    <motion.div
      initial={{ opacity: 0, y: 10 }}
      animate={{ opacity: 1, y: 0 }}
      transition={{ duration: 0.45, ease: [0.22, 1, 0.36, 1] }}
      className="space-y-6 py-2"
    >
      <header className="flex flex-wrap items-end justify-between gap-4">
        <div className="flex items-start gap-4">
          <UserAvatar
            url={avatarUrl}
            initials={avatarInitials}
            className="h-14 w-14 shrink-0 rounded-full border-2 border-[var(--accent-color,#00c2ff)]/35 shadow-[0_0_24px_rgba(0,194,255,0.2)]"
          />
          <div>
            <h1 className="font-[family-name:var(--font-brand)] text-3xl font-bold tracking-tight sm:text-4xl">
              Dashboard
            </h1>
            <p className="mt-2 max-w-xl text-sm text-[var(--text-secondary,#94a3b8)] sm:text-base">
              Welcome back, <span className="font-semibold text-[var(--text-primary,#f1f5f9)]">{username}</span>.
              Your portfolio, distilled for fast decisions.
            </p>
            {loadError && (
              <p className="mt-3 text-sm text-amber-300">
                {loadError}
              </p>
            )}
          </div>
        </div>
      </header>

      <PortfolioHero summary={summary} chartPoints={chartPoints} snapshots={snapshots} />

      <InsightsStrip holdings={summary.holdings} />

      <div className="grid gap-6 lg:grid-cols-5">
        <Card title="Holdings" subtitle="Tap a symbol to research or trade." className="lg:col-span-3">
          <HoldingsTable holdings={summary.holdings} />
        </Card>
        <Card title="Portfolio allocation" subtitle="Cash plus holdings by sector." className="lg:col-span-2">
          <SectorDonut summary={summary} />
        </Card>
      </div>

      <Card>
        <PerformanceChart points={chartPoints} range={perfRange} onRangeChange={setPerfRange} />
      </Card>

      {orders.length > 0 && (
        <Card title="Pending limit orders" subtitle="Working orders on your account.">
          <PendingOrders orders={orders} onCancelled={onOrderCancelled} />
        </Card>
      )}

      <Card title="Past trades" subtitle="Latest executions across your account.">
        <TradesTable trades={trades} />
      </Card>
    </motion.div>
  )
}

function SkeletonTitle() {
  return (
    <div className="space-y-3">
      <div className="h-9 w-48 animate-pulse rounded-lg bg-white/[0.06] sm:h-11 sm:w-56" />
      <div className="h-4 w-full max-w-md animate-pulse rounded-lg bg-white/[0.06]" />
    </div>
  )
}
