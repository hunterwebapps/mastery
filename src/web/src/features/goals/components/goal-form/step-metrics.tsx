import { useState } from 'react'
import { useFieldArray, useFormContext } from 'react-hook-form'
import { Plus, Trash2, Target, BarChart3, Shield, ArrowLeft, Sparkles } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Card, CardContent } from '@/components/ui/card'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog'
import { ScrollArea } from '@/components/ui/scroll-area'
import { useMetrics, useCreateMetricDefinition } from '../../hooks'
import { MetricForm, MetricKindSelect } from '../metric-library'
import type { CreateGoalFormData, GoalMetricFormData, CreateMetricDefinitionFormData } from '../../schemas'
import type { MetricKind, MetricDefinitionDto } from '@/types'
import { metricKindInfo, metricDataTypeInfo, metricDirectionInfo } from '@/types'

const defaultMetricValues: Partial<GoalMetricFormData> = {
  kind: 'Lead',
  target: { type: 'AtLeast', value: 0 },
  evaluationWindow: { windowType: 'Weekly' },
  aggregation: 'Sum',
  sourceHint: 'Manual',
  weight: 1,
}

function getKindIcon(kind: MetricKind) {
  switch (kind) {
    case 'Lag':
      return Target
    case 'Lead':
      return BarChart3
    case 'Constraint':
      return Shield
  }
}

interface MetricItemProps {
  index: number
  metric: GoalMetricFormData
  metricDefinitions: MetricDefinitionDto[]
  onRemove: () => void
}

function MetricItem({ index, metric, metricDefinitions, onRemove }: MetricItemProps) {
  const { setValue, watch } = useFormContext<CreateGoalFormData>()
  const kind = watch(`metrics.${index}.kind`) ?? 'Lead'
  const kindInfo = metricKindInfo[kind]
  const KindIcon = getKindIcon(kind)
  const definition = metricDefinitions.find((d) => d.id === metric.metricDefinitionId)

  return (
    <Card>
      <CardContent className="pt-4">
        <div className="space-y-4">
          <div className="flex items-start justify-between">
            <div className="flex items-center gap-2">
              <div className={`p-1.5 rounded ${kindInfo.color.replace('text-', 'bg-')}/10`}>
                <KindIcon className={`size-4 ${kindInfo.color}`} />
              </div>
              <div>
                <p className="font-medium text-sm">{definition?.name ?? 'Unknown Metric'}</p>
                <p className="text-xs text-muted-foreground">{kindInfo.label}</p>
              </div>
            </div>
            <Button type="button" variant="ghost" size="icon" onClick={onRemove}>
              <Trash2 className="size-4 text-muted-foreground" />
            </Button>
          </div>

          <div className="grid gap-4 sm:grid-cols-3">
            <div className="space-y-2">
              <Label className="text-xs">Role</Label>
              <MetricKindSelect
                value={kind}
                onValueChange={(value) => setValue(`metrics.${index}.kind`, value)}
              />
            </div>

            <div className="space-y-2">
              <Label className="text-xs">Target Type</Label>
              <Select
                value={metric.target.type}
                onValueChange={(value) => setValue(`metrics.${index}.target.type`, value as any)}
              >
                <SelectTrigger className="h-8 text-xs">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="AtLeast">At Least</SelectItem>
                  <SelectItem value="AtMost">At Most</SelectItem>
                  <SelectItem value="Between">Between</SelectItem>
                  <SelectItem value="Exactly">Exactly</SelectItem>
                </SelectContent>
              </Select>
            </div>

            <div className="space-y-2">
              <Label className="text-xs">Target Value</Label>
              <Input
                type="number"
                className="h-8 text-xs"
                value={metric.target.value}
                onChange={(e) =>
                  setValue(`metrics.${index}.target.value`, parseFloat(e.target.value) || 0)
                }
                onKeyDown={(e) => e.key === 'Enter' && e.preventDefault()}
              />
            </div>
          </div>

          <div className="grid gap-4 sm:grid-cols-2">
            <div className="space-y-2">
              <Label className="text-xs">Window</Label>
              <Select
                value={metric.evaluationWindow.windowType}
                onValueChange={(value) =>
                  setValue(`metrics.${index}.evaluationWindow.windowType`, value as any)
                }
              >
                <SelectTrigger className="h-8 text-xs">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="Daily">Daily</SelectItem>
                  <SelectItem value="Weekly">Weekly</SelectItem>
                  <SelectItem value="Monthly">Monthly</SelectItem>
                  <SelectItem value="Rolling">Rolling</SelectItem>
                </SelectContent>
              </Select>
            </div>

            <div className="space-y-2">
              <Label className="text-xs">Aggregation</Label>
              <Select
                value={metric.aggregation}
                onValueChange={(value) => setValue(`metrics.${index}.aggregation`, value as any)}
              >
                <SelectTrigger className="h-8 text-xs">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="Sum">Sum</SelectItem>
                  <SelectItem value="Average">Average</SelectItem>
                  <SelectItem value="Max">Max</SelectItem>
                  <SelectItem value="Min">Min</SelectItem>
                  <SelectItem value="Count">Count</SelectItem>
                  <SelectItem value="Latest">Latest</SelectItem>
                </SelectContent>
              </Select>
            </div>
          </div>
        </div>
      </CardContent>
    </Card>
  )
}

