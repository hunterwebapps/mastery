import { Navigate, useLocation } from 'react-router-dom'
import { useAuthStore } from '@/stores/auth-store'
import { Loader2 } from 'lucide-react'

interface RequireAuthProps {
  children: React.ReactNode
}

export function RequireAuth({ children }: RequireAuthProps) {
  const { isAuthenticated, isLoading, user } = useAuthStore()
  const location = useLocation()

  // Show loading while checking auth state
  if (isLoading) {
    return (
      <div className="flex min-h-screen items-center justify-center">
        <Loader2 className="size-8 animate-spin text-muted-foreground" />
      </div>
    )
  }

  // Not authenticated - go to onboarding (which includes registration)
  if (!isAuthenticated) {
    return <Navigate to="/onboarding" state={{ from: location }} replace />
  }

  // Authenticated but no profile - should not happen with our flow
  // but handle it gracefully
  if (user && !user.hasProfile) {
    return <Navigate to="/onboarding" state={{ from: location }} replace />
  }

  return <>{children}</>
}
