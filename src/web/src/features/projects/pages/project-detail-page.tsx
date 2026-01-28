import { useState, useCallback } from 'react'
import { useParams, Link, useNavigate } from 'react-router-dom'
import {
  ArrowLeft,
  Edit,
  Trash2,
  Loader2,
  Target,
  CheckCircle2,
  Circle,
  Calendar,
  Plus,
  MoreHorizontal,
  Play,
} from 'lucide-react'
import { format } from 'date-fns'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import { Tooltip, TooltipContent, TooltipTrigger } from '@/components/ui/tooltip'
import { Progress } from '@/components/ui/progress'
import { Input } from '@/components/ui/input'
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
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
  DialogFooter,
} from '@/components/ui/dialog'
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu'
import { cn } from '@/lib/utils'
import {
  useProject,
  useDeleteProject,
  useCompleteProject,
  useChangeProjectStatus,
  useAddMilestone,
  useCompleteMilestone,
  useRemoveMilestone,
} from '../hooks/use-projects'
import { projectStatusInfo } from '@/types/project'
import type { MilestoneDto } from '@/types/project'
import { useTasksByProject, useCompleteTask } from '@/features/tasks/hooks/use-tasks'
import { taskStatusInfo } from '@/types/task'

export function Component() {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const { data: project, isLoading, error } = useProject(id ?? '')
  const { data: projectTasks } = useTasksByProject(id ?? '')
  const completeTask = useCompleteTask()
  const deleteProject = useDeleteProject()
  const completeProject = useCompleteProject()
  const changeProjectStatus = useChangeProjectStatus()
  const addMilestone = useAddMilestone()
  const completeMilestone = useCompleteMilestone()
  const removeMilestone = useRemoveMilestone()

  const [showAddMilestone, setShowAddMilestone] = useState(false)
  const [newMilestoneTitle, setNewMilestoneTitle] = useState('')
  const [newMilestoneDate, setNewMilestoneDate] = useState('')

  const handleDelete = async () => {
    if (!id) return
    await deleteProject.mutateAsync(id)
    navigate('/projects')
  }

  const handleComplete = async () => {
    if (!id) return
    await completeProject.mutateAsync({ id })
  }

  const handleActivate = async () => {
    if (!id) return
    await changeProjectStatus.mutateAsync({ id, request: { newStatus: 'Active' } })
  }

  const handleAddMilestone = useCallback(async () => {
    if (!id || !newMilestoneTitle.trim()) return
    await addMilestone.mutateAsync({
      projectId: id,
      request: {
        title: newMilestoneTitle.trim(),
        targetDate: newMilestoneDate || undefined,
      },
    })
    setNewMilestoneTitle('')
    setNewMilestoneDate('')
    setShowAddMilestone(false)
  }, [id, newMilestoneTitle, newMilestoneDate, addMilestone])

  const handleCompleteMilestone = useCallback(
    async (milestoneId: string) => {
      if (!id) return
      await completeMilestone.mutateAsync({ projectId: id, milestoneId })
    },
    [id, completeMilestone]
  )

  const handleRemoveMilestone = useCallback(
    async (milestoneId: string) => {
      if (!id) return
      await removeMilestone.mutateAsync({ projectId: id, milestoneId })
    },
    [id, removeMilestone]
  )

  const handleCompleteTask = useCallback(
    async (taskId: string, e: React.MouseEvent) => {
      e.preventDefault()
      e.stopPropagation()
      const today = new Date().toISOString().split('T')[0]
      await completeTask.mutateAsync({ id: taskId, request: { completedOn: today } })
    },
    [completeTask]
  )

  if (isLoading) {
    return (
      <div className="min-h-screen bg-background flex items-center justify-center">
        <Loader2 className="size-8 animate-spin text-muted-foreground" />
      </div>
    )
  }

  if (error || !project) {
    return (
      <div className="min-h-screen bg-background flex items-center justify-center">
        <div className="text-center">
          <h2 className="text-xl font-semibold text-destructive">Project not found</h2>
          <p className="text-muted-foreground mt-2">
            The project you're looking for doesn't exist or has been deleted.
          </p>
          <Button asChild className="mt-4">
            <Link to="/projects">Back to Projects</Link>
          </Button>
        </div>
      </div>
    )
  }

  const statusInfo = projectStatusInfo[project.status]
  const progress =
    project.totalTasks > 0 ? Math.round((project.completedTasks / project.totalTasks) * 100) : 0

  const canEdit = project.status !== 'Completed' && project.status !== 'Archived'

  return (
    <div className="min-h-screen bg-background">
      <div className="container max-w-3xl py-8 px-4 sm:px-6 lg:px-8">
        {/* Header */}
        <div className="flex items-start justify-between mb-8">
          <div className="flex items-start gap-4">
            <Button variant="ghost" size="icon" asChild>
              <Link to="/projects">
                <ArrowLeft className="size-5" />
              </Link>
            </Button>
            <div>
              <div className="flex items-center gap-3 mb-2">
                <Badge className={cn('text-xs', statusInfo.bgColor, statusInfo.color)}>
                  {statusInfo.label}
                </Badge>
                {project.isStuck && (
                  <Tooltip>
                    <TooltipTrigger asChild>
                      <Badge variant="outline" className="text-amber-500 border-amber-500 cursor-help">
                        Stuck
                      </Badge>
                    </TooltipTrigger>
                    <TooltipContent>
                      <p>No actionable tasks - add tasks or move them to Ready</p>
                    </TooltipContent>
                  </Tooltip>
                )}
              </div>
              <h1 className="text-2xl font-bold">{project.title}</h1>
              {project.description && (
                <p className="text-muted-foreground mt-2">{project.description}</p>
              )}
            </div>
          </div>

          <div className="flex gap-2">
            {project.status === 'Draft' && (
              <Button onClick={handleActivate} disabled={changeProjectStatus.isPending}>
                {changeProjectStatus.isPending ? (
                  <Loader2 className="size-4 mr-2 animate-spin" />
                ) : (
                  <Play className="size-4 mr-2" />
                )}
                Activate
              </Button>
            )}
            {project.status === 'Active' && (
              <Button variant="outline" onClick={handleComplete}>
                <CheckCircle2 className="size-4 mr-2" />
                Complete
              </Button>
            )}
            <Button variant="outline" size="icon" asChild>
              <Link to={`/projects/${project.id}/edit`}>
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
                  <AlertDialogTitle>Archive project?</AlertDialogTitle>
                  <AlertDialogDescription>
                    This will archive the project. You can restore it later from the Archived
                    filter.
                  </AlertDialogDescription>
                </AlertDialogHeader>
                <AlertDialogFooter>
                  <AlertDialogCancel>Cancel</AlertDialogCancel>
                  <AlertDialogAction onClick={handleDelete}>Archive</AlertDialogAction>
                </AlertDialogFooter>
              </AlertDialogContent>
            </AlertDialog>
          </div>
        </div>

        {/* Progress section */}
        <Card className="mb-6">
          <CardContent className="pt-6">
            <div className="flex items-center justify-between mb-4">
              <span className="text-sm text-muted-foreground">Overall Progress</span>
              <span className="text-2xl font-bold">{progress}%</span>
            </div>
            <Progress value={progress} className="h-2" />
            <div className="flex items-center justify-between mt-4 text-sm text-muted-foreground">
              <span>
                {project.completedTasks} of {project.totalTasks} tasks complete
              </span>
              {project.inProgressTasks > 0 && <span>{project.inProgressTasks} in progress</span>}
            </div>
          </CardContent>
        </Card>

        <div className="grid gap-6 md:grid-cols-2">
          {/* Details card */}
          <Card>
            <CardHeader>
              <CardTitle className="text-sm font-medium text-muted-foreground">Details</CardTitle>
            </CardHeader>
            <CardContent className="space-y-4">
              {project.targetEndDate && (
                <div className="flex items-center gap-3">
                  <Calendar className="size-4 text-muted-foreground" />
                  <span className="text-sm">
                    Target: {format(new Date(project.targetEndDate), 'PPP')}
                  </span>
                </div>
              )}

              {project.goalTitle && (
                <Link
                  to={`/goals/${project.goalId}`}
                  className="flex items-center gap-3 p-2 rounded-md hover:bg-muted transition-colors"
                >
                  <Target className="size-4 text-muted-foreground" />
                  <span className="text-sm">{project.goalTitle}</span>
                </Link>
              )}

              {project.nextTaskTitle && (
                <div className="p-3 bg-primary/5 rounded-lg border border-primary/20">
                  <div className="text-xs text-muted-foreground mb-1">Next Action</div>
                  <Link
                    to={`/tasks/${project.nextTaskId}`}
                    className="text-sm font-medium hover:text-primary"
                  >
                    {project.nextTaskTitle}
                  </Link>
                </div>
              )}
            </CardContent>
          </Card>

          {/* Milestones card */}
          <Card>
            <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
              <CardTitle className="text-sm font-medium text-muted-foreground">
                Milestones ({project.milestones.length})
              </CardTitle>
              {canEdit && (
                <Button variant="ghost" size="sm" onClick={() => setShowAddMilestone(true)}>
                  <Plus className="size-4 mr-1" />
                  Add
                </Button>
              )}
            </CardHeader>
            <CardContent>
              {project.milestones.length > 0 ? (
                <div className="space-y-2">
                  {project.milestones.map((milestone) => (
                    <MilestoneItem
                      key={milestone.id}
                      milestone={milestone}
                      canEdit={canEdit}
                      onComplete={() => handleCompleteMilestone(milestone.id)}
                      onRemove={() => handleRemoveMilestone(milestone.id)}
                    />
                  ))}
                </div>
              ) : (
                <p className="text-sm text-muted-foreground">
                  No milestones defined for this project.
                </p>
              )}
            </CardContent>
          </Card>
        </div>

        {/* Tasks section */}
        {(() => {
          const inboxCount = projectTasks?.filter(t => t.status === 'Inbox').length ?? 0
          return (
            <Card className="mt-6">
              <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
                <div className="flex items-center gap-2">
                  <CardTitle className="text-sm font-medium text-muted-foreground">
                    Tasks ({projectTasks?.length ?? 0})
                  </CardTitle>
                  {inboxCount > 0 && (
                    <Tooltip>
                      <TooltipTrigger asChild>
                        <Badge variant="secondary" className="text-xs bg-amber-500/10 text-amber-500 cursor-help">
                          {inboxCount} to triage
                        </Badge>
                      </TooltipTrigger>
                      <TooltipContent>
                        <p>{inboxCount} task{inboxCount > 1 ? 's' : ''} in Inbox need{inboxCount === 1 ? 's' : ''} to be triaged</p>
                      </TooltipContent>
                    </Tooltip>
                  )}
                </div>
                {canEdit && (
                  <Button variant="ghost" size="sm" asChild>
                    <Link to={`/tasks/new?projectId=${id}`}>
                      <Plus className="size-4 mr-1" />
                      Add Task
                    </Link>
                  </Button>
                )}
              </CardHeader>
              <CardContent>
                {projectTasks && projectTasks.length > 0 ? (
                  <div className="space-y-2">
                    {projectTasks.map((task) => {
                      const status = taskStatusInfo[task.status]
                      const canComplete = task.status !== 'Completed' && task.status !== 'Cancelled' && task.status !== 'Archived'
                      return (
                        <div
                          key={task.id}
                          className="flex items-center gap-3 p-2 rounded-md hover:bg-muted/50 transition-colors group"
                        >
                          {/* Completion button */}
                          <button
                            type="button"
                            onClick={(e) => canComplete && handleCompleteTask(task.id, e)}
                            disabled={!canComplete || completeTask.isPending}
                            className={cn(
                              'flex-shrink-0 transition-colors',
                              canComplete && 'hover:text-green-500 cursor-pointer',
                              !canComplete && 'cursor-default'
                            )}
                          >
                            {task.status === 'Completed' ? (
                              <CheckCircle2 className="size-5 text-green-500" />
                            ) : (
                              <Circle className="size-5 text-muted-foreground" />
                            )}
                          </button>

                          <Link
                            to={`/tasks/${task.id}`}
                            className="flex-1 flex items-center gap-3 min-w-0"
                          >
                            <span
                              className={cn(
                                'text-sm flex-1 truncate',
                                task.status === 'Completed' && 'line-through text-muted-foreground',
                                task.status === 'Cancelled' && 'line-through text-muted-foreground'
                              )}
                            >
                              {task.title}
                            </span>
                            <Badge className={cn('text-xs', status.bgColor, status.color)}>
                              {status.label}
                            </Badge>
                          </Link>
                        </div>
                      )
                    })}
                  </div>
                ) : (
              <p className="text-sm text-muted-foreground">
                  No tasks in this project yet.
                </p>
              )}
            </CardContent>
          </Card>
          )
        })()}

        {/* Outcome notes (if completed) */}
        {project.outcomeNotes && (
          <Card className="mt-6">
            <CardHeader>
              <CardTitle className="text-sm font-medium text-muted-foreground">
                Outcome Notes
              </CardTitle>
            </CardHeader>
            <CardContent>
              <p className="text-sm">{project.outcomeNotes}</p>
            </CardContent>
          </Card>
        )}

        {/* Meta */}
        <div className="mt-8 text-xs text-muted-foreground">
          <p>Created: {format(new Date(project.createdAt), 'PPP')}</p>
          {project.modifiedAt && <p>Modified: {format(new Date(project.modifiedAt), 'PPP')}</p>}
          {project.completedAtUtc && (
            <p>Completed: {format(new Date(project.completedAtUtc), 'PPP')}</p>
          )}
        </div>
      </div>

      {/* Add Milestone Dialog */}
      <Dialog open={showAddMilestone} onOpenChange={setShowAddMilestone}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Add Milestone</DialogTitle>
            <DialogDescription>Add a new milestone to track progress on this project.</DialogDescription>
          </DialogHeader>
          <div className="space-y-4 py-4">
            <div className="space-y-2">
              <label className="text-sm font-medium">Title</label>
              <Input
                placeholder="Milestone title"
                value={newMilestoneTitle}
                onChange={(e) => setNewMilestoneTitle(e.target.value)}
                autoFocus
              />
            </div>
            <div className="space-y-2">
              <label className="text-sm font-medium">
                Target Date <span className="text-muted-foreground">(optional)</span>
              </label>
              <Input
                type="date"
                value={newMilestoneDate}
                onChange={(e) => setNewMilestoneDate(e.target.value)}
              />
            </div>
          </div>
          <DialogFooter>
            <Button variant="outline" onClick={() => setShowAddMilestone(false)}>
              Cancel
            </Button>
            <Button
              onClick={handleAddMilestone}
              disabled={!newMilestoneTitle.trim() || addMilestone.isPending}
            >
              {addMilestone.isPending ? (
                <Loader2 className="size-4 mr-2 animate-spin" />
              ) : (
                <Plus className="size-4 mr-2" />
              )}
              Add Milestone
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  )
}

