import { Clock, User, Target, Layers, FileText } from 'lucide-react'
import {
  Sheet,
  SheetContent,
  SheetHeader,
  SheetTitle,
  SheetDescription,
} from '@/components/ui/sheet'
import { Badge } from '@/components/ui/badge'
import { Separator } from '@/components/ui/separator'
import { Skeleton } from '@/components/ui/skeleton'
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs'
import { cn } from '@/lib/utils'
import { useRecommendationTrace } from '../hooks'
import { JsonViewer } from './json-viewer'
import { AgentRunsTable } from './agent-runs-table'
import {
  recommendationStatusInfo,
  recommendationTypeInfo,
  tierInfo,
  windowTypeInfo,
} from '@/types'
import type { RecommendationType, RecommendationStatus } from '@/types'

interface TraceDetailSheetProps {
  traceId: string | null
  open: boolean
  onOpenChange: (open: boolean) => void
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

export function TraceDetailSheet({ traceId, open, onOpenChange }: TraceDetailSheetProps) {
  const { data: trace, isLoading } = useRecommendationTrace(traceId ?? '')

  if (!open) return null

  const typeInfo = trace
    ? (recommendationTypeInfo[trace.recommendationType as RecommendationType] ?? {
        label: trace.recommendationType,
        color: 'text-gray-400',
        bgColor: 'bg-gray-500/10',
      })
    : null
  const statusInfo = trace
    ? (recommendationStatusInfo[trace.recommendationStatus as RecommendationStatus] ?? {
        label: trace.recommendationStatus,
        color: 'text-gray-400',
        bgColor: 'bg-gray-500/10',
      })
    : null
  const tier = trace ? (tierInfo[trace.finalTier] ?? tierInfo[0]) : null
  const windowType = trace ? (windowTypeInfo[trace.processingWindowType] ?? windowTypeInfo.Unknown) : null

  return (
    <Sheet open={open} onOpenChange={onOpenChange}>
      <SheetContent className="sm:max-w-2xl overflow-y-auto p-6">
        {isLoading || !trace ? (
          <>
            <SheetHeader className="p-0">
              <SheetTitle>Trace Details</SheetTitle>
              <SheetDescription>Loading trace information...</SheetDescription>
            </SheetHeader>
            <DetailSkeleton />
          </>
        ) : (
          <>
            <SheetHeader className="space-y-3 p-0">
              {/* Badges */}
              <div className="flex items-center gap-2 flex-wrap">
                <Badge className={cn('text-xs', tier!.color, tier!.bgColor)}>{tier!.label}</Badge>
                <Badge className={cn('text-xs', typeInfo!.color, typeInfo!.bgColor)}>{typeInfo!.label}</Badge>
                <Badge className={cn('text-xs', statusInfo!.color, statusInfo!.bgColor)}>{statusInfo!.label}</Badge>
                <Badge className={cn('text-xs', windowType!.color, windowType!.bgColor)}>{windowType!.label}</Badge>
              </div>

              <SheetTitle className="text-lg">{trace.recommendationTitle}</SheetTitle>
              <SheetDescription>{trace.recommendationRationale}</SheetDescription>
            </SheetHeader>

            <div className="mt-6 space-y-6">
              {/* Overview stats */}
              <div className="grid grid-cols-2 gap-4">
                <div className="flex items-center gap-2 text-sm">
                  <User className="size-4 text-muted-foreground" />
                  <span className="text-muted-foreground">User:</span>
                  <span className="font-medium truncate" title={trace.userEmail}>
                    {trace.userEmail}
                  </span>
                </div>
                <div className="flex items-center gap-2 text-sm">
                  <Target className="size-4 text-muted-foreground" />
                  <span className="text-muted-foreground">Score:</span>
                  <span className="font-medium">{Math.round(trace.recommendationScore * 100)}/100</span>
                </div>
                <div className="flex items-center gap-2 text-sm">
                  <Clock className="size-4 text-muted-foreground" />
                  <span className="text-muted-foreground">Duration:</span>
                  <span className="font-medium">{trace.totalDurationMs.toLocaleString()}ms</span>
                </div>
                <div className="flex items-center gap-2 text-sm">
                  <Layers className="size-4 text-muted-foreground" />
                  <span className="text-muted-foreground">Method:</span>
                  <span className="font-medium font-mono text-xs">{trace.selectionMethod}</span>
                </div>
              </div>

              {/* Model info if available */}
              {(trace.modelVersion || trace.promptVersion) && (
                <div className="flex items-center gap-4 text-xs text-muted-foreground">
                  {trace.modelVersion && <span>Model: {trace.modelVersion}</span>}
                  {trace.promptVersion && <span>Prompt: v{trace.promptVersion}</span>}
                </div>
              )}

              <Separator />

              {/* Tabs for different data sections */}
              <Tabs defaultValue="pipeline" className="w-full">
                <TabsList className="w-full grid grid-cols-3">
                  <TabsTrigger value="pipeline" className="text-xs">Pipeline</TabsTrigger>
                  <TabsTrigger value="state" className="text-xs">State</TabsTrigger>
                  <TabsTrigger value="llm" className="text-xs">
                    LLM ({trace.agentRuns.length})
                  </TabsTrigger>
                </TabsList>

                {/* Pipeline tab - tier-specific info */}
                <TabsContent value="pipeline" className="space-y-4 mt-4">
                  <JsonViewer
                    title="Tier 0: Triggered Rules"
                    data={trace.tier0TriggeredRules}
                    defaultExpanded
                  />

                  {trace.tier1Scores !== undefined && trace.tier1Scores !== null ? (
                    <JsonViewer
                      title="Tier 1: Assessment Scores"
                      data={trace.tier1Scores}
                      defaultExpanded
                    />
                  ) : null}

                  {trace.tier1EscalationReason && (
                    <div className="p-3 border rounded-lg bg-yellow-500/5">
                      <div className="text-sm font-medium text-yellow-500 mb-1">
                        Tier 1 Escalation Reason
                      </div>
                      <div className="text-sm text-muted-foreground">
                        {String(trace.tier1EscalationReason)}
                      </div>
                    </div>
                  )}

                  {trace.policyResult !== undefined && trace.policyResult !== null ? (
                    <JsonViewer
                      title="Policy Enforcement Result"
                      data={trace.policyResult}
                      defaultExpanded
                    />
                  ) : null}

                  <JsonViewer title="Candidate List" data={trace.candidateList} />
                  <JsonViewer title="Signals Summary" data={trace.signalsSummary} />
                </TabsContent>

                {/* State tab - full state snapshot */}
                <TabsContent value="state" className="space-y-4 mt-4">
                  <JsonViewer
                    title="State Snapshot (Decompressed)"
                    data={trace.stateSnapshot}
                    defaultExpanded
                    maxHeight="600px"
                  />
                </TabsContent>

                {/* LLM tab - agent runs and raw response */}
                <TabsContent value="llm" className="space-y-4 mt-4">
                  <AgentRunsTable agentRuns={trace.agentRuns} />

                  {trace.rawLlmResponse && (
                    <>
                      <Separator />
                      <div className="space-y-2">
                        <div className="flex items-center gap-2 text-sm font-medium">
                          <FileText className="size-4" />
                          Raw LLM Response
                        </div>
                        <pre className="p-4 text-xs overflow-auto bg-zinc-950/50 text-zinc-300 rounded-lg font-mono whitespace-pre-wrap break-words max-h-96">
                          {trace.rawLlmResponse}
                        </pre>
                      </div>
                    </>
                  )}
                </TabsContent>
              </Tabs>

              {/* Timestamps */}
              <div className="text-xs text-muted-foreground space-y-1 pt-4 border-t">
                <div>Created: {new Date(trace.createdAt).toLocaleString()}</div>
                {trace.modifiedAt && <div>Modified: {new Date(trace.modifiedAt).toLocaleString()}</div>}
                <div className="font-mono text-muted-foreground/60">ID: {trace.id}</div>
              </div>
            </div>
          </>
        )}
      </SheetContent>
    </Sheet>
  )
}
