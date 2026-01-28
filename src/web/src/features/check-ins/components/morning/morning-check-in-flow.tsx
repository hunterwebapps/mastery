import { useState, useCallback } from 'react'
import { Card, CardContent } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { Textarea } from '@/components/ui/textarea'
import { StepProgress } from '../common/step-progress'
import { EnergyStep } from './energy-step'
import { ModeStep } from './mode-step'
import { Top1Step } from './top1-step'
import { MorningSummary } from './morning-summary'
import { useSubmitMorningCheckIn } from '../../hooks/use-check-ins'
import { ArrowLeft, ArrowRight, SkipForward } from 'lucide-react'
import type { Top1Type, SubmitMorningCheckInRequest } from '@/types/check-in'

interface MorningCheckInFlowProps {
  onComplete: () => void
  onSkip: () => void
}

const STEPS = ['Energy', 'Mode', 'Top 1', 'Intention', 'Summary']

export function MorningCheckInFlow({ onComplete, onSkip }: MorningCheckInFlowProps) {
  const [step, setStep] = useState(0)
  const [energyLevel, setEnergyLevel] = useState<number | undefined>()
  const [selectedMode, setSelectedMode] = useState<string | undefined>()
  const [top1Type, setTop1Type] = useState<Top1Type | undefined>()
  const [top1EntityId, setTop1EntityId] = useState<string | undefined>()
  const [top1FreeText, setTop1FreeText] = useState<string | undefined>()
  const [intention, setIntention] = useState('')

  const submitMorning = useSubmitMorningCheckIn()

  // Suggest mode based on energy level
  const suggestedMode = energyLevel
    ? energyLevel <= 2 ? 'Minimum' : energyLevel <= 3 ? 'Maintenance' : 'Full'
    : undefined

  const canAdvance = useCallback(() => {
    switch (step) {
      case 0: return energyLevel !== undefined
      case 1: return selectedMode !== undefined
      case 2: return true // Top 1 is optional
      case 3: return true // Intention is optional
      default: return true
    }
  }, [step, energyLevel, selectedMode])

  const handleNext = useCallback(() => {
    if (step < STEPS.length - 1 && canAdvance()) {
      setStep(step + 1)
    }
  }, [step, canAdvance])

  const handleBack = useCallback(() => {
    if (step > 0) setStep(step - 1)
  }, [step])

  const handleEnergyChange = useCallback((level: number) => {
    setEnergyLevel(level)
    // Auto-advance after selection
    setTimeout(() => setStep(1), 300)
  }, [])

  const handleModeChange = useCallback((mode: string) => {
    setSelectedMode(mode)
    setTimeout(() => setStep(2), 300)
  }, [])

  const handleSubmit = useCallback(async () => {
    if (!energyLevel || !selectedMode) return

    const request: SubmitMorningCheckInRequest = {
      energyLevel,
      selectedMode,
      top1Type,
      top1EntityId,
      top1FreeText: top1Type === 'FreeText' ? top1FreeText : undefined,
      intention: intention.trim() || undefined,
    }

    try {
      await submitMorning.mutateAsync(request)
      onComplete()
    } catch {
      // Error handled by mutation
    }
  }, [energyLevel, selectedMode, top1Type, top1EntityId, top1FreeText, intention, submitMorning, onComplete])

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
            <EnergyStep
              value={energyLevel}
              onChange={handleEnergyChange}
            />
          )}

          {step === 1 && (
            <ModeStep
              value={selectedMode}
              onChange={handleModeChange}
              suggestedMode={suggestedMode}
            />
          )}

          {step === 2 && (
            <Top1Step
              top1Type={top1Type}
              top1EntityId={top1EntityId}
              top1FreeText={top1FreeText}
              onTypeChange={setTop1Type}
              onEntityIdChange={setTop1EntityId}
              onFreeTextChange={setTop1FreeText}
            />
          )}

          {step === 3 && (
            <div className="space-y-6 animate-in fade-in duration-300">
              <div className="text-center space-y-2">
                <h2 className="text-2xl font-semibold text-foreground">
                  Set an intention
                </h2>
                <p className="text-sm text-muted-foreground">
                  One sentence to guide your day (optional)
                </p>
              </div>
              <div className="space-y-2">
                <Textarea
                  placeholder="e.g., Stay focused on deep work before noon"
                  value={intention}
                  onChange={(e) => setIntention(e.target.value)}
                  maxLength={500}
                  rows={3}
                  className="resize-none"
                />
                <p className="text-xs text-muted-foreground text-right">
                  {intention.length}/500
                </p>
              </div>
            </div>
          )}

          {step === 4 && (
            <MorningSummary
              energyLevel={energyLevel!}
              selectedMode={selectedMode!}
              top1Type={top1Type}
              top1FreeText={top1FreeText}
              intention={intention.trim() || undefined}
              onSubmit={handleSubmit}
              isSubmitting={submitMorning.isPending}
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

            {(step === 2 || step === 3) && (
              <Button
                variant="ghost"
                onClick={handleNext}
                size="sm"
                className="text-muted-foreground"
              >
                Skip step
              </Button>
            )}

            <Button
              onClick={handleNext}
              disabled={!canAdvance()}
              size="sm"
            >
              Next
              <ArrowRight className="size-4 ml-1" />
            </Button>
          </div>
        </div>
      )}
    </div>
  )
}