interface MetricPickerItemProps {
  definition: MetricDefinitionDto
  onClick: () => void
}

function MetricPickerItem({ definition, onClick }: MetricPickerItemProps) {
  const dataTypeInfo = metricDataTypeInfo[definition.dataType]
  const directionInfo = metricDirectionInfo[definition.direction]

  return (
    <button
      type="button"
      className="w-full text-left p-3 rounded-lg border hover:border-primary/50 transition-colors"
      onClick={onClick}
    >
      <div className="flex items-start gap-3">
        <div className="p-2 rounded-lg bg-primary/10 shrink-0">
          <span className="text-sm">{dataTypeInfo.icon}</span>
        </div>
        <div className="flex-1 min-w-0">
          <p className="font-medium text-sm">{definition.name}</p>
          {definition.description && (
            <p className="text-xs text-muted-foreground mt-0.5 line-clamp-1">
              {definition.description}
            </p>
          )}
          <div className="flex items-center gap-2 mt-1.5">
            <span className="text-xs text-muted-foreground">{dataTypeInfo.label}</span>
            <span className="text-xs text-muted-foreground">
              {directionInfo.icon} {directionInfo.label}
            </span>
          </div>
        </div>
      </div>
    </button>
  )
}

type PickerView = 'list' | 'create'

