import { useState, useEffect } from 'react'
import { useOnboardingStore } from '@/stores/onboarding-store'
import { useCreateProfile } from '../hooks'
import { OnboardingLayout, CompletionScreen } from '../components'
import {
  StepBasics,
  StepValues,
  StepRoles,
  StepPreferences,
  StepConstraints,
} from '../components/steps'
import type { CreateProfileRequest } from '../api'

export function Component() {
  const { currentStep, totalSteps, data, setSubmitting, isSubmitting } = useOnboardingStore()
  const createProfile = useCreateProfile()
  const [error, setError] = useState<string | null>(null)
  const [isComplete, setIsComplete] = useState(false)

  // Listen for step completion on the last step
  useEffect(() => {
    if (currentStep > totalSteps && !isComplete) {
      handleSubmit()
    }
  }, [currentStep])

  const handleSubmit = async () => {
    if (!data.basics) {
      setError('Please complete the basics step')
      return
    }

    setSubmitting(true)
    setError(null)
    setIsComplete(true)

    try {
      const request: CreateProfileRequest = {
        timezone: data.basics.timezone,
        locale: data.basics.locale,
        values: data.values,
        roles: data.roles,
        preferences: data.preferences,
        constraints: data.constraints,
        initialSeason: data.season || undefined,
      }

      await createProfile.mutateAsync(request)
      // On success, the mutation will navigate to the dashboard
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to create profile')
      setSubmitting(false)
    }
  }

  // Show completion screen when submitting
  if (isComplete) {
    return (
      <OnboardingLayout currentStep={totalSteps} totalSteps={totalSteps}>
        <CompletionScreen
          isSubmitting={isSubmitting || createProfile.isPending}
          error={error || undefined}
          onRetry={handleSubmit}
        />
      </OnboardingLayout>
    )
  }

  const renderStep = () => {
    switch (currentStep) {
      case 1:
        return <StepBasics />
      case 2:
        return <StepValues />
      case 3:
        return <StepRoles />
      case 4:
        return <StepPreferences />
      case 5:
        return <StepConstraints />
      case 6:
        return <StepSeasonWithSubmit onSubmit={handleSubmit} />
      default:
        return <StepBasics />
    }
  }

  return (
    <OnboardingLayout currentStep={currentStep} totalSteps={totalSteps}>
      {renderStep()}
    </OnboardingLayout>
  )
}

// Wrapper for StepSeason that triggers submission
function StepSeasonWithSubmit({ onSubmit }: { onSubmit: () => void }) {
  const { data, setSeason, prevStep, currentStep, totalSteps, isSubmitting } =
    useOnboardingStore()

  return (
    <StepSeasonInline
      season={data.season}
      setSeason={setSeason}
      onComplete={onSubmit}
      onBack={prevStep}
      currentStep={currentStep}
      totalSteps={totalSteps}
      isSubmitting={isSubmitting}
    />
  )
}

// Inline season step that accepts a completion callback
import { Plus, X, Leaf } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Textarea } from '@/components/ui/textarea'
import { Slider } from '@/components/ui/slider'
import { Badge } from '@/components/ui/badge'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import { Alert, AlertDescription } from '@/components/ui/alert'
import type { SeasonType, CreateSeasonRequest } from '@/types'
import { seasonTypeInfo } from '@/types'
import { StepNavigation } from '../components/step-navigation'

const SEASON_TYPES: SeasonType[] = ['Sprint', 'Build', 'Maintain', 'Recover', 'Transition', 'Explore']

interface StepSeasonInlineProps {
  season: CreateSeasonRequest | null
  setSeason: (season: CreateSeasonRequest | null) => void
  onComplete: () => void
  onBack: () => void
  currentStep: number
  totalSteps: number
  isSubmitting: boolean
}

