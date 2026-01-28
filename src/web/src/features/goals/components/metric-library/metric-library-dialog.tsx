import { useState } from 'react'
import { Plus, Search, BarChart3 } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Badge } from '@/components/ui/badge'
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog'
import { Skeleton } from '@/components/ui/skeleton'
import { ScrollArea } from '@/components/ui/scroll-area'
import { useMetrics, useCreateMetricDefinition, useUpdateMetricDefinition } from '../../hooks'
import { MetricForm } from './metric-form'
import type { MetricDefinitionDto } from '@/types'
import type { CreateMetricDefinitionFormData } from '../../schemas'
import { metricDataTypeInfo, metricDirectionInfo } from '@/types'

interface MetricLibraryDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  onSelectMetric?: (metric: MetricDefinitionDto) => void
  selectionMode?: boolean
}

function MetricListItem({
  metric,
  onClick,
}: {
  metric: MetricDefinitionDto
  onClick?: () => void
}) {
  const dataTypeInfo = metricDataTypeInfo[metric.dataType]
  const directionInfo = metricDirectionInfo[metric.direction]

  return (
    <div
      className={`
        p-4 rounded-lg border border-border hover:border-primary/50 transition-colors cursor-pointer
        ${metric.isArchived ? 'opacity-60' : ''}
      `}
      onClick={onClick}
    >
      <div className="flex items-start justify-between">
        <div className="flex items-center gap-3">
          <div className="p-2 rounded-lg bg-primary/10">
            <span className="text-lg">{dataTypeInfo.icon}</span>
          </div>
          <div>
            <div className="flex items-center gap-2">
              <h4 className="font-medium">{metric.name}</h4>
              {metric.isArchived && (
                <Badge variant="outline" className="text-xs">
                  Archived
                </Badge>
              )}
            </div>
            {metric.description && (
              <p className="text-sm text-muted-foreground mt-0.5 line-clamp-1">
                {metric.description}
              </p>
            )}
          </div>
        </div>
        <Badge variant="secondary" className="text-xs">
          {directionInfo.icon} {directionInfo.label}
        </Badge>
      </div>
      <div className="flex items-center gap-4 mt-3 text-xs text-muted-foreground">
        <span>{dataTypeInfo.label}</span>
        <span>{metric.defaultCadence}</span>
        <span>{metric.defaultAggregation}</span>
        {metric.unit && (
          <span>Unit: {metric.unit.label}</span>
        )}
      </div>
      {metric.tags.length > 0 && (
        <div className="flex flex-wrap gap-1 mt-2">
          {metric.tags.map((tag) => (
            <Badge key={tag} variant="outline" className="text-xs">
              {tag}
            </Badge>
          ))}
        </div>
      )}
    </div>
  )
}

function MetricListSkeleton() {
  return (
    <div className="space-y-3">
      {Array.from({ length: 3 }).map((_, i) => (
        <div key={i} className="p-4 rounded-lg border border-border">
          <div className="flex items-start gap-3">
            <Skeleton className="size-10 rounded-lg" />
            <div className="flex-1 space-y-2">
              <Skeleton className="h-5 w-32" />
              <Skeleton className="h-4 w-48" />
            </div>
          </div>
        </div>
      ))}
    </div>
  )
}

type DialogView = 'list' | 'create' | 'edit'

