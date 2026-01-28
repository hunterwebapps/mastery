import { Link } from 'react-router-dom'
import {
  Calendar,
  Clock,
  ChevronRight,
  StickyNote,
  FlaskConical,
} from 'lucide-react'
import { Badge } from '@/components/ui/badge'
import { Card, CardContent } from '@/components/ui/card'
import { cn } from '@/lib/utils'
import type { ExperimentSummaryDto } from '@/types'
import {
  experimentStatusInfo,
  experimentCategoryInfo,
  experimentOutcomeInfo,
} from '@/types'

interface ExperimentCardProps {
  experiment: ExperimentSummaryDto
}

function formatDate(dateString: string): string {
  return new Date(dateString).toLocaleDateString('en-US', {
    month: 'short',
    day: 'numeric',
  })
}

function getCategoryBorderColor(category: ExperimentSummaryDto['category']): string {
  const map: Record<string, string> = {
    Habit: 'border-l-green-500',
    Routine: 'border-l-sky-500',
    Environment: 'border-l-teal-500',
    Mindset: 'border-l-violet-500',
    Productivity: 'border-l-blue-500',
    Health: 'border-l-rose-500',
    Social: 'border-l-amber-500',
    PlanRealism: 'border-l-indigo-500',
    FrictionReduction: 'border-l-orange-500',
    CheckInConsistency: 'border-l-cyan-500',
    Top1FollowThrough: 'border-l-purple-500',
    Other: 'border-l-gray-500',
  }
  return map[category] ?? 'border-l-gray-500'
}

export function ExperimentCard({ experiment }: ExperimentCardProps) {
  const statusInfo = experimentStatusInfo[experiment.status]
  const categoryInfo = experimentCategoryInfo[experiment.category]

  return (
    <Link to={`/experiments/${experiment.id}`} className="block group">
      <Card
        className={cn(
          'border-l-4 hover:border-primary/50 transition-all duration-200 cursor-pointer h-full',
          'hover:shadow-lg hover:shadow-primary/5 hover:-translate-y-0.5',
          getCategoryBorderColor(experiment.category)
        )}
      >
        <CardContent className="pt-5 pb-4 px-5">
          <div className="space-y-3">
            {/* Header: badges */}
            <div className="flex items-center justify-between gap-2">
              <div className="flex items-center gap-2 flex-wrap">
                <Badge
                  className={cn(
                    'text-[11px] font-medium px-2 py-0.5',
                    statusInfo.color,
                    statusInfo.bgColor
                  )}
                >
                  {statusInfo.label}
                </Badge>
                <Badge
                  variant="outline"
                  className={cn(
                    'text-[11px] font-normal px-2 py-0.5 border-border/50',
                    categoryInfo.color
                  )}
                >
                  {categoryInfo.label}
                </Badge>
              </div>
              <ChevronRight className="size-4 text-muted-foreground opacity-0 group-hover:opacity-100 transition-opacity shrink-0" />
            </div>

            {/* Title */}
            <h4 className="font-semibold text-foreground leading-snug line-clamp-1">
              {experiment.title}
            </h4>

            {/* Hypothesis summary */}
            <p className="text-sm text-muted-foreground leading-relaxed line-clamp-2">
              {experiment.hypothesisSummary}
            </p>

            {/* Meta row */}
            <div className="flex items-center gap-4 pt-1 border-t border-border/50">
              {/* Date range */}
              {experiment.startDate && (
                <div className="flex items-center gap-1.5 text-xs text-muted-foreground">
                  <Calendar className="size-3 shrink-0" />
                  <span>
                    {formatDate(experiment.startDate)}
                    {experiment.endDatePlanned && (
                      <> &rarr; {formatDate(experiment.endDatePlanned)}</>
                    )}
                  </span>
                </div>
              )}

              {/* Days remaining/elapsed */}
              {experiment.daysRemaining != null && experiment.daysRemaining > 0 && (
                <div className="flex items-center gap-1.5 text-xs text-muted-foreground">
                  <Clock className="size-3 shrink-0" />
                  <span>
                    {experiment.daysRemaining}d left
                  </span>
                </div>
              )}
              {experiment.daysRemaining == null && experiment.daysElapsed != null && (
                <div className="flex items-center gap-1.5 text-xs text-muted-foreground">
                  <Clock className="size-3 shrink-0" />
                  <span>Day {experiment.daysElapsed}</span>
                </div>
              )}

              {/* Note count */}
              {experiment.noteCount > 0 && (
                <div className="flex items-center gap-1.5 text-xs text-muted-foreground">
                  <StickyNote className="size-3 shrink-0" />
                  <span>{experiment.noteCount}</span>
                </div>
              )}

              {/* Spacer for right alignment */}
              <div className="flex-1" />

              {/* Outcome badge */}
              {experiment.outcomeClassification && (
                <Badge
                  className={cn(
                    'text-[11px] font-medium px-2 py-0.5',
                    experimentOutcomeInfo[experiment.outcomeClassification].color,
                    experimentOutcomeInfo[experiment.outcomeClassification].bgColor
                  )}
                >
                  {experimentOutcomeInfo[experiment.outcomeClassification].label}
                </Badge>
              )}

              {/* No dates fallback */}
              {!experiment.startDate && !experiment.outcomeClassification && (
                <div className="flex items-center gap-1.5 text-xs text-muted-foreground">
                  <FlaskConical className="size-3 shrink-0" />
                  <span>Not started</span>
                </div>
              )}
            </div>
          </div>
        </CardContent>
      </Card>
    </Link>
  )
}