interface MilestoneItemProps {
  milestone: MilestoneDto
  canEdit: boolean
  onComplete: () => void
  onRemove: () => void
}

function MilestoneItem({ milestone, canEdit, onComplete, onRemove }: MilestoneItemProps) {
  const isCompleted = milestone.status === 'Completed'

  return (
    <div
      className={cn(
        'flex items-start gap-3 p-2 rounded-md group hover:bg-muted/50 transition-colors',
        isCompleted && 'opacity-60'
      )}
    >
      {/* Completion button */}
      <button
        type="button"
        onClick={onComplete}
        disabled={isCompleted || !canEdit}
        className={cn(
          'mt-0.5 flex-shrink-0 transition-colors',
          !isCompleted && canEdit && 'hover:text-green-500 cursor-pointer',
          isCompleted && 'cursor-default'
        )}
      >
        {isCompleted ? (
          <CheckCircle2 className="size-5 text-green-500" />
        ) : (
          <Circle className="size-5 text-muted-foreground" />
        )}
      </button>

      {/* Milestone content */}
      <div className="flex-1 min-w-0">
        <div className={cn('text-sm font-medium', isCompleted && 'line-through')}>
          {milestone.title}
        </div>
        {milestone.targetDate && (
          <div className="text-xs text-muted-foreground">
            {format(new Date(milestone.targetDate), 'MMM d, yyyy')}
          </div>
        )}
        {milestone.notes && (
          <div className="text-xs text-muted-foreground mt-1">{milestone.notes}</div>
        )}
      </div>

      {/* Actions menu */}
      {canEdit && !isCompleted && (
        <DropdownMenu>
          <DropdownMenuTrigger asChild>
            <Button
              variant="ghost"
              size="icon"
              className="size-8 opacity-0 group-hover:opacity-100 transition-opacity"
            >
              <MoreHorizontal className="size-4" />
            </Button>
          </DropdownMenuTrigger>
          <DropdownMenuContent align="end">
            <DropdownMenuItem onClick={onComplete}>
              <CheckCircle2 className="size-4 mr-2" />
              Mark Complete
            </DropdownMenuItem>
            <DropdownMenuItem onClick={onRemove} className="text-destructive">
              <Trash2 className="size-4 mr-2" />
              Remove
            </DropdownMenuItem>
          </DropdownMenuContent>
        </DropdownMenu>
      )}
    </div>
  )
}
