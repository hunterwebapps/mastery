import { useOnboardingStore } from '@/stores/onboarding-store'
import { StepNavigation } from '../step-navigation'
import { RoleList } from '../role-editor'

export function StepRoles() {
  const { data, setRoles, nextStep, prevStep, currentStep, totalSteps } =
    useOnboardingStore()

  const isValid = data.roles.length >= 1 && data.roles.every((r) => r.label.trim() !== '')

  return (
    <div className="space-y-8">
      <div>
        <h2 className="text-xl font-semibold mb-2">What hats do you wear?</h2>
        <p className="text-muted-foreground">
          Define the different roles you play in life. This helps Mastery balance your time
          across what matters most.
        </p>
      </div>

      <RoleList
        roles={data.roles}
        onChange={setRoles}
        minRecommended={3}
        maxAllowed={8}
      />

      <StepNavigation
        currentStep={currentStep}
        totalSteps={totalSteps}
        onNext={nextStep}
        onPrev={prevStep}
        isNextDisabled={!isValid}
      />
    </div>
  )
}
