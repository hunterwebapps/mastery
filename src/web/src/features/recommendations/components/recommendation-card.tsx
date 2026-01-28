import {
  Lightbulb,
  Zap,
  Target,
  Activity,
  AlertTriangle,
  CheckCircle,
  X,
  Clock,
  ArrowRight,
} from 'lucide-react'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Card, CardContent } from '@/components/ui/card'
import { Progress } from '@/components/ui/progress'
import { cn } from '@/lib/utils'
import type { RecommendationSummaryDto, RecommendationType, RecommendationTargetKind } from '@/types'
import { recommendationTypeInfo, recommendationStatusInfo } from '@/types'

const typeIcons: Record<RecommendationType, React.ElementType> = {
  NextBestAction: Zap,
  Top1Suggestion: Target,
  HabitModeSuggestion: Activity,
  PlanRealismAdjustment: AlertTriangle,
  TaskBreakdownSuggestion: Lightbulb,
  ScheduleAdjustmentSuggestion: Clock,
  ProjectStuckFix: AlertTriangle,
  ExperimentRecommendation: Lightbulb,
  GoalScoreboardSuggestion: Target,
  HabitFromLeadMetricSuggestion: Activity,
  CheckInConsistencyNudge: Lightbulb,
  MetricObservationReminder: Activity,
}

const targetKindIcons: Record<RecommendationTargetKind, React.ElementType> = {
  Goal: Target,
  Metric: Activity,
  Habit: Activity,
  HabitOccurrence: Activity,
  Task: CheckCircle,
  Project: Lightbulb,
  Experiment: Lightbulb,
  UserProfile: Lightbulb,
}

function getTypeBorderColor(type: RecommendationType): string {
  const map: Record<RecommendationType, string> = {
    NextBestAction: 'border-l-amber-500',
    Top1Suggestion: 'border-l-purple-500',
    HabitModeSuggestion: 'border-l-green-500',
    PlanRealismAdjustment: 'border-l-orange-500',
    TaskBreakdownSuggestion: 'border-l-blue-500',
    ScheduleAdjustmentSuggestion: 'border-l-sky-500',
    ProjectStuckFix: 'border-l-red-500',
    ExperimentRecommendation: 'border-l-violet-500',
    GoalScoreboardSuggestion: 'border-l-teal-500',
    HabitFromLeadMetricSuggestion: 'border-l-emerald-500',
    CheckInConsistencyNudge: 'border-l-cyan-500',
    MetricObservationReminder: 'border-l-indigo-500',
  }
  return map[type] ?? 'border-l-gray-500'
}

function getExpiryCountdown(expiresAt: string): string | null {
  const now = Date.now()
  const expires = new Date(expiresAt).getTime()
  const diff = expires - now
  if (diff <= 0) return 'Expired'
  const hours = diff / (1000 * 60 * 60)
  if (hours > 4) return null
  if (hours >= 1) return `${Math.floor(hours)}h left`
  const minutes = Math.floor(diff / (1000 * 60))
  return `${minutes}m left`
}

interface RecommendationCardProps {
  recommendation: RecommendationSummaryDto
  onAccept: (id: string) => void
  onDismiss: (id: string) => void
  onSnooze: (id: string) => void
}

export function RecommendationCard({
  recommendation,
  onAccept,
  onDismiss,
  onSnooze,
}: RecommendationCardProps) {
  const typeInfo = recommendationTypeInfo[recommendation.type]
  const statusInfo = recommendationStatusInfo[recommendation.status]
  const TypeIcon = typeIcons[recommendation.type]
  const isPending = recommendation.status === 'Pending'
  const isSnoozed = recommendation.status === 'Snoozed'
  const isActionable = isPending || isSnoozed

  const expiryText = recommendation.expiresAt
    ? getExpiryCountdown(recommendation.expiresAt)
    : null

  return (
    <Card
      className={cn(
        'border-l-4 transition-all duration-200 h-full',
        getTypeBorderColor(recommendation.type)
      )}
    >
      <CardContent className="pt-5 pb-4 px-5">
        <div className="space-y-3">
          {/* Header: type badge + status + expiry */}
          <div className="flex items-center justify-between gap-2">
            <div className="flex items-center gap-2 flex-wrap">
              <Badge
                className={cn(
                  'text-[11px] font-medium px-2 py-0.5 gap-1',
                  typeInfo.color,
                  typeInfo.bgColor
                )}
              >
                <TypeIcon className="size-3" />
                {typeInfo.label}
              </Badge>
              {!isPending && (
                <Badge
                  className={cn(
                    'text-[11px] font-medium px-2 py-0.5',
                    statusInfo.color,
                    statusInfo.bgColor
                  )}
                >
                  {statusInfo.label}
                </Badge>
              )}
            </div>
            {expiryText && (
              <span className={cn(
                'text-[11px] font-medium flex items-center gap-1',
                expiryText === 'Expired' ? 'text-red-400' : 'text-amber-400'
              )}>
                <Clock className="size-3" />
                {expiryText}
              </span>
            )}
          </div>

          {/* Title */}
          <h4 className="font-semibold text-foreground leading-snug line-clamp-1">
            {recommendation.title}
          </h4>

          {/* Rationale */}
          <p className="text-sm text-muted-foreground leading-relaxed line-clamp-2">
            {recommendation.rationale}
          </p>

          {/* Target entity */}
          {recommendation.targetEntityTitle && (
            <div className="flex items-center gap-1.5 text-xs text-muted-foreground">
              {(() => {
                const TargetIcon = targetKindIcons[recommendation.targetKind]
                return <TargetIcon className="size-3 shrink-0" />
              })()}
              <span className="truncate">{recommendation.targetEntityTitle}</span>
              <ArrowRight className="size-3 shrink-0 text-primary/60" />
            </div>
          )}

          {/* Score indicator */}
          <div className="pt-1">
            <Progress
              value={recommendation.score * 100}
              className="h-1 bg-muted"
            />
          </div>

          {/* Action buttons */}
          {isActionable && (
            <div className="flex items-center gap-1 pt-1 border-t border-border/50">
              <Button
                variant="ghost"
                size="sm"
                className="text-green-500 hover:text-green-400 hover:bg-green-500/10"
                onClick={(e) => {
                  e.stopPropagation()
                  onAccept(recommendation.id)
                }}
              >
                <CheckCircle className="size-3.5 mr-1.5" />
                Accept
              </Button>
              <Button
                variant="ghost"
                size="sm"
                className="text-amber-500 hover:text-amber-400 hover:bg-amber-500/10"
                onClick={(e) => {
                  e.stopPropagation()
                  onSnooze(recommendation.id)
                }}
              >
                <Clock className="size-3.5 mr-1.5" />
                Snooze
              </Button>
              <Button
                variant="ghost"
                size="sm"
                className="text-muted-foreground hover:text-red-400 hover:bg-red-500/10"
                onClick={(e) => {
                  e.stopPropagation()
                  onDismiss(recommendation.id)
                }}
              >
                <X className="size-3.5 mr-1.5" />
                Dismiss
              </Button>
            </div>
          )}
        </div>
      </CardContent>
    </Card>
  )
}
