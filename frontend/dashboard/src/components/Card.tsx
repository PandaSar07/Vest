import type { ReactNode } from 'react'
import { cn } from '@/lib/utils'

type CardProps = {
  title?: string
  subtitle?: string
  action?: ReactNode
  children: ReactNode
  className?: string
}

export function Card({ title, subtitle, action, children, className }: CardProps) {
  return (
    <div
      className={cn(
        'rounded-2xl border border-[var(--border-color,rgba(255,255,255,0.1))] bg-[var(--glass-bg,rgba(17,25,40,0.75))] p-5 shadow-[0_20px_56px_-28px_rgba(0,0,0,0.75)] backdrop-blur-xl sm:p-6',
        className,
      )}
    >
      {(title || subtitle || action) && (
        <div className="mb-5 flex flex-wrap items-start justify-between gap-3">
          <div>
            {title && (
              <h2 className="font-[family-name:var(--font-brand)] text-xs font-semibold uppercase tracking-[0.14em] text-[var(--text-secondary,#94a3b8)]">
                {title}
              </h2>
            )}
            {subtitle && <p className="mt-1 text-sm text-[var(--text-secondary,#94a3b8)]">{subtitle}</p>}
          </div>
          {action}
        </div>
      )}
      {children}
    </div>
  )
}
