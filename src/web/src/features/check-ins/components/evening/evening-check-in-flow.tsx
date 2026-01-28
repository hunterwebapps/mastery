import { useState, useCallback } from 'react'
import { Card, CardContent } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { StepProgress } from '../common/step-progress'
import { Top1ReviewStep } from './top1-review-step'
import { EveningEnergyStep } from './evening-energy-step'
import { BlockerStep } from './blocker-step'
import { ReflectionStep } from './reflection-step'
import { EveningSummary } from './evening-summary'
import { useSubmitEveningCheckIn } from '../../hooks/use-check-ins'
import { ArrowLeft, ArrowRight, SkipForward } from 'lucide-react'
import type { BlockerCategory, SubmitEveningCheckInRequest, Top1Type } from '@/types/check-in'

interface EveningCheckInFlowProps {
  onComplete: () => void
  onSkip: () => void
  morningTop1Description?: string
  morningTop1Type?: Top1Type
}

const STEPS = ['Top 1 Review', 'Energy & Stress', 'Blockers', 'Reflection', 'Summary']

export function EveningCheckInFlow({
  onComplete,
  onSkip,
  morningTop1Description,
  morningTop1Type,
}: EveningCheckInFlowProps) {
  const [step, setStep] = useState(0)
  const [top1Completed, setTop1Completed] = useState<boolean | undefined>()
  const [energyLevelPm, setEnergyLevelPm] = useState<number | undefined>()
  const [stressLevel, setStressLevel] = useState<number | undefined>()
  const [blockerCategory, setBlockerCategory] = useState<BlockerCategory | undefined>()
  const [blockerNote, setBlockerNote] = useState('')
  const [reflection, setReflection] = useState('')

  const submitEvening = useSubmitEveningCheckIn()

  const handleNext = useCallback(() => {
    if (step < STEPS.length - 1) {
      setStep(step + 1)
    }
  }, [step])

  const handleBack = useCallback(() => {
    if (step > 0) setStep(step - 1)
  }, [step])

  const handleTop1Change = useCallback((completed: boolean) => {
    setTop1Completed(completed)
    setTimeout(() => setStep(1), 300)
  }, [])

  const handleSubmit = useCallback(async () => {
    const request: SubmitEveningCheckInRequest = {
      top1Completed,
      energyLevelPm,
      stressLevel,
      reflection: reflection.trim() || undefined,
      blockerCategory,
      blockerNote: blockerNote.trim() || undefined,
    }

    try {
      await submitEvening.mutateAsync(request)
      onComplete()
    } catch {
      // Error handled by mutation
    }
  }, [top1Completed, energyLevelPm, stressLevel, reflection, blockerCategory, blockerNote, submitEvening, onComplete])

  return (
    <div className="w-full max-w-lg mx-auto space-y-6">
      <StepProgress
        currentStep={step}
        totalSteps={STEPS.length}
        labels={STEPS}
      />

      <Card className="border-border/50">
        <CardContent className="pt-6">
          {step === 0 && (
            <Top1ReviewStep
              value={top1Completed}
              onChange={handleTop1Change}
              top1Description={morningTop1Description}
              top1Type={morningTop1Type}
            />
          )}

          {step === 1 && (
            <EveningEnergyStep
              energyLevel={energyLevelPm}
              stressLevel={stressLevel}
              onEnergyChange={setEnergyLevelPm}
              onStressChange={setStressLevel}
            />
          )}

          {step === 2 && (
            <BlockerStep
              category={blockerCategory}
              note={blockerNote}
              onCategoryChange={setBlockerCategory}
              onNoteChange={setBlockerNote}
            />
          )}

          {step === 3 && (
            <ReflectionStep
              value={reflection}
              onChange={setReflection}
            />
          )}

          {step === 4 && (
            <EveningSummary
              top1Completed={top1Completed}
              energyLevelPm={energyLevelPm}
              stressLevel={stressLevel}
              reflection={reflection.trim() || undefined}
              blockerCategory={blockerCategory}
              blockerNote={blockerNote.trim() || undefined}
              onSubmit={handleSubmit}
              isSubmitting={submitEvening.isPending}
            />
          )}
        </CardContent>
      </Card>

      {/* Navigation */}
      {step < 4 && (
        <div className="flex items-center justify-between">
          <Button
            variant="ghost"
            onClick={handleBack}
            disabled={step === 0}
            size="sm"
          >
            <ArrowLeft className="size-4 mr-1" />
            Back
          </Button>

          <div className="flex items-center gap-2">
            {step === 0 && (
              <Button
                variant="ghost"
                onClick={onSkip}
                size="sm"
                className="text-muted-foreground"
              >
                <SkipForward className="size-4 mr-1" />
                Skip
              </Button>
            )}

            {step >= 1 && step <= 3 && (
              <Button
                variant="ghost"
                onClick={handleNext}
                size="sm"
                className="text-muted-foreground"
              >
                Skip step
              </Button>
            )}

            <Button onClick={handleNext} size="sm">
              Next
              <ArrowRight className="size-4 ml-1" />
            </Button>
          </div>
        </div>
      )}
    </div>
  )
}
