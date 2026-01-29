import { useOnboardingStore } from '@/stores/onboarding-store'
import { useAuthStore } from '@/stores/auth-store'
import { OnboardingLayout } from '../components'
import {
  StepBasics,
  StepValues,
  StepRoles,
  StepPreferences,
  StepConstraints,
  StepSeason,
  StepAccount,
} from '../components/steps'

export function Component() {
  const { currentStep } = useOnboardingStore()
  const { isAuthenticated } = useAuthStore()

  // If user is already authenticated, skip the account step (6 steps instead of 7)
  const effectiveTotalSteps = isAuthenticated ? 6 : 7

  const renderStep = () => {
    switch (currentStep) {
      case 1:
        return <StepBasics />
      case 2:
        return <StepValues />
      case 3:
        return <StepRoles />
      case 4:
        return <StepPreferences />
      case 5:
        return <StepConstraints />
      case 6:
        // If authenticated, StepSeason handles profile creation
        // If not authenticated, show StepSeason normally
        return <StepSeason isLastStep={isAuthenticated} />
      case 7:
        // Only reachable if not authenticated
        return <StepAccount />
      default:
        return <StepBasics />
    }
  }

  return (
    <OnboardingLayout currentStep={currentStep} totalSteps={effectiveTotalSteps}>
      {renderStep()}
    </OnboardingLayout>
  )
}
