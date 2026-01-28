import { useEffect } from 'react'
import { useParams, useNavigate, Link } from 'react-router-dom'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { ArrowLeft, Loader2, Save, AlertTriangle } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Textarea } from '@/components/ui/textarea'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Skeleton } from '@/components/ui/skeleton'
import { useExperiment, useUpdateExperiment } from '../hooks'
import { createExperimentSchema, type CreateExperimentFormData } from '../schemas'
import { experimentCategoryInfo } from '@/types'
import type { ExperimentCategory } from '@/types'

function EditExperimentSkeleton() {
  return (
    <div className="min-h-screen bg-background">
      <div className="container max-w-2xl py-8 px-4 sm:px-6 lg:px-8">
        <div className="space-y-6">
          <div className="flex items-center gap-4">
            <Skeleton className="size-10" />
            <Skeleton className="h-8 w-48" />
          </div>
          <Card>
            <CardHeader>
              <Skeleton className="h-6 w-32" />
            </CardHeader>
            <CardContent className="space-y-4">
              <Skeleton className="h-10 w-full" />
              <Skeleton className="h-24 w-full" />
              <Skeleton className="h-10 w-full" />
            </CardContent>
          </Card>
          <Card>
            <CardHeader>
              <Skeleton className="h-6 w-32" />
            </CardHeader>
            <CardContent className="space-y-4">
              <Skeleton className="h-24 w-full" />
              <Skeleton className="h-24 w-full" />
              <Skeleton className="h-24 w-full" />
            </CardContent>
          </Card>
        </div>
      </div>
    </div>
  )
}

