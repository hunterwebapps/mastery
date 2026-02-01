import { Loader2, Clock, Cpu, Zap, User } from 'lucide-react'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Card, CardContent } from '@/components/ui/card'
import { cn } from '@/lib/utils'
import {
  recommendationStatusInfo,
  recommendationTypeInfo,
  recommendationContextInfo,
  tierInfo,
  windowTypeInfo,
} from '@/types'
import type { AdminTraceListDto, RecommendationType, RecommendationStatus, RecommendationContext } from '@/types'

interface TraceListProps {
  traces: AdminTraceListDto[]
  isLoading: boolean
  onSelectTrace: (id: string) => void
}

export function TraceList({ traces, isLoading, onSelectTrace }: TraceListProps) {
  if (isLoading) {
    return (
      <div className="flex items-center justify-center py-12">
        <Loader2 className="size-8 animate-spin text-muted-foreground" />
      </div>
    )
  }

  if (traces.length === 0) {
    return (
      <div className="text-center py-12">
        <Cpu className="size-12 mx-auto mb-4 text-muted-foreground opacity-50" />
        <p className="text-muted-foreground">No traces found</p>
        <p className="text-sm text-muted-foreground/60 mt-1">
          Try adjusting your filters or triggering a recommendation generation
        </p>
      </div>
    )
  }

  return (
    <div className="space-y-3">
      {traces.map((trace) => {
        const typeInfo = recommendationTypeInfo[trace.recommendationType as RecommendationType] ?? {
          label: trace.recommendationType,
          color: 'text-gray-400',
          bgColor: 'bg-gray-500/10',
        }
        const statusInfo = recommendationStatusInfo[trace.recommendationStatus as RecommendationStatus] ?? {
          label: trace.recommendationStatus,
          color: 'text-gray-400',
          bgColor: 'bg-gray-500/10',
        }
        const contextInfo = recommendationContextInfo[trace.context as RecommendationContext] ?? {
          label: trace.context,
          description: '',
        }
        const tier = tierInfo[trace.finalTier] ?? tierInfo[0]
        const windowType = windowTypeInfo[trace.processingWindowType] ?? windowTypeInfo.Unknown

        return (
          <Card key={trace.id} className="hover:border-primary/50 transition-colors">
            <CardContent className="p-4">
              <div className="flex items-start justify-between gap-4">
                <div className="space-y-2 flex-1 min-w-0">
                  {/* Badges row */}
                  <div className="flex items-center gap-2 flex-wrap">
                    <Badge className={cn('text-xs', tier.color, tier.bgColor)}>
                      {tier.label}
                    </Badge>
                    <Badge className={cn('text-xs', typeInfo.color, typeInfo.bgColor)}>
                      {typeInfo.label}
                    </Badge>
                    <Badge className={cn('text-xs', statusInfo.color, statusInfo.bgColor)}>
                      {statusInfo.label}
                    </Badge>
                    <Badge variant="outline" className="text-xs">
                      {contextInfo.label}
                    </Badge>
                    <Badge className={cn('text-xs', windowType.color, windowType.bgColor)}>
                      {windowType.label}
                    </Badge>
                  </div>

                  {/* User info */}
                  <div className="flex items-center gap-2 text-sm text-muted-foreground">
                    <User className="size-3.5" />
                    <span className="truncate max-w-60" title={trace.userEmail}>
                      {trace.userEmail}
                    </span>
                  </div>

                  {/* Stats row */}
                  <div className="flex items-center gap-4 text-xs text-muted-foreground">
                    <div className="flex items-center gap-1">
                      <Clock className="size-3.5" />
                      <span>{trace.totalDurationMs.toLocaleString()}ms</span>
                    </div>
                    {trace.totalTokens > 0 && (
                      <div className="flex items-center gap-1">
                        <Zap className="size-3.5" />
                        <span>{trace.totalTokens.toLocaleString()} tokens</span>
                      </div>
                    )}
                    {trace.agentRunCount > 0 && (
                      <div className="flex items-center gap-1">
                        <Cpu className="size-3.5" />
                        <span>{trace.agentRunCount} LLM calls</span>
                      </div>
                    )}
                    <div className="text-muted-foreground/60">
                      {new Date(trace.createdAt).toLocaleString()}
                    </div>
                  </div>
                </div>

                <Button variant="outline" size="sm" onClick={() => onSelectTrace(trace.id)}>
                  Details
                </Button>
              </div>
            </CardContent>
          </Card>
        )
      })}
    </div>
  )
}
