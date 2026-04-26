import { motion } from 'framer-motion'
import { Button } from '@/components/ui/button'

export function FinalCta() {
  return (
    <section id="cta" className="relative w-full border-t border-white/5 bg-gradient-to-b from-[#0a0e1a]/10 to-[#00c2ff]/10 overflow-hidden">
      <div className="pointer-events-none absolute inset-0 bg-[radial-gradient(circle_at_50%_0%,rgba(0,194,255,0.15),transparent_60%)]" />
      
      <div className="mx-auto max-w-4xl px-6 py-24 text-center relative z-10">
        <motion.div
          initial={{ opacity: 0, y: 20 }}
          whileInView={{ opacity: 1, y: 0 }}
          viewport={{ once: true, amount: 0.3 }}
          transition={{ duration: 0.6 }}
        >
          <div className="mb-6 inline-flex rounded-full border border-[#00c2ff]/20 bg-[#00c2ff]/10 px-4 py-1.5 text-sm font-semibold tracking-wide text-[#00c2ff]">
            Ready to start learning?
          </div>
          
          <h2 className="text-balance text-4xl font-bold tracking-tight text-white md:text-5xl lg:text-6xl">
            Your investing journey begins here.
          </h2>
          
          <p className="mx-auto mt-6 max-w-2xl text-lg text-slate-400">
            Join thousands of others who are stepping into the market with confidence and discipline. No jargon, just results.
          </p>
          
          <div className="mt-10 flex flex-col sm:flex-row items-center justify-center gap-4">
            <Button asChild size="lg" className="w-full sm:w-auto bg-[#00c2ff] text-[#0a0e1a] hover:bg-[#00c2ff]/80 shadow-[0_0_20px_rgba(0,194,255,0.3)]">
              <a href="/Home/Signup">Create free account</a>
            </Button>
            <Button asChild variant="ghost" size="lg" className="w-full sm:w-auto text-slate-300 hover:text-white border border-white/10 hover:bg-white/5">
              <a href="/Stocks/Index">Explore markets</a>
            </Button>
          </div>
        </motion.div>
      </div>
    </section>
  )
}

