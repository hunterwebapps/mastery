import { useState } from 'react'
import { useParams } from 'react-router-dom'
import { Skeleton } from '@/components/ui/skeleton'
import { useGoal, useUpdateGoalStatus, useUpdateGoalScoreboard, useDeleteGoal, useMetrics, useCreateMetricDefinition } from '../hooks'
import { AddMetricDialog, GoalHeader, GoalScoreboard } from '../components/goal-detail'
import type { MetricConfiguration } from '../components/goal-detail/add-metric-dialog'
import type { GoalStatus, GoalMetricDto, UpdateGoalMetricRequest } from '@/types'
import type { CreateMetricDefinitionFormData } from '../schemas'

function GoalDetailSkeleton() {
  return (
    <div className="min-h-screen bg-background">
      <div className="container max-w-4xl py-8 px-4 sm:px-6 lg:px-8">
        <div className="space-y-4">
          <div className="flex items-center gap-4">
            <Skeleton className="size-10 rounded-lg" />
            <div className="space-y-2">
              <Skeleton className="h-8 w-64" />
              <Skeleton className="h-4 w-32" />
            </div>
          </div>
          <Skeleton className="h-16 w-full" />
        </div>
        <div className="mt-8">
          <Skeleton className="h-64 w-full" />
        </div>
      </div>
    </div>
  )
}

function mapMetricToRequest(metric: GoalMetricDto): UpdateGoalMetricRequest {
  return {
    id: metric.id,
    metricDefinitionId: metric.metricDefinitionId,
    kind: metric.kind,
    target: {
      type: metric.target.type,
      value: metric.target.value,
      maxValue: metric.target.maxValue,
    },
    evaluationWindow: {
      windowType: metric.evaluationWindow.windowType,
      rollingDays: metric.evaluationWindow.rollingDays,
      startDay: metric.evaluationWindow.startDay,
    },
    aggregation: metric.aggregation,
    sourceHint: metric.sourceHint,
    weight: metric.weight,
    displayOrder: metric.displayOrder,
    baseline: metric.baseline,
    minimumThreshold: metric.minimumThreshold,
  }
}

export function Component() {
  const { id } = useParams<{ id: string }>()
  const { data: goal, isLoading } = useGoal(id!)
  const updateStatus = useUpdateGoalStatus()
  const updateScoreboard = useUpdateGoalScoreboard()
  const deleteGoal = useDeleteGoal()
  const { data: metricDefinitions = [], refetch: refetchMetrics } = useMetrics()
  const createMetric = useCreateMetricDefinition()

  const [showAddMetricDialog, setShowAddMetricDialog] = useState(false)

  if (isLoading || !goal) {
    return <GoalDetailSkeleton />
  }

  const handleUpdateStatus = (newStatus: GoalStatus, completionNotes?: string) => {
    updateStatus.mutate({
      id: goal.id,
      request: { newStatus, completionNotes },
    })
  }

  const handleDelete = () => {
    deleteGoal.mutate(goal.id)
  }

  const handleUpdateMetric = (updatedMetric: GoalMetricDto) => {
    // Replace the updated metric in the array and send all metrics
    const updatedMetrics = goal.metrics.map((m) =>
      m.id === updatedMetric.id ? updatedMetric : m
    )

    updateScoreboard.mutate({
      id: goal.id,
      request: {
        metrics: updatedMetrics.map(mapMetricToRequest),
      },
    })
  }

  const handleRemoveMetric = (metricId: string) => {
    // Remove the metric from the array and send remaining metrics
    const remainingMetrics = goal.metrics.filter((m) => m.id !== metricId)

    updateScoreboard.mutate({
      id: goal.id,
      request: {
        metrics: remainingMetrics.map(mapMetricToRequest),
      },
    })
  }

  const handleOpenAddMetric = () => {
    setShowAddMetricDialog(true)
  }

  const handleAddMetricWithConfig = (config: MetricConfiguration) => {
    const newMetric: UpdateGoalMetricRequest = {
      metricDefinitionId: config.metricDefinitionId,
      kind: config.kind,
      target: config.target,
      evaluationWindow: config.evaluationWindow,
      aggregation: config.aggregation,
      sourceHint: 'Manual',
      weight: 1,
      displayOrder: goal.metrics.length,
    }

    updateScoreboard.mutate({
      id: goal.id,
      request: {
        metrics: [...goal.metrics.map(mapMetricToRequest), newMetric],
      },
    })
  }

  const handleCreateAndAddMetric = async (
    data: CreateMetricDefinitionFormData,
    config: Omit<MetricConfiguration, 'metricDefinitionId'>
  ) => {
    // Create the metric definition
    const metricId = await createMetric.mutateAsync(data as any)

    // Refetch to get the updated list
    await refetchMetrics()

    // Create a new metric with the new definition and user's config
    const newMetric: UpdateGoalMetricRequest = {
      metricDefinitionId: metricId,
      kind: config.kind,
      target: config.target,
      evaluationWindow: config.evaluationWindow,
      aggregation: config.aggregation,
      sourceHint: 'Manual',
      weight: 1,
      displayOrder: goal.metrics.length,
    }

    updateScoreboard.mutate({
      id: goal.id,
      request: {
        metrics: [...goal.metrics.map(mapMetricToRequest), newMetric],
      },
    })
  }

  // Filter out metrics that are already in the scoreboard
  const usedDefinitionIds = new Set(goal.metrics.map((m) => m.metricDefinitionId))
  const availableDefinitions = metricDefinitions.filter(
    (d) => !usedDefinitionIds.has(d.id) && !d.isArchived
  )

  return (
    <div className="min-h-screen bg-background">
      <div className="container max-w-4xl py-8 px-4 sm:px-6 lg:px-8">
        <GoalHeader
          goal={goal}
          onUpdateStatus={handleUpdateStatus}
          onDelete={handleDelete}
          isUpdating={updateStatus.isPending}
        />

        <div className="mt-8">
          <GoalScoreboard
            metrics={goal.metrics}
            onAddMetric={handleOpenAddMetric}
            onUpdateMetric={handleUpdateMetric}
            onRemoveMetric={handleRemoveMetric}
            isUpdating={updateScoreboard.isPending}
          />
        </div>
      </div>

      <AddMetricDialog
        open={showAddMetricDialog}
        onOpenChange={setShowAddMetricDialog}
        availableDefinitions={availableDefinitions}
        onAddMetric={handleAddMetricWithConfig}
        onCreateAndAddMetric={handleCreateAndAddMetric}
        isAdding={updateScoreboard.isPending}
        isCreating={createMetric.isPending}
      />
    </div>
  )
}
