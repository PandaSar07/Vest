import { motion } from 'framer-motion'
import { Section, SectionHeader } from '@/components/landing/Section'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'

export function ProductReveal() {
  return (
    <Section id="product" className="relative">
      <SectionHeader
        eyebrow="Product reveal"
        title="A pinned moment that reveals the product"
        description="This section pins briefly on scroll while content reveals in layers—like a high-end product demo."
      />

      <div className="reveal-pin mx-auto mt-14 max-w-6xl rounded-3xl border border-white/10 bg-slate-900/35 p-4 shadow-[0_50px_140px_-110px_rgba(56,189,248,0.9)] backdrop-blur-md md:p-6">
        <div className="grid min-h-[72vh] grid-cols-1 gap-5 md:grid-cols-2">
          <motion.div
            className="gsap-reveal"
            initial={{ opacity: 0, y: 18 }}
            whileInView={{ opacity: 1, y: 0 }}
            viewport={{ once: true, amount: 0.25 }}
            transition={{ duration: 0.5 }}
          >
            <Card className="overflow-hidden">
              <CardHeader>
                <CardTitle>Demo surface</CardTitle>
                <p className="mt-2 text-sm text-slate-400">
                  A premium “device” surface with subtle grid + glow that moves with scroll.
                </p>
              </CardHeader>
              <CardContent className="pt-0">
                <div className="relative overflow-hidden rounded-2xl border border-white/10 bg-slate-950/30 p-6">
                  <div className="demo-parallax pointer-events-none absolute inset-0 bg-[radial-gradient(circle_at_20%_20%,rgba(56,189,248,0.18),transparent_40%),radial-gradient(circle_at_80%_70%,rgba(168,85,247,0.12),transparent_45%)]" />
                  <div className="pointer-events-none absolute inset-0 opacity-[0.35] [background-image:linear-gradient(to_right,rgba(255,255,255,0.06)_1px,transparent_1px),linear-gradient(to_bottom,rgba(255,255,255,0.06)_1px,transparent_1px)] [background-size:28px_28px]" />
                  <div className="relative space-y-3">
                    <div className="h-10 w-2/3 rounded-xl bg-white/5" />
                    <div className="h-24 rounded-2xl bg-white/[0.035]" />
                    <div className="grid grid-cols-3 gap-3">
                      <div className="h-14 rounded-xl bg-white/[0.04]" />
                      <div className="h-14 rounded-xl bg-white/[0.04]" />
                      <div className="h-14 rounded-xl bg-white/[0.04]" />
                    </div>
                    <div className="h-28 rounded-2xl bg-white/[0.03]" />
                  </div>
                </div>
              </CardContent>
            </Card>
          </motion.div>

          <div className="space-y-5">
            {[
              {
                t: 'Scroll reveals, not dumps',
                d: 'Each element appears only when it becomes relevant—better pacing and perceived quality.',
              },
              {
                t: 'Pinned chapter',
                d: 'The reveal stays in view briefly so the user absorbs the “why” without rushing.',
              },
              {
                t: 'Parallax lighting',
                d: 'Background glow moves slower than content to create depth without chaos.',
              },
            ].map((x) => (
              <motion.div
                key={x.t}
                className="gsap-reveal"
                initial={{ opacity: 0, y: 18 }}
                whileInView={{ opacity: 1, y: 0 }}
                viewport={{ once: true, amount: 0.35 }}
                transition={{ duration: 0.45 }}
                whileHover={{ scale: 1.01 }}
              >
                <Card>
                  <CardHeader>
                    <CardTitle>{x.t}</CardTitle>
                  </CardHeader>
                  <CardContent className="pt-0 text-sm text-slate-400">{x.d}</CardContent>
                </Card>
              </motion.div>
            ))}
          </div>
        </div>
      </div>
    </Section>
  )
}

