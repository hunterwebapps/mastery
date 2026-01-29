import { useEffect } from 'react'
import { Outlet, useNavigate } from 'react-router-dom'
import { useProfile } from '@/features/profile/hooks/use-profile'
import { MainNav } from '@/components/navigation'

export function RootLayout() {
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
        <MainNav />
      </header>
      <main className="mx-auto max-w-7xl px-4 py-8">
        <Outlet />
      </main>
    </div>
  )
}
