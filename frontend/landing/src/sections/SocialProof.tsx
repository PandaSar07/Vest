import { motion } from 'framer-motion'
import { Section, SectionHeader } from '@/components/landing/Section'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'

const stats = [
  { k: '12k+', v: 'symbols screened monthly' },
  { k: '87%', v: 'weekly watchlist usage' },
  { k: '4.9', v: 'learning satisfaction' },
  { k: '42', v: 'avg sessions per active learner' },
]

const quotes = [
  {
    q: '“The pinned reveal section made the product feel like a real launch, not a template.”',
    a: 'Product-minded learner',
  },
  {
    q: '“The UI pacing helps me focus. I scroll, I learn, and I actually remember the flow.”',
    a: 'Beginner investor',
  },
  {
    q: '“Subtle motion, clean hierarchy—this feels premium and calm.”',
    a: 'Finance student',
  },
]

export function SocialProof() {
  return (
    <Section id="proof">
      <SectionHeader
        eyebrow="Proof"
        title="Trust built through details"
        description="Stats and social proof act as pace breakers—so momentum stays high."
      />

      <div className="mx-auto mt-14 max-w-6xl space-y-10">
        <div className="grid grid-cols-2 gap-5 md:grid-cols-4">
          {stats.map((s, i) => (
            <motion.div
              key={s.k}
              className="gsap-reveal"
              initial={{ opacity: 0, y: 18 }}
              whileInView={{ opacity: 1, y: 0 }}
              viewport={{ once: true, amount: 0.35 }}
              transition={{ duration: 0.45, delay: i * 0.03 }}
            >
              <Card className="h-full">
                <CardHeader className="pb-0">
                  <p className="text-2xl font-semibold tracking-tight text-white">{s.k}</p>
                </CardHeader>
                <CardContent className="pt-2 text-sm text-slate-400">{s.v}</CardContent>
              </Card>
            </motion.div>
          ))}
        </div>

        <div className="grid grid-cols-1 gap-5 md:grid-cols-3">
          {quotes.map((x) => (
            <motion.div
              key={x.q}
              className="gsap-reveal"
              initial={{ opacity: 0, y: 18 }}
              whileInView={{ opacity: 1, y: 0 }}
              viewport={{ once: true, amount: 0.25 }}
              transition={{ duration: 0.45 }}
              whileHover={{ scale: 1.01 }}
            >
              <Card className="h-full">
                <CardHeader>
                  <CardTitle className="text-base font-semibold text-white">What people notice</CardTitle>
                </CardHeader>
                <CardContent className="pt-0 text-sm text-slate-300">
                  <p className="leading-relaxed">{x.q}</p>
                  <p className="mt-4 text-xs font-semibold tracking-wide text-slate-500">{x.a}</p>
                </CardContent>
              </Card>
            </motion.div>
          ))}
        </div>
      </div>
    </Section>
  )
}

