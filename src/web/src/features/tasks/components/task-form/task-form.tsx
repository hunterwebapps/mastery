import { useState, useEffect } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { useNavigate } from 'react-router-dom'
import { ArrowLeft, Loader2, Save, Inbox, FolderKanban, Target } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Textarea } from '@/components/ui/textarea'
import { Label } from '@/components/ui/label'
import { Slider } from '@/components/ui/slider'
import { Badge } from '@/components/ui/badge'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import { AiSuggestionBanner } from '@/components/ai-suggestion-banner'
import { cn } from '@/lib/utils'
import { createTaskSchema, type CreateTaskFormData } from '../../schemas/task-schema'
import { useCreateTask, useUpdateTask, useMoveTaskToReady } from '../../hooks/use-tasks'
import { useProjects } from '@/features/projects/hooks/use-projects'
import { useGoals } from '@/features/goals/hooks/use-goals'
import type { TaskDto, ContextTag } from '@/types/task'
import { contextTagInfo, energyCostInfo, priorityInfo } from '@/types/task'

interface TaskFormProps {
  mode: 'create' | 'edit'
  initialData?: TaskDto | Partial<CreateTaskFormData>
  defaultProjectId?: string
  /** When provided, called with the new task ID on successful creation instead of navigating. */
  onCreated?: (taskId: string) => void
  /** When true, hides the header back button and cancel navigates via callback instead. */
  embedded?: boolean
  /** Called when cancel is clicked in embedded mode. */
  onCancel?: () => void
  /** Show the AI suggestion banner when form is pre-filled from recommendation. */
  showAiBanner?: boolean
  /** Called after successful save (for clearing recommendation payload). */
  onSuccess?: () => void
}

const contextTags: ContextTag[] = [
  'Computer',
  'Phone',
  'Errands',
  'Home',
  'Office',
  'DeepWork',
  'LowEnergy',
  'Anywhere',
]

