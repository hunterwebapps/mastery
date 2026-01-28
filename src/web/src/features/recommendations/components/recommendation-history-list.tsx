import { useState } from 'react'
import {
  Sparkles,
  CheckCircle,
  X,
  Clock,
  AlertTriangle,
  Zap,
} from 'lucide-react'
import { Skeleton } from '@/components/ui/skeleton'
import { cn } from '@/lib/utils'
import { recommendationTypeInfo, recommendationStatusInfo } from '@/types'
import type { RecommendationSummaryDto, RecommendationStatus } from '@/types'
import { RecommendationDetailSheet } from './recommendation-detail-sheet'

interface RecommendationHistoryListProps {
  recommendations: RecommendationSummaryDto[]
  isLoading?: boolean
}

function HistoryItemSkeleton() {
  return (
    <div className="flex items-start gap-3 rounded-lg border border-border p-4">
      <Skeleton className="size-8 rounded-full shrink-0" />
      <div className="flex-1 space-y-2">
        <Skeleton className="h-4 w-3/4" />
        <div className="flex gap-3">
          <Skeleton className="h-3 w-20" />
          <Skeleton className="h-3 w-24" />
          <Skeleton className="h-3 w-16" />
        </div>
      </div>
    </div>
  )
}

const statusIcons: Record<RecommendationStatus, React.ElementType> = {
  Pending: Clock,
  Accepted: CheckCircle,
  Dismissed: X,
  Snoozed: Clock,
  Expired: AlertTriangle,
  Executed: Zap,
}

function formatDate(dateString: string): string {
  return new Date(dateString).toLocaleDateString('en-US', {
    weekday: 'long',
    year: 'numeric',
    month: 'long',
    day: 'numeric',
  })
}

function formatTime(dateString: string): string {
  return new Date(dateString).toLocaleTimeString('en-US', {
    hour: 'numeric',
    minute: '2-digit',
  })
}

export function RecommendationHistoryList({
  recommendations,
  isLoading,
}: RecommendationHistoryListProps) {
  const [selectedId, setSelectedId] = useState<string | null>(null)
  const [sheetOpen, setSheetOpen] = useState(false)

  const handleViewDetail = (id: string) => {
    setSelectedId(id)
    setSheetOpen(true)
  }

  if (isLoading) {
    return (
      <div className="space-y-3">
        {Array.from({ length: 6 }).map((_, i) => (
          <HistoryItemSkeleton key={i} />
        ))}
      </div>
    )
  }

  if (recommendations.length === 0) {
    return (
      <div className="flex flex-col items-center justify-center py-20 px-4">
        <div className="p-4 rounded-2xl bg-muted/50 mb-5">
          <Sparkles className="size-10 text-muted-foreground/60" />
        </div>
        <h3 className="text-lg font-semibold text-foreground mb-1">
          No recommendation history
        </h3>
        <p className="text-sm text-muted-foreground text-center max-w-md">
          Past recommendations and their outcomes will appear here as you interact with the system.
        </p>
      </div>
    )
  }

  // Group by date
  const grouped = recommendations.reduce<Record<string, RecommendationSummaryDto[]>>((acc, rec) => {
    const date = formatDate(rec.createdAt)
    if (!acc[date]) acc[date] = []
    acc[date].push(rec)
    return acc
  }, {})

  return (
    <>
      <div className="space-y-6">
        {Object.entries(grouped).map(([date, recs]) => (
          <div key={date}>
            <h3 className="text-sm font-medium text-muted-foreground mb-3">{date}</h3>
            <div className="space-y-2">
              {recs.map((rec) => {
                const typeInfo = recommendationTypeInfo[rec.type]
                const statusInfo = recommendationStatusInfo[rec.status]
                const StatusIcon = statusIcons[rec.status]

                return (
                  <div
                    key={rec.id}
                    className="flex items-start gap-3 rounded-lg border border-border hover:border-primary/30 transition-colors p-4 cursor-pointer group"
                    onClick={() => handleViewDetail(rec.id)}
                  >
                    <div className={cn('p-2 rounded-full shrink-0', typeInfo.bgColor)}>
                      <StatusIcon className={cn('size-4', statusInfo.color)} />
                    </div>
                    <div className="flex-1 min-w-0">
                      <h4 className="text-sm font-medium text-foreground group-hover:text-primary transition-colors truncate">
                        {rec.title}
                      </h4>
                      <div className="flex items-center gap-3 mt-1.5">
                        <span className={cn('text-xs font-medium', typeInfo.color)}>
                          {typeInfo.label}
                        </span>
                        <span className={cn('text-xs font-medium', statusInfo.color, statusInfo.bgColor, 'px-1.5 py-0.5 rounded')}>
                          {statusInfo.label}
                        </span>
                        <span className="text-xs text-muted-foreground/60">
                          {formatTime(rec.createdAt)}
                        </span>
                      </div>
                    </div>
                  </div>
                )
              })}
            </div>
          </div>
        ))}
      </div>

      <RecommendationDetailSheet
        recommendationId={selectedId}
        open={sheetOpen}
        onOpenChange={setSheetOpen}
      />
    </>
  )
}
