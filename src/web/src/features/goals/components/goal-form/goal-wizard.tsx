import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { useForm, FormProvider } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { ArrowLeft, ArrowRight, Check, Loader2 } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Progress } from '@/components/ui/progress'
import { AiSuggestionBanner } from '@/components/ai-suggestion-banner'
import { useCreateGoal } from '../../hooks'
import { createGoalSchema, type CreateGoalFormData } from '../../schemas'
import { StepBasics } from './step-basics'
import { StepMetrics } from './step-metrics'
import { StepReview } from './step-review'

const STEPS = [
  { id: 'basics', title: 'Basics', description: 'Define your goal' },
  { id: 'metrics', title: 'Metrics', description: 'Set up your scoreboard' },
  { id: 'review', title: 'Review', description: 'Confirm and create' },
]

export interface GoalWizardProps {
  /** Initial data for form pre-population */
  initialData?: Partial<CreateGoalFormData>
  /** Show the AI suggestion banner */
  showAiBanner?: boolean
  /** Called after successful creation */
  onSuccess?: () => void
}

export function GoalWizard({ initialData, showAiBanner, onSuccess }: GoalWizardProps = {}) {
  const navigate = useNavigate()
  const [currentStep, setCurrentStep] = useState(0)
  const createGoal = useCreateGoal()

  const defaultValues: CreateGoalFormData = {
    title: '',
    description: '',
    why: '',
    priority: 3,
    deadline: '',
    metrics: [],
  }

  const methods = useForm<CreateGoalFormData>({
    resolver: zodResolver(createGoalSchema),
    defaultValues: initialData ? { ...defaultValues, ...initialData } : defaultValues,
    mode: 'onChange',
  })

  const {
    handleSubmit,
    trigger,
    formState: { isSubmitting },
  } = methods

  const progress = ((currentStep + 1) / STEPS.length) * 100

  const handleNext = async () => {
    // Validate current step before proceeding
    let isValid = true
    if (currentStep === 0) {
      isValid = await trigger(['title', 'description', 'why', 'priority', 'deadline'])
    }

    if (isValid && currentStep < STEPS.length - 1) {
      setCurrentStep((prev) => prev + 1)
    }
  }

  const handleBack = () => {
    if (currentStep > 0) {
      setCurrentStep((prev) => prev - 1)
    }
  }

  const onSubmit = async (data: CreateGoalFormData) => {
    // Only allow submission from the review step
    if (currentStep !== STEPS.length - 1) {
      return
    }

    try {
      const goalId = await createGoal.mutateAsync({
        ...data,
        deadline: data.deadline || undefined,
        metrics: data.metrics?.map((m, index) => ({
          ...m,
          displayOrder: index,
        })),
      })
      onSuccess?.()
      navigate(`/goals/${goalId}`)
    } catch (error) {
      console.error('Failed to create goal:', error)
    }
  }

  const handleKeyDown = (e: React.KeyboardEvent) => {
    // Prevent Enter from submitting the form except on the review step
    if (e.key === 'Enter' && currentStep !== STEPS.length - 1) {
      e.preventDefault()
    }
  }

  return (
    <FormProvider {...methods}>
      <form onSubmit={(e) => e.preventDefault()} onKeyDown={handleKeyDown} className="space-y-8">
        {/* Header */}
        <div className="space-y-4">
          <div className="flex items-center gap-4">
            <Button type="button" variant="ghost" size="icon" onClick={() => navigate('/goals')}>
              <ArrowLeft className="size-4" />
            </Button>
            <div>
              <h1 className="text-2xl font-bold">Create New Goal</h1>
              <p className="text-sm text-muted-foreground">
                Step {currentStep + 1} of {STEPS.length}: {STEPS[currentStep].description}
              </p>
            </div>
          </div>
          <Progress value={progress} className="h-1" />
        </div>

        {/* Step Indicators */}
        <div className="flex justify-center">
          <div className="flex items-center gap-2">
            {STEPS.map((step, index) => (
              <div key={step.id} className="flex items-center">
                <button
                  type="button"
                  onClick={() => index < currentStep && setCurrentStep(index)}
                  className={`
                    flex items-center gap-2 px-3 py-1.5 rounded-full text-sm transition-colors
                    ${
                      index === currentStep
                        ? 'bg-primary text-primary-foreground'
                        : index < currentStep
                          ? 'bg-primary/20 text-primary cursor-pointer hover:bg-primary/30'
                          : 'bg-muted text-muted-foreground cursor-default'
                    }
                  `}
                  disabled={index > currentStep}
                >
                  {index < currentStep ? (
                    <Check className="size-4" />
                  ) : (
                    <span className="size-5 flex items-center justify-center rounded-full border text-xs">
                      {index + 1}
                    </span>
                  )}
                  <span className="hidden sm:inline">{step.title}</span>
                </button>
                {index < STEPS.length - 1 && (
                  <div
                    className={`w-8 h-0.5 mx-1 ${
                      index < currentStep ? 'bg-primary' : 'bg-muted'
                    }`}
                  />
                )}
              </div>
            ))}
          </div>
        </div>

        {/* AI Suggestion Banner */}
        {showAiBanner && currentStep === 0 && <AiSuggestionBanner />}

        {/* Step Content */}
        <div className="min-h-[400px]">
          {currentStep === 0 && <StepBasics />}
          {currentStep === 1 && <StepMetrics />}
          {currentStep === 2 && <StepReview />}
        </div>

        {/* Navigation */}
        <div className="flex justify-between pt-4 border-t">
          <Button
            type="button"
            variant="outline"
            onClick={handleBack}
            disabled={currentStep === 0}
          >
            <ArrowLeft className="size-4 mr-2" />
            Back
          </Button>

          {currentStep < STEPS.length - 1 ? (
            <Button type="button" onClick={handleNext}>
              Next
              <ArrowRight className="size-4 ml-2" />
            </Button>
          ) : (
            <Button
              type="button"
              onClick={handleSubmit(onSubmit)}
              disabled={isSubmitting || createGoal.isPending}
            >
              {(isSubmitting || createGoal.isPending) && (
                <Loader2 className="size-4 mr-2 animate-spin" />
              )}
              Create Goal
              <Check className="size-4 ml-2" />
            </Button>
          )}
        </div>
      </form>
    </FormProvider>
  )
}
