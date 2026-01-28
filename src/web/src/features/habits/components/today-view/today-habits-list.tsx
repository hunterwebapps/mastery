import { useMemo } from 'react'
import { CheckCircle2, Circle, Loader2 } from 'lucide-react'
import { cn } from '@/lib/utils'
import { Progress } from '@/components/ui/progress'
import type { TodayHabitDto, HabitMode } from '@/types/habit'
import { TodayHabitCard } from './today-habit-card'

interface TodayHabitsListProps {
  habits: TodayHabitDto[]
  onComplete: (habitId: string, data: { mode?: HabitMode; value?: number; note?: string }) => Promise<void>
  onUndo: (habitId: string) => Promise<void>
  onSkip: (habitId: string, reason?: string) => Promise<void>
  isLoading?: boolean
}

export function TodayHabitsList({
  habits,
  onComplete,
  onUndo,
  onSkip,
  isLoading = false,
}: TodayHabitsListProps) {
  // Calculate progress stats
  const stats = useMemo(() => {
    const due = habits.filter(h => h.isDue)
    const completed = due.filter(h => h.todayOccurrence?.status === 'Completed')
    const skipped = due.filter(h => h.todayOccurrence?.status === 'Skipped')
    const pending = due.filter(h => !h.todayOccurrence || h.todayOccurrence.status === 'Pending')

    return {
      total: due.length,
      completed: completed.length,
      skipped: skipped.length,
      pending: pending.length,
      progressPercent: due.length > 0 ? (completed.length / due.length) * 100 : 0,
      isAllDone: pending.length === 0 && due.length > 0,
    }
  }, [habits])

  // Sort: pending first, then completed, then skipped
  const sortedHabits = useMemo(() => {
    return [...habits]
      .filter(h => h.isDue)
      .sort((a, b) => {
        const statusOrder = (h: TodayHabitDto) => {
          if (!h.todayOccurrence || h.todayOccurrence.status === 'Pending') return 0
          if (h.todayOccurrence.status === 'Completed') return 1
          if (h.todayOccurrence.status === 'Skipped') return 2
          return 3
        }
        const orderDiff = statusOrder(a) - statusOrder(b)
        if (orderDiff !== 0) return orderDiff
        // Then by display order
        return a.displayOrder - b.displayOrder
      })
  }, [habits])

  if (isLoading) {
    return (
      <div className="flex items-center justify-center py-12">
        <Loader2 className="size-8 animate-spin text-muted-foreground" />
      </div>
    )
  }

  if (habits.length === 0 || sortedHabits.length === 0) {
    return (
      <div className="text-center py-12">
        <Circle className="size-12 mx-auto mb-4 text-muted-foreground/50" />
        <h3 className="text-lg font-medium text-muted-foreground">No habits due today</h3>
        <p className="text-sm text-muted-foreground mt-1">
          Enjoy your free day or create a new habit to track.
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

          {stats.skipped > 0 && (
            <span className="text-sm text-muted-foreground">
              {stats.skipped} skipped
            </span>
          )}
        </div>

        <Progress
          value={stats.progressPercent}
          className={cn(
            'h-2 transition-all',
            stats.isAllDone && '[&>div]:bg-green-500'
          )}
        />
      </div>

      {/* Habits list */}
      <div className="space-y-3">
        {sortedHabits.map((habit) => (
          <TodayHabitCard
            key={habit.id}
            habit={habit}
            onComplete={(data) => onComplete(habit.id, data)}
            onUndo={() => onUndo(habit.id)}
            onSkip={(reason) => onSkip(habit.id, reason)}
          />
        ))}
      </div>

      {/* Motivational message when all done */}
      {stats.isAllDone && (
        <div className="text-center py-4 animate-in fade-in slide-in-from-bottom-2 duration-500">
          <p className="text-lg font-medium text-green-500">
            Great work today!
          </p>
          <p className="text-sm text-muted-foreground mt-1">
            You've completed all {stats.total} habit{stats.total > 1 ? 's' : ''} for today.
          </p>
        </div>
      )}
    </div>
  )
}
