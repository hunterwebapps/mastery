import { Sparkles } from 'lucide-react'
import { Skeleton } from '@/components/ui/skeleton'
import { RecommendationCard } from './recommendation-card'
import type { RecommendationSummaryDto } from '@/types'

interface RecommendationsListProps {
  recommendations: RecommendationSummaryDto[]
  isLoading?: boolean
  onAccept: (id: string) => void
  onDismiss: (id: string) => void
  onSnooze: (id: string) => void
}

function RecommendationCardSkeleton() {
  return (
    <div className="rounded-lg border border-border border-l-4 border-l-muted p-5 space-y-3">
      <div className="flex items-center gap-2">
        <Skeleton className="h-5 w-28" />
        <Skeleton className="h-5 w-16" />
      </div>
      <Skeleton className="h-5 w-3/4" />
      <Skeleton className="h-4 w-full" />
      <Skeleton className="h-4 w-2/3" />
      <Skeleton className="h-1 w-full mt-1" />
      <div className="pt-1 border-t border-border/50">
        <div className="flex gap-2 pt-2">
          <Skeleton className="h-8 w-20" />
          <Skeleton className="h-8 w-20" />
          <Skeleton className="h-8 w-20" />
        </div>
      </div>
    </div>
  )
}

export function RecommendationsList({
  recommendations,
  isLoading,
  onAccept,
  onDismiss,
  onSnooze,
}: RecommendationsListProps) {
  if (isLoading) {
    return (
      <div className="grid gap-4 sm:grid-cols-2">
        {Array.from({ length: 3 }).map((_, i) => (
          <RecommendationCardSkeleton key={i} />
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
          No recommendations right now
        </h3>
        <p className="text-sm text-muted-foreground text-center max-w-md">
          Check back after your next check-in.
        </p>
      </div>
    )
  }

  return (
    <div className="grid gap-4 sm:grid-cols-2">
      {recommendations.map((rec) => (
        <RecommendationCard
          key={rec.id}
          recommendation={rec}
          onAccept={onAccept}
          onDismiss={onDismiss}
          onSnooze={onSnooze}
        />
      ))}
    </div>
  )
}
