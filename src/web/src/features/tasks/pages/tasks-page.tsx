import { useState, useCallback } from 'react'
import { Link } from 'react-router-dom'
import { Plus, ListTodo, Calendar, Inbox } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Tabs, TabsList, TabsTrigger, TabsContent } from '@/components/ui/tabs'
import {
  useTodayTasks,
  useTasks,
  useCompleteTask,
  useUndoTaskCompletion,
  useRescheduleTask,
} from '../hooks/use-tasks'
import { TodayTasksList } from '../components/today-view'
import { TasksList } from '../components/task-list'
import type { TaskStatus, RescheduleReason } from '@/types/task'

type ViewTab = 'today' | 'all'
type StatusFilter = TaskStatus | 'all'

export function Component() {
  const [viewTab, setViewTab] = useState<ViewTab>('today')
  const [statusFilter, setStatusFilter] = useState<StatusFilter>('Ready')

  // Today view data
  const {
    data: todayTasks,
    isLoading: isTodayLoading,
  } = useTodayTasks()

  // All tasks data
  const {
    data: allTasks,
    isLoading: isAllLoading,
  } = useTasks(statusFilter === 'all' ? undefined : { status: statusFilter })

  // Mutations
  const completeTask = useCompleteTask()
  const undoTaskCompletion = useUndoTaskCompletion()
  const rescheduleTask = useRescheduleTask()

  // Handlers
  const handleComplete = useCallback(
    async (taskId: string, data: {
      completedOn: string
      actualMinutes?: number
      note?: string
      enteredValue?: number
    }) => {
      await completeTask.mutateAsync({
        id: taskId,
        request: data,
      })
    },
    [completeTask]
  )

  const handleUndo = useCallback(
    async (taskId: string) => {
      await undoTaskCompletion.mutateAsync(taskId)
    },
    [undoTaskCompletion]
  )

  const handleReschedule = useCallback(
    async (taskId: string, newDate: string, reason?: RescheduleReason) => {
      await rescheduleTask.mutateAsync({
        id: taskId,
        request: { newDate, reason },
      })
    },
    [rescheduleTask]
  )

  // Calculate today stats for header
  const todayStats = todayTasks
    ? {
        total: todayTasks.filter(t => t.status !== 'Cancelled' && t.status !== 'Archived').length,
        completed: todayTasks.filter(t => t.status === 'Completed').length,
        overdue: todayTasks.filter(t => t.isOverdue && t.status !== 'Completed').length,
      }
    : { total: 0, completed: 0, overdue: 0 }

  return (
    <div className="min-h-screen bg-background">
      <div className="container max-w-4xl py-8 px-4 sm:px-6 lg:px-8">
        {/* Header */}
        <div className="flex items-center justify-between mb-8">
          <div className="flex items-center gap-3">
            <div className="p-2 rounded-lg bg-primary/10">
              <ListTodo className="size-6 text-primary" />
            </div>
            <div>
              <h1 className="text-2xl font-bold text-foreground">Tasks</h1>
              <p className="text-sm text-muted-foreground">
                {viewTab === 'today' ? (
                  todayStats.total > 0 ? (
                    <>
                      <span className="text-foreground font-medium">{todayStats.completed}</span>
                      <span> of </span>
                      <span className="text-foreground font-medium">{todayStats.total}</span>
                      <span> complete today</span>
                      {todayStats.overdue > 0 && (
                        <span className="text-red-500 ml-2">
                          ({todayStats.overdue} overdue)
                        </span>
                      )}
                    </>
                  ) : (
                    'No tasks scheduled for today'
                  )
                ) : (
                  'Turn intentions into action'
                )}
              </p>
            </div>
          </div>
          <div className="flex gap-2">
            <Button variant="outline" asChild>
              <Link to="/tasks/inbox">
                <Inbox className="size-4 mr-2" />
                Inbox
              </Link>
            </Button>
            <Button asChild>
              <Link to="/tasks/new">
                <Plus className="size-4 mr-2" />
                New Task
              </Link>
            </Button>
          </div>
        </div>

        {/* View tabs */}
        <Tabs value={viewTab} onValueChange={(v) => setViewTab(v as ViewTab)}>
          <div className="flex items-center justify-between mb-6">
            <TabsList>
              <TabsTrigger value="today" className="gap-2">
                <Calendar className="size-4" />
                Today
              </TabsTrigger>
              <TabsTrigger value="all" className="gap-2">
                <ListTodo className="size-4" />
                All Tasks
              </TabsTrigger>
            </TabsList>

            {/* Status filter for All view */}
            {viewTab === 'all' && (
              <Tabs
                value={statusFilter}
                onValueChange={(v) => setStatusFilter(v as StatusFilter)}
              >
                <TabsList>
                  <TabsTrigger value="Ready">Ready</TabsTrigger>
                  <TabsTrigger value="Scheduled">Scheduled</TabsTrigger>
                  <TabsTrigger value="Completed">Completed</TabsTrigger>
                  <TabsTrigger value="all">All</TabsTrigger>
                </TabsList>
              </Tabs>
            )}
          </div>

          {/* Today view */}
          <TabsContent value="today" className="mt-0">
            <TodayTasksList
              tasks={todayTasks ?? []}
              onComplete={handleComplete}
              onUndo={handleUndo}
              onReschedule={handleReschedule}
              isLoading={isTodayLoading}
            />
          </TabsContent>

          {/* All tasks view */}
          <TabsContent value="all" className="mt-0">
            <TasksList
              tasks={allTasks ?? []}
              isLoading={isAllLoading}
              emptyMessage={
                statusFilter === 'all'
                  ? 'No tasks yet. Create your first task to get started.'
                  : `No ${statusFilter.toLowerCase()} tasks found.`
              }
            />
          </TabsContent>
        </Tabs>
      </div>
    </div>
  )
}
