import { useState } from 'react'
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
  ChevronUp,
  Loader2,
  Pencil,
  Trash2,
  Link2,
} from 'lucide-react'
import {
  Sheet,
  SheetContent,
  SheetHeader,
  SheetTitle,
  SheetDescription,
} from '@/components/ui/sheet'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { Separator } from '@/components/ui/separator'
import { Skeleton } from '@/components/ui/skeleton'
import { Textarea } from '@/components/ui/textarea'
import { cn } from '@/lib/utils'
import {
  recommendationTypeInfo,
  recommendationStatusInfo,
} from '@/types'
import type { RecommendationType } from '@/types'
import {
  useRecommendation,
  useAcceptRecommendation,
  useDismissRecommendation,
  useSnoozeRecommendation,
} from '../hooks'

const typeIcons: Record<RecommendationType, React.ElementType> = {
  NextBestAction: Zap,
  Top1Suggestion: Target,
  HabitModeSuggestion: Activity,
  PlanRealismAdjustment: AlertTriangle,
  TaskBreakdownSuggestion: Lightbulb,
  ScheduleAdjustmentSuggestion: Clock,
  ProjectStuckFix: AlertTriangle,
  ProjectSuggestion: Lightbulb,
  ProjectGoalLinkSuggestion: Link2,
  ExperimentRecommendation: Lightbulb,
  GoalScoreboardSuggestion: Target,
  HabitFromLeadMetricSuggestion: Activity,
  CheckInConsistencyNudge: Lightbulb,
  MetricObservationReminder: Activity,
  // Edit suggestions
  TaskEditSuggestion: Pencil,
  HabitEditSuggestion: Pencil,
  GoalEditSuggestion: Pencil,
  ProjectEditSuggestion: Pencil,
  MetricEditSuggestion: Pencil,
  ExperimentEditSuggestion: Pencil,
  // Archive suggestions
  TaskArchiveSuggestion: Trash2,
  HabitArchiveSuggestion: Trash2,
  GoalArchiveSuggestion: Trash2,
  ProjectArchiveSuggestion: Trash2,
  ExperimentArchiveSuggestion: Trash2,
}

function DetailSkeleton() {
  return (
    <div className="space-y-6 p-1">
      <div className="space-y-3">
        <Skeleton className="h-6 w-3/4" />
        <Skeleton className="h-4 w-full" />
        <Skeleton className="h-4 w-2/3" />
      </div>
      <Skeleton className="h-px w-full" />
      <div className="space-y-3">
        <Skeleton className="h-4 w-24" />
        <Skeleton className="h-16 w-full" />
      </div>
      <div className="space-y-3">
        <Skeleton className="h-4 w-24" />
        <Skeleton className="h-24 w-full" />
      </div>
    </div>
  )
}

interface RecommendationDetailSheetProps {
  recommendationId: string | null
  open: boolean
  onOpenChange: (open: boolean) => void
}

