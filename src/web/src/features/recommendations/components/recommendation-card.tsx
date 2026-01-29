import { useState, useRef, useEffect } from 'react'
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
  ChevronDown,
} from 'lucide-react'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Card, CardContent } from '@/components/ui/card'
import { Progress } from '@/components/ui/progress'
import { cn } from '@/lib/utils'
import type { RecommendationSummaryDto, RecommendationType, RecommendationTargetKind } from '@/types'
import { recommendationTypeInfo, recommendationStatusInfo } from '@/types'
import { ActionPreview } from './action-preview'

const typeIcons: Record<RecommendationType, React.ElementType> = {
  NextBestAction: Zap,
  Top1Suggestion: Target,
  HabitModeSuggestion: Activity,
  PlanRealismAdjustment: AlertTriangle,
  TaskBreakdownSuggestion: Lightbulb,
  ScheduleAdjustmentSuggestion: Clock,
  ProjectStuckFix: AlertTriangle,
  ProjectSuggestion: Lightbulb,
  ExperimentRecommendation: Lightbulb,
  GoalScoreboardSuggestion: Target,
  HabitFromLeadMetricSuggestion: Activity,
  CheckInConsistencyNudge: Lightbulb,
  MetricObservationReminder: Activity,
  // Edit/Archive types
  TaskEditSuggestion: CheckCircle,
  TaskArchiveSuggestion: CheckCircle,
  HabitEditSuggestion: Activity,
  HabitArchiveSuggestion: Activity,
  GoalEditSuggestion: Target,
  GoalArchiveSuggestion: Target,
  ProjectEditSuggestion: Lightbulb,
  ProjectArchiveSuggestion: Lightbulb,
  MetricEditSuggestion: Activity,
  ExperimentEditSuggestion: Lightbulb,
  ExperimentArchiveSuggestion: Lightbulb,
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
    ProjectSuggestion: 'border-l-pink-500',
    ExperimentRecommendation: 'border-l-violet-500',
    GoalScoreboardSuggestion: 'border-l-teal-500',
    HabitFromLeadMetricSuggestion: 'border-l-emerald-500',
    CheckInConsistencyNudge: 'border-l-cyan-500',
    MetricObservationReminder: 'border-l-indigo-500',
    // Edit types - use blue-ish colors
    TaskEditSuggestion: 'border-l-blue-500',
    HabitEditSuggestion: 'border-l-sky-500',
    GoalEditSuggestion: 'border-l-teal-500',
    ProjectEditSuggestion: 'border-l-cyan-500',
    MetricEditSuggestion: 'border-l-indigo-500',
    ExperimentEditSuggestion: 'border-l-violet-500',
    // Archive types - use gray/red-ish colors
    TaskArchiveSuggestion: 'border-l-slate-500',
    HabitArchiveSuggestion: 'border-l-slate-500',
    GoalArchiveSuggestion: 'border-l-slate-500',
    ProjectArchiveSuggestion: 'border-l-slate-500',
    ExperimentArchiveSuggestion: 'border-l-slate-500',
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
  onAccept: (recommendation: RecommendationSummaryDto) => void
  onDismiss: (id: string) => void
  onSnooze: (id: string) => void
}

export function RecommendationCard({
  recommendation,
  onAccept,
  onDismiss,
  onSnooze,
}: RecommendationCardProps) {
  const [isExpanded, setIsExpanded] = useState(false)
  const [showExpandButton, setShowExpandButton] = useState(false)
  const rationaleRef = useRef<HTMLParagraphElement>(null)

  const typeInfo = recommendationTypeInfo[recommendation.type]
  const statusInfo = recommendationStatusInfo[recommendation.status]
  const TypeIcon = typeIcons[recommendation.type]
  const isPending = recommendation.status === 'Pending'
  const isSnoozed = recommendation.status === 'Snoozed'
  const isActionable = isPending || isSnoozed

  const expiryText = recommendation.expiresAt
    ? getExpiryCountdown(recommendation.expiresAt)
    : null

  // Check if text is truncated (scrollHeight > clientHeight means overflow)
  // Only check when collapsed, and preserve button visibility when expanded
  useEffect(() => {
    if (isExpanded) return // Don't recalculate when expanded

    const checkTruncation = () => {
      const el = rationaleRef.current
      if (el) {
        // scrollHeight is the full content height, clientHeight is visible height
        // When line-clamp is applied, scrollHeight > clientHeight if text is truncated
        const isTruncated = el.scrollHeight > el.clientHeight
        setShowExpandButton(isTruncated)
      }
    }

    // Check immediately and after a frame to ensure layout is complete
    checkTruncation()
    const frameId = requestAnimationFrame(checkTruncation)
    return () => cancelAnimationFrame(frameId)
  }, [recommendation.rationale, isExpanded])

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

          {/* Rationale with expandable content */}
          <div className="space-y-1.5">
            <p
              ref={rationaleRef}
              className={cn(
                'text-sm text-muted-foreground leading-relaxed',
                !isExpanded && 'line-clamp-2'
              )}
            >
              {recommendation.rationale}
            </p>
            {showExpandButton && (
              <button
                type="button"
                onClick={(e) => {
                  e.stopPropagation()
                  setIsExpanded(!isExpanded)
                }}
                className="flex items-center gap-1 text-xs text-primary hover:text-primary/80 transition-colors"
              >
                <span>{isExpanded ? 'View less' : 'View more'}</span>
                <ChevronDown
                  className={cn(
                    'size-3 transition-transform duration-300',
                    isExpanded && 'rotate-180'
                  )}
                />
              </button>
            )}
          </div>

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

          {/* Action Preview */}
          <ActionPreview
            actionKind={recommendation.actionKind}
            targetKind={recommendation.targetKind}
            targetTitle={recommendation.targetEntityTitle}
            actionSummary={recommendation.actionSummary}
          />

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
                  onAccept(recommendation)
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
