import { Link } from 'react-router-dom'
import { FlaskConical, Plus } from 'lucide-react'
import { Skeleton } from '@/components/ui/skeleton'
import { Button } from '@/components/ui/button'
import { ExperimentCard } from './experiment-card'
import type { ExperimentSummaryDto, ExperimentStatus } from '@/types'

interface ExperimentsListProps {
  experiments: ExperimentSummaryDto[]
  isLoading?: boolean
  statusFilter?: ExperimentStatus | 'all'
}

function ExperimentCardSkeleton() {
  return (
    <div className="rounded-lg border border-border border-l-4 border-l-muted p-5 space-y-3">
      <div className="flex items-center gap-2">
        <Skeleton className="h-5 w-16" />
        <Skeleton className="h-5 w-24" />
      </div>
      <Skeleton className="h-5 w-3/4" />
      <Skeleton className="h-4 w-full" />
      <Skeleton className="h-4 w-2/3" />
      <div className="pt-1 border-t border-border/50">
        <div className="flex gap-4 pt-2">
          <Skeleton className="h-3 w-28" />
          <Skeleton className="h-3 w-14" />
        </div>
      </div>
    </div>
  )
}

function getEmptyStateMessage(statusFilter?: ExperimentStatus | 'all'): {
  title: string
  description: string
  showCta: boolean
} {
  switch (statusFilter) {
    case 'Active':
      return {
        title: 'No active experiments',
        description: 'Start an experiment from your drafts or create a new one to begin testing.',
        showCta: true,
      }
    case 'Draft':
      return {
        title: 'No draft experiments',
        description: 'Create a new experiment to start designing your next test.',
        showCta: true,
      }
    case 'Paused':
      return {
        title: 'No paused experiments',
        description: 'Paused experiments will appear here when you temporarily put them on hold.',
        showCta: false,
      }
    case 'Completed':
      return {
        title: 'No completed experiments yet',
        description: 'Completed experiments with their results will show here. Keep experimenting!',
        showCta: true,
      }
    case 'Abandoned':
      return {
        title: 'No abandoned experiments',
        description: 'Experiments you stop early will appear here for future reference.',
        showCta: false,
      }
    case 'Archived':
      return {
        title: 'No archived experiments',
        description: 'Archived experiments are stored here for long-term reference.',
        showCta: false,
      }
    default:
      return {
        title: 'No experiments yet',
        description: 'Experiments help you systematically test what works for you. Start your first one!',
        showCta: true,
      }
  }
}

export function ExperimentsList({ experiments, isLoading, statusFilter }: ExperimentsListProps) {
  if (isLoading) {
    return (
      <div className="grid gap-4 sm:grid-cols-2">
        {Array.from({ length: 4 }).map((_, i) => (
          <ExperimentCardSkeleton key={i} />
        ))}
      </div>
    )
  }

  if (experiments.length === 0) {
    const emptyState = getEmptyStateMessage(statusFilter)
    return (
      <div className="flex flex-col items-center justify-center py-20 px-4">
        <div className="p-4 rounded-2xl bg-muted/50 mb-5">
          <FlaskConical className="size-10 text-muted-foreground/60" />
        </div>
        <h3 className="text-lg font-semibold text-foreground mb-1">
          {emptyState.title}
        </h3>
        <p className="text-sm text-muted-foreground text-center max-w-md mb-6">
          {emptyState.description}
        </p>
        {emptyState.showCta && (
          <Button asChild>
            <Link to="/experiments/new">
              <Plus className="size-4 mr-2" />
              New Experiment
            </Link>
          </Button>
        )}
      </div>
    )
  }

  return (
    <div className="grid gap-4 sm:grid-cols-2">
      {experiments.map((experiment) => (
        <ExperimentCard key={experiment.id} experiment={experiment} />
      ))}
    </div>
  )
}