function StepSeasonInline({
  season: initialSeason,
  setSeason,
  onComplete,
  onBack,
  currentStep,
  totalSteps,
  isSubmitting,
}: StepSeasonInlineProps) {
  const [showForm, setShowForm] = useState(!!initialSeason)
  const [season, setSeasonData] = useState<CreateSeasonRequest>(
    initialSeason || {
      label: '',
      type: 'Build',
      startDate: new Date().toISOString().split('T')[0],
      intensity: 5,
      nonNegotiables: [],
    }
  )
  const [newNonNegotiable, setNewNonNegotiable] = useState('')

  const handleAddNonNegotiable = () => {
    const trimmed = newNonNegotiable.trim()
    if (trimmed && !season.nonNegotiables?.includes(trimmed)) {
      setSeasonData({
        ...season,
        nonNegotiables: [...(season.nonNegotiables || []), trimmed],
      })
      setNewNonNegotiable('')
    }
  }

  const handleRemoveNonNegotiable = (item: string) => {
    setSeasonData({
      ...season,
      nonNegotiables: season.nonNegotiables?.filter((n) => n !== item),
    })
  }

  const handleNext = () => {
    if (showForm && season.label.trim()) {
      setSeason(season)
    } else {
      setSeason(null)
    }
    onComplete()
  }

  const handleSkip = () => {
    setSeason(null)
    onComplete()
  }

  return (
    <div className="space-y-8">
      <div>
        <h2 className="text-xl font-semibold mb-2">Define your current season</h2>
        <p className="text-muted-foreground">
          A season represents your current life phase and intensity level. This helps
          Mastery calibrate expectations and coaching style.
        </p>
      </div>

      {!showForm ? (
        <div className="space-y-6">
          <Alert className="border-blue-500/50 bg-blue-500/10">
            <Leaf className="size-4 text-blue-500" />
            <AlertDescription className="text-blue-700 dark:text-blue-300">
              <strong>Why define a season?</strong> Seasons help you be intentional about
              your current focus. Whether you're sprinting toward a deadline or recovering
              from burnout, Mastery adjusts its coaching accordingly.
            </AlertDescription>
          </Alert>

          <Button onClick={() => setShowForm(true)} className="w-full">
            <Plus className="size-4 mr-2" />
            Define My First Season
          </Button>
        </div>
      ) : (
        <div className="space-y-6">
          <div className="space-y-2">
            <Label>Season Name</Label>
            <Input
              placeholder="e.g., Q1 Product Launch, Post-Move Reset"
              value={season.label}
              onChange={(e) => setSeasonData({ ...season, label: e.target.value })}
            />
          </div>

          <div className="space-y-2">
            <Label>Season Type</Label>
            <Select
              value={season.type}
              onValueChange={(value) =>
                setSeasonData({ ...season, type: value as SeasonType })
              }
            >
              <SelectTrigger>
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                {SEASON_TYPES.map((type) => (
                  <SelectItem key={type} value={type}>
                    <div className="flex flex-col">
                      <span className={seasonTypeInfo[type].color}>
                        {seasonTypeInfo[type].label}
                      </span>
                      <span className="text-xs text-muted-foreground">
                        {seasonTypeInfo[type].description}
                      </span>
                    </div>
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>

          <div className="grid gap-4 sm:grid-cols-2">
            <div className="space-y-2">
              <Label>Start Date</Label>
              <Input
                type="date"
                value={season.startDate}
                onChange={(e) => setSeasonData({ ...season, startDate: e.target.value })}
              />
            </div>
            <div className="space-y-2">
              <Label>Expected End Date (optional)</Label>
              <Input
                type="date"
                value={season.expectedEndDate || ''}
                onChange={(e) =>
                  setSeasonData({ ...season, expectedEndDate: e.target.value || undefined })
                }
              />
            </div>
          </div>

          <div className="space-y-3">
            <div className="flex items-center justify-between">
              <Label>Intensity Level</Label>
              <span className="text-sm font-medium text-primary">
                {season.intensity}/10
              </span>
            </div>
            <Slider
              value={[season.intensity || 5]}
              onValueChange={([value]) => setSeasonData({ ...season, intensity: value })}
              min={1}
              max={10}
              step={1}
            />
            <p className="text-xs text-muted-foreground">
              Higher intensity means more aggressive planning and nudges
            </p>
          </div>

          <div className="space-y-2">
            <Label>Success Statement (optional)</Label>
            <Textarea
              placeholder="What does success look like at the end of this season?"
              value={season.successStatement || ''}
              onChange={(e) =>
                setSeasonData({ ...season, successStatement: e.target.value || undefined })
              }
              rows={2}
              className="resize-none"
            />
          </div>

          <div className="space-y-3">
            <Label>Non-Negotiables (optional)</Label>
            <p className="text-xs text-muted-foreground">
              Things you absolutely won't compromise on during this season
            </p>
            {season.nonNegotiables && season.nonNegotiables.length > 0 && (
              <div className="flex flex-wrap gap-2">
                {season.nonNegotiables.map((item) => (
                  <Badge key={item} variant="secondary" className="gap-1 pr-1">
                    {item}
                    <button
                      type="button"
                      onClick={() => handleRemoveNonNegotiable(item)}
                      className="ml-1 rounded-full hover:bg-muted-foreground/20 p-0.5"
                    >
                      <X className="size-3" />
                    </button>
                  </Badge>
                ))}
              </div>
            )}
            <div className="flex gap-2">
              <Input
                placeholder="e.g., 8 hours sleep, family dinner"
                value={newNonNegotiable}
                onChange={(e) => setNewNonNegotiable(e.target.value)}
                onKeyDown={(e) => {
                  if (e.key === 'Enter') {
                    e.preventDefault()
                    handleAddNonNegotiable()
                  }
                }}
              />
              <Button
                type="button"
                variant="outline"
                size="icon"
                onClick={handleAddNonNegotiable}
              >
                <Plus className="size-4" />
              </Button>
            </div>
          </div>

          <Button
            variant="outline"
            onClick={() => setShowForm(false)}
            className="w-full"
          >
            Cancel and skip
          </Button>
        </div>
      )}

      <StepNavigation
        currentStep={currentStep}
        totalSteps={totalSteps}
        onNext={handleNext}
        onPrev={onBack}
        isNextDisabled={showForm && !season.label.trim()}
        isSubmitting={isSubmitting}
        nextLabel="Complete Setup"
        showSkip={!showForm}
        onSkip={handleSkip}
      />
    </div>
  )
}
