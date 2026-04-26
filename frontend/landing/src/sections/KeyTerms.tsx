import { motion } from 'framer-motion'
import { Section, SectionHeader } from '@/components/landing/Section'

const terms = [
  { term: 'Market Cap', def: 'The total value of all a company\'s shares of stock.' },
  { term: 'P/E Ratio', def: 'The ratio of a company\'s share price to its earnings per share.' },
  { term: 'Dividend Yield', def: 'How much a company pays out in dividends relative to its stock price.' },
  { term: 'Blue Chip', def: 'A huge company with an excellent reputation.' },
  { term: 'Bear Market', def: 'When a market experiences prolonged price declines.' },
  { term: 'Bull Market', def: 'When a market experiences prolonged price increases.' },
  { term: 'ETF', def: 'A basket of securities that trades on an exchange just like a stock.' },
  { term: 'Liquidity', def: 'How easily an asset can be bought or sold without affecting its price.' }
]

export function KeyTerms() {
  return (
    <Section id="jargon">
      <SectionHeader
        eyebrow="Jargon Buster"
        title="We speak your language"
        description="Don't let Wall Street terminology intimidate you. Here is a quick decoder ring."
      />

      <motion.div
        initial="hidden"
        whileInView="show"
        viewport={{ once: true, amount: 0.2 }}
        variants={{ show: { transition: { staggerChildren: 0.05 } } }}
        className="mx-auto mt-14 flex max-w-4xl flex-wrap justify-center gap-3 md:gap-4"
      >
        {terms.map((t) => (
          <motion.div
            key={t.term}
            variants={{
              hidden: { opacity: 0, scale: 0.9 },
              show: { opacity: 1, scale: 1 }
            }}
            whileHover={{ scale: 1.05 }}
            className="group relative cursor-pointer overflow-hidden rounded-full border border-white/10 bg-[#0a0e1a]/80 px-5 py-2.5 backdrop-blur-md transition-all hover:border-[#00c2ff]/50 hover:bg-[#00c2ff]/5 hover:shadow-[0_0_15px_rgba(0,194,255,0.2)]"
          >
            <div className="flex items-center gap-2">
              <span className="font-semibold tracking-tight text-slate-200 group-hover:text-[#00c2ff] transition-colors">{t.term}</span>
              <span className="h-1 w-1 rounded-full bg-slate-600 transition-colors group-hover:bg-[#00c2ff]"></span>
              <span className="text-sm text-slate-400 group-hover:text-slate-300 transition-colors hidden sm:inline-block">
                {t.def}
              </span>
            </div>
            
            {/* Mobile tooltip-like behavior since hover doesn't show default on mobile as well */}
            <div className="sm:hidden text-xs text-slate-400 mt-1 max-w-[200px]">
              {t.def}
            </div>
          </motion.div>
        ))}
      </motion.div>
    </Section>
  )
}
