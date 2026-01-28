import { cn } from '@/lib/utils'
import { Check } from 'lucide-react'

interface StepProgressProps {
  currentStep: number
  totalSteps: number
  labels?: string[]
}

export function StepProgress({ currentStep, totalSteps, labels }: StepProgressProps) {
  return (
    <div className="w-full">
      {/* Mobile: compact bar */}
      <div className="sm:hidden">
        <div className="flex items-center justify-between mb-2">
          <span className="text-xs text-muted-foreground">
            Step {currentStep + 1} of {totalSteps}
          </span>
          {labels?.[currentStep] && (
            <span className="text-xs font-medium text-foreground">
              {labels[currentStep]}
            </span>
          )}
        </div>
        <div className="h-1.5 w-full rounded-full bg-muted overflow-hidden">
          <div
            className="h-full rounded-full bg-primary transition-all duration-500 ease-out"
            style={{ width: `${((currentStep + 1) / totalSteps) * 100}%` }}
          />
        </div>
      </div>

      {/* Desktop: dot indicators */}
      <div className="hidden sm:flex items-center justify-center gap-2">
        {Array.from({ length: totalSteps }).map((_, i) => (
          <div key={i} className="flex items-center">
            <div
              className={cn(
                'flex size-8 items-center justify-center rounded-full border-2 transition-all duration-300',
                i < currentStep && 'border-primary bg-primary text-primary-foreground',
                i === currentStep && 'border-primary bg-primary/10 text-primary ring-2 ring-primary/20',
                i > currentStep && 'border-muted bg-muted/50 text-muted-foreground'
              )}
            >
              {i < currentStep ? (
                <Check className="size-4" />
              ) : (
                <span className="text-xs font-medium">{i + 1}</span>
              )}
            </div>
            {i < totalSteps - 1 && (
              <div
                className={cn(
                  'h-0.5 w-8 transition-all duration-300',
                  i < currentStep ? 'bg-primary' : 'bg-muted'
                )}
              />
            )}
          </div>
        ))}
      </div>
    </div>
  )
}
