import { useState, useCallback } from 'react'
import { Link } from 'react-router-dom'
import { ArrowLeft, Inbox, Check, Archive, Loader2 } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Card } from '@/components/ui/card'
import { Input } from '@/components/ui/input'
import { cn } from '@/lib/utils'
import {
  useInboxTasks,
  useCreateTask,
  useMoveTaskToReady,
  useDeleteTask,
} from '../hooks/use-tasks'
import { EnergyIndicator, ContextTags } from '../components/common'
import type { CreateTaskRequest } from '@/types/task'

export function Component() {
  const [newTaskTitle, setNewTaskTitle] = useState('')
  const [isAdding, setIsAdding] = useState(false)

  const { data: inboxTasks, isLoading } = useInboxTasks()
  const createTask = useCreateTask()
  const moveToReady = useMoveTaskToReady()
  const deleteTask = useDeleteTask()

  const handleQuickAdd = useCallback(async (e: React.FormEvent) => {
    e.preventDefault()
    if (!newTaskTitle.trim()) return

    setIsAdding(true)
    try {
      const request: CreateTaskRequest = {
        title: newTaskTitle.trim(),
      }
      await createTask.mutateAsync(request)
      setNewTaskTitle('')
    } finally {
      setIsAdding(false)
    }
  }, [newTaskTitle, createTask])

  const handleMoveToReady = useCallback(async (taskId: string) => {
    await moveToReady.mutateAsync(taskId)
  }, [moveToReady])

  const handleArchive = useCallback(async (taskId: string) => {
    await deleteTask.mutateAsync(taskId)
  }, [deleteTask])

  return (
    <div className="min-h-screen bg-background">
      <div className="container max-w-2xl py-8 px-4 sm:px-6 lg:px-8">
        {/* Header */}
        <div className="flex items-center gap-4 mb-8">
          <Button variant="ghost" size="icon" asChild>
            <Link to="/tasks">
              <ArrowLeft className="size-5" />
            </Link>
          </Button>
          <div className="flex items-center gap-3">
            <div className="p-2 rounded-lg bg-primary/10">
              <Inbox className="size-6 text-primary" />
            </div>
            <div>
              <h1 className="text-2xl font-bold text-foreground">Inbox</h1>
              <p className="text-sm text-muted-foreground">
                Capture tasks quickly, triage later
              </p>
            </div>
          </div>
        </div>

        {/* Quick add form */}
        <form onSubmit={handleQuickAdd} className="mb-8">
          <div className="relative">
            <Input
              type="text"
              placeholder="What needs to be done?"
              value={newTaskTitle}
              onChange={(e) => setNewTaskTitle(e.target.value)}
              disabled={isAdding}
              className="h-14 text-lg pl-4 pr-12"
              autoFocus
            />
            <Button
              type="submit"
              size="icon"
              disabled={!newTaskTitle.trim() || isAdding}
              className="absolute right-2 top-1/2 -translate-y-1/2"
            >
              {isAdding ? (
                <Loader2 className="size-4 animate-spin" />
              ) : (
                <Check className="size-4" />
              )}
            </Button>
          </div>
          <p className="text-xs text-muted-foreground mt-2">
            Press Enter to add • Task will be created in Inbox status
          </p>
        </form>

        {/* Inbox tasks list */}
        <div className="space-y-3">
          {isLoading ? (
            <div className="flex items-center justify-center py-12">
              <Loader2 className="size-8 animate-spin text-muted-foreground" />
            </div>
          ) : inboxTasks && inboxTasks.length > 0 ? (
            <>
              <div className="text-sm text-muted-foreground mb-4">
                {inboxTasks.length} task{inboxTasks.length !== 1 ? 's' : ''} to triage
              </div>
              {inboxTasks.map((task) => (
                <InboxTaskCard
                  key={task.id}
                  task={task}
                  onMoveToReady={() => handleMoveToReady(task.id)}
                  onArchive={() => handleArchive(task.id)}
                />
              ))}
            </>
          ) : (
            <div className="text-center py-12">
              <Inbox className="size-12 mx-auto mb-4 text-muted-foreground/50" />
              <h3 className="text-lg font-medium text-muted-foreground">Inbox zero!</h3>
              <p className="text-sm text-muted-foreground mt-1">
                All tasks have been triaged. Great job!
              </p>
            </div>
          )}
        </div>
      </div>
    </div>
  )
}

interface InboxTaskCardProps {
  task: {
    id: string
    title: string
    description?: string
    estimatedMinutes: number
    energyCost: number
    contextTags: string[]
    createdAt: string
  }
  onMoveToReady: () => void
  onArchive: () => void
}

function InboxTaskCard({ task, onMoveToReady, onArchive }: InboxTaskCardProps) {
  const [isMoving, setIsMoving] = useState(false)
  const [isArchiving, setIsArchiving] = useState(false)

  const handleMoveToReady = async () => {
    setIsMoving(true)
    try {
      await onMoveToReady()
    } finally {
      setIsMoving(false)
    }
  }

  const handleArchive = async () => {
    setIsArchiving(true)
    try {
      await onArchive()
    } finally {
      setIsArchiving(false)
    }
  }

  return (
    <Card className={cn(
      'p-4 transition-all hover:shadow-md',
      (isMoving || isArchiving) && 'opacity-50'
    )}>
      <div className="flex items-start gap-4">
        <div className="flex-1 min-w-0">
          <Link to={`/tasks/${task.id}/edit`} className="group">
            <h3 className="font-medium group-hover:text-primary transition-colors">
              {task.title}
            </h3>
          </Link>
          {task.description && (
            <p className="text-sm text-muted-foreground truncate mt-0.5">
              {task.description}
            </p>
          )}
          <div className="flex items-center gap-3 mt-2">
            <span className="text-xs text-muted-foreground">
              {task.estimatedMinutes}m
            </span>
            <EnergyIndicator energyCost={task.energyCost} />
            <ContextTags tags={task.contextTags as any} max={2} />
          </div>
        </div>

        <div className="flex gap-2">
          <Button
            variant="outline"
            size="sm"
            onClick={handleArchive}
            disabled={isMoving || isArchiving}
          >
            {isArchiving ? (
              <Loader2 className="size-4 animate-spin" />
            ) : (
              <Archive className="size-4" />
            )}
          </Button>
          <Button
            size="sm"
            onClick={handleMoveToReady}
            disabled={isMoving || isArchiving}
          >
            {isMoving ? (
              <Loader2 className="size-4 animate-spin mr-2" />
            ) : (
              <Check className="size-4 mr-2" />
            )}
            Ready
          </Button>
        </div>
      </div>
    </Card>
  )
}
