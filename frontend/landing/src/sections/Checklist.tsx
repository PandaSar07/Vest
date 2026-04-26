import { useState } from 'react'
import { motion } from 'framer-motion'
import { Section, SectionHeader } from '@/components/landing/Section'
import { Check } from 'lucide-react'

const checklistItems = [
  'Understand what gives a stock value',
  'Learn how to read high-level financials',
  'Identify competitive advantages (Moats)',
  'Build a risk-first portfolio strategy',
  'Track investments without emotional bias'
]

export function Checklist() {
  const [checkedItems, setCheckedItems] = useState<number[]>([0])

  const toggleItem = (idx: number) => {
    if (checkedItems.includes(idx)) {
      setCheckedItems(checkedItems.filter((i) => i !== idx))
    } else {
      setCheckedItems([...checkedItems, idx])
    }
  }

  return (
    <Section id="checklist">
      <SectionHeader
        eyebrow="Readiness"
        title="Your pre-flight checklist"
        description="Master the fundamentals before risking real capital in the markets."
      />

      <motion.div
        initial="hidden"
        whileInView="show"
        viewport={{ once: true, amount: 0.2 }}
        className="mx-auto mt-14 max-w-2xl relative"
      >
        <div className="absolute -inset-1 rounded-3xl bg-gradient-to-b from-[#00c2ff]/30 to-fuchsia-500/10 blur-xl opacity-60" />
        
        <div className="relative rounded-3xl border border-white/10 bg-[#0a0e1a]/90 backdrop-blur-xl p-8 shadow-2xl">
          <div className="space-y-4">
            {checklistItems.map((item, idx) => {
              const isChecked = checkedItems.includes(idx)
              
              return (
                <div 
                  key={idx}
                  onClick={() => toggleItem(idx)}
                  className="group flex cursor-pointer items-center gap-4 rounded-xl border border-transparent p-3 transition-colors hover:bg-white/5 hover:border-white/5"
                >
                  <div className={`relative flex h-7 w-7 shrink-0 items-center justify-center rounded-md border transition-all duration-300 ${isChecked ? 'border-[#00c2ff] bg-[#00c2ff]/20' : 'border-slate-600 bg-slate-800 group-hover:border-[#00c2ff]/50'}`}>
                    <motion.div
                      initial={false}
                      animate={{ scale: isChecked ? 1 : 0, opacity: isChecked ? 1 : 0 }}
                      transition={{ type: 'spring', stiffness: 400, damping: 25 }}
                    >
                      <Check className="h-4 w-4 text-[#00c2ff]" strokeWidth={3} />
                    </motion.div>
                  </div>
                  <span className={`text-base transition-colors duration-300 ${isChecked ? 'text-white' : 'text-slate-400 group-hover:text-slate-300'}`}>
                    {item}
                  </span>
                </div>
              )
            })}
          </div>

          <div className="mt-8 pt-6 border-t border-white/10 text-center">
            <p className="text-sm text-slate-400">
              <strong className="text-white">{checkedItems.length} of {checklistItems.length}</strong> core concepts mastered. Keep going.
            </p>
          </div>
        </div>
      </motion.div>
    </Section>
  )
}
