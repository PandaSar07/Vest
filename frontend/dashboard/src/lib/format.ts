export function fmtMoney(n: number | null | undefined, digits = 2): string {
  if (n == null || Number.isNaN(n)) return '—'
  return n.toLocaleString('en-US', {
    minimumFractionDigits: digits,
    maximumFractionDigits: digits,
  })
}

export function parseApiDate(value: string | null | undefined): Date | null {
  if (!value) return null
  if (/^\d{4}-\d{2}-\d{2}$/.test(value)) {
    const [y, m, d] = value.split('-').map(Number)
    return new Date(y, m - 1, d)
  }
  const dt = new Date(value)
  return Number.isNaN(dt.getTime()) ? null : dt
}

export function calendarDayUtc(iso: string): string {
  const d = parseApiDate(iso)
  if (!d) return iso.slice(0, 10)
  return d.toISOString().slice(0, 10)
}

export function displaySymbol(symbol: string): string {
  return symbol
}

export function qtyLabel(shares: number): string {
  return fmtMoney(shares, 4)
}
