import { useEffect, useState } from 'react'
import { useNavigate, useSearchParams } from 'react-router-dom'
import { Loader2 } from 'lucide-react'
import { Alert, AlertDescription } from '@/components/ui/alert'
import { Button } from '@/components/ui/button'
import { useAuthStore } from '@/stores/auth-store'
import { useOnboardingStore, type OnboardingData } from '@/stores/onboarding-store'
import { createProfileApi } from '@/features/onboarding/api'

export function Component() {
  const navigate = useNavigate()
  const [searchParams] = useSearchParams()
  const [error, setError] = useState<string | null>(null)
  const [isProcessing, setIsProcessing] = useState(true)

  const { setAuth } = useAuthStore()
  const { data: storeData, reset, hasPendingData } = useOnboardingStore()

  useEffect(() => {
    const processCallback = async () => {
      // Extract tokens from URL params (sent by backend after OAuth)
      const accessToken = searchParams.get('token')
      const refreshToken = searchParams.get('refresh')
      const hasProfile = searchParams.get('hasProfile') === 'true'
      const userId = searchParams.get('userId')
      const email = searchParams.get('email')
      const displayName = searchParams.get('displayName')
      const authProvider = searchParams.get('provider') as 'Google' | 'Apple' | 'Microsoft'
      const errorParam = searchParams.get('error')

      // Handle OAuth error
      if (errorParam) {
        setError(decodeURIComponent(errorParam))
        setIsProcessing(false)
        return
      }

      // Validate required params
      if (!accessToken || !userId || !email) {
        setError('Invalid callback parameters. Please try again.')
        setIsProcessing(false)
        return
      }

      // Check if user needs a profile created
      if (!hasProfile) {
        // Try to get onboarding data from sessionStorage (set before OAuth redirect)
        // or fall back to the persisted store data
        let onboardingData: OnboardingData | null = null

        const sessionData = sessionStorage.getItem('onboarding-data')
        if (sessionData) {
          try {
            onboardingData = JSON.parse(sessionData)
            sessionStorage.removeItem('onboarding-data')
          } catch {
            // Invalid JSON, ignore
          }
        }

        // Fall back to persisted store data
        if (!onboardingData?.basics && hasPendingData()) {
          onboardingData = storeData
        }

        if (onboardingData?.basics) {
          // Create profile with collected onboarding data
          try {
            await createProfileApi.createProfile({
              timezone: onboardingData.basics.timezone,
              locale: onboardingData.basics.locale,
              values: onboardingData.values,
              roles: onboardingData.roles,
              preferences: onboardingData.preferences,
              constraints: onboardingData.constraints,
              initialSeason: onboardingData.season || undefined,
            })
            // Set auth state WITH hasProfile=true since we just created it
            setAuth({
              success: true,
              accessToken,
              refreshToken: refreshToken || undefined,
              user: {
                id: userId,
                email,
                displayName: displayName || null,
                hasProfile: true, // Profile was just created
                authProvider: authProvider || 'Google',
                roles: ['User'],
              },
            })
            reset()
            navigate('/', { replace: true })
          } catch {
            setError('Account created but profile setup failed. Please complete your profile.')
            setIsProcessing(false)
          }
        } else {
          // No onboarding data - set auth and redirect to onboarding to collect it
          setAuth({
            success: true,
            accessToken,
            refreshToken: refreshToken || undefined,
            user: {
              id: userId,
              email,
              displayName: displayName || null,
              hasProfile: false,
              authProvider: authProvider || 'Google',
              roles: ['User'],
            },
          })
          navigate('/onboarding', { replace: true })
        }
      } else {
        // User already has a profile - set auth and go to dashboard
        setAuth({
          success: true,
          accessToken,
          refreshToken: refreshToken || undefined,
          user: {
            id: userId,
            email,
            displayName: displayName || null,
            hasProfile: true,
            authProvider: authProvider || 'Google',
            roles: ['User'],
          },
        })
        reset()
        navigate('/', { replace: true })
      }
    }

    processCallback()
  }, [searchParams, setAuth, storeData, reset, hasPendingData, navigate])

  if (error) {
    return (
      <div className="min-h-screen flex items-center justify-center bg-background p-4">
        <div className="w-full max-w-md space-y-4">
          <Alert variant="destructive">
            <AlertDescription>{error}</AlertDescription>
          </Alert>
          <div className="flex gap-2 justify-center">
            <Button variant="outline" onClick={() => navigate('/login')}>
              Try Login
            </Button>
            <Button onClick={() => navigate('/onboarding')}>
              Start Over
            </Button>
          </div>
        </div>
      </div>
    )
  }

  return (
    <div className="min-h-screen flex items-center justify-center bg-background">
      <div className="flex flex-col items-center gap-4">
        <Loader2 className="size-8 animate-spin text-muted-foreground" />
        <p className="text-muted-foreground">
          {isProcessing ? 'Completing sign in...' : 'Redirecting...'}
        </p>
      </div>
    </div>
  )
}