export function RecommendationDetailSheet({
  recommendationId,
  open,
  onOpenChange,
}: RecommendationDetailSheetProps) {
  const [showTrace, setShowTrace] = useState(false)
  const [showDismissReason, setShowDismissReason] = useState(false)
  const [dismissReason, setDismissReason] = useState('')

  const { data: recommendation, isLoading } = useRecommendation(recommendationId ?? '')
  const acceptMutation = useAcceptRecommendation()
  const dismissMutation = useDismissRecommendation()
  const snoozeMutation = useSnoozeRecommendation()

  const isActionPending = acceptMutation.isPending || dismissMutation.isPending || snoozeMutation.isPending

  const handleAccept = () => {
    if (!recommendation) return
    acceptMutation.mutate(recommendation)
  }

  const handleDismiss = () => {
    if (!recommendation) return
    dismissMutation.mutate({
      id: recommendation.id,
      reason: dismissReason || undefined,
    })
    setShowDismissReason(false)
    setDismissReason('')
  }

  const handleSnooze = () => {
    if (!recommendation) return
    snoozeMutation.mutate(recommendation.id)
  }

  const isActionable = recommendation?.status === 'Pending' || recommendation?.status === 'Snoozed'

  return (
    <Sheet open={open} onOpenChange={onOpenChange}>
      <SheetContent className="sm:max-w-lg overflow-y-auto">
        {isLoading || !recommendation ? (
          <>
            <SheetHeader>
              <SheetTitle>Recommendation</SheetTitle>
              <SheetDescription>Loading details...</SheetDescription>
            </SheetHeader>
            <DetailSkeleton />
          </>
        ) : (
          <>
            <SheetHeader>
              <div className="flex items-center gap-2 flex-wrap mb-2">
                {(() => {
                  const typeInfo = recommendationTypeInfo[recommendation.type]
                  const TypeIcon = typeIcons[recommendation.type]
                  return (
                    <Badge
                      className={cn(
                        'text-xs font-medium px-2.5 py-1 gap-1',
                        typeInfo.color,
                        typeInfo.bgColor
                      )}
                    >
                      <TypeIcon className="size-3" />
                      {typeInfo.label}
                    </Badge>
                  )
                })()}
                <Badge
                  className={cn(
                    'text-xs font-medium px-2.5 py-1',
                    recommendationStatusInfo[recommendation.status].color,
                    recommendationStatusInfo[recommendation.status].bgColor
                  )}
                >
                  {recommendationStatusInfo[recommendation.status].label}
                </Badge>
                <Badge variant="outline" className="text-xs font-normal px-2.5 py-1 border-border/50">
                  {recommendation.context}
                </Badge>
              </div>
              <SheetTitle className="text-lg">{recommendation.title}</SheetTitle>
              <SheetDescription>{recommendation.rationale}</SheetDescription>
            </SheetHeader>

            <div className="space-y-6 mt-6">
              {/* Target info */}
              {recommendation.targetEntityTitle && (
                <>
                  <div>
                    <h4 className="text-sm font-medium text-foreground mb-2">Target</h4>
                    <div className="flex items-center gap-2 text-sm text-muted-foreground">
                      <Badge variant="outline" className="text-xs">
                        {recommendation.targetKind}
                      </Badge>
                      <span>{recommendation.targetEntityTitle}</span>
                      <ArrowRight className="size-3 text-primary/60" />
                    </div>
                  </div>
                  <Separator />
                </>
              )}

              {/* Score */}
              <div>
                <h4 className="text-sm font-medium text-foreground mb-2">Score</h4>
                <div className="flex items-center gap-3">
                  <span className="text-2xl font-bold text-foreground">
                    {Math.round(recommendation.score * 100)}
                  </span>
                  <span className="text-sm text-muted-foreground">/ 100</span>
                </div>
              </div>

              <Separator />

              {/* Action payload */}
              {recommendation.actionPayload && (
                <>
                  <div>
                    <h4 className="text-sm font-medium text-foreground mb-2">Action Details</h4>
                    <pre className="text-xs text-muted-foreground bg-muted/50 rounded-lg p-3 overflow-x-auto whitespace-pre-wrap break-words">
                      {recommendation.actionPayload}
                    </pre>
                  </div>
                  <Separator />
                </>
              )}

              {/* Trace section (collapsible) */}
              {recommendation.trace && (
                <>
                  <div>
                    <button
                      type="button"
                      className="flex items-center gap-2 text-sm font-medium text-foreground hover:text-primary transition-colors w-full"
                      onClick={() => setShowTrace(!showTrace)}
                    >
                      {showTrace ? (
                        <ChevronUp className="size-4" />
                      ) : (
                        <ChevronDown className="size-4" />
                      )}
                      Recommendation Trace
                    </button>
                    {showTrace && (
                      <div className="mt-3 space-y-3 text-xs">
                        {recommendation.trace.signalsSummaryJson && (
                          <div>
                            <span className="text-muted-foreground font-medium">Signals Summary</span>
                            <pre className="mt-1 bg-muted/50 rounded-lg p-2 overflow-x-auto whitespace-pre-wrap break-words text-muted-foreground">
                              {recommendation.trace.signalsSummaryJson}
                            </pre>
                          </div>
                        )}
                        {recommendation.trace.candidateListJson && (
                          <div>
                            <span className="text-muted-foreground font-medium">Candidate List</span>
                            <pre className="mt-1 bg-muted/50 rounded-lg p-2 overflow-x-auto whitespace-pre-wrap break-words text-muted-foreground">
                              {recommendation.trace.candidateListJson}
                            </pre>
                          </div>
                        )}
                        <div className="flex items-center gap-4 text-muted-foreground">
                          <span>Selection: {recommendation.trace.selectionMethod}</span>
                          {recommendation.trace.modelVersion && (
                            <span>Model: {recommendation.trace.modelVersion}</span>
                          )}
                          {recommendation.trace.promptVersion && (
                            <span>Prompt: v{recommendation.trace.promptVersion}</span>
                          )}
                        </div>
                      </div>
                    )}
                  </div>
                  <Separator />
                </>
              )}

              {/* Timestamps */}
              <div className="text-xs text-muted-foreground space-y-1">
                <div>Created: {new Date(recommendation.createdAt).toLocaleString()}</div>
                {recommendation.respondedAt && (
                  <div>Responded: {new Date(recommendation.respondedAt).toLocaleString()}</div>
                )}
                {recommendation.dismissReason && (
                  <div>Dismiss reason: {recommendation.dismissReason}</div>
                )}
                {recommendation.expiresAt && (
                  <div>Expires: {new Date(recommendation.expiresAt).toLocaleString()}</div>
                )}
              </div>

              {/* Action buttons */}
              {isActionable && (
                <div className="space-y-3 pt-2">
                  <div className="flex items-center gap-2">
                    <Button
                      className="bg-green-600 hover:bg-green-700 text-white"
                      onClick={handleAccept}
                      disabled={isActionPending}
                    >
                      {acceptMutation.isPending ? (
                        <Loader2 className="size-4 mr-1.5 animate-spin" />
                      ) : (
                        <CheckCircle className="size-4 mr-1.5" />
                      )}
                      Accept
                    </Button>
                    <Button
                      variant="outline"
                      className="text-amber-500 border-amber-500/30 hover:bg-amber-500/10"
                      onClick={handleSnooze}
                      disabled={isActionPending}
                    >
                      {snoozeMutation.isPending ? (
                        <Loader2 className="size-4 mr-1.5 animate-spin" />
                      ) : (
                        <Clock className="size-4 mr-1.5" />
                      )}
                      Snooze
                    </Button>
                    <Button
                      variant="ghost"
                      className="text-muted-foreground hover:text-red-400"
                      onClick={() => {
                        if (showDismissReason) {
                          handleDismiss()
                        } else {
                          setShowDismissReason(true)
                        }
                      }}
                      disabled={isActionPending}
                    >
                      {dismissMutation.isPending ? (
                        <Loader2 className="size-4 mr-1.5 animate-spin" />
                      ) : (
                        <X className="size-4 mr-1.5" />
                      )}
                      Dismiss
                    </Button>
                  </div>
                  {showDismissReason && (
                    <div className="space-y-2">
                      <Textarea
                        placeholder="Reason for dismissing (optional)..."
                        value={dismissReason}
                        onChange={(e) => setDismissReason(e.target.value)}
                        rows={2}
                      />
                      <div className="flex items-center gap-2">
                        <Button
                          size="sm"
                          variant="destructive"
                          onClick={handleDismiss}
                          disabled={isActionPending}
                        >
                          Confirm Dismiss
                        </Button>
                        <Button
                          size="sm"
                          variant="ghost"
                          onClick={() => {
                            setShowDismissReason(false)
                            setDismissReason('')
                          }}
                        >
                          Cancel
                        </Button>
                      </div>
                    </div>
                  )}
                </div>
              )}
            </div>
          </>
        )}
      </SheetContent>
    </Sheet>
  )
}
