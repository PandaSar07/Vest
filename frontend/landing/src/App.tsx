import { useLayoutEffect, useRef } from 'react'
import { gsap } from 'gsap'
import { ScrollTrigger } from 'gsap/ScrollTrigger'
import { Hero } from '@/sections/Hero'
import { Features } from '@/sections/Features'
import { HowItWorks } from '@/sections/HowItWorks'
import { Benefits } from '@/sections/Benefits'
import { SocialProof } from '@/sections/SocialProof'
import { FinalCta } from '@/sections/FinalCta'
import { BlankDeveloperSection } from '@/sections/BlankDeveloperSection'

gsap.registerPlugin(ScrollTrigger)

function App() {
  const rootRef = useRef<HTMLDivElement | null>(null)

  useLayoutEffect(() => {
    if (!rootRef.current || window.matchMedia('(prefers-reduced-motion: reduce)').matches) return

    const ctx = gsap.context(() => {
      // GSAP: cinematic progressive reveals
      // - We animate *each* element individually so the page feels paced and deliberate.
      // - Using toggleActions gives a premium "reveal as you scroll" behavior.
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

      // GSAP: background parallax (slower than foreground) for depth.
      gsap.utils.toArray<HTMLElement>('.hero-parallax').forEach((el) => {
        gsap.to(el, {
          yPercent: 18,
          ease: 'none',
          scrollTrigger: {
            trigger: '.hero-wrap',
            start: 'top top',
            end: 'bottom top',
            scrub: true,
          },
        })
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
    <div ref={rootRef} className="bg-[#060a12] text-slate-100">
      <div className="pointer-events-none fixed left-6 top-1/2 hidden h-40 w-px -translate-y-1/2 bg-white/10 md:block">
        <div data-scroll-progress className="h-full w-full origin-top scale-y-0 bg-sky-300/70" />
      </div>
      <div className="pointer-events-none fixed inset-0 -z-10 bg-[radial-gradient(circle_at_15%_10%,rgba(56,189,248,0.12),transparent_40%),radial-gradient(circle_at_85%_75%,rgba(168,85,247,0.10),transparent_40%)]" />

      <div className="mx-auto w-full max-w-7xl px-6 pb-24 pt-10 md:pt-14">
        <Hero />
        <Features />
        <HowItWorks />
        <BlankDeveloperSection />
        <Benefits />
        <SocialProof />
        <FinalCta />
      </div>
    </div>
  )
}

export default App
