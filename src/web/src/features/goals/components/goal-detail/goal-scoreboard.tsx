import { useState } from 'react'
import { Plus, Target, BarChart3, Shield, Trash2, Loader2 } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
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
import { MetricCard } from './metric-card'
import { MetricKindSelect } from '../metric-library'
import type { GoalMetricDto, TargetType, WindowType, MetricAggregation } from '@/types'

interface GoalScoreboardProps {
  metrics: GoalMetricDto[]
  onAddMetric?: () => void
  onUpdateMetric?: (metric: GoalMetricDto) => void
  onRemoveMetric?: (metricId: string) => void
  isUpdating?: boolean
}

interface MetricGroupProps {
  title: string
  description: string
  icon: React.ElementType
  iconColor: string
  metrics: GoalMetricDto[]
  onEditMetric?: (metric: GoalMetricDto) => void
}

function MetricGroup({ title, description, icon: Icon, iconColor, metrics, onEditMetric }: MetricGroupProps) {
  if (metrics.length === 0) return null

  return (
    <div className="space-y-3">
      <div className="flex items-center gap-2">
        <Icon className={`size-4 ${iconColor}`} />
        <div>
          <h3 className="font-medium text-sm">{title}</h3>
          <p className="text-xs text-muted-foreground">{description}</p>
        </div>
      </div>
      <div className="grid gap-3 sm:grid-cols-2">
        {metrics.map((metric) => (
          <MetricCard
            key={metric.id}
            metric={metric}
            className="hover:border-primary/50"
            onClick={onEditMetric ? () => onEditMetric(metric) : undefined}
          />
        ))}
      </div>
    </div>
  )
}

interface EditMetricDialogProps {
  metric: GoalMetricDto | null
  open: boolean
  onOpenChange: (open: boolean) => void
  onSave: (metric: GoalMetricDto) => void
  onRemove: (metricId: string) => void
  isUpdating?: boolean
}

