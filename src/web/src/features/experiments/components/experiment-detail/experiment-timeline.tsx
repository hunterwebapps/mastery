import { Check, Circle } from 'lucide-react'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { cn } from '@/lib/utils'
import type { ExperimentDto } from '@/types'

interface ExperimentTimelineProps {
  experiment: ExperimentDto
}

interface TimelineStep {
  key: string
  label: string
  date?: string
  status: 'completed' | 'current' | 'upcoming'
}

function formatShortDate(dateString?: string): string | undefined {
  if (!dateString) return undefined
  const date = new Date(dateString)
  return date.toLocaleDateString('en-US', {
    month: 'short',
    day: 'numeric',
  })
}

function buildSteps(experiment: ExperimentDto): TimelineStep[] {
  const { status, createdAt, startDate, endDateActual, endDatePlanned } = experiment

  const statusOrder: Record<string, number> = {
    Draft: 0,
    Active: 1,
    Paused: 1,
    Completed: 2,
    Abandoned: 2,
    Archived: 3,
  }

  const currentIndex = statusOrder[status] ?? 0
  const isTerminal = status === 'Completed' || status === 'Abandoned' || status === 'Archived'

  const steps: TimelineStep[] = []

  // Step 0: Draft / Created
  steps.push({
    key: 'draft',
    label: 'Created',
    date: formatShortDate(createdAt),
    status: currentIndex > 0 ? 'completed' : 'current',
  })

  // Step 1: Active (or Paused if currently paused)
  steps.push({
    key: 'active',
    label: status === 'Paused' ? 'Paused' : 'Active',
    date: formatShortDate(startDate),
    status:
      currentIndex > 1
        ? 'completed'
        : currentIndex === 1
          ? 'current'
          : 'upcoming',
  })

  // Step 2: Terminal state
  if (status === 'Abandoned') {
    steps.push({
      key: 'terminal',
      label: 'Abandoned',
      date: formatShortDate(endDateActual),
      status: isTerminal ? 'current' : 'upcoming',
    })
  } else {
    steps.push({
      key: 'terminal',
      label: 'Completed',
      date: formatShortDate(endDateActual) ?? (endDatePlanned ? `Target: ${formatShortDate(endDatePlanned)}` : undefined),
      status:
        status === 'Completed' || status === 'Archived'
          ? 'completed'
          : 'upcoming',
    })
  }

  return steps
}

export function ExperimentTimeline({ experiment }: ExperimentTimelineProps) {
  const steps = buildSteps(experiment)

  return (
    <Card>
      <CardHeader className="pb-4">
        <CardTitle className="text-base font-semibold flex items-center gap-2">
          <Circle className="size-4 text-violet-400" />
          Lifecycle
        </CardTitle>
      </CardHeader>
      <CardContent>
        <div className="flex items-start">
          {steps.map((step, index) => {
            const isLast = index === steps.length - 1
            const isAbandoned = step.key === 'terminal' && experiment.status === 'Abandoned'

            return (
              <div
                key={step.key}
                className={cn('flex flex-col items-center', !isLast && 'flex-1')}
              >
                {/* Dot + connector row */}
                <div className="flex items-center w-full">
                  {/* Leading connector */}
                  {index > 0 && (
                    <div
                      className={cn(
                        'h-0.5 flex-1',
                        step.status === 'upcoming'
                          ? 'bg-border/40'
                          : isAbandoned
                            ? 'bg-red-500/30'
                            : 'bg-violet-500/40'
                      )}
                    />
                  )}

                  {/* Step dot */}
                  <div
                    className={cn(
                      'shrink-0 flex items-center justify-center rounded-full transition-all',
                      step.status === 'completed' && !isAbandoned &&
                        'size-7 bg-violet-500/15 ring-2 ring-violet-500/40',
                      step.status === 'current' && !isAbandoned &&
                        'size-7 bg-violet-500/20 ring-2 ring-violet-500 shadow-[0_0_8px_rgba(139,92,246,0.3)]',
                      step.status === 'current' && isAbandoned &&
                        'size-7 bg-red-500/20 ring-2 ring-red-500 shadow-[0_0_8px_rgba(239,68,68,0.3)]',
                      step.status === 'upcoming' &&
                        'size-7 bg-muted ring-2 ring-border/50',
                    )}
                  >
                    {step.status === 'completed' && !isAbandoned && (
                      <Check className="size-3.5 text-violet-400" />
                    )}
                    {step.status === 'current' && !isAbandoned && (
                      <div className="size-2.5 rounded-full bg-violet-400" />
                    )}
                    {step.status === 'current' && isAbandoned && (
                      <div className="size-2.5 rounded-full bg-red-400" />
                    )}
                    {step.status === 'upcoming' && (
                      <div className="size-2 rounded-full bg-border" />
                    )}
                  </div>

                  {/* Trailing connector */}
                  {!isLast && (
                    <div
                      className={cn(
                        'h-0.5 flex-1',
                        steps[index + 1].status === 'upcoming'
                          ? 'bg-border/40'
                          : 'bg-violet-500/40'
                      )}
                    />
                  )}
                </div>

                {/* Labels below dot */}
                <div className="mt-2.5 flex flex-col items-center text-center px-1">
                  <span
                    className={cn(
                      'text-xs font-medium',
                      step.status === 'current' && !isAbandoned && 'text-violet-400',
                      step.status === 'current' && isAbandoned && 'text-red-400',
                      step.status === 'completed' && 'text-foreground',
                      step.status === 'upcoming' && 'text-muted-foreground/50',
                    )}
                  >
                    {step.label}
                  </span>
                  {step.date && (
                    <span
                      className={cn(
                        'text-[10px] mt-0.5',
                        step.status === 'upcoming'
                          ? 'text-muted-foreground/40'
                          : 'text-muted-foreground/70',
                      )}
                    >
                      {step.date}
                    </span>
                  )}
                </div>
              </div>
            )
          })}
        </div>
      </CardContent>
    </Card>
  )
}
