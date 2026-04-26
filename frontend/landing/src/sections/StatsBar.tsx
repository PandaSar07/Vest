import { motion } from 'framer-motion'
import { Section } from '@/components/landing/Section'

export function StatsBar() {
  const stats = [
    { label: "Active learners", value: "10,000+" },
    { label: "Stocks tracked", value: "500+" },
    { label: "Community", value: "Global" },
    { label: "Pricing", value: "Free to start" }
  ]

  return (
    <Section id="stats" className="py-6 border-y border-white/5 bg-white/[0.02]">
      <div className="mx-auto max-w-7xl px-6">
        <div className="grid grid-cols-2 gap-y-8 md:grid-cols-4 divide-y divide-white/5 md:divide-y-0 md:divide-x">
          {stats.map((stat, i) => (
            <motion.div
              key={stat.label}
              initial={{ opacity: 0, y: 10 }}
              whileInView={{ opacity: 1, y: 0 }}
              viewport={{ once: true }}
              transition={{ delay: i * 0.1, duration: 0.5 }}
              className="flex flex-col items-center justify-center p-4 text-center"
            >
              <div className="text-3xl font-bold tracking-tight text-white mb-1">
                {stat.value}
              </div>
              <div className="text-sm font-medium text-slate-500 uppercase tracking-wider">
                {stat.label}
              </div>
            </motion.div>
          ))}
        </div>
      </div>
    </Section>
  )
}
