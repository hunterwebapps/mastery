import { useMemo } from 'react'
import { CheckCircle2, Circle, Loader2, AlertTriangle, Clock } from 'lucide-react'
import { cn } from '@/lib/utils'
import { Progress } from '@/components/ui/progress'
import type { TodayTaskDto, RescheduleReason } from '@/types/task'
import { TodayTaskCard } from './today-task-card'

interface TodayTasksListProps {
  tasks: TodayTaskDto[]
  onComplete: (taskId: string, data: {
    completedOn: string
    actualMinutes?: number
    note?: string
    enteredValue?: number
  }) => Promise<void>
  onUndo: (taskId: string) => Promise<void>
  onReschedule: (taskId: string, newDate: string, reason?: RescheduleReason) => Promise<void>
  isLoading?: boolean
}

export function TodayTasksList({
  tasks,
  onComplete,
  onUndo,
  onReschedule,
  isLoading = false,
}: TodayTasksListProps) {
  // Calculate stats
  const stats = useMemo(() => {
    const activeTasks = tasks.filter(t => t.status !== 'Cancelled' && t.status !== 'Archived')
    const completed = activeTasks.filter(t => t.status === 'Completed')
    const overdue = activeTasks.filter(t => t.isOverdue && t.status !== 'Completed')
    const blocked = activeTasks.filter(t => t.isBlocked && t.status !== 'Completed')
    const pending = activeTasks.filter(t =>
      t.status !== 'Completed' && !t.isOverdue && !t.isBlocked
    )

    const totalMinutes = activeTasks
      .filter(t => t.status !== 'Completed')
      .reduce((sum, t) => sum + t.estimatedMinutes, 0)

    return {
      total: activeTasks.length,
      completed: completed.length,
      overdue: overdue.length,
      blocked: blocked.length,
      pending: pending.length,
      totalMinutes,
      progressPercent: activeTasks.length > 0 ? (completed.length / activeTasks.length) * 100 : 0,
      isAllDone: completed.length === activeTasks.length && activeTasks.length > 0,
    }
  }, [tasks])

  // Group and sort tasks
  const groupedTasks = useMemo(() => {
    const overdue: TodayTaskDto[] = []
    const dueToday: TodayTaskDto[] = []
    const scheduled: TodayTaskDto[] = []
    const completed: TodayTaskDto[] = []

    tasks.forEach((task) => {
      if (task.status === 'Completed') {
        completed.push(task)
      } else if (task.isOverdue) {
        overdue.push(task)
      } else if (task.dueOn) {
        dueToday.push(task)
      } else {
        scheduled.push(task)
      }
    })

    // Sort each group by priority, then display order
    const sortByPriorityAndOrder = (a: TodayTaskDto, b: TodayTaskDto) => {
      const priorityDiff = a.priority - b.priority
      if (priorityDiff !== 0) return priorityDiff
      return a.displayOrder - b.displayOrder
    }

    return {
      overdue: overdue.sort(sortByPriorityAndOrder),
      dueToday: dueToday.sort(sortByPriorityAndOrder),
      scheduled: scheduled.sort(sortByPriorityAndOrder),
      completed: completed.sort(sortByPriorityAndOrder),
    }
  }, [tasks])

  if (isLoading) {
    return (
      <div className="flex items-center justify-center py-12">
        <Loader2 className="size-8 animate-spin text-muted-foreground" />
      </div>
    )
  }

  if (tasks.length === 0) {
    return (
      <div className="text-center py-12">
        <Circle className="size-12 mx-auto mb-4 text-muted-foreground/50" />
        <h3 className="text-lg font-medium text-muted-foreground">No tasks for today</h3>
        <p className="text-sm text-muted-foreground mt-1">
          Schedule some tasks or enjoy your free day!
        </p>
      </div>
    )
  }

  return (
    <div className="space-y-6">
      {/* Progress header */}
      <div className="space-y-3">
        <div className="flex items-center justify-between">
          <div className="flex items-center gap-2">
            {stats.isAllDone ? (
              <CheckCircle2 className="size-5 text-green-500" />
            ) : (
              <Circle className="size-5 text-muted-foreground" />
            )}
            <span className="font-medium">
              {stats.isAllDone ? (
                <span className="text-green-500">All done!</span>
              ) : (
                <>
                  <span className="text-foreground">{stats.completed}</span>
                  <span className="text-muted-foreground"> of </span>
                  <span className="text-foreground">{stats.total}</span>
                  <span className="text-muted-foreground"> complete</span>
                </>
              )}
            </span>
          </div>

          <div className="flex items-center gap-3 text-sm text-muted-foreground">
            {stats.overdue > 0 && (
              <span className="flex items-center gap-1 text-red-500">
                <AlertTriangle className="size-4" />
                {stats.overdue} overdue
              </span>
            )}
            {stats.totalMinutes > 0 && (
              <span className="flex items-center gap-1">
                <Clock className="size-4" />
                ~{Math.round(stats.totalMinutes / 60)}h remaining
              </span>
            )}
          </div>
        </div>

        <Progress
          value={stats.progressPercent}
          className={cn(
            'h-2 transition-all',
            stats.isAllDone && '[&>div]:bg-green-500'
          )}
        />
      </div>

      {/* Overdue section */}
      {groupedTasks.overdue.length > 0 && (
        <TaskSection
          title="Overdue"
          icon={<AlertTriangle className="size-4" />}
          className="text-red-500"
          tasks={groupedTasks.overdue}
          onComplete={onComplete}
          onUndo={onUndo}
          onReschedule={onReschedule}
        />
      )}

      {/* Due today section */}
      {groupedTasks.dueToday.length > 0 && (
        <TaskSection
          title="Due Today"
          tasks={groupedTasks.dueToday}
          onComplete={onComplete}
          onUndo={onUndo}
          onReschedule={onReschedule}
        />
      )}

      {/* Scheduled section */}
      {groupedTasks.scheduled.length > 0 && (
        <TaskSection
          title="Scheduled"
          tasks={groupedTasks.scheduled}
          onComplete={onComplete}
          onUndo={onUndo}
          onReschedule={onReschedule}
        />
      )}

      {/* Completed section */}
      {groupedTasks.completed.length > 0 && (
        <TaskSection
          title="Completed"
          icon={<CheckCircle2 className="size-4" />}
          className="text-green-500"
          tasks={groupedTasks.completed}
          onComplete={onComplete}
          onUndo={onUndo}
          onReschedule={onReschedule}
        />
      )}

      {/* Motivational message when all done */}
      {stats.isAllDone && (
        <div className="text-center py-4 animate-in fade-in slide-in-from-bottom-2 duration-500">
          <p className="text-lg font-medium text-green-500">
            Great work today!
          </p>
          <p className="text-sm text-muted-foreground mt-1">
            You've completed all {stats.total} task{stats.total > 1 ? 's' : ''} for today.
          </p>
        </div>
      )}
    </div>
  )
}

interface TaskSectionProps {
  title: string
  icon?: React.ReactNode
  className?: string
  tasks: TodayTaskDto[]
  onComplete: TodayTasksListProps['onComplete']
  onUndo: TodayTasksListProps['onUndo']
  onReschedule: TodayTasksListProps['onReschedule']
}

function TaskSection({
  title,
  icon,
  className,
  tasks,
  onComplete,
  onUndo,
  onReschedule,
}: TaskSectionProps) {
  return (
    <div className="space-y-3">
      <div className={cn('flex items-center gap-2 text-sm font-medium', className)}>
        {icon}
        <span>{title}</span>
        <span className="text-muted-foreground">({tasks.length})</span>
      </div>
      <div className="space-y-3">
        {tasks.map((task) => (
          <TodayTaskCard
            key={task.id}
            task={task}
            onComplete={(data) => onComplete(task.id, data)}
            onUndo={() => onUndo(task.id)}
            onReschedule={(newDate, reason) => onReschedule(task.id, newDate, reason)}
          />
        ))}
      </div>
    </div>
  )
}
