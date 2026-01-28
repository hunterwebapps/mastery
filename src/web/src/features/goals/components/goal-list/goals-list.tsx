import { GoalCard } from './goal-card'
import { Skeleton } from '@/components/ui/skeleton'
import type { GoalSummaryDto } from '@/types'

interface GoalsListProps {
  goals: GoalSummaryDto[]
  isLoading?: boolean
}

function GoalCardSkeleton() {
  return (
    <div className="rounded-lg border border-border p-6 space-y-4">
      <div className="flex items-start justify-between">
        <div className="space-y-2">
          <Skeleton className="h-5 w-40" />
          <Skeleton className="h-5 w-16" />
        </div>
        <Skeleton className="h-5 w-14" />
      </div>
      <Skeleton className="h-4 w-32" />
      <div className="pt-2 border-t border-border">
        <Skeleton className="h-3 w-20 mb-2" />
        <div className="flex gap-4">
          <Skeleton className="h-4 w-20" />
          <Skeleton className="h-4 w-16" />
        </div>
      </div>
    </div>
  )
}

export function GoalsList({ goals, isLoading }: GoalsListProps) {
  if (isLoading) {
    return (
      <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
        {Array.from({ length: 6 }).map((_, i) => (
          <GoalCardSkeleton key={i} />
        ))}
      </div>
    )
  }

  if (goals.length === 0) {
    return (
      <div className="text-center py-12">
        <p className="text-muted-foreground">No goals yet. Create your first goal to get started.</p>
      </div>
    )
  }

  return (
    <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
      {goals.map((goal) => (
        <GoalCard key={goal.id} goal={goal} />
      ))}
    </div>
  )
}
