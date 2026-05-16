export type PositionRisk = {
  symbol: string
  entryPrice: number
  stopLossPrice?: number | null
  takeProfitPrice?: number | null
  stopLossPct?: number | null
  takeProfitPct?: number | null
  status?: string
}

export type Holding = {
  symbol: string
  shares: number
  avgCost: number
  livePrice: number
  marketValue: number
  gainLoss: number
  gainLossPct: number
  sector: string
  risk?: PositionRisk | null
}

export type PortfolioSummary = {
  cash: number
  stockValue: number
  totalValue: number
  holdings: Holding[]
}

export type Snapshot = {
  value: number
  snappedAt: string
}

export type Trade = {
  id?: number
  symbol: string
  action: string
  shares: number
  price: number
  total: number
  tradedAt: string
  exitReason?: string | null
}

export type LimitOrder = {
  id: number
  symbol: string
  action: string
  shares: number
  limitPrice: number
  createdAt: string
}

export type PerfRange = '1D' | '1W' | '1M' | '1Y' | 'ALL'
