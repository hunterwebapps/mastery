import { Target, BarChart3, Shield, TrendingUp, TrendingDown, Minus } from 'lucide-react'
import { Badge } from '@/components/ui/badge'
import { Card, CardContent } from '@/components/ui/card'
import { Progress } from '@/components/ui/progress'
import type { GoalMetricDto, MetricKind, TargetType } from '@/types'
import { metricKindInfo } from '@/types'
import { cn } from '@/lib/utils'

interface MetricCardProps {
  metric: GoalMetricDto
  currentValue?: number
  className?: string
  onClick?: () => void
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

function formatTargetLabel(type: TargetType, value: number, maxValue?: number, unit?: string): string {
  const unitSuffix = unit ? ` ${unit}` : ''
  switch (type) {
    case 'AtLeast':
      return `>= ${value}${unitSuffix}`
    case 'AtMost':
      return `<= ${value}${unitSuffix}`
    case 'Between':
      return `${value} - ${maxValue}${unitSuffix}`
    case 'Exactly':
      return `= ${value}${unitSuffix}`
  }
}

function formatWindowLabel(windowType: string, rollingDays?: number): string {
  switch (windowType) {
    case 'Daily':
      return 'Daily'
    case 'Weekly':
      return 'Weekly'
    case 'Monthly':
      return 'Monthly'
    case 'Rolling':
      return rollingDays ? `${rollingDays}-day rolling` : 'Rolling'
    default:
      return windowType
  }
}

function calculateProgress(current: number, target: number, type: TargetType, maxValue?: number): number {
  switch (type) {
    case 'AtLeast':
      return Math.min((current / target) * 100, 100)
    case 'AtMost':
      // Invert: lower is better
      if (current <= target) return 100
      return Math.max(0, 100 - ((current - target) / target) * 100)
    case 'Between':
      if (maxValue === undefined) return 0
      if (current < target) return (current / target) * 100
      if (current > maxValue) return Math.max(0, 100 - ((current - maxValue) / maxValue) * 100)
      return 100
    case 'Exactly':
      const diff = Math.abs(current - target)
      return Math.max(0, 100 - (diff / target) * 100)
    default:
      return 0
  }
}

function getTrendIcon(current: number | undefined, baseline: number | undefined) {
  if (current === undefined || baseline === undefined) return null
  if (current > baseline) return TrendingUp
  if (current < baseline) return TrendingDown
  return Minus
}

export function MetricCard({ metric, currentValue, className, onClick }: MetricCardProps) {
  const kindInfo = metricKindInfo[metric.kind]
  const KindIcon = getKindIcon(metric.kind)
  const TrendIcon = getTrendIcon(currentValue, metric.baseline)

  const progress = currentValue !== undefined
    ? calculateProgress(currentValue, metric.target.value, metric.target.type, metric.target.maxValue)
    : 0

  const unitLabel = metric.unit?.label

  return (
    <Card
      className={cn('transition-colors', onClick && 'cursor-pointer', className)}
      onClick={onClick}
    >
      <CardContent className="pt-4 pb-4">
        <div className="space-y-3">
          <div className="flex items-start justify-between gap-2">
            <div className="flex items-center gap-2 min-w-0">
              <div className={cn('p-1.5 rounded', `bg-${kindInfo.color.replace('text-', '')}/10`)}>
                <KindIcon className={cn('size-4', kindInfo.color)} />
              </div>
              <div className="min-w-0">
                <p className="font-medium text-sm truncate">{metric.metricName}</p>
                <Badge variant="outline" className={cn('text-xs mt-0.5', kindInfo.color)}>
                  {kindInfo.label}
                </Badge>
              </div>
            </div>
            {TrendIcon && (
              <TrendIcon className={cn(
                'size-4 shrink-0',
                currentValue !== undefined && metric.baseline !== undefined
                  ? currentValue > metric.baseline
                    ? 'text-green-400'
                    : currentValue < metric.baseline
                      ? 'text-red-400'
                      : 'text-muted-foreground'
                  : 'text-muted-foreground'
              )} />
            )}
          </div>

          <div className="space-y-1.5">
            <div className="flex items-baseline justify-between text-sm">
              <span className="text-muted-foreground">
                {formatTargetLabel(metric.target.type, metric.target.value, metric.target.maxValue, unitLabel)}
              </span>
              <span className="font-semibold">
                {currentValue !== undefined ? (
                  <>
                    {currentValue}
                    {unitLabel && <span className="text-xs text-muted-foreground ml-1">{unitLabel}</span>}
                  </>
                ) : (
                  <span className="text-muted-foreground">--</span>
                )}
              </span>
            </div>
            <Progress value={progress} className="h-1.5" />
          </div>

          <div className="flex items-center justify-between text-xs text-muted-foreground">
            <span>{formatWindowLabel(metric.evaluationWindow.windowType, metric.evaluationWindow.rollingDays)}</span>
            <span className="capitalize">{metric.aggregation.toLowerCase()}</span>
          </div>
        </div>
      </CardContent>
    </Card>
  )
}
