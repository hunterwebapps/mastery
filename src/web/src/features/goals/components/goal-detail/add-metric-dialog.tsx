import { useState } from 'react'
import { ArrowLeft, Sparkles } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import { ScrollArea } from '@/components/ui/scroll-area'
import { MetricForm, MetricKindSelect } from '../metric-library'
import type { MetricDefinitionDto, MetricKind, TargetType, WindowType, MetricAggregation } from '@/types'
import type { CreateMetricDefinitionFormData } from '../../schemas'
import { metricDataTypeInfo, metricDirectionInfo } from '@/types'

export interface MetricConfiguration {
  metricDefinitionId: string
  kind: MetricKind
  target: {
    type: TargetType
    value: number
    maxValue?: number
  }
  evaluationWindow: {
    windowType: WindowType
  }
  aggregation: MetricAggregation
}

interface AddMetricDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  availableDefinitions: MetricDefinitionDto[]
  onAddMetric: (config: MetricConfiguration) => void
  onCreateAndAddMetric: (data: CreateMetricDefinitionFormData, config: Omit<MetricConfiguration, 'metricDefinitionId'>) => Promise<void>
  isAdding?: boolean
  isCreating?: boolean
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

type DialogView = 'list' | 'create' | 'configure'

interface ConfigState {
  kind: MetricKind
  target: {
    type: TargetType
    value: number
    maxValue?: number
  }
  evaluationWindow: {
    windowType: WindowType
  }
  aggregation: MetricAggregation
}

const defaultConfig: ConfigState = {
  kind: 'Lead',
  target: { type: 'AtLeast', value: 0 },
  evaluationWindow: { windowType: 'Weekly' },
  aggregation: 'Sum',
}

