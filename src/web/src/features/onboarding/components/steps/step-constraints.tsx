import { useOnboardingStore } from '@/stores/onboarding-store'
import { StepNavigation } from '../step-navigation'
import {
  CapacityLimits,
  BlockedWindowsEditor,
  HealthNotesInput,
} from '@/features/profile/components/constraints'
import type { ConstraintsDto } from '@/types'

export function StepConstraints() {
  const { data, setConstraints, nextStep, prevStep, currentStep, totalSteps } =
    useOnboardingStore()

  const constraints = data.constraints

  const handleUpdate = (updates: Partial<ConstraintsDto>) => {
    setConstraints({ ...constraints, ...updates })
  }

  return (
    <div className="space-y-8">
      <div>
        <h2 className="text-xl font-semibold mb-2">Set your boundaries</h2>
        <p className="text-muted-foreground">
          Help Mastery understand your capacity limits so it can create realistic plans.
        </p>
      </div>

      <div className="space-y-8">
        {/* Capacity Limits */}
        <div className="space-y-3">
          <h3 className="font-medium">Daily Capacity</h3>
          <p className="text-sm text-muted-foreground">
            How much time can you realistically dedicate to planned activities?
          </p>
          <CapacityLimits
            weekdayMinutes={constraints.maxPlannedMinutesWeekday}
            weekendMinutes={constraints.maxPlannedMinutesWeekend}
            onChange={(weekday, weekend) =>
              handleUpdate({
                maxPlannedMinutesWeekday: weekday,
                maxPlannedMinutesWeekend: weekend,
              })
            }
          />
        </div>

        {/* Blocked Windows */}
        <div className="pt-6 border-t border-border">
          <BlockedWindowsEditor
            windows={constraints.blockedTimeWindows}
            onChange={(windows) => handleUpdate({ blockedTimeWindows: windows })}
          />
        </div>

        {/* Health Notes */}
        <div className="pt-6 border-t border-border">
          <HealthNotesInput
            value={constraints.healthNotes}
            onChange={(notes) => handleUpdate({ healthNotes: notes })}
          />
        </div>
      </div>

      <StepNavigation
        currentStep={currentStep}
        totalSteps={totalSteps}
        onNext={nextStep}
        onPrev={prevStep}
      />
    </div>
  )
}
