import { motion } from 'framer-motion'
import { Section, SectionHeader } from '@/components/landing/Section'

const processSteps = [
  { step: '01', title: 'Sign Up', desc: 'Create your free account.' },
  { step: '02', title: 'Explore', desc: 'Browse the market database.' },
  { step: '03', title: 'Learn', desc: 'Study the essential metrics.' },
  { step: '04', title: 'Invest', desc: 'Put your knowledge into practice.' }
]

export function StepProcess() {
  return (
    <Section id="process">
      <SectionHeader
        eyebrow="Getting Started"
        title="Your journey to mastery"
        description="Four simple steps from complete beginner to confident investor."
      />

      <div className="mx-auto mt-20 max-w-5xl px-4">
        <div className="relative flex flex-col md:flex-row justify-between items-start md:items-center">
          
          {/* Animated Connecting Line (Desktop) */}
          <div className="hidden md:block absolute top-[2.5rem] left-[5%] w-[90%] h-px bg-white/10 -z-10">
            <motion.div 
              className="h-full bg-gradient-to-r from-[#00c2ff]/30 via-[#00c2ff] to-[#00c2ff]/30"
              initial={{ scaleX: 0, originX: 0 }}
              whileInView={{ scaleX: 1 }}
              viewport={{ once: true, amount: 0.8 }}
              transition={{ duration: 1.5, ease: "easeInOut" }}
            />
          </div>

          {processSteps.map((s, i) => (
             <motion.div
              key={s.step}
              className="relative flex flex-col items-center flex-1 w-full text-center group mb-12 md:mb-0"
              initial={{ opacity: 0, y: 20 }}
              whileInView={{ opacity: 1, y: 0 }}
              viewport={{ once: true, amount: 0.5 }}
              transition={{ duration: 0.5, delay: i * 0.2 }}
             >
                {/* Connecting Line (Mobile) */}
                {i !== processSteps.length - 1 && (
                  <div className="md:hidden absolute top-[4rem] left-[50%] w-px h-24 bg-white/10 -z-10">
                     <motion.div 
                      className="w-full bg-[#00c2ff]"
                      initial={{ scaleY: 0, originY: 0 }}
                      whileInView={{ scaleY: 1 }}
                      viewport={{ once: true }}
                      transition={{ duration: 0.5, delay: i * 0.2 }}
                    />
                  </div>
                )}

               <div className="flex flex-col items-center">
                 <div className="mb-4 flex h-20 w-20 items-center justify-center rounded-full border border-white/10 bg-[#0a0e1a] text-2xl font-bold tracking-tight text-white transition-all duration-300 group-hover:border-[#00c2ff]/50 group-hover:shadow-[0_0_20px_-5px_rgba(0,194,255,0.4)]">
                   {s.step}
                 </div>
                 <h4 className="mb-2 text-lg font-semibold text-white">{s.title}</h4>
                 <p className="max-w-[200px] text-sm text-slate-400">{s.desc}</p>
               </div>
             </motion.div>
          ))}

        </div>
      </div>
    </Section>
  )
}
