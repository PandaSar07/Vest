export type Holding = {
  symbol: string
  shares: number
  avgCost: number
  livePrice: number
  marketValue: number
  gainLoss: number
  gainLossPct: number
  sector: string
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
