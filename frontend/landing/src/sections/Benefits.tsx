import { motion } from 'framer-motion'
import { Section, SectionHeader } from '@/components/landing/Section'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'

const bullets = [
  {
    t: 'Less cognitive load',
    d: 'A calmer interface helps you think in systems, not impulses.',
  },
  {
    t: 'Consistent decisions',
    d: 'The workflow is designed to repeat weekly without friction.',
  },
  {
    t: 'Higher perceived quality',
    d: 'Polished surfaces and pacing build trust, even before sign up.',
  },
]

export function Benefits() {
  return (
    <Section id="benefits" className="relative">
      <SectionHeader
        eyebrow="Benefits"
        title="Designed to feel expensive—because focus is expensive"
        description="Every section has a purpose: guide attention, reduce friction, and keep users scrolling."
      />

      <div className="mx-auto mt-14 grid max-w-6xl grid-cols-1 gap-5 md:grid-cols-3">
        {bullets.map((b, i) => (
          <motion.div
            key={b.t}
            className="gsap-reveal"
            initial={{ opacity: 0, y: 18 }}
            whileInView={{ opacity: 1, y: 0 }}
            viewport={{ once: true, amount: 0.25 }}
            transition={{ duration: 0.45, delay: i * 0.04 }}
            whileHover={{ scale: 1.02 }}
          >
            <Card className="h-full">
              <CardHeader>
                <CardTitle>{b.t}</CardTitle>
              </CardHeader>
              <CardContent className="pt-0 text-sm text-slate-400">{b.d}</CardContent>
            </Card>
          </motion.div>
        ))}
      </div>
    </Section>
  )
}

