import { motion } from 'framer-motion'
import { Section, SectionHeader } from '@/components/landing/Section'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'

const steps = [
  {
    k: 'Browse',
    v: 'Start broad. Build intuition for movement and context.',
  },
  {
    k: 'Filter',
    v: 'Use a screener-style workflow to reduce noise fast.',
  },
  {
    k: 'Save',
    v: 'Create a watchlist that behaves like a decision queue.',
  },
  {
    k: 'Track',
    v: 'Review ideas and positions with a calmer dashboard loop.',
  },
]

export function HowItWorks() {
  return (
    <Section id="how-it-works">
      <SectionHeader
        eyebrow="How it works"
        title="A disciplined flow that feels effortless"
        description="A long-scroll narrative works best when the user always knows what comes next."
      />

      <div className="mx-auto mt-14 max-w-6xl">
        <div className="grid grid-cols-1 gap-5 md:grid-cols-4">
          {steps.map((s, idx) => (
            <motion.div
              key={s.k}
              className="gsap-reveal"
              initial={{ opacity: 0, y: 18 }}
              whileInView={{ opacity: 1, y: 0 }}
              viewport={{ once: true, amount: 0.25 }}
              transition={{ duration: 0.45, delay: idx * 0.04 }}
              whileHover={{ scale: 1.02 }}
            >
              <Card className="h-full">
                <CardHeader>
                  <div className="mb-3 inline-flex h-8 w-8 items-center justify-center rounded-xl border border-sky-300/25 bg-sky-300/10 text-sm font-semibold text-sky-200">
                    {idx + 1}
                  </div>
                  <CardTitle>{s.k}</CardTitle>
                </CardHeader>
                <CardContent className="pt-0 text-sm text-slate-400">{s.v}</CardContent>
              </Card>
            </motion.div>
          ))}
        </div>
      </div>
    </Section>
  )
}

