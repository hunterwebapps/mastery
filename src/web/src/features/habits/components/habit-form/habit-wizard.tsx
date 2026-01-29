import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { useForm, FormProvider } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { ArrowLeft, ArrowRight, Check, Loader2 } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Progress } from '@/components/ui/progress'
import { AiSuggestionBanner } from '@/components/ai-suggestion-banner'
import { useCreateHabit, useUpdateHabit } from '../../hooks/use-habits'
import { createHabitSchema, type CreateHabitFormData } from '../../schemas/habit-schema'
import { getDefaultHabitFormData } from '../../utils'
import { StepBasics } from './step-basics'
import { StepSchedule } from './step-schedule'
import { StepVariants } from './step-variants'
import { StepReview } from './step-review'

const STEPS = [
  { id: 'basics', title: 'Basics', description: 'Name your habit' },
  { id: 'schedule', title: 'Schedule', description: 'Set frequency' },
  { id: 'variants', title: 'Modes', description: 'Add variants' },
  { id: 'review', title: 'Review', description: 'Confirm details' },
]

export interface HabitWizardProps {
  /** Mode determines create vs edit behavior */
  mode: 'create' | 'edit'
  /** Initial data for edit mode - pre-populates the form */
  initialData?: Partial<CreateHabitFormData>
  /** Habit ID for edit mode */
  habitId?: string
  /** Where to navigate on cancel */
  cancelPath?: string
  /** Show the AI suggestion banner when form is pre-filled from recommendation */
  showAiBanner?: boolean
  /** Called after successful save (for clearing recommendation payload) */
  onSuccess?: () => void
}

export function HabitWizard({
  mode,
  initialData,
  habitId,
  cancelPath = '/habits',
  showAiBanner,
  onSuccess,
}: HabitWizardProps) {
  const navigate = useNavigate()
  const [currentStep, setCurrentStep] = useState(0)
  const createHabit = useCreateHabit()
  const updateHabit = useUpdateHabit()

  const isEditMode = mode === 'edit'
  const isSubmitting = createHabit.isPending || updateHabit.isPending

  const methods = useForm<CreateHabitFormData>({
    resolver: zodResolver(createHabitSchema),
    defaultValues: { ...getDefaultHabitFormData(), ...initialData },
    mode: 'onChange',
  })

  const { handleSubmit, trigger, getValues } = methods

  const progress = ((currentStep + 1) / STEPS.length) * 100

  const handleNext = async () => {
    let isValid = true

    if (currentStep === 0) {
      // Validate basics step
      isValid = await trigger(['title', 'description', 'why'])
    } else if (currentStep === 1) {
      // For schedule step, manually validate the type-specific requirements
      // The UI already enforces valid selections, but we double-check here
      const schedule = getValues('schedule')

      if (!schedule.type) {
        isValid = false
      } else if (schedule.type === 'DaysOfWeek') {
        isValid = Array.isArray(schedule.daysOfWeek) && schedule.daysOfWeek.length > 0
      } else if (schedule.type === 'WeeklyFrequency') {
        isValid = typeof schedule.frequencyPerWeek === 'number' && schedule.frequencyPerWeek >= 1
      } else if (schedule.type === 'Interval') {
        isValid = typeof schedule.intervalDays === 'number' && schedule.intervalDays >= 2
      }
      // 'Daily' type has no additional requirements, isValid stays true
    } else if (currentStep === 2) {
      // Validate variants step
      isValid = await trigger(['defaultMode', 'variants'])
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

  const onSubmit = async (data: CreateHabitFormData) => {
    if (currentStep !== STEPS.length - 1) {
      return
    }

    try {
      const requestData = {
        title: data.title,
        description: data.description || undefined,
        why: data.why || undefined,
        schedule: {
          type: data.schedule.type,
          daysOfWeek: data.schedule.daysOfWeek,
          preferredTimes: data.schedule.preferredTimes,
          frequencyPerWeek: data.schedule.frequencyPerWeek,
          intervalDays: data.schedule.intervalDays,
          startDate: data.schedule.startDate || undefined,
          endDate: data.schedule.endDate || undefined,
        },
        defaultMode: data.defaultMode,
        policy: data.policy
          ? {
              allowLateCompletion: data.policy.allowLateCompletion,
              lateCutoffTime: data.policy.lateCutoffTime,
              allowSkip: data.policy.allowSkip,
              requireMissReason: data.policy.requireMissReason,
              allowBackfill: data.policy.allowBackfill,
              maxBackfillDays: data.policy.maxBackfillDays,
            }
          : undefined,
        variants: data.variants?.map((v) => ({
          mode: v.mode,
          label: v.label,
          defaultValue: v.defaultValue,
          estimatedMinutes: v.estimatedMinutes,
          energyCost: v.energyCost,
          countsAsCompletion: v.countsAsCompletion,
        })),
      }

      if (isEditMode && habitId) {
        await updateHabit.mutateAsync({
          id: habitId,
          request: requestData,
        })
        onSuccess?.()
        navigate(`/habits/${habitId}`)
      } else {
        const newHabitId = await createHabit.mutateAsync(requestData)
        onSuccess?.()
        navigate(`/habits/${newHabitId}`)
      }
    } catch (error) {
      console.error(`Failed to ${isEditMode ? 'update' : 'create'} habit:`, error)
    }
  }

  const handleKeyDown = (e: React.KeyboardEvent) => {
    if (e.key === 'Enter' && currentStep !== STEPS.length - 1) {
      e.preventDefault()
    }
  }

  const handleCancel = () => {
    navigate(cancelPath)
  }

  return (
    <FormProvider {...methods}>
      <form onSubmit={(e) => e.preventDefault()} onKeyDown={handleKeyDown} className="space-y-8">
        {/* Header */}
        <div className="space-y-4">
          <div className="flex items-center gap-4">
            <Button type="button" variant="ghost" size="icon" onClick={handleCancel}>
              <ArrowLeft className="size-4" />
            </Button>
            <div>
              <h1 className="text-2xl font-bold">
                {isEditMode ? 'Edit Habit' : 'Create New Habit'}
              </h1>
              <p className="text-sm text-muted-foreground">
                Step {currentStep + 1} of {STEPS.length}: {STEPS[currentStep].description}
              </p>
            </div>
          </div>
          <Progress value={progress} className="h-1" />
        </div>

        {/* AI Suggestion Banner */}
        {showAiBanner && currentStep === 0 && <AiSuggestionBanner />}

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

        {/* Step Content */}
        <div className="min-h-[400px]">
          {currentStep === 0 && <StepBasics />}
          {currentStep === 1 && <StepSchedule />}
          {currentStep === 2 && <StepVariants />}
          {currentStep === 3 && <StepReview mode={mode} />}
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
              disabled={isSubmitting}
            >
              {isSubmitting && <Loader2 className="size-4 mr-2 animate-spin" />}
              {isEditMode ? 'Save Changes' : 'Create Habit'}
              <Check className="size-4 ml-2" />
            </Button>
          )}
        </div>
      </form>
    </FormProvider>
  )
}