export function Component() {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const { data: experiment, isLoading } = useExperiment(id!)
  const updateExperiment = useUpdateExperiment()

  const {
    register,
    handleSubmit,
    setValue,
    watch,
    reset,
    formState: { errors, isDirty },
  } = useForm<CreateExperimentFormData>({
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
  })

  useEffect(() => {
    if (experiment) {
      reset({
        title: experiment.title,
        description: experiment.description ?? '',
        category: experiment.category,
        createdFrom: experiment.createdFrom,
        hypothesis: {
          change: experiment.hypothesis.change,
          expectedOutcome: experiment.hypothesis.expectedOutcome,
          rationale: experiment.hypothesis.rationale ?? '',
        },
        measurementPlan: {
          primaryMetricDefinitionId: experiment.measurementPlan.primaryMetricDefinitionId,
          primaryAggregation: experiment.measurementPlan.primaryAggregation,
          baselineWindowDays: experiment.measurementPlan.baselineWindowDays,
          runWindowDays: experiment.measurementPlan.runWindowDays,
          guardrailMetricDefinitionIds: experiment.measurementPlan.guardrailMetricDefinitionIds,
          minComplianceThreshold: experiment.measurementPlan.minComplianceThreshold,
        },
        linkedGoalIds: experiment.linkedGoalIds,
        startDate: experiment.startDate ?? '',
        endDatePlanned: experiment.endDatePlanned ?? '',
      })
    }
  }, [experiment, reset])

  const category = watch('category')

  const onSubmit = async (data: CreateExperimentFormData) => {
    try {
      await updateExperiment.mutateAsync({
        id: id!,
        request: {
          title: data.title,
          description: data.description || undefined,
          category: data.category,
          hypothesis: {
            change: data.hypothesis.change,
            expectedOutcome: data.hypothesis.expectedOutcome,
            rationale: data.hypothesis.rationale || undefined,
          },
          measurementPlan: {
            primaryMetricDefinitionId: data.measurementPlan.primaryMetricDefinitionId,
            primaryAggregation: data.measurementPlan.primaryAggregation,
            baselineWindowDays: data.measurementPlan.baselineWindowDays,
            runWindowDays: data.measurementPlan.runWindowDays,
            guardrailMetricDefinitionIds: data.measurementPlan.guardrailMetricDefinitionIds,
            minComplianceThreshold: data.measurementPlan.minComplianceThreshold,
          },
          linkedGoalIds: data.linkedGoalIds,
          startDate: data.startDate || undefined,
          endDatePlanned: data.endDatePlanned || undefined,
        },
      })
      navigate(`/experiments/${id}`)
    } catch (error) {
      console.error('Failed to update experiment:', error)
    }
  }

  if (isLoading || !experiment) {
    return <EditExperimentSkeleton />
  }

  if (experiment.status !== 'Draft') {
    return (
      <div className="min-h-screen bg-background">
        <div className="container max-w-2xl py-8 px-4 sm:px-6 lg:px-8">
          <div className="flex flex-col items-center justify-center py-16 text-center space-y-4">
            <div className="p-3 rounded-full bg-yellow-500/10">
              <AlertTriangle className="size-8 text-yellow-500" />
            </div>
            <h2 className="text-xl font-semibold text-foreground">Cannot Edit Experiment</h2>
            <p className="text-muted-foreground max-w-md">
              Only experiments in Draft status can be edited. This experiment is currently{' '}
              <span className="font-medium">{experiment.status}</span>.
            </p>
            <Button variant="outline" asChild>
              <Link to={`/experiments/${id}`}>Back to Experiment</Link>
            </Button>
          </div>
        </div>
      </div>
    )
  }

  return (
    <div className="min-h-screen bg-background">
      <div className="container max-w-2xl py-8 px-4 sm:px-6 lg:px-8">
        <form onSubmit={handleSubmit(onSubmit)} className="space-y-6">
          <div className="flex items-center justify-between">
            <div className="flex items-center gap-4">
              <Button type="button" variant="ghost" size="icon" asChild>
                <Link to={`/experiments/${id}`}>
                  <ArrowLeft className="size-4" />
                </Link>
              </Button>
              <h1 className="text-2xl font-bold">Edit Experiment</h1>
            </div>
            <div className="flex items-center gap-2">
              <Button type="button" variant="outline" asChild>
                <Link to={`/experiments/${id}`}>Cancel</Link>
              </Button>
              <Button type="submit" disabled={!isDirty || updateExperiment.isPending}>
                {updateExperiment.isPending && <Loader2 className="size-4 mr-2 animate-spin" />}
                <Save className="size-4 mr-2" />
                Save Changes
              </Button>
            </div>
          </div>

          <Card>
            <CardHeader>
              <CardTitle>Experiment Details</CardTitle>
            </CardHeader>
            <CardContent className="space-y-4">
              <div className="space-y-2">
                <Label htmlFor="title">
                  Title <span className="text-destructive">*</span>
                </Label>
                <Input
                  id="title"
                  placeholder="What are you testing?"
                  {...register('title')}
                  className={errors.title ? 'border-destructive' : ''}
                />
                {errors.title && (
                  <p className="text-sm text-destructive">{errors.title.message}</p>
                )}
              </div>

              <div className="space-y-2">
                <Label htmlFor="description">Description</Label>
                <Textarea
                  id="description"
                  placeholder="Describe the experiment in more detail..."
                  rows={3}
                  {...register('description')}
                />
              </div>

              <div className="space-y-2">
                <Label>Category</Label>
                <Select
                  value={category}
                  onValueChange={(value) =>
                    setValue('category', value as ExperimentCategory, { shouldDirty: true })
                  }
                >
                  <SelectTrigger>
                    <SelectValue placeholder="Select category" />
                  </SelectTrigger>
                  <SelectContent>
                    {Object.entries(experimentCategoryInfo).map(([key, info]) => (
                      <SelectItem key={key} value={key}>
                        {info.label}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </div>

              <div className="grid gap-4 sm:grid-cols-2">
                <div className="space-y-2">
                  <Label htmlFor="startDate">Start Date</Label>
                  <Input id="startDate" type="date" {...register('startDate')} />
                </div>
                <div className="space-y-2">
                  <Label htmlFor="endDatePlanned">Planned End Date</Label>
                  <Input id="endDatePlanned" type="date" {...register('endDatePlanned')} />
                </div>
              </div>
            </CardContent>
          </Card>

          <Card>
            <CardHeader>
              <CardTitle>Hypothesis</CardTitle>
            </CardHeader>
            <CardContent className="space-y-4">
              <div className="space-y-2">
                <Label htmlFor="hypothesis.change">
                  What will you change? <span className="text-destructive">*</span>
                </Label>
                <Textarea
                  id="hypothesis.change"
                  placeholder="Describe the specific change you'll make..."
                  rows={2}
                  {...register('hypothesis.change')}
                  className={errors.hypothesis?.change ? 'border-destructive' : ''}
                />
                {errors.hypothesis?.change && (
                  <p className="text-sm text-destructive">{errors.hypothesis.change.message}</p>
                )}
              </div>

              <div className="space-y-2">
                <Label htmlFor="hypothesis.expectedOutcome">
                  Expected Outcome <span className="text-destructive">*</span>
                </Label>
                <Textarea
                  id="hypothesis.expectedOutcome"
                  placeholder="What do you expect to happen?"
                  rows={2}
                  {...register('hypothesis.expectedOutcome')}
                  className={errors.hypothesis?.expectedOutcome ? 'border-destructive' : ''}
                />
                {errors.hypothesis?.expectedOutcome && (
                  <p className="text-sm text-destructive">
                    {errors.hypothesis.expectedOutcome.message}
                  </p>
                )}
              </div>

              <div className="space-y-2">
                <Label htmlFor="hypothesis.rationale">Rationale</Label>
                <Textarea
                  id="hypothesis.rationale"
                  placeholder="Why do you think this will work?"
                  rows={2}
                  {...register('hypothesis.rationale')}
                />
              </div>
            </CardContent>
          </Card>

          <Card>
            <CardHeader>
              <CardTitle>Measurement Plan</CardTitle>
            </CardHeader>
            <CardContent className="space-y-4">
              <div className="grid gap-4 sm:grid-cols-2">
                <div className="space-y-2">
                  <Label htmlFor="measurementPlan.baselineWindowDays">Baseline Window (days)</Label>
                  <Input
                    id="measurementPlan.baselineWindowDays"
                    type="number"
                    min={1}
                    max={90}
                    {...register('measurementPlan.baselineWindowDays', { valueAsNumber: true })}
                  />
                </div>
                <div className="space-y-2">
                  <Label htmlFor="measurementPlan.runWindowDays">Run Window (days)</Label>
                  <Input
                    id="measurementPlan.runWindowDays"
                    type="number"
                    min={1}
                    max={90}
                    {...register('measurementPlan.runWindowDays', { valueAsNumber: true })}
                  />
                </div>
              </div>

              <div className="space-y-2">
                <Label htmlFor="measurementPlan.minComplianceThreshold">
                  Min. Compliance Threshold
                </Label>
                <Input
                  id="measurementPlan.minComplianceThreshold"
                  type="number"
                  min={0}
                  max={1}
                  step={0.05}
                  {...register('measurementPlan.minComplianceThreshold', { valueAsNumber: true })}
                />
                <p className="text-xs text-muted-foreground">
                  Value between 0 and 1 (e.g. 0.7 = 70% compliance required)
                </p>
              </div>
            </CardContent>
          </Card>
        </form>
      </div>
    </div>
  )
}
