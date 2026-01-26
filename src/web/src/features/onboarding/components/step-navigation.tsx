import { ArrowLeft, ArrowRight } from 'lucide-react'
import { Button } from '@/components/ui/button'

interface StepNavigationProps {
  currentStep: number
  totalSteps: number
  onNext: () => void
  onPrev: () => void
  isNextDisabled?: boolean
  isSubmitting?: boolean
  nextLabel?: string
  showSkip?: boolean
  onSkip?: () => void
}

export function StepNavigation({
  currentStep,
  totalSteps,
  onNext,
  onPrev,
  isNextDisabled = false,
  isSubmitting = false,
  nextLabel,
  showSkip = false,
  onSkip,
}: StepNavigationProps) {
  const isFirstStep = currentStep === 1
  const isLastStep = currentStep === totalSteps

  const getNextLabel = () => {
    if (nextLabel) return nextLabel
    if (isLastStep) return 'Complete Setup'
    return 'Continue'
  }

  return (
    <div className="flex items-center justify-between pt-6 border-t border-border">
      <Button
        variant="outline"
        onClick={onPrev}
        disabled={isFirstStep || isSubmitting}
      >
        <ArrowLeft className="mr-2 size-4" />
        Back
      </Button>

      <div className="flex items-center gap-3">
        {showSkip && onSkip && (
          <Button
            variant="ghost"
            onClick={onSkip}
            disabled={isSubmitting}
          >
            Skip for now
          </Button>
        )}
        <Button onClick={onNext} disabled={isNextDisabled || isSubmitting}>
          {isSubmitting ? 'Saving...' : getNextLabel()}
          {!isLastStep && !isSubmitting && <ArrowRight className="ml-2 size-4" />}
        </Button>
      </div>
    </div>
  )
}
