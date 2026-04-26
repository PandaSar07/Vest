import { useLayoutEffect, useRef } from 'react'
import { gsap } from 'gsap'
import { ScrollTrigger } from 'gsap/ScrollTrigger'
import { Navbar } from '@/components/landing/Navbar'
import { Hero } from '@/sections/Hero'
import { Features } from '@/sections/Features'
import { HowItWorks } from '@/sections/HowItWorks'
import { StepProcess } from '@/sections/StepProcess'
import { KeyTerms } from '@/sections/KeyTerms'
import { Checklist } from '@/sections/Checklist'
import { SocialProof } from '@/sections/SocialProof'
import { FinalCta } from '@/sections/FinalCta'

gsap.registerPlugin(ScrollTrigger)

function App() {
  const rootRef = useRef<HTMLDivElement | null>(null)

  useLayoutEffect(() => {
    if (!rootRef.current || window.matchMedia('(prefers-reduced-motion: reduce)').matches) return

    const ctx = gsap.context(() => {
      // GSAP: cinematic progressive reveals
      gsap.utils.toArray<HTMLElement>('.gsap-reveal').forEach((el) => {
        gsap.fromTo(
          el,
          { y: 26, opacity: 0, filter: 'blur(2px)' },
          {
            y: 0,
            opacity: 1,
            filter: 'blur(0px)',
            duration: 0.85,
            ease: 'power2.out',
            scrollTrigger: {
              trigger: el,
              start: 'top 86%',
              end: 'bottom 70%',
              toggleActions: 'play none none none',
              once: true,
            },
          },
        )
      })

      // GSAP: scroll progress cue (subtle, premium).
      const progress = document.querySelector<HTMLElement>('[data-scroll-progress]')
      if (progress) {
        gsap.to(progress, {
          scaleY: 1,
          ease: 'none',
          transformOrigin: 'top',
          scrollTrigger: {
            trigger: rootRef.current,
            start: 'top top',
            end: 'bottom bottom',
            scrub: true,
          },
        })
      }
    }, rootRef)

    return () => ctx.revert()
  }, [])

  return (
    <div ref={rootRef} className="bg-[#0a0e1a] text-slate-100 min-h-screen font-mono">
      <div className="pointer-events-none fixed left-6 top-1/2 hidden h-40 w-px -translate-y-1/2 bg-white/10 md:block z-50">
        <div data-scroll-progress className="h-full w-full origin-top scale-y-0 bg-[#00c2ff]/70" />
      </div>
      <div className="pointer-events-none fixed inset-0 -z-10 bg-[radial-gradient(circle_at_15%_10%,rgba(0,194,255,0.08),transparent_40%),radial-gradient(circle_at_85%_75%,rgba(0,194,255,0.03),transparent_40%)]" />

      <Navbar />

      <main className="flex flex-col gap-12 pb-24 pt-10 md:pt-14">
        <Hero />

        
        <div className="mx-auto w-full max-w-7xl px-6">
          <Features />
          <HowItWorks />
          <StepProcess />
          <KeyTerms />
          <Checklist />
          <SocialProof />
        </div>
        
        <FinalCta />
      </main>
    </div>
  )
}

export default App
