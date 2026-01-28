import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { useForm, Controller } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { ArrowLeft, ArrowRight, Check, FlaskConical, Lightbulb, BarChart3, Eye, Loader2, ChevronsUpDown, X, Target, Plus } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Textarea } from '@/components/ui/textarea'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select'
import { Slider } from '@/components/ui/slider'
import { Badge } from '@/components/ui/badge'
import { Popover, PopoverContent, PopoverTrigger } from '@/components/ui/popover'
import { Command, CommandEmpty, CommandGroup, CommandInput, CommandItem, CommandList } from '@/components/ui/command'
import { cn } from '@/lib/utils'
import { useCreateExperiment } from '../../hooks'
import { createExperimentSchema, type CreateExperimentFormData } from '../../schemas'
import { experimentCategoryInfo, metricDataTypeInfo, metricDirectionInfo } from '@/types'
import type { MetricDefinitionDto } from '@/types'
import { useGoals } from '@/features/goals/hooks/use-goals'
import { useMetrics } from '@/features/goals/hooks/use-metrics'
import { MetricLibraryDialog } from '@/features/goals/components/metric-library/metric-library-dialog'

const STEPS = [
  { label: 'Basics', icon: FlaskConical },
  { label: 'Hypothesis', icon: Lightbulb },
  { label: 'Measurement', icon: BarChart3 },
  { label: 'Review', icon: Eye },
] as const

const CATEGORY_OPTIONS: Array<keyof typeof experimentCategoryInfo> = [
  'Habit',
  'Routine',
  'Environment',
  'Mindset',
  'Productivity',
  'Health',
  'Social',
  'PlanRealism',
  'FrictionReduction',
  'CheckInConsistency',
  'Top1FollowThrough',
  'Other',
]

const AGGREGATION_OPTIONS = [
  { value: 'Sum', label: 'Sum' },
  { value: 'Average', label: 'Average' },
  { value: 'Max', label: 'Max' },
  { value: 'Min', label: 'Min' },
  { value: 'Count', label: 'Count' },
  { value: 'Latest', label: 'Latest' },
] as const

// Fields validated per step for partial validation
const STEP_FIELDS: Record<number, Array<keyof CreateExperimentFormData | string>> = {
  0: ['title', 'category', 'createdFrom'],
  1: ['hypothesis.change', 'hypothesis.expectedOutcome'],
  2: ['measurementPlan.primaryMetricDefinitionId', 'measurementPlan.primaryAggregation'],
  3: [],
}