export function StepMetrics() {
  const [showPicker, setShowPicker] = useState(false)
  const [pickerView, setPickerView] = useState<PickerView>('list')

  const { data: metricDefinitions = [], refetch: refetchMetrics } = useMetrics()
  const createMetric = useCreateMetricDefinition()

  const { control, watch } = useFormContext<CreateGoalFormData>()
  const { fields, append, remove } = useFieldArray({
    control,
    name: 'metrics',
  })
  const metrics = watch('metrics') ?? []

  const handleAddMetric = (definitionId: string) => {
    append({
      ...defaultMetricValues,
      metricDefinitionId: definitionId,
    } as GoalMetricFormData)
    closePicker()
  }

  const handleCreateAndAddMetric = async (data: CreateMetricDefinitionFormData) => {
    // Create the metric definition
    const metricId = await createMetric.mutateAsync(data as any)

    // Refetch to get the updated list
    await refetchMetrics()

    // Add it to the goal's scoreboard
    append({
      ...defaultMetricValues,
      metricDefinitionId: metricId,
      // Use the metric's defaults for aggregation and cadence
      aggregation: data.defaultAggregation ?? 'Sum',
      evaluationWindow: { windowType: data.defaultCadence ?? 'Weekly' },
    } as GoalMetricFormData)

    closePicker()
  }

  const closePicker = () => {
    setShowPicker(false)
    // Reset view after dialog closes
    setTimeout(() => setPickerView('list'), 200)
  }

  const usedDefinitionIds = new Set(metrics.map((m) => m.metricDefinitionId))
  const availableDefinitions = metricDefinitions.filter(
    (d) => !usedDefinitionIds.has(d.id) && !d.isArchived
  )

  return (
    <div className="space-y-6">
      <div>
        <h3 className="text-lg font-medium">Define Your Scoreboard</h3>
        <p className="text-sm text-muted-foreground mt-1">
          Add metrics to track your progress. Use outcome metrics for results, leading indicators
          for behaviors, and constraints for guardrails.
        </p>
      </div>

      {fields.length > 0 ? (
        <div className="space-y-4">
          {fields.map((field, index) => (
            <MetricItem
              key={field.id}
              index={index}
              metric={metrics[index]}
              metricDefinitions={metricDefinitions}
              onRemove={() => remove(index)}
            />
          ))}
        </div>
      ) : (
        <div className="text-center py-8 border border-dashed rounded-lg">
          <Target className="size-8 text-muted-foreground mx-auto mb-3" />
          <p className="text-sm text-muted-foreground mb-4">
            No metrics added yet. Add metrics to create your scoreboard.
          </p>
        </div>
      )}

      <Button type="button" variant="outline" onClick={() => setShowPicker(true)}>
        <Plus className="size-4 mr-2" />
        Add Metric
      </Button>

      <Dialog open={showPicker} onOpenChange={(open) => !open && closePicker()}>
        <DialogContent className="max-w-lg">
          <DialogHeader>
            <DialogTitle className="flex items-center gap-2">
              {pickerView === 'create' && (
                <Button
                  type="button"
                  variant="ghost"
                  size="icon"
                  className="size-8 -ml-2"
                  onClick={() => setPickerView('list')}
                >
                  <ArrowLeft className="size-4" />
                </Button>
              )}
              {pickerView === 'list' ? 'Add Metric to Scoreboard' : 'Create New Metric'}
            </DialogTitle>
          </DialogHeader>

          {pickerView === 'list' ? (
            <div className="space-y-4">
              {/* Create New Metric Button */}
              <button
                type="button"
                className="w-full text-left p-3 rounded-lg border-2 border-dashed border-primary/30 hover:border-primary/50 hover:bg-primary/5 transition-colors"
                onClick={() => setPickerView('create')}
              >
                <div className="flex items-center gap-3">
                  <div className="p-2 rounded-lg bg-primary/10">
                    <Sparkles className="size-4 text-primary" />
                  </div>
                  <div>
                    <p className="font-medium text-sm text-primary">Create New Metric</p>
                    <p className="text-xs text-muted-foreground">
                      Define a new metric and add it to your scoreboard
                    </p>
                  </div>
                </div>
              </button>

              {/* Existing Metrics List */}
              {availableDefinitions.length > 0 && (
                <>
                  <div className="relative">
                    <div className="absolute inset-0 flex items-center">
                      <span className="w-full border-t" />
                    </div>
                    <div className="relative flex justify-center text-xs uppercase">
                      <span className="bg-background px-2 text-muted-foreground">
                        Or choose from library
                      </span>
                    </div>
                  </div>

                  <ScrollArea className="max-h-[300px] -mx-2 px-2">
                    <div className="space-y-2">
                      {availableDefinitions.map((definition) => (
                        <MetricPickerItem
                          key={definition.id}
                          definition={definition}
                          onClick={() => handleAddMetric(definition.id)}
                        />
                      ))}
                    </div>
                  </ScrollArea>
                </>
              )}

              {availableDefinitions.length === 0 && metricDefinitions.length > 0 && (
                <p className="text-sm text-muted-foreground text-center py-4">
                  All available metrics have been added to this goal.
                </p>
              )}
            </div>
          ) : (
            <MetricForm
              onSubmit={handleCreateAndAddMetric}
              onCancel={() => setPickerView('list')}
              isSubmitting={createMetric.isPending}
              submitLabel="Create & Add to Scoreboard"
              cancelLabel="Back"
            />
          )}
        </DialogContent>
      </Dialog>
    </div>
  )
}
