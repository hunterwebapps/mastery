import { Check } from 'lucide-react'
import { cn } from '@/lib/utils'

interface StepIndicatorProps {
  currentStep: number
  totalSteps: number
  labels?: string[]
}

const defaultLabels = [
  'Basics',
  'Values',
  'Roles',
  'Preferences',
  'Constraints',
  'Season',
  'Account',
]

export function StepIndicator({
  currentStep,
  totalSteps,
  labels = defaultLabels,
}: StepIndicatorProps) {
  return (
    <div className="w-full">
      {/* Desktop view */}
      <div className="hidden sm:block relative">
        {/* Connector lines - positioned behind the circles */}
        <div className="absolute top-5 left-0 right-0 flex" style={{ paddingLeft: '20px', paddingRight: '20px' }}>
          {Array.from({ length: totalSteps - 1 }).map((_, index) => (
            <div
              key={index}
              className={cn(
                'flex-1 h-0.5',
                index + 1 < currentStep ? 'bg-primary' : 'bg-muted'
              )}
            />
          ))}
        </div>
        {/* Step circles and labels */}
        <div className="relative flex justify-between">
          {Array.from({ length: totalSteps }).map((_, index) => {
            const step = index + 1
            const isCompleted = step < currentStep
            const isCurrent = step === currentStep

            return (
              <div key={step} className="flex flex-col items-center">
                <div
                  className={cn(
                    'size-10 rounded-full flex items-center justify-center text-sm font-medium transition-colors bg-background',
                    isCompleted && 'bg-primary text-primary-foreground',
                    isCurrent && 'bg-primary text-primary-foreground ring-4 ring-primary/20',
                    !isCompleted && !isCurrent && 'bg-muted text-muted-foreground'
                  )}
                >
                  {isCompleted ? <Check className="size-5" /> : step}
                </div>
                <span
                  className={cn(
                    'mt-2 text-xs font-medium',
                    isCurrent && 'text-primary',
                    !isCurrent && 'text-muted-foreground'
                  )}
                >
                  {labels[index]}
                </span>
              </div>
            )
          })}
        </div>
      </div>

      {/* Mobile view */}
      <div className="flex sm:hidden items-center justify-between">
        <span className="text-sm font-medium text-primary">
          Step {currentStep} of {totalSteps}
        </span>
        <span className="text-sm text-muted-foreground">
          {labels[currentStep - 1]}
        </span>
      </div>

      {/* Mobile progress bar */}
      <div className="mt-3 sm:hidden h-1 w-full bg-muted rounded-full overflow-hidden">
        <div
          className="h-full bg-primary transition-all duration-300"
          style={{ width: `${(currentStep / totalSteps) * 100}%` }}
        />
      </div>
    </div>
  )
}
