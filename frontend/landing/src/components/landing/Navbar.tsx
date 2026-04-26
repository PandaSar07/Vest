import { Rocket } from 'lucide-react'
import { Button } from '@/components/ui/button'

export function Navbar() {
  return (
    <>
      <style>{`
        @keyframes glow-pulse {
          0%, 100% { box-shadow: 0 0 16px rgba(0,194,255,0.25), inset 0 1px 0 rgba(255,255,255,0.1); }
          50% { box-shadow: 0 0 28px rgba(0,194,255,0.45), inset 0 1px 0 rgba(255,255,255,0.15); }
        }
        .nav-link {
          position: relative;
          overflow: hidden;
        }
        .signup-btn {
          animation: glow-pulse 3s ease-in-out infinite;
        }
        .signup-btn:hover {
          animation: none;
        }
      `}</style>
      <header className="sticky top-0 z-50 w-full border-b border-white/5 bg-[#0a0e1a]/80 backdrop-blur-xl">
        <div className="mx-auto flex h-[72px] max-w-7xl items-center justify-between px-6">
          {/* Brand */}
          <a href="/" className="flex items-center gap-2.5 group">
            <div className="flex h-9 w-9 items-center justify-center rounded-lg bg-[#00c2ff]/10 border border-[#00c2ff]/20 transition-all group-hover:bg-[#00c2ff]/20 group-hover:shadow-[0_0_14px_rgba(0,194,255,0.25)]">
              <Rocket className="h-5 w-5 text-[#00c2ff]" />
            </div>
            <span className="text-[1.35rem] font-extrabold tracking-[0.12em] text-white uppercase" style={{ fontFamily: "'Inter', system-ui, sans-serif" }}>
              VEST
            </span>
          </a>
          
          {/* Nav Links */}
          <nav className="hidden items-center gap-0.5 md:flex">
            {[
              { label: 'Home', href: '/' },
              { label: 'About', href: '/Home/About' },
              { label: 'Contact', href: '/Home/Contact' },
              { label: 'Privacy', href: '/Home/Privacy' },
            ].map((link) => (
              <a
                key={link.label}
                href={link.href}
                className="nav-link rounded-full px-5 py-2 text-[0.85rem] font-medium text-slate-400 transition-all duration-300 hover:text-white hover:bg-white/[0.06] hover:backdrop-blur-sm"
                style={{ fontFamily: "'Inter', system-ui, sans-serif", letterSpacing: '0.02em' }}
              >
                {link.label}
              </a>
            ))}
          </nav>
          
          {/* Auth Buttons */}
          <div className="flex items-center gap-3">
            <Button variant="ghost" asChild className="hidden rounded-full border border-white/[0.08] px-5 py-2 text-[0.85rem] font-medium text-slate-300 hover:text-white hover:bg-white/[0.06] hover:border-white/[0.15] transition-all duration-300 md:inline-flex" style={{ fontFamily: "'Inter', system-ui, sans-serif", letterSpacing: '0.02em' }}>
              <a href="/Home/Log">Log in</a>
            </Button>
            <Button asChild className="signup-btn rounded-full bg-gradient-to-r from-[#00d4ff] to-[#00a8e0] px-6 py-2 text-[0.85rem] font-semibold text-[#0a0e1a] hover:from-[#33ddff] hover:to-[#00b8f0] hover:shadow-[0_0_32px_rgba(0,194,255,0.5)] transition-all duration-300" style={{ fontFamily: "'Inter', system-ui, sans-serif", letterSpacing: '0.02em' }}>
              <a href="/Home/Signup">Sign up</a>
            </Button>
          </div>
        </div>
      </header>
    </>
  )
}
