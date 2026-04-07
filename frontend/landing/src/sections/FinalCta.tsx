import { motion } from 'framer-motion'
import { Button } from '@/components/ui/button'
import { Section } from '@/components/landing/Section'

export function FinalCta() {
  return (
    <Section id="cta" className="pb-32">
      <motion.div
        className="gsap-reveal mx-auto max-w-6xl overflow-hidden rounded-3xl border border-white/10 bg-slate-900/50 p-8 shadow-[0_40px_120px_-80px_rgba(56,189,248,0.9)] md:p-12"
        initial={{ opacity: 0, y: 18 }}
        whileInView={{ opacity: 1, y: 0 }}
        viewport={{ once: true, amount: 0.3 }}
        transition={{ duration: 0.55 }}
      >
        <div className="pointer-events-none absolute inset-0 bg-[radial-gradient(circle_at_20%_10%,rgba(56,189,248,0.16),transparent_55%),radial-gradient(circle_at_80%_80%,rgba(168,85,247,0.10),transparent_55%)]" />
        <div className="relative">
          <h2 className="text-balance text-3xl font-semibold tracking-tight text-white md:text-4xl">
            Keep scrolling. Keep learning. Keep your process clean.
          </h2>
          <p className="mt-4 max-w-2xl text-base leading-relaxed text-slate-400">
            Start free, explore the markets flow, and experience a product reveal that’s designed to feel premium and immersive.
          </p>
          <div className="mt-8 flex flex-wrap gap-3">
            <Button asChild size="lg">
              <a href="/Home/Signup">Create free account</a>
            </Button>
            <Button asChild variant="outline" size="lg">
              <a href="/Home/Log">Log in</a>
            </Button>
            <Button asChild variant="outline" size="lg">
              <a href="/Stocks/Index">Explore markets</a>
            </Button>
          </div>
        </div>
      </motion.div>
    </Section>
  )
}

