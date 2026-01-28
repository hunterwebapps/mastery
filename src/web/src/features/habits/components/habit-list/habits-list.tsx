import { Link } from 'react-router-dom'
import { Loader2, Sparkles, Plus } from 'lucide-react'
import { Button } from '@/components/ui/button'
import type { HabitSummaryDto } from '@/types/habit'
import { HabitCard } from './habit-card'
import { useUpdateHabitStatus } from '../../hooks/use-habits'

interface HabitsListProps {
  habits: HabitSummaryDto[]
  isLoading?: boolean
}

export function HabitsList({ habits, isLoading }: HabitsListProps) {
  const updateStatus = useUpdateHabitStatus()

  const handleStatusChange = (id: string, status: string) => {
    updateStatus.mutate({ id, status })
  }

  if (isLoading) {
    return (
      <div className="flex items-center justify-center py-12">
        <Loader2 className="size-8 animate-spin text-muted-foreground" />
      </div>
    )
  }

  if (habits.length === 0) {
    return (
      <div className="text-center py-12">
        <Sparkles className="size-12 mx-auto mb-4 text-muted-foreground/50" />
        <h3 className="text-lg font-medium text-muted-foreground">No habits yet</h3>
        <p className="text-sm text-muted-foreground mt-1 mb-4">
          Start building positive routines by creating your first habit.
        </p>
        <Button asChild>
          <Link to="/habits/new">
            <Plus className="size-4 mr-2" />
            Create Habit
          </Link>
        </Button>
      </div>
    )
  }

  return (
    <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
      {habits.map((habit) => (
        <HabitCard
          key={habit.id}
          habit={habit}
          onStatusChange={handleStatusChange}
        />
      ))}
    </div>
  )
}
