import { motion } from 'framer-motion'
import { Search, Filter, BookmarkPlus, LineChart } from 'lucide-react'
import { Section, SectionHeader } from '@/components/landing/Section'

const strategies = [
  {
    title: 'Discover Opportunities',
    description: 'Find companies that match your investing philosophy through intuitive discovery tools rather than overwhelming screeners.',
    icon: Search,
    color: 'bg-emerald-500',
  },
  {
    title: 'Filter the Noise',
    description: 'Quickly eliminate companies with poor fundamentals or high risk profiles using preset health checks.',
    icon: Filter,
    color: 'bg-purple-500',
  },
  {
    title: 'Curate a Watchlist',
    description: 'Save high-conviction ideas to a dedicated watchlist that behaves like a focused decision queue.',
    icon: BookmarkPlus,
    color: 'bg-[#00c2ff]',
  },
  {
    title: 'Track Performance',
    description: 'Monitor your portfolio with clear, actionable insights that help you stay disciplined during market volatility.',
    icon: LineChart,
    color: 'bg-amber-500',
  },
]

export function HowItWorks() {
  return (
    <Section id="how-it-works">
      <SectionHeader
        eyebrow="The Workflow"
        title="A disciplined approach to investing"
        description="Learn to evaluate companies like an owner with our simplified 4-pillar methodology."
      />

      <div className="mx-auto mt-14 max-w-5xl">
        <div className="grid grid-cols-1 gap-6 md:grid-cols-2">
          {strategies.map((s, idx) => (
            <motion.div
              key={s.title}
              className="group relative flex overflow-hidden rounded-2xl bg-[#0a0e1a] border border-white/5 transition-all duration-300 hover:-translate-y-1 hover:shadow-xl hover:shadow-black/50"
              initial={{ opacity: 0, y: 18 }}
              whileInView={{ opacity: 1, y: 0 }}
              viewport={{ once: true, amount: 0.25 }}
              transition={{ duration: 0.45, delay: idx * 0.1 }}
            >
              {/* Left accent border */}
              <div className={`w-1.5 shrink-0 ${s.color} transition-opacity opacity-70 group-hover:opacity-100`} />
              
              <div className="flex p-6 sm:p-8">
                <div className="mr-6 shrink-0">
                  <div className={`flex h-12 w-12 items-center justify-center rounded-xl bg-slate-900 ring-1 ring-white/10 text-white transition-all duration-300 group-hover:bg-white/5`}>
                    <s.icon className="h-6 w-6" />
                  </div>
                </div>
                <div>
                  <h3 className="text-xl font-semibold text-white tracking-tight mb-2">{s.title}</h3>
                  <p className="text-slate-400 leading-relaxed text-sm sm:text-base">
                    {s.description}
                  </p>
                </div>
              </div>
            </motion.div>
          ))}
        </div>
      </div>
    </Section>
  )
}

