import { useEffect } from 'react'
import { Link, Outlet, useLocation, useNavigate } from 'react-router-dom'
import { cn } from '@/lib/utils'
import { useProfile } from '@/features/profile/hooks/use-profile'

const navLinks = [
  { href: '/', label: 'Dashboard' },
  { href: '/goals', label: 'Goals' },
  { href: '/projects', label: 'Projects' },
  { href: '/tasks', label: 'Tasks' },
  { href: '/habits', label: 'Habits' },
  { href: '/experiments', label: 'Experiments' },
  { href: '/recommendations', label: 'Recommendations' },
  { href: '/check-in', label: 'Check-in' },
  { href: '/profile', label: 'Profile' },
]

export function RootLayout() {
  const location = useLocation()
  const navigate = useNavigate()
  const { data: profile, isLoading, isFetched } = useProfile()

  // Redirect to onboarding if no profile exists
  useEffect(() => {
    if (isFetched && profile === null) {
      navigate('/onboarding', { replace: true })
    }
  }, [isFetched, profile, navigate])

  // Show loading while checking profile
  if (isLoading || (isFetched && profile === null)) {
    return (
      <div className="min-h-screen bg-background flex items-center justify-center">
        <div className="text-center space-y-3">
          <div className="text-xl font-bold text-primary">Mastery</div>
          <div className="animate-pulse text-muted-foreground">Loading...</div>
        </div>
      </div>
    )
  }

  return (
    <div className="min-h-screen bg-background text-foreground">
      <header className="border-b border-border bg-card">
        <nav className="mx-auto flex max-w-7xl items-center justify-between px-4 py-4">
          <Link to="/" className="text-xl font-bold text-primary">
            Mastery
          </Link>
          <div className="flex gap-6">
            {navLinks.map((link) => {
              const isActive = location.pathname === link.href
              return (
                <Link
                  key={link.href}
                  to={link.href}
                  className={cn(
                    'text-sm font-medium transition-colors hover:text-primary',
                    isActive
                      ? 'text-primary'
                      : 'text-muted-foreground'
                  )}
                >
                  {link.label}
                </Link>
              )
            })}
          </div>
        </nav>
      </header>
      <main className="mx-auto max-w-7xl px-4 py-8">
        <Outlet />
      </main>
    </div>
  )
}
