import { motion } from 'framer-motion'
import { Building2, TrendingUp, HandCoins, ScrollText } from 'lucide-react'
import { Section, SectionHeader } from '@/components/landing/Section'

const characteristics = [
  {
    title: 'Pieces of a Business',
    description: 'When you buy a stock, you are buying a fractional ownership stake in a real company. If they succeed, you succeed.',
    icon: Building2,
    span: 'col-span-1 md:col-span-2 lg:col-span-1',
  },
  {
    title: 'Growth Potential',
    description: 'As the company increases its earnings and expands its market share, the value of your fractional stake typically rises over time.',
    icon: TrendingUp,
    span: 'col-span-1 md:col-span-2 lg:col-span-1',
  },
  {
    title: 'Share in the Profits',
    description: 'Many established companies distribute a portion of their profits back to shareholders as cash dividends.',
    icon: HandCoins,
    span: 'col-span-1 md:col-span-2 lg:col-span-1',
  },
  {
    title: 'Voting Rights',
    description: 'Owning shares often gives you a voice in major corporate decisions, like electing the board of directors.',
    icon: ScrollText,
    span: 'col-span-1 md:col-span-2 lg:col-span-1',
  },
]

export function Features() {
  return (
    <Section id="what-are-stocks" className="relative">
      <SectionHeader
        eyebrow="The Basics"
        title="What exactly are stocks?"
        description="Before diving into complex strategies, understand the fundamental building blocks of the market."
      />

      <motion.div
        initial="hidden"
        whileInView="show"
        viewport={{ once: true, amount: 0.2 }}
        variants={{ show: { transition: { staggerChildren: 0.08 } } }}
        className="mx-auto mt-14 grid max-w-5xl grid-cols-1 gap-6 md:grid-cols-2"
      >
        {characteristics.map((it) => (
          <motion.div
            key={it.title}
            variants={{ hidden: { opacity: 0, y: 20 }, show: { opacity: 1, y: 0 } }}
            className={`group relative overflow-hidden rounded-3xl border border-white/10 bg-[#0a0e1a]/40 p-8 backdrop-blur-xl transition-all duration-300 hover:border-[#00c2ff]/30 hover:bg-[#00c2ff]/[0.02] hover:shadow-[0_0_30px_-5px_rgba(0,194,255,0.15)] ${it.span}`}
          >
            <div className="absolute -right-12 -top-12 h-32 w-32 rounded-full bg-[#00c2ff]/10 blur-[40px] transition-all duration-500 group-hover:bg-[#00c2ff]/20" />
            <div className="relative z-10">
              <div className="mb-6 inline-flex h-12 w-12 items-center justify-center rounded-2xl bg-[#00c2ff]/10 text-[#00c2ff] ring-1 ring-[#00c2ff]/20">
                <it.icon className="h-6 w-6" />
              </div>
              <h3 className="mb-3 text-xl font-semibold text-white tracking-tight">{it.title}</h3>
              <p className="text-base leading-relaxed text-slate-400">{it.description}</p>
            </div>
          </motion.div>
        ))}
      </motion.div>
    </Section>
  )
}

