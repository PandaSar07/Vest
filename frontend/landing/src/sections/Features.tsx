import { motion } from 'framer-motion'
import { ChartNoAxesCombined, Layers, ShieldCheck, Sparkles, WandSparkles, Zap } from 'lucide-react'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Section, SectionHeader } from '@/components/landing/Section'

const items = [
  {
    title: 'Signal-first market view',
    description: 'Scan movement with calm hierarchy and clear next steps.',
    icon: ChartNoAxesCombined,
  },
  {
    title: 'Bento feature system',
    description: 'Modular cards that feel like a premium dashboard reveal.',
    icon: Layers,
  },
  {
    title: 'Risk-first framing',
    description: 'Designed around probability, downside, and discipline.',
    icon: ShieldCheck,
  },
  {
    title: 'Cinematic pacing',
    description: 'Sections appear when they matter—never all at once.',
    icon: Sparkles,
  },
  {
    title: 'Subtle micro-interactions',
    description: 'Hover lift, brightness shifts, and controlled glow.',
    icon: WandSparkles,
  },
  {
    title: 'Fast, responsive UI',
    description: 'Feels crisp on mobile and precise on desktop.',
    icon: Zap,
  },
]

export function Features() {
  return (
    <Section id="features" className="relative">
      <SectionHeader
        eyebrow="Features"
        title="A premium toolkit—built to guide attention"
        description="A Stripe/Linear-style layout with deliberate spacing, minimal noise, and polished motion."
      />

      <motion.div
        initial="hidden"
        whileInView="show"
        viewport={{ once: true, amount: 0.2 }}
        variants={{ show: { transition: { staggerChildren: 0.08 } } }}
        className="mx-auto mt-14 grid max-w-6xl grid-cols-1 gap-5 md:grid-cols-3"
      >
        {items.map((it) => (
          <motion.div
            key={it.title}
            variants={{ hidden: { opacity: 0, y: 18 }, show: { opacity: 1, y: 0 } }}
            className="gsap-reveal"
            whileHover={{ scale: 1.02 }}
            transition={{ duration: 0.22 }}
          >
            <Card className="h-full transition-shadow duration-300 hover:shadow-[0_30px_70px_-50px_rgba(56,189,248,0.95)]">
              <CardHeader>
                <it.icon className="mb-4 h-5 w-5 text-sky-300" />
                <CardTitle>{it.title}</CardTitle>
                <CardDescription>{it.description}</CardDescription>
              </CardHeader>
              <CardContent className="pt-0 text-sm text-slate-500">
                Built with consistent rhythm (spacing, type scale, and surfaces) so it feels high-end—not template-like.
              </CardContent>
            </Card>
          </motion.div>
        ))}
      </motion.div>
    </Section>
  )
}

