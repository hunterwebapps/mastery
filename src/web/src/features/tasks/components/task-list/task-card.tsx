import { Link } from 'react-router-dom'
import { format } from 'date-fns'
import {
  Clock,
  Calendar,
  Target,
  FolderKanban,
  ChevronRight,
  AlertCircle,
  Lock,
} from 'lucide-react'
import { cn } from '@/lib/utils'
import { Card } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import type { TaskSummaryDto } from '@/types/task'
import { taskStatusInfo, dueTypeInfo } from '@/types/task'
import { EnergyIndicator } from '../common/energy-indicator'
import { ContextTags } from '../common/context-tag-badge'
import { PriorityBadge } from '../common/priority-badge'

interface TaskCardProps {
  task: TaskSummaryDto
}

export function TaskCard({ task }: TaskCardProps) {
  const statusInfo = taskStatusInfo[task.status]
  const isCompleted = task.status === 'Completed'
  const isCancelled = task.status === 'Cancelled'
  const isArchived = task.status === 'Archived'
  const isDimmed = isCompleted || isCancelled || isArchived

  return (
    <Link to={`/tasks/${task.id}`} className="block group">
      <Card
        className={cn(
          'relative overflow-hidden transition-all duration-200',
          'hover:shadow-md hover:border-primary/30',
          isCompleted && 'border-green-500/20 bg-green-500/5',
          isCancelled && 'border-muted bg-muted/30',
          isArchived && 'border-muted bg-muted/20',
          task.isOverdue && !isDimmed && 'border-red-500/50',
          task.isBlocked && !isDimmed && 'opacity-70'
        )}
      >
        <div className="p-4">
          <div className="flex items-start gap-3">
            {/* Status indicator */}
            <div
              className={cn(
                'flex-shrink-0 size-3 rounded-full mt-1.5',
                statusInfo.bgColor,
                isCompleted && 'bg-green-500',
                isCancelled && 'bg-red-500/50',
                isArchived && 'bg-muted-foreground/30'
              )}
            />

            {/* Task info */}
            <div className="flex-1 min-w-0">
              <div className="flex items-start justify-between gap-2">
                <div className="flex-1 min-w-0">
                  <div className="flex items-center gap-1">
                    <h3
                      className={cn(
                        'font-medium truncate transition-colors',
                        isDimmed && 'line-through text-muted-foreground',
                        !isDimmed && 'group-hover:text-primary'
                      )}
                    >
                      {task.title}
                    </h3>
                    <ChevronRight className="size-4 opacity-0 -ml-1 transition-all group-hover:opacity-50 group-hover:ml-0 flex-shrink-0" />
                  </div>

                  {task.description && (
                    <p className="text-sm text-muted-foreground truncate mt-0.5">
                      {task.description}
                    </p>
                  )}
                </div>

                {/* Status and priority badges */}
                <div className="flex items-center gap-1.5 flex-shrink-0">
                  <PriorityBadge priority={task.priority} />
                  <Badge className={cn('text-xs', statusInfo.bgColor, statusInfo.color)}>
                    {statusInfo.label}
                  </Badge>
                </div>
              </div>

              {/* Meta row */}
              <div className="flex items-center flex-wrap gap-x-4 gap-y-2 mt-3">
                {/* Time estimate */}
                <div className="flex items-center gap-1 text-xs text-muted-foreground">
                  <Clock className="size-3.5" />
                  <span>{task.estimatedMinutes}m</span>
                </div>

                {/* Energy cost */}
                <EnergyIndicator energyCost={task.energyCost} />

                {/* Context tags */}
                <ContextTags tags={task.contextTags} max={3} />

                {/* Due date */}
                {task.dueOn && (
                  <div
                    className={cn(
                      'flex items-center gap-1 text-xs',
                      task.isOverdue && !isDimmed
                        ? 'text-red-500'
                        : task.dueType === 'Hard'
                          ? 'text-orange-400'
                          : 'text-muted-foreground'
                    )}
                  >
                    <Calendar className="size-3.5" />
                    <span>
                      {format(new Date(task.dueOn), 'MMM d')}
                      {task.dueType === 'Hard' && (
                        <span className="ml-1 text-[10px]">
                          ({dueTypeInfo.Hard.label})
                        </span>
                      )}
                    </span>
                  </div>
                )}

                {/* Scheduled date */}
                {task.scheduledOn && !task.dueOn && (
                  <div className="flex items-center gap-1 text-xs text-muted-foreground">
                    <Calendar className="size-3.5" />
                    <span>{format(new Date(task.scheduledOn), 'MMM d')}</span>
                  </div>
                )}

                {/* Project link */}
                {task.projectTitle && (
                  <div className="flex items-center gap-1 text-xs text-muted-foreground">
                    <FolderKanban className="size-3.5" />
                    <span className="truncate max-w-[120px]">{task.projectTitle}</span>
                  </div>
                )}

                {/* Goal link */}
                {task.goalTitle && !task.projectTitle && (
                  <div className="flex items-center gap-1 text-xs text-muted-foreground">
                    <Target className="size-3.5" />
                    <span className="truncate max-w-[120px]">{task.goalTitle}</span>
                  </div>
                )}

                {/* Warnings */}
                {task.isBlocked && !isDimmed && (
                  <div className="flex items-center gap-1 text-xs text-amber-500">
                    <Lock className="size-3" />
                    <span>Blocked</span>
                  </div>
                )}

                {task.rescheduleCount > 0 && !isDimmed && (
                  <div className="flex items-center gap-1 text-xs text-amber-500">
                    <AlertCircle className="size-3" />
                    <span>Rescheduled {task.rescheduleCount}x</span>
                  </div>
                )}
              </div>
            </div>
          </div>
        </div>
      </Card>
    </Link>
  )
}