function EditMetricDialog({ metric, open, onOpenChange, onSave, onRemove, isUpdating }: EditMetricDialogProps) {
  const [editedMetric, setEditedMetric] = useState<GoalMetricDto | null>(null)
  const [showDeleteConfirm, setShowDeleteConfirm] = useState(false)

  // Update local state when metric changes
  if (metric && (!editedMetric || editedMetric.id !== metric.id)) {
    setEditedMetric({ ...metric })
  }

  const handleSave = () => {
    if (editedMetric) {
      onSave(editedMetric)
    }
  }

  const handleRemove = () => {
    if (editedMetric) {
      onRemove(editedMetric.id)
      setShowDeleteConfirm(false)
    }
  }

  const handleClose = () => {
    onOpenChange(false)
    setEditedMetric(null)
    setShowDeleteConfirm(false)
  }

  if (!editedMetric) return null

  return (
    <Dialog open={open} onOpenChange={handleClose}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>Edit Metric</DialogTitle>
          <DialogDescription>
            Modify the settings for "{editedMetric.metricName}"
          </DialogDescription>
        </DialogHeader>

        {showDeleteConfirm ? (
          <div className="space-y-4">
            <p className="text-sm text-muted-foreground">
              Are you sure you want to remove this metric from the scoreboard? This won't delete the metric definition.
            </p>
            <DialogFooter>
              <Button type="button" variant="outline" onClick={() => setShowDeleteConfirm(false)}>
                Cancel
              </Button>
              <Button type="button" variant="destructive" onClick={handleRemove} disabled={isUpdating}>
                {isUpdating && <Loader2 className="size-4 mr-2 animate-spin" />}
                Remove Metric
              </Button>
            </DialogFooter>
          </div>
        ) : (
          <div className="space-y-4">
            <div className="space-y-2">
              <Label className="text-sm">Role</Label>
              <MetricKindSelect
                value={editedMetric.kind}
                onValueChange={(value) =>
                  setEditedMetric({ ...editedMetric, kind: value })
                }
              />
            </div>

            <div className="grid gap-4 sm:grid-cols-2">
              <div className="space-y-2">
                <Label className="text-sm">Target Type</Label>
                <Select
                  value={editedMetric.target.type}
                  onValueChange={(value) =>
                    setEditedMetric({
                      ...editedMetric,
                      target: { ...editedMetric.target, type: value as TargetType },
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
                  value={editedMetric.target.value}
                  onChange={(e) =>
                    setEditedMetric({
                      ...editedMetric,
                      target: { ...editedMetric.target, value: parseFloat(e.target.value) || 0 },
                    })
                  }
                />
              </div>
            </div>

            {editedMetric.target.type === 'Between' && (
              <div className="space-y-2">
                <Label className="text-sm">Max Value</Label>
                <Input
                  type="number"
                  value={editedMetric.target.maxValue ?? ''}
                  onChange={(e) =>
                    setEditedMetric({
                      ...editedMetric,
                      target: { ...editedMetric.target, maxValue: parseFloat(e.target.value) || undefined },
                    })
                  }
                />
              </div>
            )}

            <div className="grid gap-4 sm:grid-cols-2">
              <div className="space-y-2">
                <Label className="text-sm">Window</Label>
                <Select
                  value={editedMetric.evaluationWindow.windowType}
                  onValueChange={(value) =>
                    setEditedMetric({
                      ...editedMetric,
                      evaluationWindow: { ...editedMetric.evaluationWindow, windowType: value as WindowType },
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
                  value={editedMetric.aggregation}
                  onValueChange={(value) =>
                    setEditedMetric({ ...editedMetric, aggregation: value as MetricAggregation })
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

            <DialogFooter className="flex-col sm:flex-row gap-2">
              <Button
                type="button"
                variant="ghost"
                className="text-destructive hover:text-destructive hover:bg-destructive/10 sm:mr-auto"
                onClick={() => setShowDeleteConfirm(true)}
              >
                <Trash2 className="size-4 mr-2" />
                Remove
              </Button>
              <Button type="button" variant="outline" onClick={handleClose}>
                Cancel
              </Button>
              <Button type="button" onClick={handleSave} disabled={isUpdating}>
                {isUpdating && <Loader2 className="size-4 mr-2 animate-spin" />}
                Save Changes
              </Button>
            </DialogFooter>
          </div>
        )}
      </DialogContent>
    </Dialog>
  )
}

export function GoalScoreboard({ metrics, onAddMetric, onUpdateMetric, onRemoveMetric, isUpdating }: GoalScoreboardProps) {
  const [editingMetric, setEditingMetric] = useState<GoalMetricDto | null>(null)

  const lagMetrics = metrics.filter((m) => m.kind === 'Lag')
  const leadMetrics = metrics.filter((m) => m.kind === 'Lead')
  const constraintMetrics = metrics.filter((m) => m.kind === 'Constraint')

  const isEmpty = metrics.length === 0
  const canEdit = !!onUpdateMetric

  const handleEditMetric = canEdit ? (metric: GoalMetricDto) => setEditingMetric(metric) : undefined

  const handleSaveMetric = (metric: GoalMetricDto) => {
    onUpdateMetric?.(metric)
    setEditingMetric(null)
  }

  const handleRemoveMetric = (metricId: string) => {
    onRemoveMetric?.(metricId)
    setEditingMetric(null)
  }

  return (
    <>
      <Card>
        <CardHeader className="flex flex-row items-center justify-between pb-2">
          <CardTitle className="text-lg">Scoreboard</CardTitle>
          {onAddMetric && (
            <Button variant="outline" size="sm" onClick={onAddMetric}>
              <Plus className="size-4 mr-1" />
              Add Metric
            </Button>
          )}
        </CardHeader>
        <CardContent>
          {isEmpty ? (
            <div className="text-center py-8">
              <div className="inline-flex items-center justify-center w-12 h-12 rounded-full bg-muted mb-3">
                <Target className="size-6 text-muted-foreground" />
              </div>
              <p className="text-sm text-muted-foreground mb-4">
                No metrics defined yet. Add metrics to track your progress.
              </p>
              {onAddMetric && (
                <Button variant="outline" size="sm" onClick={onAddMetric}>
                  <Plus className="size-4 mr-1" />
                  Add Your First Metric
                </Button>
              )}
            </div>
          ) : (
            <div className="space-y-6">
              <MetricGroup
                title="Outcome Metrics"
                description="The results you're trying to achieve"
                icon={Target}
                iconColor="text-purple-400"
                metrics={lagMetrics}
                onEditMetric={handleEditMetric}
              />
              <MetricGroup
                title="Leading Indicators"
                description="Predictive behaviors that drive outcomes"
                icon={BarChart3}
                iconColor="text-blue-400"
                metrics={leadMetrics}
                onEditMetric={handleEditMetric}
              />
              <MetricGroup
                title="Constraints"
                description="Guardrails - what not to sacrifice"
                icon={Shield}
                iconColor="text-orange-400"
                metrics={constraintMetrics}
                onEditMetric={handleEditMetric}
              />
            </div>
          )}
        </CardContent>
      </Card>

      <EditMetricDialog
        metric={editingMetric}
        open={editingMetric !== null}
        onOpenChange={(open) => !open && setEditingMetric(null)}
        onSave={handleSaveMetric}
        onRemove={handleRemoveMetric}
        isUpdating={isUpdating}
      />
    </>
  )
}
