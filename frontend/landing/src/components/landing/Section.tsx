import type { PropsWithChildren } from 'react'
import { cn } from '@/lib/utils'

export function Section({
  id,
  className,
  children,
}: PropsWithChildren<{ id?: string; className?: string }>) {
  return (
    <section
      id={id}
      data-section
      className={cn('relative scroll-mt-24 py-24 md:py-28', className)}
    >
      {children}
    </section>
  )
}

export function SectionHeader({
  eyebrow,
  title,
  description,
}: {
  eyebrow?: string
  title: string
  description?: string
}) {
  return (
    <div className="mx-auto max-w-3xl text-center">
      {eyebrow ? (
        <p className="mb-3 inline-flex items-center rounded-full border border-white/10 bg-white/[0.03] px-3 py-1 text-xs font-semibold tracking-wide text-slate-300">
          {eyebrow}
        </p>
      ) : null}
      <h2 className="text-balance text-3xl font-semibold tracking-tight text-white md:text-4xl">
        {title}
      </h2>
      {description ? (
        <p className="mx-auto mt-4 max-w-2xl text-pretty text-base leading-relaxed text-slate-400">
          {description}
        </p>
      ) : null}
    </div>
  )
}