export function ExperimentWizard() {
  const [currentStep, setCurrentStep] = useState(0)
  const [goalsOpen, setGoalsOpen] = useState(false)
  const [primaryMetricDialogOpen, setPrimaryMetricDialogOpen] = useState(false)
  const [guardrailMetricsOpen, setGuardrailMetricsOpen] = useState(false)
  const [guardrailMetricDialogOpen, setGuardrailMetricDialogOpen] = useState(false)
  const navigate = useNavigate()
  const createExperiment = useCreateExperiment()
  const { data: goals } = useGoals('Active')
  const { data: metrics } = useMetrics()

  const form = useForm<CreateExperimentFormData>({
    resolver: zodResolver(createExperimentSchema),
    defaultValues: {
      title: '',
      description: '',
      category: 'Other',
      createdFrom: 'Manual',
      hypothesis: {
        change: '',
        expectedOutcome: '',
        rationale: '',
      },
      measurementPlan: {
        primaryMetricDefinitionId: '',
        primaryAggregation: 'Average',
        baselineWindowDays: 7,
        runWindowDays: 7,
        guardrailMetricDefinitionIds: [],
        minComplianceThreshold: 0.7,
      },
      linkedGoalIds: [],
      startDate: '',
      endDatePlanned: '',
    },
    mode: 'onTouched',
  })

  const {
    register,
    control,
    handleSubmit,
    trigger,
    watch,
    formState: { errors },
  } = form

  const formValues = watch()

  async function handleNext() {
    const fieldsToValidate = STEP_FIELDS[currentStep]
    if (fieldsToValidate && fieldsToValidate.length > 0) {
      const valid = await trigger(fieldsToValidate as any)
      if (!valid) return
    }
    setCurrentStep((s) => Math.min(s + 1, STEPS.length - 1))
  }

  function handleBack() {
    setCurrentStep((s) => Math.max(s - 1, 0))
  }

  function handleJumpToStep(step: number) {
    setCurrentStep(step)
  }

  async function onSubmit(data: CreateExperimentFormData) {
    try {
      const result = await createExperiment.mutateAsync({
        title: data.title,
        category: data.category,
        createdFrom: data.createdFrom,
        hypothesis: {
          change: data.hypothesis.change,
          expectedOutcome: data.hypothesis.expectedOutcome,
          rationale: data.hypothesis.rationale,
        },
        measurementPlan: {
          primaryMetricDefinitionId: data.measurementPlan.primaryMetricDefinitionId,
          primaryAggregation: data.measurementPlan.primaryAggregation,
          baselineWindowDays: data.measurementPlan.baselineWindowDays,
          runWindowDays: data.measurementPlan.runWindowDays,
          guardrailMetricDefinitionIds: data.measurementPlan.guardrailMetricDefinitionIds?.filter(
            id => id !== data.measurementPlan.primaryMetricDefinitionId
          ),
          minComplianceThreshold: data.measurementPlan.minComplianceThreshold,
        },
        description: data.description || undefined,
        linkedGoalIds: data.linkedGoalIds?.length ? data.linkedGoalIds : undefined,
        startDate: data.startDate || undefined,
        endDatePlanned: data.endDatePlanned || undefined,
      })
      navigate(`/experiments/${result}`)
    } catch {
      // Error is handled by TanStack Query
    }
  }

  // Helper to get nested errors
  function getError(path: string): string | undefined {
    const parts = path.split('.')
    let current: any = errors
    for (const part of parts) {
      if (!current?.[part]) return undefined
      current = current[part]
    }
    return current?.message as string | undefined
  }

  return (
    <div className="mx-auto max-w-2xl space-y-8">
      {/* Step Indicator */}
      <nav aria-label="Wizard progress" className="px-2">
        <ol className="flex items-center justify-between">
          {STEPS.map((step, index) => {
            const Icon = step.icon
            const isCompleted = index < currentStep
            const isCurrent = index === currentStep
            return (
              <li key={step.label} className="flex items-center">
                <button
                  type="button"
                  onClick={() => index < currentStep && handleJumpToStep(index)}
                  disabled={index > currentStep}
                  className={cn(
                    'flex flex-col items-center gap-2 transition-colors',
                    index <= currentStep ? 'cursor-pointer' : 'cursor-not-allowed',
                  )}
                >
                  <div
                    className={cn(
                      'flex h-10 w-10 items-center justify-center rounded-full border-2 transition-all duration-300',
                      isCompleted && 'border-emerald-500 bg-emerald-500/20 text-emerald-400',
                      isCurrent && 'border-violet-500 bg-violet-500/20 text-violet-400 ring-4 ring-violet-500/10',
                      !isCompleted && !isCurrent && 'border-zinc-700 bg-zinc-800/50 text-zinc-500',
                    )}
                  >
                    {isCompleted ? (
                      <Check className="h-4 w-4" />
                    ) : (
                      <Icon className="h-4 w-4" />
                    )}
                  </div>
                  <span
                    className={cn(
                      'text-xs font-medium transition-colors',
                      isCurrent && 'text-violet-400',
                      isCompleted && 'text-emerald-400',
                      !isCompleted && !isCurrent && 'text-zinc-500',
                    )}
                  >
                    {step.label}
                  </span>
                </button>
                {index < STEPS.length - 1 && (
                  <div
                    className={cn(
                      'mx-3 mt-[-1.5rem] h-0.5 w-12 rounded-full transition-colors duration-300 sm:w-20',
                      index < currentStep ? 'bg-emerald-500/50' : 'bg-zinc-700',
                    )}
                  />
                )}
              </li>
            )
          })}
        </ol>
      </nav>

      {/* Step Content */}
      <div className="space-y-6">
        <div className="min-h-[420px]">
          {/* Step 0: Basics */}
          {currentStep === 0 && (
            <div className="space-y-6 animate-in fade-in slide-in-from-right-4 duration-300">
              <div>
                <h2 className="text-xl font-semibold text-zinc-100">Experiment Basics</h2>
                <p className="mt-1 text-sm text-zinc-400">
                  Name your experiment and choose a category to get started.
                </p>
              </div>

              <div className="space-y-4">
                <div className="space-y-2">
                  <Label htmlFor="title">Title *</Label>
                  <Input
                    id="title"
                    placeholder="e.g., Morning block scheduling for deep work"
                    {...register('title')}
                  />
                  {getError('title') && (
                    <p className="text-sm text-red-400">{getError('title')}</p>
                  )}
                </div>

                <div className="space-y-2">
                  <Label htmlFor="description">Description</Label>
                  <Textarea
                    id="description"
                    placeholder="Describe what you want to test and why..."
                    rows={3}
                    {...register('description')}
                  />
                  {getError('description') && (
                    <p className="text-sm text-red-400">{getError('description')}</p>
                  )}
                </div>

                <div className="space-y-2">
                  <Label>Category *</Label>
                  <Controller
                    name="category"
                    control={control}
                    render={({ field }) => (
                      <Select value={field.value} onValueChange={field.onChange}>
                        <SelectTrigger>
                          <SelectValue placeholder="Select a category" />
                        </SelectTrigger>
                        <SelectContent>
                          {CATEGORY_OPTIONS.map((cat) => {
                            const info = experimentCategoryInfo[cat]
                            return (
                              <SelectItem key={cat} value={cat}>
                                <span className={info.color}>{info.label}</span>
                                <span className="ml-2 text-xs text-zinc-500">
                                  {info.description}
                                </span>
                              </SelectItem>
                            )
                          })}
                        </SelectContent>
                      </Select>
                    )}
                  />
                  {getError('category') && (
                    <p className="text-sm text-red-400">{getError('category')}</p>
                  )}
                </div>

                <div className="space-y-2">
                  <Label>Created From</Label>
                  <Controller
                    name="createdFrom"
                    control={control}
                    render={({ field }) => (
                      <Select value={field.value} onValueChange={field.onChange}>
                        <SelectTrigger>
                          <SelectValue />
                        </SelectTrigger>
                        <SelectContent>
                          <SelectItem value="Manual">Manual</SelectItem>
                          <SelectItem value="WeeklyReview">Weekly Review</SelectItem>
                          <SelectItem value="Diagnostic">Diagnostic</SelectItem>
                          <SelectItem value="Coaching">Coaching</SelectItem>
                        </SelectContent>
                      </Select>
                    )}
                  />
                </div>

                <div className="grid grid-cols-2 gap-4">
                  <div className="space-y-2">
                    <Label htmlFor="startDate">Start Date</Label>
                    <Input
                      id="startDate"
                      type="date"
                      {...register('startDate')}
                    />
                  </div>
                  <div className="space-y-2">
                    <Label htmlFor="endDatePlanned">End Date (Planned)</Label>
                    <Input
                      id="endDatePlanned"
                      type="date"
                      {...register('endDatePlanned')}
                    />
                  </div>
                </div>

                <div className="space-y-2">
                  <Label>Linked Goals</Label>
                  <Controller
                    name="linkedGoalIds"
                    control={control}
                    render={({ field }) => {
                      const selectedIds = field.value ?? []
                      const selectedGoals = (goals ?? []).filter(g => selectedIds.includes(g.id))

                      return (
                        <div className="space-y-2">
                          <Popover open={goalsOpen} onOpenChange={setGoalsOpen}>
                            <PopoverTrigger asChild>
                              <Button
                                type="button"
                                variant="outline"
                                role="combobox"
                                aria-expanded={goalsOpen}
                                className="w-full justify-between font-normal"
                              >
                                <span className="truncate text-muted-foreground">
                                  {selectedIds.length === 0
                                    ? 'Select goals...'
                                    : `${selectedIds.length} goal${selectedIds.length > 1 ? 's' : ''} selected`}
                                </span>
                                <ChevronsUpDown className="ml-2 h-4 w-4 shrink-0 opacity-50" />
                              </Button>
                            </PopoverTrigger>
                            <PopoverContent className="w-[--radix-popover-trigger-width] p-0" align="start">
                              <Command>
                                <CommandInput placeholder="Search goals..." />
                                <CommandList>
                                  <CommandEmpty>No goals found.</CommandEmpty>
                                  <CommandGroup>
                                    {(goals ?? []).map((goal) => {
                                      const isSelected = selectedIds.includes(goal.id)
                                      return (
                                        <CommandItem
                                          key={goal.id}
                                          value={goal.title}
                                          onSelect={() => {
                                            const next = isSelected
                                              ? selectedIds.filter((id: string) => id !== goal.id)
                                              : [...selectedIds, goal.id]
                                            field.onChange(next)
                                          }}
                                        >
                                          <div className={cn(
                                            'mr-2 flex h-4 w-4 items-center justify-center rounded-sm border',
                                            isSelected
                                              ? 'border-primary bg-primary text-primary-foreground'
                                              : 'border-muted-foreground/40 opacity-50',
                                          )}>
                                            {isSelected && <Check className="h-3 w-3" />}
                                          </div>
                                          <span className="truncate">{goal.title}</span>
                                        </CommandItem>
                                      )
                                    })}
                                  </CommandGroup>
                                </CommandList>
                              </Command>
                            </PopoverContent>
                          </Popover>

                          {/* Selected goal chips */}
                          {selectedGoals.length > 0 && (
                            <div className="flex flex-wrap gap-1.5">
                              {selectedGoals.map((goal) => (
                                <Badge
                                  key={goal.id}
                                  variant="secondary"
                                  className="gap-1 pl-2 pr-1 text-xs"
                                >
                                  <Target className="h-3 w-3 text-green-400" />
                                  {goal.title}
                                  <button
                                    type="button"
                                    className="ml-0.5 rounded-full p-0.5 hover:bg-muted"
                                    onClick={() => {
                                      field.onChange(selectedIds.filter((id: string) => id !== goal.id))
                                    }}
                                  >
                                    <X className="h-3 w-3" />
                                  </button>
                                </Badge>
                              ))}
                            </div>
                          )}
                        </div>
                      )
                    }}
                  />
                  <p className="text-xs text-zinc-500">
                    Optionally link this experiment to active goals.
                  </p>
                </div>
              </div>
            </div>
          )}

          {/* Step 1: Hypothesis */}
          {currentStep === 1 && (
            <div className="space-y-6 animate-in fade-in slide-in-from-right-4 duration-300">
              <div>
                <h2 className="text-xl font-semibold text-zinc-100">Build Your Hypothesis</h2>
                <p className="mt-1 text-sm text-zinc-400">
                  Structure your experiment as an If / Then / Because statement.
                </p>
              </div>

              <div className="space-y-5">
                {/* IF - Change */}
                <div className="rounded-lg border border-blue-500/30 bg-blue-500/5 p-4">
                  <div className="mb-3 flex items-center gap-2">
                    <div className="h-6 w-1 rounded-full bg-blue-500" />
                    <Label className="text-sm font-semibold uppercase tracking-wider text-blue-400">
                      If I...
                    </Label>
                  </div>
                  <Textarea
                    placeholder="Describe the specific change you will make..."
                    rows={3}
                    className="border-blue-500/20 bg-zinc-900/50 focus-visible:ring-blue-500/30"
                    {...register('hypothesis.change')}
                  />
                  {getError('hypothesis.change') && (
                    <p className="mt-1.5 text-sm text-red-400">{getError('hypothesis.change')}</p>
                  )}
                </div>

                {/* THEN - Expected Outcome */}
                <div className="rounded-lg border border-emerald-500/30 bg-emerald-500/5 p-4">
                  <div className="mb-3 flex items-center gap-2">
                    <div className="h-6 w-1 rounded-full bg-emerald-500" />
                    <Label className="text-sm font-semibold uppercase tracking-wider text-emerald-400">
                      Then I expect...
                    </Label>
                  </div>
                  <Textarea
                    placeholder="Describe what outcome you expect to see..."
                    rows={3}
                    className="border-emerald-500/20 bg-zinc-900/50 focus-visible:ring-emerald-500/30"
                    {...register('hypothesis.expectedOutcome')}
                  />
                  {getError('hypothesis.expectedOutcome') && (
                    <p className="mt-1.5 text-sm text-red-400">{getError('hypothesis.expectedOutcome')}</p>
                  )}
                </div>

                {/* BECAUSE - Rationale */}
                <div className="rounded-lg border border-amber-500/30 bg-amber-500/5 p-4">
                  <div className="mb-3 flex items-center gap-2">
                    <div className="h-6 w-1 rounded-full bg-amber-500" />
                    <Label className="text-sm font-semibold uppercase tracking-wider text-amber-400">
                      Because...
                    </Label>
                    <Badge variant="outline" className="ml-auto border-zinc-700 text-xs text-zinc-500">
                      Optional
                    </Badge>
                  </div>
                  <Textarea
                    placeholder="Explain the reasoning or evidence behind your hypothesis..."
                    rows={3}
                    className="border-amber-500/20 bg-zinc-900/50 focus-visible:ring-amber-500/30"
                    {...register('hypothesis.rationale')}
                  />
                </div>
              </div>
            </div>
          )}

          {/* Step 2: Measurement */}
          {currentStep === 2 && (
            <div className="space-y-6 animate-in fade-in slide-in-from-right-4 duration-300">
              <div>
                <h2 className="text-xl font-semibold text-zinc-100">Measurement Plan</h2>
                <p className="mt-1 text-sm text-zinc-400">
                  Define how you will measure the experiment's outcome.
                </p>
              </div>

              <div className="space-y-5">
                <div className="space-y-2">
                  <Label>Primary Metric *</Label>
                  <Controller
                    name="measurementPlan.primaryMetricDefinitionId"
                    control={control}
                    render={({ field }) => {
                      const selectedMetric = (metrics ?? []).find(m => m.id === field.value)

                      return (
                        <div className="space-y-2">
                          {selectedMetric ? (
                            <div className="flex items-center gap-3 rounded-lg border border-cyan-500/30 bg-cyan-500/5 p-3">
                              <div className="flex size-9 items-center justify-center rounded-lg bg-cyan-500/10">
                                <span className="text-base">{metricDataTypeInfo[selectedMetric.dataType].icon}</span>
                              </div>
                              <div className="flex-1 min-w-0">
                                <p className="text-sm font-medium text-zinc-200 truncate">{selectedMetric.name}</p>
                                <p className="text-xs text-zinc-500">
                                  {metricDataTypeInfo[selectedMetric.dataType].label}
                                  {' · '}
                                  {metricDirectionInfo[selectedMetric.direction].icon}{' '}
                                  {metricDirectionInfo[selectedMetric.direction].label}
                                  {selectedMetric.unit && ` · ${selectedMetric.unit.label}`}
                                </p>
                              </div>
                              <Button
                                type="button"
                                variant="ghost"
                                size="sm"
                                className="shrink-0 text-xs text-zinc-400 hover:text-zinc-200"
                                onClick={() => setPrimaryMetricDialogOpen(true)}
                              >
                                Change
                              </Button>
                            </div>
                          ) : (
                            <Button
                              type="button"
                              variant="outline"
                              className="w-full justify-start gap-2 font-normal text-muted-foreground"
                              onClick={() => setPrimaryMetricDialogOpen(true)}
                            >
                              <BarChart3 className="h-4 w-4" />
                              Select a metric...
                            </Button>
                          )}

                          <MetricLibraryDialog
                            open={primaryMetricDialogOpen}
                            onOpenChange={setPrimaryMetricDialogOpen}
                            selectionMode
                            onSelectMetric={(metric: MetricDefinitionDto) => {
                              field.onChange(metric.id)
                            }}
                          />
                        </div>
                      )
                    }}
                  />
                  {getError('measurementPlan.primaryMetricDefinitionId') && (
                    <p className="text-sm text-red-400">
                      {getError('measurementPlan.primaryMetricDefinitionId')}
                    </p>
                  )}
                </div>

                <div className="space-y-2">
                  <Label>Aggregation Method *</Label>
                  <Controller
                    name="measurementPlan.primaryAggregation"
                    control={control}
                    render={({ field }) => (
                      <Select value={field.value} onValueChange={field.onChange}>
                        <SelectTrigger>
                          <SelectValue placeholder="Select aggregation" />
                        </SelectTrigger>
                        <SelectContent>
                          {AGGREGATION_OPTIONS.map((opt) => (
                            <SelectItem key={opt.value} value={opt.value}>
                              {opt.label}
                            </SelectItem>
                          ))}
                        </SelectContent>
                      </Select>
                    )}
                  />
                  {getError('measurementPlan.primaryAggregation') && (
                    <p className="text-sm text-red-400">
                      {getError('measurementPlan.primaryAggregation')}
                    </p>
                  )}
                </div>

                <div className="grid grid-cols-2 gap-6">
                  {/* Baseline Window */}
                  <div className="space-y-3">
                    <div className="flex items-center justify-between">
                      <Label>Baseline Window</Label>
                      <span className="text-sm font-medium text-zinc-300">
                        {formValues.measurementPlan?.baselineWindowDays ?? 7} days
                      </span>
                    </div>
                    <Controller
                      name="measurementPlan.baselineWindowDays"
                      control={control}
                      render={({ field }) => (
                        <Slider
                          min={1}
                          max={90}
                          step={1}
                          value={[field.value]}
                          onValueChange={([v]) => field.onChange(v)}
                        />
                      )}
                    />
                    <p className="text-xs text-zinc-500">
                      Days of data collected before the experiment starts.
                    </p>
                  </div>

                  {/* Run Window */}
                  <div className="space-y-3">
                    <div className="flex items-center justify-between">
                      <Label>Run Window</Label>
                      <span className="text-sm font-medium text-zinc-300">
                        {formValues.measurementPlan?.runWindowDays ?? 7} days
                      </span>
                    </div>
                    <Controller
                      name="measurementPlan.runWindowDays"
                      control={control}
                      render={({ field }) => (
                        <Slider
                          min={1}
                          max={90}
                          step={1}
                          value={[field.value]}
                          onValueChange={([v]) => field.onChange(v)}
                        />
                      )}
                    />
                    <p className="text-xs text-zinc-500">
                      Days the experiment will actively run.
                    </p>
                  </div>
                </div>

                {/* Compliance Threshold */}
                <div className="space-y-3">
                  <div className="flex items-center justify-between">
                    <Label>Minimum Compliance Threshold</Label>
                    <span className="text-sm font-medium text-zinc-300">
                      {Math.round((formValues.measurementPlan?.minComplianceThreshold ?? 0.7) * 100)}%
                    </span>
                  </div>
                  <Controller
                    name="measurementPlan.minComplianceThreshold"
                    control={control}
                    render={({ field }) => (
                      <Slider
                        min={0}
                        max={100}
                        step={5}
                        value={[Math.round(field.value * 100)]}
                        onValueChange={([v]) => field.onChange(v / 100)}
                      />
                    )}
                  />
                  <p className="text-xs text-zinc-500">
                    Minimum adherence rate for the experiment to be considered valid.
                  </p>
                </div>

                <div className="space-y-2">
                  <Label>Guardrail Metrics</Label>
                  <Controller
                    name="measurementPlan.guardrailMetricDefinitionIds"
                    control={control}
                    render={({ field }) => {
                      const selectedIds = field.value ?? []
                      const primaryId = formValues.measurementPlan?.primaryMetricDefinitionId
                      const availableMetrics = (metrics ?? []).filter(m => !m.isArchived && m.id !== primaryId)
                      const selectedMetrics = availableMetrics.filter(m => selectedIds.includes(m.id))

                      return (
                        <div className="space-y-2">
                          <div className="flex gap-2">
                            <Popover open={guardrailMetricsOpen} onOpenChange={setGuardrailMetricsOpen}>
                              <PopoverTrigger asChild>
                                <Button
                                  type="button"
                                  variant="outline"
                                  role="combobox"
                                  aria-expanded={guardrailMetricsOpen}
                                  className="flex-1 justify-between font-normal"
                                >
                                  <span className="truncate text-muted-foreground">
                                    {selectedIds.length === 0
                                      ? 'Select guardrail metrics...'
                                      : `${selectedIds.length} metric${selectedIds.length > 1 ? 's' : ''} selected`}
                                  </span>
                                  <ChevronsUpDown className="ml-2 h-4 w-4 shrink-0 opacity-50" />
                                </Button>
                              </PopoverTrigger>
                              <PopoverContent className="w-[--radix-popover-trigger-width] p-0" align="start">
                                <Command>
                                  <CommandInput placeholder="Search metrics..." />
                                  <CommandList>
                                    <CommandEmpty>No metrics found.</CommandEmpty>
                                    <CommandGroup>
                                      {availableMetrics.map((metric) => {
                                        const isSelected = selectedIds.includes(metric.id)
                                        return (
                                          <CommandItem
                                            key={metric.id}
                                            value={metric.name}
                                            onSelect={() => {
                                              const next = isSelected
                                                ? selectedIds.filter((id: string) => id !== metric.id)
                                                : [...selectedIds, metric.id]
                                              field.onChange(next)
                                            }}
                                          >
                                            <div className={cn(
                                              'mr-2 flex h-4 w-4 items-center justify-center rounded-sm border',
                                              isSelected
                                                ? 'border-primary bg-primary text-primary-foreground'
                                                : 'border-muted-foreground/40 opacity-50',
                                            )}>
                                              {isSelected && <Check className="h-3 w-3" />}
                                            </div>
                                            <span className="truncate">{metric.name}</span>
                                            <span className="ml-auto text-xs text-zinc-500">
                                              {metricDataTypeInfo[metric.dataType].icon}
                                            </span>
                                          </CommandItem>
                                        )
                                      })}
                                    </CommandGroup>
                                  </CommandList>
                                </Command>
                              </PopoverContent>
                            </Popover>

                            <Button
                              type="button"
                              variant="outline"
                              size="icon"
                              className="shrink-0"
                              title="Create new metric"
                              onClick={() => setGuardrailMetricDialogOpen(true)}
                            >
                              <Plus className="h-4 w-4" />
                            </Button>
                          </div>

                          <MetricLibraryDialog
                            open={guardrailMetricDialogOpen}
                            onOpenChange={setGuardrailMetricDialogOpen}
                            selectionMode
                            onSelectMetric={(metric: MetricDefinitionDto) => {
                              if (!selectedIds.includes(metric.id)) {
                                field.onChange([...selectedIds, metric.id])
                              }
                            }}
                          />

                          {selectedMetrics.length > 0 && (
                            <div className="flex flex-wrap gap-1.5">
                              {selectedMetrics.map((metric) => (
                                <Badge
                                  key={metric.id}
                                  variant="secondary"
                                  className="gap-1 pl-2 pr-1 text-xs"
                                >
                                  <span>{metricDataTypeInfo[metric.dataType].icon}</span>
                                  {metric.name}
                                  <button
                                    type="button"
                                    className="ml-0.5 rounded-full p-0.5 hover:bg-muted"
                                    onClick={() => {
                                      field.onChange(selectedIds.filter((id: string) => id !== metric.id))
                                    }}
                                  >
                                    <X className="h-3 w-3" />
                                  </button>
                                </Badge>
                              ))}
                            </div>
                          )}
                        </div>
                      )
                    }}
                  />
                  <p className="text-xs text-zinc-500">
                    Metrics to monitor so the experiment doesn't cause harm elsewhere.
                  </p>
                </div>
              </div>
            </div>
          )}

          {/* Step 3: Review */}
          {currentStep === 3 && (
            <div className="space-y-6 animate-in fade-in slide-in-from-right-4 duration-300">
              <div>
                <h2 className="text-xl font-semibold text-zinc-100">Review Your Experiment</h2>
                <p className="mt-1 text-sm text-zinc-400">
                  Double-check everything before creating your experiment.
                </p>
              </div>

              {/* Basics Summary */}
              <Card className="border-zinc-800 bg-zinc-900/50">
                <CardHeader className="flex flex-row items-center justify-between pb-3">
                  <CardTitle className="flex items-center gap-2 text-sm font-medium text-zinc-300">
                    <FlaskConical className="h-4 w-4 text-violet-400" />
                    Basics
                  </CardTitle>
                  <Button
                    type="button"
                    variant="ghost"
                    size="sm"
                    className="h-7 text-xs text-zinc-400 hover:text-zinc-200"
                    onClick={() => handleJumpToStep(0)}
                  >
                    Edit
                  </Button>
                </CardHeader>
                <CardContent className="space-y-2 text-sm">
                  <div className="flex justify-between">
                    <span className="text-zinc-500">Title</span>
                    <span className="text-zinc-200">{formValues.title || '---'}</span>
                  </div>
                  {formValues.description && (
                    <div className="flex justify-between gap-4">
                      <span className="shrink-0 text-zinc-500">Description</span>
                      <span className="text-right text-zinc-200">{formValues.description}</span>
                    </div>
                  )}
                  <div className="flex justify-between">
                    <span className="text-zinc-500">Category</span>
                    <Badge variant="outline" className={cn('text-xs', experimentCategoryInfo[formValues.category]?.color)}>
                      {experimentCategoryInfo[formValues.category]?.label ?? formValues.category}
                    </Badge>
                  </div>
                  <div className="flex justify-between">
                    <span className="text-zinc-500">Created From</span>
                    <span className="text-zinc-200">{formValues.createdFrom}</span>
                  </div>
                  {(formValues.startDate || formValues.endDatePlanned) && (
                    <div className="flex justify-between">
                      <span className="text-zinc-500">Timeline</span>
                      <span className="text-zinc-200">
                        {formValues.startDate || '?'} &rarr; {formValues.endDatePlanned || '?'}
                      </span>
                    </div>
                  )}
                  {formValues.linkedGoalIds && formValues.linkedGoalIds.length > 0 && (
                    <div className="flex justify-between gap-4">
                      <span className="shrink-0 text-zinc-500">Linked Goals</span>
                      <div className="flex flex-wrap justify-end gap-1">
                        {formValues.linkedGoalIds.map((id) => {
                          const goal = (goals ?? []).find(g => g.id === id)
                          return (
                            <Badge key={id} variant="secondary" className="gap-1 text-xs">
                              <Target className="h-3 w-3 text-green-400" />
                              {goal?.title ?? id.slice(0, 8)}
                            </Badge>
                          )
                        })}
                      </div>
                    </div>
                  )}
                </CardContent>
              </Card>

              {/* Hypothesis Summary */}
              <Card className="border-zinc-800 bg-zinc-900/50">
                <CardHeader className="flex flex-row items-center justify-between pb-3">
                  <CardTitle className="flex items-center gap-2 text-sm font-medium text-zinc-300">
                    <Lightbulb className="h-4 w-4 text-amber-400" />
                    Hypothesis
                  </CardTitle>
                  <Button
                    type="button"
                    variant="ghost"
                    size="sm"
                    className="h-7 text-xs text-zinc-400 hover:text-zinc-200"
                    onClick={() => handleJumpToStep(1)}
                  >
                    Edit
                  </Button>
                </CardHeader>
                <CardContent className="space-y-3">
                  <div className="rounded border-l-2 border-blue-500 bg-blue-500/5 py-2 pl-3 pr-2">
                    <p className="text-xs font-medium uppercase tracking-wider text-blue-400">If I...</p>
                    <p className="mt-1 text-sm text-zinc-200">
                      {formValues.hypothesis?.change || '---'}
                    </p>
                  </div>
                  <div className="rounded border-l-2 border-emerald-500 bg-emerald-500/5 py-2 pl-3 pr-2">
                    <p className="text-xs font-medium uppercase tracking-wider text-emerald-400">Then I expect...</p>
                    <p className="mt-1 text-sm text-zinc-200">
                      {formValues.hypothesis?.expectedOutcome || '---'}
                    </p>
                  </div>
                  {formValues.hypothesis?.rationale && (
                    <div className="rounded border-l-2 border-amber-500 bg-amber-500/5 py-2 pl-3 pr-2">
                      <p className="text-xs font-medium uppercase tracking-wider text-amber-400">Because...</p>
                      <p className="mt-1 text-sm text-zinc-200">
                        {formValues.hypothesis.rationale}
                      </p>
                    </div>
                  )}
                </CardContent>
              </Card>

              {/* Measurement Summary */}
              <Card className="border-zinc-800 bg-zinc-900/50">
                <CardHeader className="flex flex-row items-center justify-between pb-3">
                  <CardTitle className="flex items-center gap-2 text-sm font-medium text-zinc-300">
                    <BarChart3 className="h-4 w-4 text-cyan-400" />
                    Measurement Plan
                  </CardTitle>
                  <Button
                    type="button"
                    variant="ghost"
                    size="sm"
                    className="h-7 text-xs text-zinc-400 hover:text-zinc-200"
                    onClick={() => handleJumpToStep(2)}
                  >
                    Edit
                  </Button>
                </CardHeader>
                <CardContent className="space-y-2 text-sm">
                  <div className="flex justify-between gap-4">
                    <span className="text-zinc-500">Primary Metric</span>
                    {(() => {
                      const pm = (metrics ?? []).find(m => m.id === formValues.measurementPlan?.primaryMetricDefinitionId)
                      return pm ? (
                        <Badge variant="secondary" className="gap-1 text-xs">
                          <span>{metricDataTypeInfo[pm.dataType].icon}</span>
                          {pm.name}
                        </Badge>
                      ) : (
                        <span className="text-zinc-400">---</span>
                      )
                    })()}
                  </div>
                  <div className="flex justify-between">
                    <span className="text-zinc-500">Aggregation</span>
                    <span className="text-zinc-200">{formValues.measurementPlan?.primaryAggregation}</span>
                  </div>
                  <div className="flex justify-between">
                    <span className="text-zinc-500">Baseline Window</span>
                    <span className="text-zinc-200">{formValues.measurementPlan?.baselineWindowDays} days</span>
                  </div>
                  <div className="flex justify-between">
                    <span className="text-zinc-500">Run Window</span>
                    <span className="text-zinc-200">{formValues.measurementPlan?.runWindowDays} days</span>
                  </div>
                  <div className="flex justify-between">
                    <span className="text-zinc-500">Compliance Threshold</span>
                    <span className="text-zinc-200">
                      {Math.round((formValues.measurementPlan?.minComplianceThreshold ?? 0.7) * 100)}%
                    </span>
                  </div>
                  {formValues.measurementPlan?.guardrailMetricDefinitionIds &&
                    formValues.measurementPlan.guardrailMetricDefinitionIds.length > 0 && (
                      <div className="flex justify-between gap-4">
                        <span className="shrink-0 text-zinc-500">Guardrail Metrics</span>
                        <div className="flex flex-wrap justify-end gap-1">
                          {formValues.measurementPlan.guardrailMetricDefinitionIds.map((id) => {
                            const metric = (metrics ?? []).find(m => m.id === id)
                            return (
                              <Badge key={id} variant="secondary" className="gap-1 text-xs">
                                {metric ? (
                                  <>
                                    <span>{metricDataTypeInfo[metric.dataType].icon}</span>
                                    {metric.name}
                                  </>
                                ) : (
                                  id.slice(0, 8)
                                )}
                              </Badge>
                            )
                          })}
                        </div>
                      </div>
                    )}
                </CardContent>
              </Card>
            </div>
          )}
        </div>

        {/* Navigation */}
        <div className="flex items-center justify-between border-t border-zinc-800 pt-5">
          <Button
            type="button"
            variant="ghost"
            onClick={handleBack}
            disabled={currentStep === 0}
            className="gap-2 text-zinc-400 hover:text-zinc-200"
          >
            <ArrowLeft className="h-4 w-4" />
            Back
          </Button>

          {currentStep < STEPS.length - 1 ? (
            <Button
              type="button"
              onClick={handleNext}
              className="gap-2 bg-violet-600 text-white hover:bg-violet-500"
            >
              Next
              <ArrowRight className="h-4 w-4" />
            </Button>
          ) : (
            <Button
              type="button"
              disabled={createExperiment.isPending}
              onClick={handleSubmit(onSubmit)}
              className="gap-2 bg-emerald-600 text-white hover:bg-emerald-500"
            >
              {createExperiment.isPending ? (
                <>
                  <Loader2 className="h-4 w-4 animate-spin" />
                  Creating...
                </>
              ) : (
                <>
                  <Check className="h-4 w-4" />
                  Create Experiment
                </>
              )}
            </Button>
          )}
        </div>
      </div>
    </div>
  )
}
