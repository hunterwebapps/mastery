import { BarChart3, Shield, Clock } from 'lucide-react'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import { cn } from '@/lib/utils'
import { metricDataTypeInfo, metricDirectionInfo } from '@/types'
import type { MeasurementPlanDto } from '@/types'
import { useMetrics } from '@/features/goals/hooks/use-metrics'

interface MeasurementPlanCardProps {
  plan: MeasurementPlanDto
}

export function MeasurementPlanCard({ plan }: MeasurementPlanCardProps) {
  const { data: metrics } = useMetrics()
  const primaryMetric = (metrics ?? []).find(m => m.id === plan.primaryMetricDefinitionId)
  const compliancePercent = Math.round(plan.minComplianceThreshold * 100)

  return (
    <Card>
      <CardHeader className="pb-4">
        <CardTitle className="text-base font-semibold flex items-center gap-2">
          <BarChart3 className="size-4 text-indigo-400" />
          Measurement Plan
        </CardTitle>
      </CardHeader>
      <CardContent className="space-y-5">
        {/* Primary Metric */}
        <div className="space-y-2">
          <div className="flex items-center justify-between">
            <span className="text-xs font-medium uppercase tracking-wider text-muted-foreground">
              Primary Metric
            </span>
            <Badge variant="outline" className="text-[11px] font-normal text-indigo-400 border-indigo-500/30">
              {plan.primaryAggregation}
            </Badge>
          </div>
          <div className="flex items-center gap-2 rounded-lg bg-muted/50 px-3 py-2.5">
            {primaryMetric ? (
              <>
                <span className="text-base shrink-0">{metricDataTypeInfo[primaryMetric.dataType].icon}</span>
                <div className="min-w-0 flex-1">
                  <p className="text-sm font-medium text-foreground truncate">{primaryMetric.name}</p>
                  <p className="text-xs text-muted-foreground">
                    {metricDirectionInfo[primaryMetric.direction].icon}{' '}
                    {metricDirectionInfo[primaryMetric.direction].label}
                    {primaryMetric.unit && ` · ${primaryMetric.unit.label}`}
                  </p>
                </div>
              </>
            ) : (
              <>
                <BarChart3 className="size-4 text-muted-foreground shrink-0" />
                <code className="text-xs text-muted-foreground font-mono truncate">
                  {plan.primaryMetricDefinitionId}
                </code>
              </>
            )}
          </div>
        </div>

        {/* Windows comparison */}
        <div className="grid grid-cols-2 gap-3">
          <div className="rounded-lg border border-border bg-card p-3 space-y-1.5">
            <div className="flex items-center gap-1.5">
              <Clock className="size-3 text-blue-400" />
              <span className="text-xs font-medium uppercase tracking-wider text-blue-400">
                Baseline
              </span>
            </div>
            <p className="text-2xl font-bold text-foreground">
              {plan.baselineWindowDays}
              <span className="text-sm font-normal text-muted-foreground ml-1">days</span>
            </p>
          </div>
          <div className="rounded-lg border border-border bg-card p-3 space-y-1.5">
            <div className="flex items-center gap-1.5">
              <Clock className="size-3 text-green-400" />
              <span className="text-xs font-medium uppercase tracking-wider text-green-400">
                Run Window
              </span>
            </div>
            <p className="text-2xl font-bold text-foreground">
              {plan.runWindowDays}
              <span className="text-sm font-normal text-muted-foreground ml-1">days</span>
            </p>
          </div>
        </div>

        {/* Compliance threshold */}
        <div className="space-y-2.5">
          <div className="flex items-center justify-between">
            <span className="text-xs font-medium uppercase tracking-wider text-muted-foreground">
              Min. Compliance Threshold
            </span>
            <span className="text-sm font-semibold text-foreground">
              {compliancePercent}%
            </span>
          </div>
          <div className="h-2 rounded-full bg-muted overflow-hidden">
            <div
              className={cn(
                'h-full rounded-full transition-all duration-500',
                compliancePercent >= 80
                  ? 'bg-green-500'
                  : compliancePercent >= 50
                    ? 'bg-yellow-500'
                    : 'bg-red-500'
              )}
              style={{ width: `${compliancePercent}%` }}
            />
          </div>
        </div>

        {/* Guardrail metrics */}
        {plan.guardrailMetricDefinitionIds.length > 0 && (
          <div className="space-y-2 pt-2 border-t border-border/50">
            <div className="flex items-center gap-1.5">
              <Shield className="size-3.5 text-orange-400" />
              <span className="text-xs font-medium uppercase tracking-wider text-muted-foreground">
                Guardrail Metrics
              </span>
              <Badge variant="outline" className="text-[10px] ml-auto">
                {plan.guardrailMetricDefinitionIds.length}
              </Badge>
            </div>
            <div className="space-y-1">
              {plan.guardrailMetricDefinitionIds.map((id) => {
                const metric = (metrics ?? []).find(m => m.id === id)
                return (
                  <div
                    key={id}
                    className="flex items-center gap-2 rounded bg-muted/30 px-2.5 py-1.5"
                  >
                    {metric ? (
                      <>
                        <span className="text-sm shrink-0">{metricDataTypeInfo[metric.dataType].icon}</span>
                        <span className="text-xs text-foreground truncate">{metric.name}</span>
                      </>
                    ) : (
                      <>
                        <div className="size-1.5 rounded-full bg-orange-400 shrink-0" />
                        <code className="text-xs text-muted-foreground font-mono truncate">{id}</code>
                      </>
                    )}
                  </div>
                )
              })}
            </div>
          </div>
        )}
      </CardContent>
    </Card>
  )
}