export function TaskForm({
  mode,
  initialData,
  defaultProjectId,
  onCreated,
  embedded,
  onCancel,
  showAiBanner,
  onSuccess,
}: TaskFormProps) {
  const navigate = useNavigate()
  const createTask = useCreateTask()
  const updateTask = useUpdateTask()
  const moveToReady = useMoveTaskToReady()
  const { data: projects } = useProjects({ status: 'Active' })
  const { data: goals } = useGoals('Active')
  const [startAsReady, setStartAsReady] = useState(false)

  const isLoading = createTask.isPending || updateTask.isPending || moveToReady.isPending
  // Check if initialData is a full TaskDto (has id) vs partial form data
  const taskDto = initialData && 'id' in initialData ? initialData as TaskDto : undefined
  const isInInbox = taskDto?.status === 'Inbox'

  const {
    register,
    handleSubmit,
    watch,
    setValue,
    reset,
    formState: { errors },
  } = useForm<CreateTaskFormData>({
    resolver: zodResolver(createTaskSchema),
    defaultValues: {
      title: initialData?.title ?? '',
      description: initialData?.description ?? '',
      estimatedMinutes: initialData?.estimatedMinutes ?? 30,
      energyCost: initialData?.energyCost ?? 3,
      priority: initialData?.priority ?? 3,
      projectId: initialData?.projectId ?? defaultProjectId ?? undefined,
      goalId: initialData?.goalId ?? undefined,
      contextTags: initialData?.contextTags ?? [],
    },
  })

  // Reset form when initialData changes (e.g., when recommendation payload is loaded)
  useEffect(() => {
    if (initialData) {
      reset({
        title: initialData.title ?? '',
        description: initialData.description ?? '',
        estimatedMinutes: initialData.estimatedMinutes ?? 30,
        energyCost: initialData.energyCost ?? 3,
        priority: initialData.priority ?? 3,
        projectId: initialData.projectId ?? defaultProjectId ?? undefined,
        goalId: initialData.goalId ?? undefined,
        contextTags: initialData.contextTags ?? [],
      })
    }
  }, [initialData, defaultProjectId, reset])

  const selectedTags = watch('contextTags') ?? []
  const energyCost = watch('energyCost') ?? 3
  const priority = watch('priority') ?? 3

  const toggleTag = (tag: ContextTag) => {
    const current = selectedTags
    if (current.includes(tag)) {
      setValue('contextTags', current.filter((t) => t !== tag))
    } else {
      setValue('contextTags', [...current, tag])
    }
  }

  const handleSave = async (data: CreateTaskFormData, asReady: boolean) => {
    try {
      if (mode === 'create') {
        const taskId = await createTask.mutateAsync({ ...data, startAsReady: asReady })
        onSuccess?.()
        if (onCreated) {
          onCreated(taskId)
        } else {
          navigate('/tasks')
        }
        return
      } else if (taskDto) {
        await updateTask.mutateAsync({
          id: taskDto.id,
          request: data,
        })
        // If task was in Inbox and user chose to move to Ready, do that now
        if (isInInbox && asReady) {
          await moveToReady.mutateAsync(taskDto.id)
        }
        onSuccess?.()
        navigate(`/tasks/${taskDto.id}`)
      }
    } catch (error) {
      console.error('Failed to save task:', error)
    }
  }

  const handleSaveToInbox = () => {
    setStartAsReady(false)
    handleSubmit(
      (data) => handleSave(data, false),
      (formErrors) => {
        console.error('Form validation errors:', formErrors)
      }
    )()
  }

  const handleSaveAsReady = () => {
    setStartAsReady(true)
    handleSubmit(
      (data) => handleSave(data, true),
      (formErrors) => {
        console.error('Form validation errors:', formErrors)
      }
    )()
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      {!embedded && (
        <div className="flex items-center gap-4">
          <Button variant="ghost" size="icon" onClick={() => navigate(-1)}>
            <ArrowLeft className="size-5" />
          </Button>
          <h1 className="text-2xl font-bold">
            {mode === 'create' ? 'New Task' : 'Edit Task'}
          </h1>
        </div>
      )}

      {/* AI Suggestion Banner */}
      {showAiBanner && <AiSuggestionBanner />}

      <form className="space-y-8">
        {/* Title */}
        <div className="space-y-2">
          <Label htmlFor="title">
            Title <span className="text-destructive">*</span>
          </Label>
          <Input
            id="title"
            placeholder="What needs to be done?"
            {...register('title')}
            className={cn(errors.title && 'border-destructive')}
          />
          {errors.title && (
            <p className="text-sm text-destructive">{errors.title.message}</p>
          )}
        </div>

        {/* Description */}
        <div className="space-y-2">
          <Label htmlFor="description">Description</Label>
          <Textarea
            id="description"
            placeholder="Add details, notes, or context..."
            rows={3}
            {...register('description')}
          />
        </div>

        {/* Project */}
        <div className="space-y-2">
          <Label>Project</Label>
          <Select
            value={watch('projectId') ?? 'none'}
            onValueChange={(value) => setValue('projectId', value === 'none' ? undefined : value)}
          >
            <SelectTrigger className="w-full">
              <SelectValue placeholder="Select a project (optional)">
                {watch('projectId') ? (
                  <span className="flex items-center gap-2">
                    <FolderKanban className="size-4" />
                    {projects?.find(p => p.id === watch('projectId'))?.title ?? 'Select a project'}
                  </span>
                ) : (
                  'No project'
                )}
              </SelectValue>
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="none">No project</SelectItem>
              {projects?.map((project) => (
                <SelectItem key={project.id} value={project.id}>
                  <span className="flex items-center gap-2">
                    <FolderKanban className="size-4" />
                    {project.title}
                  </span>
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
          <p className="text-sm text-muted-foreground">
            Associate this task with a project
          </p>
        </div>

        {/* Goal */}
        <div className="space-y-2">
          <Label>Goal</Label>
          <Select
            value={watch('goalId') ?? 'none'}
            onValueChange={(value) => setValue('goalId', value === 'none' ? undefined : value)}
          >
            <SelectTrigger className="w-full">
              <SelectValue placeholder="Select a goal (optional)">
                {watch('goalId') ? (
                  <span className="flex items-center gap-2">
                    <Target className="size-4" />
                    {goals?.find(g => g.id === watch('goalId'))?.title ?? 'Select a goal'}
                  </span>
                ) : (
                  'No goal'
                )}
              </SelectValue>
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="none">No goal</SelectItem>
              {goals?.map((goal) => (
                <SelectItem key={goal.id} value={goal.id}>
                  <span className="flex items-center gap-2">
                    <Target className="size-4" />
                    {goal.title}
                  </span>
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
          <p className="text-sm text-muted-foreground">
            Link this task to a goal
          </p>
        </div>

        {/* Time estimate */}
        <div className="space-y-2">
          <Label htmlFor="estimatedMinutes">Time Estimate (minutes)</Label>
          <Input
            id="estimatedMinutes"
            type="number"
            min={1}
            max={480}
            {...register('estimatedMinutes', { valueAsNumber: true })}
            className="w-32"
          />
        </div>

        {/* Energy cost */}
        <div className="space-y-4">
          <div className="flex items-center justify-between">
            <Label>Energy Cost</Label>
            <span className={cn('text-sm font-medium', energyCostInfo[energyCost]?.color)}>
              {energyCostInfo[energyCost]?.label}
            </span>
          </div>
          <Slider
            value={[energyCost]}
            onValueChange={([value]) => setValue('energyCost', value)}
            min={1}
            max={5}
            step={1}
          />
          <div className="flex justify-between text-xs text-muted-foreground">
            <span>Low</span>
            <span>High</span>
          </div>
        </div>

        {/* Priority */}
        <div className="space-y-4">
          <div className="flex items-center justify-between">
            <Label>Priority</Label>
            <span className={cn('text-sm font-medium', priorityInfo[priority]?.color)}>
              {priorityInfo[priority]?.label}
            </span>
          </div>
          <Slider
            value={[priority]}
            onValueChange={([value]) => setValue('priority', value)}
            min={1}
            max={5}
            step={1}
          />
          <div className="flex justify-between text-xs text-muted-foreground">
            <span>Highest</span>
            <span>Lowest</span>
          </div>
        </div>

        {/* Context tags */}
        <div className="space-y-2">
          <Label>Context Tags</Label>
          <p className="text-sm text-muted-foreground">
            Where or when can you work on this?
          </p>
          <div className="flex flex-wrap gap-2 mt-2">
            {contextTags.map((tag) => {
              const info = contextTagInfo[tag]
              const isSelected = selectedTags.includes(tag)
              return (
                <Badge
                  key={tag}
                  variant={isSelected ? 'default' : 'outline'}
                  className={cn(
                    'cursor-pointer transition-colors',
                    isSelected && 'bg-primary'
                  )}
                  onClick={() => toggleTag(tag)}
                >
                  <span className="mr-1">{info.emoji}</span>
                  {info.label}
                </Badge>
              )
            })}
          </div>
        </div>

        {/* Submit */}
        <div className="flex gap-3 pt-4">
          <Button
            type="button"
            variant="outline"
            onClick={embedded && onCancel ? onCancel : () => navigate(-1)}
            disabled={isLoading}
          >
            Cancel
          </Button>
          {mode === 'create' ? (
            <>
              <Button
                type="button"
                variant="outline"
                onClick={handleSaveToInbox}
                disabled={isLoading}
              >
                {isLoading && !startAsReady ? (
                  <>
                    <Loader2 className="size-4 mr-2 animate-spin" />
                    Saving...
                  </>
                ) : (
                  <>
                    <Inbox className="size-4 mr-2" />
                    Save to Inbox
                  </>
                )}
              </Button>
              <Button
                type="button"
                onClick={handleSaveAsReady}
                disabled={isLoading}
              >
                {isLoading && startAsReady ? (
                  <>
                    <Loader2 className="size-4 mr-2 animate-spin" />
                    Saving...
                  </>
                ) : (
                  <>
                    <Save className="size-4 mr-2" />
                    Save as Ready
                  </>
                )}
              </Button>
            </>
          ) : isInInbox ? (
            <>
              <Button
                type="button"
                variant="outline"
                onClick={handleSaveToInbox}
                disabled={isLoading}
              >
                {isLoading && !startAsReady ? (
                  <>
                    <Loader2 className="size-4 mr-2 animate-spin" />
                    Saving...
                  </>
                ) : (
                  <>
                    <Inbox className="size-4 mr-2" />
                    Save & Keep in Inbox
                  </>
                )}
              </Button>
              <Button
                type="button"
                onClick={handleSaveAsReady}
                disabled={isLoading}
              >
                {isLoading && startAsReady ? (
                  <>
                    <Loader2 className="size-4 mr-2 animate-spin" />
                    Saving...
                  </>
                ) : (
                  <>
                    <Save className="size-4 mr-2" />
                    Save & Move to Ready
                  </>
                )}
              </Button>
            </>
          ) : (
            <Button
              type="button"
              onClick={() => handleSubmit((data) => handleSave(data, false))()}
              disabled={isLoading}
            >
              {isLoading ? (
                <>
                  <Loader2 className="size-4 mr-2 animate-spin" />
                  Saving...
                </>
              ) : (
                <>
                  <Save className="size-4 mr-2" />
                  Save Changes
                </>
              )}
            </Button>
          )}
        </div>
      </form>
    </div>
  )
}
