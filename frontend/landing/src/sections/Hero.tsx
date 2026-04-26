import { motion } from 'framer-motion'
import { ArrowRight, TrendingUp } from 'lucide-react'
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from '@/components/ui/dialog'

export function Hero() {
  return (
    <section className="relative w-full overflow-hidden pt-12 pb-20 md:pt-24 md:pb-32">
      {/* Gray grid background */}
      <div className="absolute inset-0 -z-10" style={{
        backgroundImage: `linear-gradient(rgba(148,163,184,0.07) 1px, transparent 1px),
                          linear-gradient(90deg, rgba(148,163,184,0.07) 1px, transparent 1px)`,
        backgroundSize: '48px 48px',
        maskImage: 'radial-gradient(ellipse 80% 70% at 50% 0%, black 40%, transparent 100%)',
        WebkitMaskImage: 'radial-gradient(ellipse 80% 70% at 50% 0%, black 40%, transparent 100%)',
      }} />
      {/* Cyan glow blob */}
      <div className="absolute top-0 left-1/2 -z-10 h-[600px] w-[800px] -translate-x-1/2 rounded-full bg-[#00c2ff]/10 blur-[120px]" />
      
      <div className="mx-auto grid max-w-7xl grid-cols-1 gap-12 px-6 lg:grid-cols-2 lg:gap-8 items-center">
        <motion.div
          initial={{ opacity: 0, x: -20 }}
          animate={{ opacity: 1, x: 0 }}
          transition={{ duration: 0.8, ease: 'easeOut' }}
          className="flex flex-col items-start"
        >
          <div className="mb-4 rounded-full border border-[#00c2ff]/20 bg-[#00c2ff]/10 px-4 py-1.5 text-sm font-medium tracking-wide text-[#00c2ff]">
            A new era of market intelligence
          </div>

          <div className="mb-1">
            <span className="text-6xl font-black tracking-[0.25em] uppercase sm:text-7xl md:text-8xl bg-gradient-to-r from-[#00c2ff] via-white to-[#00c2ff] bg-clip-text text-transparent drop-shadow-[0_0_40px_rgba(0,194,255,0.35)]" style={{ fontFamily: "'Inter', system-ui, sans-serif" }}>
              VEST
            </span>
          </div>
          
          <h1 className="text-4xl font-bold tracking-tight sm:text-5xl md:text-6xl" style={{ background: 'none', WebkitBackgroundClip: 'unset', backgroundClip: 'unset', WebkitTextFillColor: '#ffffff', color: '#ffffff', fontFamily: "'Inter', system-ui, sans-serif" }}>
            Learn investing<br/>without the jargon.
          </h1>
          
          <p className="mt-4 max-w-lg text-lg text-slate-400" style={{ fontFamily: "'Inter', system-ui, sans-serif" }}>
            Vest is the premium platform for mastering stocks and crypto. We strip away the noise and give you the signal.
          </p>

          {/* Hero CTA buttons — Antigravity style */}
          <style>{`
            @keyframes shimmer-sweep {
              0%   { transform: translateX(-130%) skewX(-12deg); }
              100% { transform: translateX(230%) skewX(-12deg); }
            }
            .hero-cta-primary {
              position: relative;
              display: inline-flex;
              align-items: center;
              gap: 0.5rem;
              padding: 0.85rem 2rem;
              border-radius: 9999px;
              font-weight: 700;
              font-size: 0.95rem;
              letter-spacing: 0.03em;
              color: #0a0e1a;
              background: linear-gradient(135deg, #33ddff 0%, #00c2ff 45%, #0099cc 100%);
              box-shadow:
                0 0 0 1px rgba(0,194,255,0.5),
                0 4px 16px rgba(0,194,255,0.35),
                0 12px 40px rgba(0,194,255,0.2),
                inset 0 1px 0 rgba(255,255,255,0.35);
              transition: transform 0.3s cubic-bezier(0.34,1.56,0.64,1), opacity 0.3s ease;
              will-change: transform;
              overflow: hidden;
              cursor: pointer;
              text-decoration: none;
            }
            .hero-cta-primary::before {
              content: '';
              position: absolute;
              inset: 0;
              background: linear-gradient(135deg, #55eeff 0%, #00c2ff 50%, #0088bb 100%);
              opacity: 0;
              transition: opacity 0.3s ease;
              border-radius: inherit;
            }
            .hero-cta-primary::after {
              content: '';
              position: absolute;
              top: 0; bottom: 0;
              left: 0; width: 45%;
              background: linear-gradient(90deg, transparent, rgba(255,255,255,0.38), transparent);
              animation: shimmer-sweep 3.2s ease-in-out infinite;
              pointer-events: none;
            }
            .hero-cta-primary:hover {
              transform: translateY(-3px) scale(1.03);
            }
            .hero-cta-primary:hover::before { opacity: 1; }
            .hero-cta-primary:active { transform: translateY(-1px) scale(1.01); }

            .hero-cta-secondary {
              position: relative;
              display: inline-flex;
              align-items: center;
              gap: 0.5rem;
              padding: 0.85rem 1.75rem;
              border-radius: 9999px;
              font-weight: 600;
              font-size: 0.95rem;
              letter-spacing: 0.03em;
              color: rgba(226,232,240,0.9);
              background: rgba(255,255,255,0.04);
              border: 1px solid rgba(255,255,255,0.12);
              backdrop-filter: blur(12px);
              -webkit-backdrop-filter: blur(12px);
              box-shadow:
                inset 0 1px 0 rgba(255,255,255,0.08),
                0 2px 12px rgba(0,0,0,0.25);
              transition: transform 0.3s cubic-bezier(0.34,1.56,0.64,1), background 0.3s ease, border-color 0.3s ease, color 0.3s ease;
              will-change: transform;
              cursor: pointer;
              text-decoration: none;
            }
            .hero-cta-secondary:hover {
              transform: translateY(-2px) scale(1.02);
              background: rgba(255,255,255,0.08);
              border-color: rgba(0,194,255,0.35);
              color: #fff;
            }
            .hero-cta-secondary:active { transform: translateY(0) scale(1); }
          `}</style>

          <div className="mt-8 flex flex-wrap items-center gap-4">
            {/* Primary CTA */}
            <a href="/Home/Signup" className="hero-cta-primary" style={{ fontFamily: "'Inter', system-ui, sans-serif" }}>
              <span style={{ position: 'relative', zIndex: 1 }}>Start for free</span>
              <ArrowRight style={{ position: 'relative', zIndex: 1, width: '1rem', height: '1rem' }} />
            </a>

            {/* Secondary ghost CTA */}
            <a href="/Home/Log" className="hero-cta-secondary" style={{ fontFamily: "'Inter', system-ui, sans-serif" }}>
              Log in
            </a>

            <Dialog>
              <DialogTrigger asChild>
                <button className="hero-cta-secondary" style={{ fontFamily: "'Inter', system-ui, sans-serif", border: 'none', background: 'transparent', padding: '0.85rem 0', color: 'rgba(148,163,184,0.8)', cursor: 'pointer', fontSize: '0.9rem' }}>
                  See how it works
                </button>
              </DialogTrigger>
              <DialogContent className="border-white/10 bg-[#0a0e1a] text-white">
                <DialogHeader>
                  <DialogTitle className="text-[#00c2ff]">Designed for calm momentum</DialogTitle>
                  <DialogDescription className="text-slate-400">
                    This landing uses staged sections, parallax, and pinned chapters so the experience unfolds over scroll—like a high-end product reveal.
                  </DialogDescription>
                </DialogHeader>
              </DialogContent>
            </Dialog>
          </div>
        </motion.div>

        <motion.div
          initial={{ opacity: 0, scale: 0.95 }}
          animate={{ opacity: 1, scale: 1 }}
          transition={{ duration: 0.9, delay: 0.2, ease: 'easeOut' }}
          className="relative lg:ml-auto w-full max-w-md perspective-1000"
        >
          <div className="absolute -inset-4 bg-gradient-to-r from-[#00c2ff]/20 to-fuchsia-500/20 blur-2xl opacity-50 rounded-2xl" />
          
          <motion.div
            animate={{ y: [-10, 10, -10] }}
            transition={{ repeat: Infinity, duration: 6, ease: 'easeInOut' }}
            className="relative rounded-2xl border border-white/10 bg-[#0a0e1a]/80 p-6 backdrop-blur-xl shadow-2xl overflow-hidden"
          >
            <div className="flex items-center justify-between border-b border-white/5 pb-4">
              <div className="flex items-center gap-3">
                <div className="flex h-10 w-10 items-center justify-center rounded-xl bg-[#00c2ff]/20 text-[#00c2ff]">
                  <TrendingUp className="h-5 w-5" />
                </div>
                <div>
                  <h3 className="font-semibold text-white">AAPL</h3>
                  <p className="text-xs text-slate-500">Apple Inc.</p>
                </div>
              </div>
              <div className="text-right">
                <p className="font-mono font-medium text-white">$184.92</p>
                <p className="font-mono text-xs text-green-400">+1.24%</p>
              </div>
            </div>

            <div className="mt-8 h-32 w-full">
              {/* Minimal SVG chart for depth */}
              <svg viewBox="0 0 400 100" className="w-full h-full overflow-visible" preserveAspectRatio="none">
                <defs>
                  <linearGradient id="chart-grad" x1="0" y1="0" x2="0" y2="1">
                    <stop offset="0%" stopColor="#00c2ff" stopOpacity="0.3"/>
                    <stop offset="100%" stopColor="#00c2ff" stopOpacity="0"/>
                  </linearGradient>
                </defs>
                <path d="M0,80 C40,70 80,90 120,60 C160,30 200,50 240,40 C280,30 320,10 360,20 L400,10 L400,100 L0,100 Z" fill="url(#chart-grad)" />
                <path d="M0,80 C40,70 80,90 120,60 C160,30 200,50 240,40 C280,30 320,10 360,20 L400,10" fill="none" stroke="#00c2ff" strokeWidth="3" vectorEffect="non-scaling-stroke" />
              </svg>
            </div>
          </motion.div>
        </motion.div>
      </div>
    </section>
  )
}

