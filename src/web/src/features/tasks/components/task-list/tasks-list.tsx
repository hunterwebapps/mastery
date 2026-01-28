import { Loader2, ListTodo } from 'lucide-react'
import type { TaskSummaryDto } from '@/types/task'
import { TaskCard } from './task-card'

interface TasksListProps {
  tasks: TaskSummaryDto[]
  isLoading?: boolean
  emptyMessage?: string
}

export function TasksList({ tasks, isLoading, emptyMessage = 'No tasks found' }: TasksListProps) {
  if (isLoading) {
    return (
      <div className="flex items-center justify-center py-12">
        <Loader2 className="size-8 animate-spin text-muted-foreground" />
      </div>
    )
  }

  if (tasks.length === 0) {
    return (
      <div className="flex flex-col items-center justify-center py-12 text-center">
        <div className="p-4 rounded-full bg-muted/50 mb-4">
          <ListTodo className="size-8 text-muted-foreground" />
        </div>
        <p className="text-muted-foreground">{emptyMessage}</p>
      </div>
    )
  }

  return (
    <div className="space-y-2">
      {tasks.map((task) => (
        <TaskCard key={task.id} task={task} />
      ))}
    </div>
  )
}
