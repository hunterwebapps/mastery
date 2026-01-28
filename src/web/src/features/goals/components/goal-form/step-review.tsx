import { useFormContext } from 'react-hook-form'
import { Calendar, Target, BarChart3, Shield, AlertCircle } from 'lucide-react'
import { Badge } from '@/components/ui/badge'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Alert, AlertDescription } from '@/components/ui/alert'
import { useMetrics } from '../../hooks'
import type { CreateGoalFormData } from '../../schemas'

function formatDate(dateString: string): string {
  return new Date(dateString).toLocaleDateString('en-US', {
    month: 'long',
    day: 'numeric',
    year: 'numeric',
  })
}

function getPriorityLabel(priority: number): string {
  switch (priority) {
    case 1:
      return 'Critical'
    case 2:
      return 'High'
    case 3:
      return 'Medium'
    case 4:
      return 'Low'
    case 5:
      return 'Someday'
    default:
      return 'Medium'
  }
}

export function StepReview() {
  const { watch, formState: { errors } } = useFormContext<CreateGoalFormData>()
  const { data: metricDefinitions = [] } = useMetrics()
  const formData = watch()
  const metrics = formData.metrics ?? []

  const hasErrors = Object.keys(errors).length > 0
  const lagMetrics = metrics.filter((m) => m.kind === 'Lag')
  const leadMetrics = metrics.filter((m) => m.kind === 'Lead')
  const constraintMetrics = metrics.filter((m) => m.kind === 'Constraint')

  const getMetricName = (id: string) =>
    metricDefinitions.find((d) => d.id === id)?.name ?? 'Unknown'

  return (
    <div className="space-y-6">
      <div>
        <h3 className="text-lg font-medium">Review Your Goal</h3>
        <p className="text-sm text-muted-foreground mt-1">
          Make sure everything looks good before creating your goal.
        </p>
      </div>

      {hasErrors && (
        <Alert variant="destructive">
          <AlertCircle className="size-4" />
          <AlertDescription>
            Please fix the errors in previous steps before creating the goal.
          </AlertDescription>
        </Alert>
      )}

      <Card>
        <CardHeader>
          <CardTitle className="text-base">Goal Details</CardTitle>
        </CardHeader>
        <CardContent className="space-y-4">
          <div>
            <p className="text-sm text-muted-foreground">Title</p>
            <p className="font-medium">{formData.title || '-'}</p>
          </div>

          {formData.description && (
            <div>
              <p className="text-sm text-muted-foreground">Description</p>
              <p className="text-sm">{formData.description}</p>
            </div>
          )}

          {formData.why && (
            <div>
              <p className="text-sm text-muted-foreground">Why it matters</p>
              <p className="text-sm">{formData.why}</p>
            </div>
          )}

          <div className="flex flex-wrap gap-4">
            <div>
              <p className="text-sm text-muted-foreground">Priority</p>
              <Badge variant="outline">{getPriorityLabel(formData.priority ?? 3)}</Badge>
            </div>

            {formData.deadline && (
              <div>
                <p className="text-sm text-muted-foreground">Target Date</p>
                <div className="flex items-center gap-1 text-sm">
                  <Calendar className="size-4" />
                  {formatDate(formData.deadline)}
                </div>
              </div>
            )}
          </div>
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle className="text-base">Scoreboard ({metrics.length} metrics)</CardTitle>
        </CardHeader>
        <CardContent>
          {metrics.length === 0 ? (
            <p className="text-sm text-muted-foreground">
              No metrics defined. You can add metrics after creating the goal.
            </p>
          ) : (
            <div className="space-y-4">
              {lagMetrics.length > 0 && (
                <div>
                  <div className="flex items-center gap-2 mb-2">
                    <Target className="size-4 text-purple-400" />
                    <p className="text-sm font-medium">Outcome Metrics ({lagMetrics.length})</p>
                  </div>
                  <div className="pl-6 space-y-1">
                    {lagMetrics.map((m, i) => (
                      <p key={i} className="text-sm text-muted-foreground">
                        {getMetricName(m.metricDefinitionId)}
                        {' - '}
                        {m.target.type === 'AtLeast' && `>= ${m.target.value}`}
                        {m.target.type === 'AtMost' && `<= ${m.target.value}`}
                        {m.target.type === 'Between' && `${m.target.value} - ${m.target.maxValue}`}
                        {m.target.type === 'Exactly' && `= ${m.target.value}`}
                        {' '}
                        ({m.evaluationWindow.windowType.toLowerCase()})
                      </p>
                    ))}
                  </div>
                </div>
              )}

              {leadMetrics.length > 0 && (
                <div>
                  <div className="flex items-center gap-2 mb-2">
                    <BarChart3 className="size-4 text-blue-400" />
                    <p className="text-sm font-medium">Leading Indicators ({leadMetrics.length})</p>
                  </div>
                  <div className="pl-6 space-y-1">
                    {leadMetrics.map((m, i) => (
                      <p key={i} className="text-sm text-muted-foreground">
                        {getMetricName(m.metricDefinitionId)}
                        {' - '}
                        {m.target.type === 'AtLeast' && `>= ${m.target.value}`}
                        {m.target.type === 'AtMost' && `<= ${m.target.value}`}
                        {m.target.type === 'Between' && `${m.target.value} - ${m.target.maxValue}`}
                        {m.target.type === 'Exactly' && `= ${m.target.value}`}
                        {' '}
                        ({m.evaluationWindow.windowType.toLowerCase()})
                      </p>
                    ))}
                  </div>
                </div>
              )}

              {constraintMetrics.length > 0 && (
                <div>
                  <div className="flex items-center gap-2 mb-2">
                    <Shield className="size-4 text-orange-400" />
                    <p className="text-sm font-medium">Constraints ({constraintMetrics.length})</p>
                  </div>
                  <div className="pl-6 space-y-1">
                    {constraintMetrics.map((m, i) => (
                      <p key={i} className="text-sm text-muted-foreground">
                        {getMetricName(m.metricDefinitionId)}
                        {' - '}
                        {m.target.type === 'AtLeast' && `>= ${m.target.value}`}
                        {m.target.type === 'AtMost' && `<= ${m.target.value}`}
                        {m.target.type === 'Between' && `${m.target.value} - ${m.target.maxValue}`}
                        {m.target.type === 'Exactly' && `= ${m.target.value}`}
                        {' '}
                        ({m.evaluationWindow.windowType.toLowerCase()})
                      </p>
                    ))}
                  </div>
                </div>
              )}
            </div>
          )}
        </CardContent>
      </Card>
    </div>
  )
}
