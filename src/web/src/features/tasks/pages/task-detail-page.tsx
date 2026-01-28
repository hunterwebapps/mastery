import { useParams, Link, useNavigate } from 'react-router-dom'
import { ArrowLeft, Edit, Trash2, Loader2, Clock, Calendar, Target, Folder } from 'lucide-react'
import { format } from 'date-fns'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import { Separator } from '@/components/ui/separator'
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
  AlertDialogTrigger,
} from '@/components/ui/alert-dialog'
import { cn } from '@/lib/utils'
import { useTask, useDeleteTask } from '../hooks/use-tasks'
import { EnergyIndicator, ContextTags, PriorityBadge } from '../components/common'
import { taskStatusInfo, dueTypeInfo } from '@/types/task'

export function Component() {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const { data: task, isLoading, error } = useTask(id ?? '')
  const deleteTask = useDeleteTask()

  const handleDelete = async () => {
    if (!id) return
    await deleteTask.mutateAsync(id)
    navigate('/tasks')
  }

  if (isLoading) {
    return (
      <div className="min-h-screen bg-background flex items-center justify-center">
        <Loader2 className="size-8 animate-spin text-muted-foreground" />
      </div>
    )
  }

  if (error || !task) {
    return (
      <div className="min-h-screen bg-background flex items-center justify-center">
        <div className="text-center">
          <h2 className="text-xl font-semibold text-destructive">Task not found</h2>
          <p className="text-muted-foreground mt-2">
            The task you're looking for doesn't exist or has been deleted.
          </p>
          <Button asChild className="mt-4">
            <Link to="/tasks">Back to Tasks</Link>
          </Button>
        </div>
      </div>
    )
  }

  const statusInfo = taskStatusInfo[task.status]

  return (
    <div className="min-h-screen bg-background">
      <div className="container max-w-3xl py-8 px-4 sm:px-6 lg:px-8">
        {/* Header */}
        <div className="flex items-start justify-between mb-8">
          <div className="flex items-start gap-4">
            <Button variant="ghost" size="icon" asChild>
              <Link to="/tasks">
                <ArrowLeft className="size-5" />
              </Link>
            </Button>
            <div>
              <div className="flex items-center gap-3 mb-2">
                <Badge className={cn('text-xs', statusInfo.bgColor, statusInfo.color)}>
                  {statusInfo.label}
                </Badge>
                <PriorityBadge priority={task.priority} showLabel />
              </div>
              <h1 className="text-2xl font-bold">{task.title}</h1>
              {task.description && (
                <p className="text-muted-foreground mt-2">{task.description}</p>
              )}
            </div>
          </div>

          <div className="flex gap-2">
            <Button variant="outline" size="icon" asChild>
              <Link to={`/tasks/${task.id}/edit`}>
                <Edit className="size-4" />
              </Link>
            </Button>
            <AlertDialog>
              <AlertDialogTrigger asChild>
                <Button variant="outline" size="icon" className="text-destructive">
                  <Trash2 className="size-4" />
                </Button>
              </AlertDialogTrigger>
              <AlertDialogContent>
                <AlertDialogHeader>
                  <AlertDialogTitle>Delete task?</AlertDialogTitle>
                  <AlertDialogDescription>
                    This will archive the task. You can restore it later from the Archived filter.
                  </AlertDialogDescription>
                </AlertDialogHeader>
                <AlertDialogFooter>
                  <AlertDialogCancel>Cancel</AlertDialogCancel>
                  <AlertDialogAction onClick={handleDelete}>Delete</AlertDialogAction>
                </AlertDialogFooter>
              </AlertDialogContent>
            </AlertDialog>
          </div>
        </div>

        <div className="grid gap-6 md:grid-cols-2">
          {/* Details card */}
          <Card>
            <CardHeader>
              <CardTitle className="text-sm font-medium text-muted-foreground">Details</CardTitle>
            </CardHeader>
            <CardContent className="space-y-4">
              <div className="flex items-center gap-3">
                <Clock className="size-4 text-muted-foreground" />
                <span className="text-sm">
                  Estimated: {task.estimatedMinutes} minutes
                </span>
              </div>

              <div className="flex items-center gap-3">
                <span className="text-sm text-muted-foreground">Energy:</span>
                <EnergyIndicator energyCost={task.energyCost} showLabel />
              </div>

              {task.due && (
                <div className="flex items-center gap-3">
                  <Calendar className="size-4 text-muted-foreground" />
                  <span className="text-sm">
                    Due: {task.due.dueOn}
                    {task.due.dueType && (
                      <Badge variant="outline" className={cn('ml-2 text-xs', dueTypeInfo[task.due.dueType].color)}>
                        {dueTypeInfo[task.due.dueType].label}
                      </Badge>
                    )}
                  </span>
                </div>
              )}

              {task.scheduling && (
                <div className="flex items-center gap-3">
                  <Calendar className="size-4 text-muted-foreground" />
                  <span className="text-sm">
                    Scheduled: {task.scheduling.scheduledOn}
                  </span>
                </div>
              )}

              {task.contextTags.length > 0 && (
                <>
                  <Separator />
                  <div>
                    <span className="text-sm text-muted-foreground block mb-2">Context</span>
                    <ContextTags tags={task.contextTags} showEmoji />
                  </div>
                </>
              )}
            </CardContent>
          </Card>

          {/* Relationships card */}
          <Card>
            <CardHeader>
              <CardTitle className="text-sm font-medium text-muted-foreground">Relationships</CardTitle>
            </CardHeader>
            <CardContent className="space-y-4">
              {task.projectId ? (
                <Link
                  to={`/projects/${task.projectId}`}
                  className="flex items-center gap-3 p-2 rounded-md hover:bg-muted transition-colors"
                >
                  <Folder className="size-4 text-muted-foreground" />
                  <span className="text-sm">{task.projectTitle ?? 'View Project'}</span>
                </Link>
              ) : (
                <div className="text-sm text-muted-foreground">No project linked</div>
              )}

              {task.goalId ? (
                <Link
                  to={`/goals/${task.goalId}`}
                  className="flex items-center gap-3 p-2 rounded-md hover:bg-muted transition-colors"
                >
                  <Target className="size-4 text-muted-foreground" />
                  <span className="text-sm">{task.goalTitle ?? 'View Goal'}</span>
                </Link>
              ) : (
                <div className="text-sm text-muted-foreground">No goal linked</div>
              )}

              {task.dependencyTaskIds.length > 0 && (
                <>
                  <Separator />
                  <div>
                    <span className="text-sm text-muted-foreground block mb-2">
                      Blocked by {task.dependencyTaskIds.length} task(s)
                    </span>
                  </div>
                </>
              )}
            </CardContent>
          </Card>

          {/* Completion info (if completed) */}
          {task.completion && (
            <Card className="md:col-span-2">
              <CardHeader>
                <CardTitle className="text-sm font-medium text-muted-foreground">Completion</CardTitle>
              </CardHeader>
              <CardContent className="space-y-2">
                <div className="text-sm">
                  Completed on: {task.completion.completedOn}
                </div>
                {task.completion.actualMinutes && (
                  <div className="text-sm">
                    Actual time: {task.completion.actualMinutes} minutes
                  </div>
                )}
                {task.completion.completionNote && (
                  <div className="text-sm">
                    Note: {task.completion.completionNote}
                  </div>
                )}
              </CardContent>
            </Card>
          )}

          {/* Reschedule info */}
          {task.rescheduleCount > 0 && (
            <Card className="md:col-span-2 border-amber-500/50">
              <CardContent className="pt-4">
                <div className="flex items-center gap-2 text-amber-500">
                  <span className="text-sm">
                    This task has been rescheduled {task.rescheduleCount} time(s)
                  </span>
                  {task.lastRescheduleReason && (
                    <Badge variant="outline" className="text-amber-500">
                      {task.lastRescheduleReason}
                    </Badge>
                  )}
                </div>
              </CardContent>
            </Card>
          )}
        </div>

        {/* Meta */}
        <div className="mt-8 text-xs text-muted-foreground">
          <p>Created: {format(new Date(task.createdAt), 'PPP')}</p>
          {task.modifiedAt && (
            <p>Modified: {format(new Date(task.modifiedAt), 'PPP')}</p>
          )}
        </div>
      </div>
    </div>
  )
}
