import { useState } from 'react'
import { Mail, Lock, User } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Alert, AlertDescription } from '@/components/ui/alert'
import { useOnboardingStore } from '@/stores/onboarding-store'
import { useAuthStore } from '@/stores/auth-store'
import { authApi } from '@/features/auth/api/auth-api'
import { createProfileApi } from '@/features/onboarding/api'
import { OAuthButtons, PasswordStrength } from '@/features/auth/components'
import { StepNavigation } from '../step-navigation'
import { useNavigate } from 'react-router-dom'

export function StepAccount() {
  const navigate = useNavigate()
  const [mode, setMode] = useState<'register' | 'login'>('register')
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [displayName, setDisplayName] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [isSubmitting, setIsSubmitting] = useState(false)

  const { data, prevStep, currentStep, totalSteps, reset } = useOnboardingStore()
  const { setAuth, setHasProfile } = useAuthStore()

  const handleOAuth = (provider: 'Google' | 'Apple' | 'Microsoft') => {
    // Store onboarding data in sessionStorage before redirect
    sessionStorage.setItem('onboarding-data', JSON.stringify(data))
    window.location.href = authApi.getOAuthUrl(provider)
  }

  const handleEmailRegister = async () => {
    if (!data.basics) {
      setError('Please complete the basics step first')
      return
    }

    setIsSubmitting(true)
    setError(null)

    try {
      const response = await authApi.registerWithProfile({
        email,
        password,
        displayName: displayName || undefined,
        timezone: data.basics.timezone,
        locale: data.basics.locale,
        values: data.values,
        roles: data.roles,
        preferences: data.preferences,
        constraints: data.constraints,
        initialSeason: data.season || undefined,
      })

      const result = response.data

      if (result.success && result.accessToken) {
        setAuth(result)
        reset() // Clear onboarding data
        navigate('/', { replace: true })
      } else {
        setError(result.error || 'Registration failed')
      }
    } catch (err) {
      if (err instanceof Error) {
        setError(err.message)
      } else {
        setError('Registration failed. Please try again.')
      }
    } finally {
      setIsSubmitting(false)
    }
  }

  const handleEmailLogin = async () => {
    setIsSubmitting(true)
    setError(null)

    try {
      const response = await authApi.login({ email, password })
      const result = response.data

      if (result.success && result.accessToken) {
        setAuth(result)

        // Check if user needs a profile created
        if (!result.user?.hasProfile) {
          // User has no profile - create one with onboarding data if available
          if (data.basics) {
            try {
              await createProfileApi.createProfile({
                timezone: data.basics.timezone,
                locale: data.basics.locale,
                values: data.values,
                roles: data.roles,
                preferences: data.preferences,
                constraints: data.constraints,
                initialSeason: data.season || undefined,
              })
              setHasProfile(true) // Update auth store to reflect profile exists
              reset()
              navigate('/', { replace: true })
            } catch (profileErr) {
              // Profile creation failed - stay on onboarding but user is now logged in
              setError('Account created but profile setup failed. Please try again.')
            }
          } else {
            // No onboarding data - redirect back to start of onboarding
            navigate('/onboarding', { replace: true })
          }
        } else {
          // User already has a profile - just go to dashboard
          reset()
          navigate('/', { replace: true })
        }
      } else {
        setError(result.error || 'Login failed')
      }
    } catch (err) {
      if (err instanceof Error) {
        setError(err.message)
      } else {
        setError('Login failed. Please try again.')
      }
    } finally {
      setIsSubmitting(false)
    }
  }

  const isEmailValid = email.includes('@') && email.includes('.')
  const isPasswordValid = password.length >= 8

  return (
    <div className="space-y-8">
      <div>
        <h2 className="text-xl font-semibold mb-2">
          {mode === 'register'
            ? 'Create your account to save your setup'
            : 'Welcome back'}
        </h2>
        <p className="text-muted-foreground">
          {mode === 'register'
            ? "You're almost done! Create an account to save your personalized settings."
            : 'Sign in to access your existing profile.'}
        </p>
      </div>

      {error && (
        <Alert variant="destructive">
          <AlertDescription>{error}</AlertDescription>
        </Alert>
      )}

      {/* Social Login Buttons */}
      <OAuthButtons
        onGoogle={() => handleOAuth('Google')}
        onApple={() => handleOAuth('Apple')}
        onMicrosoft={() => handleOAuth('Microsoft')}
        disabled={isSubmitting}
        label={mode === 'register' ? 'Sign up with' : 'Sign in with'}
      />

      {/* Divider */}
      <div className="relative">
        <div className="absolute inset-0 flex items-center">
          <span className="w-full border-t" />
        </div>
        <div className="relative flex justify-center text-xs uppercase">
          <span className="bg-background px-2 text-muted-foreground">
            or continue with email
          </span>
        </div>
      </div>

      {/* Email/Password Form */}
      <div className="space-y-4">
        {mode === 'register' && (
          <div className="space-y-2">
            <Label htmlFor="displayName" className="flex items-center gap-2">
              <User className="size-4" />
              Display Name (optional)
            </Label>
            <Input
              id="displayName"
              type="text"
              placeholder="How should we call you?"
              value={displayName}
              onChange={(e) => setDisplayName(e.target.value)}
              disabled={isSubmitting}
            />
          </div>
        )}

        <div className="space-y-2">
          <Label htmlFor="email" className="flex items-center gap-2">
            <Mail className="size-4" />
            Email
          </Label>
          <Input
            id="email"
            type="email"
            placeholder="you@example.com"
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            disabled={isSubmitting}
          />
        </div>

        <div className="space-y-2">
          <Label htmlFor="password" className="flex items-center gap-2">
            <Lock className="size-4" />
            Password
          </Label>
          <Input
            id="password"
            type="password"
            placeholder={mode === 'register' ? 'At least 8 characters' : 'Your password'}
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            disabled={isSubmitting}
          />
          {mode === 'register' && password && (
            <PasswordStrength password={password} />
          )}
        </div>

        {mode === 'login' && (
          <div className="text-right">
            <a
              href="/forgot-password"
              className="text-sm text-primary hover:underline"
            >
              Forgot password?
            </a>
          </div>
        )}

        <Button
          className="w-full"
          onClick={mode === 'register' ? handleEmailRegister : handleEmailLogin}
          disabled={isSubmitting || !isEmailValid || !isPasswordValid}
        >
          {isSubmitting
            ? 'Please wait...'
            : mode === 'register'
            ? 'Create Account & Save Setup'
            : 'Sign In'}
        </Button>
      </div>

      {/* Toggle Mode */}
      <p className="text-center text-sm text-muted-foreground">
        {mode === 'register' ? (
          <>
            Already have an account?{' '}
            <button
              type="button"
              onClick={() => setMode('login')}
              className="text-primary hover:underline"
              disabled={isSubmitting}
            >
              Sign in instead
            </button>
          </>
        ) : (
          <>
            New here?{' '}
            <button
              type="button"
              onClick={() => setMode('register')}
              className="text-primary hover:underline"
              disabled={isSubmitting}
            >
              Create an account
            </button>
          </>
        )}
      </p>

      <StepNavigation
        currentStep={currentStep}
        totalSteps={totalSteps}
        onNext={() => {}} // Handled by form submission
        onPrev={prevStep}
        isNextDisabled={true} // Use the form buttons instead
        isSubmitting={isSubmitting}
        nextLabel="Create Account"
      />
    </div>
  )
}
