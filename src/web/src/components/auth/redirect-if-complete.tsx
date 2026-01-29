import { Navigate } from 'react-router-dom'
import { useAuthStore } from '@/stores/auth-store'
import { useOnboardingStore } from '@/stores/onboarding-store'
import { Loader2 } from 'lucide-react'

interface RedirectIfCompleteProps {
  children: React.ReactNode
}

/**
 * Guard component that redirects authenticated users with completed profiles
 * away from the onboarding flow. This prevents users who have already completed
 * onboarding from accidentally re-entering the flow.
 */
export function RedirectIfComplete({ children }: RedirectIfCompleteProps) {
  const { isAuthenticated, isLoading, user } = useAuthStore()
  // Use a selector to get reactive state, not a function reference
  const hasPendingData = useOnboardingStore((state) => state.data.basics !== null)

  // Show loading while checking auth state
  if (isLoading) {
    return (
      <div className="flex min-h-screen items-center justify-center">
        <Loader2 className="size-8 animate-spin text-muted-foreground" />
      </div>
    )
  }

  // If user is authenticated AND has a profile AND has no pending onboarding data,
  // redirect them away from onboarding
  if (isAuthenticated && user?.hasProfile && !hasPendingData) {
    return <Navigate to="/" replace />
  }

  // Allow access to onboarding in all other cases:
  // - Not authenticated (new users)
  // - Authenticated but no profile (need to complete setup)
  // - Has pending onboarding data (in the middle of the flow)
  return <>{children}</>
}
