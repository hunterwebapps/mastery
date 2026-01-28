import { TrendingUp, TrendingDown, Minus, Award } from 'lucide-react'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import { cn } from '@/lib/utils'
import type { ExperimentResultDto } from '@/types'
import { experimentOutcomeInfo } from '@/types'

interface ExperimentResultsCardProps {
  result: ExperimentResultDto
}

function formatDelta(delta: number): string {
  const sign = delta > 0 ? '+' : ''
  return `${sign}${delta.toFixed(2)}`
}

function formatDeltaPercent(deltaPercent: number): string {
  const sign = deltaPercent > 0 ? '+' : ''
  return `${sign}${deltaPercent.toFixed(1)}%`
}

export function ExperimentResultsCard({ result }: ExperimentResultsCardProps) {
  const outcomeInfo = experimentOutcomeInfo[result.outcomeClassification]
  const compliancePercent = result.complianceRate != null ? Math.round(result.complianceRate * 100) : null
  const isPositive = result.delta != null && result.delta > 0
  const isNegative = result.delta != null && result.delta < 0
  const isNeutral = result.delta != null && result.delta === 0

  return (
    <Card className="border-t-4 border-t-indigo-500/50">
      <CardHeader className="pb-4">
        <div className="flex items-center justify-between">
          <CardTitle className="text-base font-semibold flex items-center gap-2">
            <Award className="size-4 text-indigo-400" />
            Results
          </CardTitle>
          <Badge
            className={cn(
              'text-sm font-semibold px-3 py-1',
              outcomeInfo.color,
              outcomeInfo.bgColor
            )}
          >
            {outcomeInfo.label}
          </Badge>
        </div>
      </CardHeader>
      <CardContent className="space-y-5">
        {/* Value comparison */}
        {(result.baselineValue != null || result.runValue != null) && (
          <div className="grid grid-cols-[1fr,auto,1fr] items-center gap-4">
            {/* Baseline */}
            <div className="rounded-lg border border-border bg-card p-4 text-center">
              <p className="text-xs font-medium uppercase tracking-wider text-muted-foreground mb-1">
                Baseline
              </p>
              <p className="text-3xl font-bold text-foreground">
                {result.baselineValue != null ? result.baselineValue.toFixed(2) : '--'}
              </p>
            </div>

            {/* Arrow indicator */}
            <div className="flex flex-col items-center gap-1">
              {isPositive && <TrendingUp className="size-6 text-green-400" />}
              {isNegative && <TrendingDown className="size-6 text-red-400" />}
              {isNeutral && <Minus className="size-6 text-muted-foreground" />}
              {result.delta == null && <Minus className="size-6 text-muted-foreground" />}
              {result.delta != null && (
                <span
                  className={cn(
                    'text-sm font-semibold',
                    isPositive && 'text-green-400',
                    isNegative && 'text-red-400',
                    isNeutral && 'text-muted-foreground'
                  )}
                >
                  {formatDelta(result.delta)}
                </span>
              )}
              {result.deltaPercent != null && (
                <span
                  className={cn(
                    'text-xs',
                    isPositive && 'text-green-400/80',
                    isNegative && 'text-red-400/80',
                    isNeutral && 'text-muted-foreground'
                  )}
                >
                  {formatDeltaPercent(result.deltaPercent)}
                </span>
              )}
            </div>

            {/* Run value */}
            <div
              className={cn(
                'rounded-lg border p-4 text-center',
                isPositive && 'border-green-500/30 bg-green-500/5',
                isNegative && 'border-red-500/30 bg-red-500/5',
                !isPositive && !isNegative && 'border-border bg-card'
              )}
            >
              <p className="text-xs font-medium uppercase tracking-wider text-muted-foreground mb-1">
                Run
              </p>
              <p
                className={cn(
                  'text-3xl font-bold',
                  isPositive && 'text-green-400',
                  isNegative && 'text-red-400',
                  !isPositive && !isNegative && 'text-foreground'
                )}
              >
                {result.runValue != null ? result.runValue.toFixed(2) : '--'}
              </p>
            </div>
          </div>
        )}

        {/* Compliance rate */}
        {compliancePercent != null && (
          <div className="space-y-2.5">
            <div className="flex items-center justify-between">
              <span className="text-xs font-medium uppercase tracking-wider text-muted-foreground">
                Compliance Rate
              </span>
              <span className="text-sm font-semibold text-foreground">
                {compliancePercent}%
              </span>
            </div>
            <div className="h-2.5 rounded-full bg-muted overflow-hidden">
              <div
                className={cn(
                  'h-full rounded-full transition-all duration-700',
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
        )}

        {/* Narrative summary */}
        {result.narrativeSummary && (
          <div className="rounded-lg bg-muted/30 p-4 border border-border/50">
            <p className="text-xs font-medium uppercase tracking-wider text-muted-foreground mb-2">
              Summary
            </p>
            <p className="text-sm text-foreground leading-relaxed">
              {result.narrativeSummary}
            </p>
          </div>
        )}
      </CardContent>
    </Card>
  )
}
