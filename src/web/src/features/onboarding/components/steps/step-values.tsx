import { useOnboardingStore } from '@/stores/onboarding-store'
import { StepNavigation } from '../step-navigation'
import { ValuePicker } from '../value-picker'

export function StepValues() {
  const { data, setValues, nextStep, prevStep, currentStep, totalSteps } =
    useOnboardingStore()

  const isValid = data.values.length >= 1

  return (
    <div className="space-y-8">
      <div>
        <h2 className="text-xl font-semibold mb-2">What matters most to you?</h2>
        <p className="text-muted-foreground">
          Your values guide how Mastery prioritizes and coaches you. Select at least 5 values
          that resonate with who you want to be.
        </p>
      </div>

      <ValuePicker
        values={data.values}
        onChange={setValues}
        minRecommended={5}
        maxAllowed={10}
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
