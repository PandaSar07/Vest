import { motion } from 'framer-motion'
import { ArrowRight } from 'lucide-react'
import { Button } from '@/components/ui/button'
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from '@/components/ui/dialog'

export function Hero() {
  return (
    <motion.section
      data-section
      className="hero-wrap relative overflow-hidden rounded-3xl border border-white/10 bg-slate-900/55 p-8 shadow-[0_40px_120px_-60px_rgba(56,189,248,0.75)] backdrop-blur-md md:p-12"
      initial={{ opacity: 0, y: 18 }}
      animate={{ opacity: 1, y: 0 }}
      transition={{ duration: 0.9, ease: [0.16, 1, 0.3, 1], delay: 0.15 }}
    >
      <div className="hero-parallax pointer-events-none absolute -top-40 left-1/2 h-[520px] w-[820px] -translate-x-1/2 rounded-full bg-sky-400/15 blur-[90px]" />
      <div className="hero-parallax pointer-events-none absolute -bottom-48 right-0 h-[420px] w-[520px] rounded-full bg-fuchsia-500/10 blur-[85px]" />

      <p className="mb-4 inline-flex items-center rounded-full border border-sky-300/25 bg-sky-300/10 px-3 py-1 text-xs font-semibold tracking-wide text-sky-200">
        A cinematic, process-first investing experience
      </p>

      <h1 className="text-balance max-w-4xl text-4xl font-bold leading-[1.06] tracking-tight text-white md:text-6xl">
        Markets, revealed like a high-end product launch.
      </h1>
      <p className="mt-5 max-w-2xl text-pretty text-base leading-relaxed text-slate-300">
        Vest is a premium learning flow for stocks and crypto—built around clarity, disciplined tooling, and subtle motion that guides attention.
      </p>

      <div className="mt-8 flex flex-wrap items-center gap-3">
        <Button asChild size="lg">
          <a href="/Home/Signup">Create free account</a>
        </Button>
        <Button asChild variant="outline" size="lg">
          <a href="/Stocks/Index">Explore markets</a>
        </Button>
        <Dialog>
          <DialogTrigger asChild>
            <Button variant="ghost" size="lg">
              See the reveal flow <ArrowRight className="ml-1 h-4 w-4" />
            </Button>
          </DialogTrigger>
          <DialogContent>
            <DialogHeader>
              <DialogTitle>Designed for calm momentum</DialogTitle>
              <DialogDescription>
                This landing uses staged sections, parallax, and pinned chapters so the experience unfolds over scroll—like a high-end product reveal.
              </DialogDescription>
            </DialogHeader>
          </DialogContent>
        </Dialog>
      </div>

      <div className="mt-10 grid grid-cols-1 gap-3 md:grid-cols-3">
        {[
          ['Precision over noise', 'A flow that rewards consistency.'],
          ['Market-first workflow', 'Discover → research → save → track.'],
          ['Premium micro-interactions', 'Subtle lift, glow, and motion.'],
        ].map(([k, v]) => (
          <div
            key={k}
            className="rounded-2xl border border-white/10 bg-white/[0.02] p-4"
          >
            <p className="text-sm font-semibold text-white">{k}</p>
            <p className="mt-1 text-sm text-slate-400">{v}</p>
          </div>
        ))}
      </div>
    </motion.section>
  )
}