export function AddMetricDialog({
  open,
  onOpenChange,
  availableDefinitions,
  onAddMetric,
  onCreateAndAddMetric,
  isAdding,
  isCreating,
}: AddMetricDialogProps) {
  const [view, setView] = useState<DialogView>('list')
  const [selectedDefinition, setSelectedDefinition] = useState<MetricDefinitionDto | null>(null)
  const [pendingCreateData, setPendingCreateData] = useState<CreateMetricDefinitionFormData | null>(null)
  const [config, setConfig] = useState<ConfigState>(defaultConfig)

  const handleClose = () => {
    onOpenChange(false)
    // Reset state after dialog closes
    setTimeout(() => {
      setView('list')
      setSelectedDefinition(null)
      setPendingCreateData(null)
      setConfig(defaultConfig)
    }, 200)
  }

  const handleSelectMetric = (definition: MetricDefinitionDto) => {
    setSelectedDefinition(definition)
    // Initialize config with metric's defaults
    setConfig({
      ...defaultConfig,
      evaluationWindow: { windowType: definition.defaultCadence as WindowType ?? 'Weekly' },
      aggregation: definition.defaultAggregation as MetricAggregation ?? 'Sum',
    })
    setView('configure')
  }

  const handleCreateMetric = async (data: CreateMetricDefinitionFormData) => {
    setPendingCreateData(data)
    // Initialize config with the new metric's defaults
    setConfig({
      ...defaultConfig,
      evaluationWindow: { windowType: data.defaultCadence as WindowType ?? 'Weekly' },
      aggregation: data.defaultAggregation as MetricAggregation ?? 'Sum',
    })
    setView('configure')
  }

  const handleConfirmAdd = async () => {
    if (pendingCreateData) {
      // Creating a new metric and adding it
      await onCreateAndAddMetric(pendingCreateData, config)
    } else if (selectedDefinition) {
      // Adding existing metric
      onAddMetric({
        metricDefinitionId: selectedDefinition.id,
        ...config,
      })
    }
    handleClose()
  }

  const handleBack = () => {
    if (view === 'configure') {
      if (pendingCreateData) {
        setView('create')
      } else {
        setView('list')
        setSelectedDefinition(null)
      }
    } else if (view === 'create') {
      setView('list')
      setPendingCreateData(null)
    }
  }

  const getTitle = () => {
    switch (view) {
      case 'list':
        return 'Add Metric to Scoreboard'
      case 'create':
        return 'Create New Metric'
      case 'configure':
        return 'Configure Metric'
    }
  }

  const metricName = selectedDefinition?.name ?? pendingCreateData?.name ?? 'metric'

  return (
    <Dialog open={open} onOpenChange={(isOpen) => !isOpen && handleClose()}>
      <DialogContent className="max-w-lg">
        <DialogHeader>
          <DialogTitle className="flex items-center gap-2">
            {view !== 'list' && (
              <Button
                type="button"
                variant="ghost"
                size="icon"
                className="size-8 -ml-2"
                onClick={handleBack}
              >
                <ArrowLeft className="size-4" />
              </Button>
            )}
            {getTitle()}
          </DialogTitle>
          {view === 'configure' && (
            <DialogDescription>
              Configure how "{metricName}" will be tracked for this goal
            </DialogDescription>
          )}
        </DialogHeader>

        {view === 'list' && (
          <div className="space-y-4">
            {/* Create New Metric Button */}
            <button
              type="button"
              className="w-full text-left p-3 rounded-lg border-2 border-dashed border-primary/30 hover:border-primary/50 hover:bg-primary/5 transition-colors"
              onClick={() => setView('create')}
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
                        onClick={() => handleSelectMetric(definition)}
                      />
                    ))}
                  </div>
                </ScrollArea>
              </>
            )}

            {availableDefinitions.length === 0 && (
              <p className="text-sm text-muted-foreground text-center py-4">
                All available metrics have been added to this goal.
              </p>
            )}
          </div>
        )}

        {view === 'create' && (
          <MetricForm
            onSubmit={handleCreateMetric}
            onCancel={() => setView('list')}
            isSubmitting={false}
            submitLabel="Next: Configure"
            cancelLabel="Back"
          />
        )}

        {view === 'configure' && (
          <div className="space-y-4">
            <div className="space-y-2">
              <Label className="text-sm">Role</Label>
              <MetricKindSelect
                value={config.kind}
                onValueChange={(value) => setConfig({ ...config, kind: value })}
              />
            </div>

            <div className="grid gap-4 sm:grid-cols-2">
              <div className="space-y-2">
                <Label className="text-sm">Target Type</Label>
                <Select
                  value={config.target.type}
                  onValueChange={(value) =>
                    setConfig({
                      ...config,
                      target: { ...config.target, type: value as TargetType },
                    })
                  }
                >
                  <SelectTrigger>
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
                <Label className="text-sm">Target Value</Label>
                <Input
                  type="number"
                  value={config.target.value}
                  onChange={(e) =>
                    setConfig({
                      ...config,
                      target: { ...config.target, value: parseFloat(e.target.value) || 0 },
                    })
                  }
                />
              </div>
            </div>

            {config.target.type === 'Between' && (
              <div className="space-y-2">
                <Label className="text-sm">Max Value</Label>
                <Input
                  type="number"
                  value={config.target.maxValue ?? ''}
                  onChange={(e) =>
                    setConfig({
                      ...config,
                      target: { ...config.target, maxValue: parseFloat(e.target.value) || undefined },
                    })
                  }
                />
              </div>
            )}

            <div className="grid gap-4 sm:grid-cols-2">
              <div className="space-y-2">
                <Label className="text-sm">Window</Label>
                <Select
                  value={config.evaluationWindow.windowType}
                  onValueChange={(value) =>
                    setConfig({
                      ...config,
                      evaluationWindow: { windowType: value as WindowType },
                    })
                  }
                >
                  <SelectTrigger>
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
                <Label className="text-sm">Aggregation</Label>
                <Select
                  value={config.aggregation}
                  onValueChange={(value) =>
                    setConfig({ ...config, aggregation: value as MetricAggregation })
                  }
                >
                  <SelectTrigger>
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

            <DialogFooter>
              <Button type="button" variant="outline" onClick={handleBack}>
                Back
              </Button>
              <Button
                type="button"
                onClick={handleConfirmAdd}
                disabled={isAdding || isCreating}
              >
                Add to Scoreboard
              </Button>
            </DialogFooter>
          </div>
        )}
      </DialogContent>
    </Dialog>
  )
}