export function MetricLibraryDialog({
  open,
  onOpenChange,
  onSelectMetric,
  selectionMode = false,
}: MetricLibraryDialogProps) {
  const [view, setView] = useState<DialogView>('list')
  const [searchQuery, setSearchQuery] = useState('')
  const [showArchived, setShowArchived] = useState(false)
  const [editingMetric, setEditingMetric] = useState<MetricDefinitionDto | null>(null)

  const { data: metrics, isLoading } = useMetrics(showArchived)
  const createMetric = useCreateMetricDefinition()
  const updateMetric = useUpdateMetricDefinition()

  const filteredMetrics = metrics?.filter((m) =>
    m.name.toLowerCase().includes(searchQuery.toLowerCase()) ||
    m.description?.toLowerCase().includes(searchQuery.toLowerCase())
  ) ?? []

  const handleClose = () => {
    onOpenChange(false)
    // Reset state after dialog closes
    setTimeout(() => {
      setView('list')
      setEditingMetric(null)
    }, 200)
  }

  const handleCreateMetric = async (data: CreateMetricDefinitionFormData) => {
    await createMetric.mutateAsync(data as any)
    setView('list')
  }

  const handleEditMetric = (metric: MetricDefinitionDto) => {
    setEditingMetric(metric)
    setView('edit')
  }

  const handleUpdateMetric = async (data: CreateMetricDefinitionFormData) => {
    if (!editingMetric) return

    await updateMetric.mutateAsync({
      id: editingMetric.id,
      request: {
        name: data.name,
        description: data.description,
        dataType: data.dataType,
        direction: data.direction,
        defaultCadence: data.defaultCadence,
        defaultAggregation: data.defaultAggregation,
        unit: data.unit,
        tags: data.tags,
        isArchived: editingMetric.isArchived,
      },
    })
    setView('list')
    setEditingMetric(null)
  }

  const handleArchiveMetric = async (metric: MetricDefinitionDto) => {
    await updateMetric.mutateAsync({
      id: metric.id,
      request: {
        name: metric.name,
        description: metric.description,
        dataType: metric.dataType,
        direction: metric.direction,
        defaultCadence: metric.defaultCadence,
        defaultAggregation: metric.defaultAggregation,
        isArchived: true,
        tags: metric.tags,
      },
    })
  }

  const handleRestoreMetric = async (metric: MetricDefinitionDto) => {
    await updateMetric.mutateAsync({
      id: metric.id,
      request: {
        name: metric.name,
        description: metric.description,
        dataType: metric.dataType,
        direction: metric.direction,
        defaultCadence: metric.defaultCadence,
        defaultAggregation: metric.defaultAggregation,
        isArchived: false,
        tags: metric.tags,
      },
    })
  }

  const getTitle = () => {
    if (selectionMode) return 'Select a Metric'
    switch (view) {
      case 'list':
        return 'Metric Library'
      case 'create':
        return 'Create New Metric'
      case 'edit':
        return 'Edit Metric'
    }
  }

  return (
    <Dialog open={open} onOpenChange={handleClose}>
      <DialogContent className="max-w-2xl max-h-[80vh] flex flex-col">
        <DialogHeader>
          <DialogTitle className="flex items-center gap-2">
            <BarChart3 className="size-5" />
            {getTitle()}
          </DialogTitle>
        </DialogHeader>

        {view === 'create' && (
          <div className="flex-1 overflow-y-auto">
            <MetricForm
              onSubmit={handleCreateMetric}
              onCancel={() => setView('list')}
              isSubmitting={createMetric.isPending}
            />
          </div>
        )}

        {view === 'edit' && editingMetric && (
          <div className="flex-1 overflow-y-auto">
            <MetricForm
              onSubmit={handleUpdateMetric}
              onCancel={() => {
                setView('list')
                setEditingMetric(null)
              }}
              isSubmitting={updateMetric.isPending}
              defaultValues={{
                name: editingMetric.name,
                description: editingMetric.description ?? '',
                dataType: editingMetric.dataType,
                direction: editingMetric.direction,
                defaultCadence: editingMetric.defaultCadence as 'Daily' | 'Weekly' | 'Monthly' | 'Rolling',
                defaultAggregation: editingMetric.defaultAggregation as 'Sum' | 'Average' | 'Max' | 'Min' | 'Count' | 'Latest',
                unit: editingMetric.unit,
                tags: editingMetric.tags,
              }}
              submitLabel="Save Changes"
              cancelLabel="Cancel"
              isArchived={editingMetric.isArchived}
              onArchive={async () => {
                await handleArchiveMetric(editingMetric)
                setView('list')
                setEditingMetric(null)
              }}
              onRestore={async () => {
                await handleRestoreMetric(editingMetric)
                setView('list')
                setEditingMetric(null)
              }}
            />
          </div>
        )}

        {view === 'list' && (
          <>
            <div className="flex items-center gap-2">
              <div className="relative flex-1">
                <Search className="absolute left-3 top-1/2 -translate-y-1/2 size-4 text-muted-foreground" />
                <Input
                  placeholder="Search metrics..."
                  className="pl-9"
                  value={searchQuery}
                  onChange={(e) => setSearchQuery(e.target.value)}
                />
              </div>
              <Button
                variant="outline"
                size="sm"
                onClick={() => setShowArchived(!showArchived)}
              >
                {showArchived ? 'Hide Archived' : 'Show Archived'}
              </Button>
              <Button size="sm" onClick={() => setView('create')}>
                <Plus className="size-4 mr-1" />
                New
              </Button>
            </div>

            <ScrollArea className="flex-1 -mx-6 px-6">
              {isLoading ? (
                <MetricListSkeleton />
              ) : filteredMetrics.length === 0 ? (
                <div className="text-center py-8">
                  <BarChart3 className="size-12 text-muted-foreground mx-auto mb-3" />
                  <p className="text-muted-foreground">
                    {searchQuery
                      ? 'No metrics found matching your search.'
                      : 'No metrics in your library yet.'}
                  </p>
                  <Button
                    variant="outline"
                    size="sm"
                    className="mt-4"
                    onClick={() => setView('create')}
                  >
                    <Plus className="size-4 mr-1" />
                    Create Your First Metric
                  </Button>
                </div>
              ) : (
                <div className="space-y-3 pb-4">
                  {filteredMetrics.map((metric) => (
                    <MetricListItem
                      key={metric.id}
                      metric={metric}
                      onClick={
                        selectionMode
                          ? !metric.isArchived
                            ? () => {
                                onSelectMetric?.(metric)
                                onOpenChange(false)
                              }
                            : undefined
                          : () => handleEditMetric(metric)
                      }
                    />
                  ))}
                </div>
              )}
            </ScrollArea>
          </>
        )}
      </DialogContent>
    </Dialog>
  )
}
