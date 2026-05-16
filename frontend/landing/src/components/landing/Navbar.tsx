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
            <div className="flex h-9 w-9 shrink-0 items-center justify-center rounded-lg bg-[#00c2ff]/10 border border-[#00c2ff]/20 transition-all group-hover:bg-[#00c2ff]/20 group-hover:shadow-[0_0_14px_rgba(0,194,255,0.25)]">
              <img
                src="/images/vest-mark.svg"
                alt=""
                width={23}
                height={22}
                className="h-[22px] w-[23px] object-contain drop-shadow-[0_0_10px_rgba(0,194,255,0.35)]"
              />
            </div>
            <span
              className="text-[1.35rem] font-extrabold tracking-[0.12em] uppercase bg-gradient-to-r from-[#00c2ff] to-[#c084fc] bg-clip-text text-transparent"
              style={{ fontFamily: "'Inter', system-ui, sans-serif" }}
            >
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
            <a
              href="/Home/Log"
              className="nav-link hidden rounded-full px-5 py-2 text-[0.85rem] font-medium text-slate-400 transition-all duration-300 hover:text-white hover:bg-white/[0.06] hover:backdrop-blur-sm md:inline-flex"
              style={{ fontFamily: "'Inter', system-ui, sans-serif", letterSpacing: '0.02em' }}
            >
              Log in
            </a>
            <a
              href="/Home/Signup"
              className="nav-link rounded-full px-5 py-2 text-[0.85rem] font-medium text-slate-400 transition-all duration-300 hover:text-white hover:bg-white/[0.06] hover:backdrop-blur-sm"
              style={{ fontFamily: "'Inter', system-ui, sans-serif", letterSpacing: '0.02em' }}
            >
              Sign up
            </a>
          </div>
        </div>
      </header>
    </>
  )
}
