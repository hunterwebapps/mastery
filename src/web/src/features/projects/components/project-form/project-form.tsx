import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { useNavigate } from 'react-router-dom'
import { ArrowLeft, Loader2, Save, Plus, Trash2, FileText } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Textarea } from '@/components/ui/textarea'
import { Label } from '@/components/ui/label'
import { Slider } from '@/components/ui/slider'
import { Card } from '@/components/ui/card'
import { cn } from '@/lib/utils'
import { createProjectSchema, type CreateProjectFormData } from '../../schemas/project-schema'
import { useCreateProject } from '../../hooks/use-projects'
import type { ProjectDto } from '@/types/project'
import { priorityInfo } from '@/types/task'

interface ProjectFormProps {
  mode: 'create' | 'edit'
  initialData?: ProjectDto
}

export function ProjectForm({ mode, initialData }: ProjectFormProps) {
  const navigate = useNavigate()
  const createProject = useCreateProject()
  const [isSavingAsDraft, setIsSavingAsDraft] = useState(false)

  const isLoading = createProject.isPending

  const {
    register,
    handleSubmit,
    watch,
    setValue,
    formState: { errors },
  } = useForm<CreateProjectFormData>({
    resolver: zodResolver(createProjectSchema),
    defaultValues: {
      title: initialData?.title ?? '',
      description: initialData?.description ?? '',
      priority: initialData?.priority ?? 3,
      targetEndDate: initialData?.targetEndDate ?? '',
      milestones: initialData?.milestones?.map(m => ({
        title: m.title,
        targetDate: m.targetDate,
        notes: m.notes,
      })) ?? [],
    },
  })

  const priority = watch('priority') ?? 3
  const milestones = watch('milestones') ?? []

  const addMilestone = () => {
    setValue('milestones', [...milestones, { title: '' }])
  }

  const removeMilestone = (index: number) => {
    setValue('milestones', milestones.filter((_, i) => i !== index))
  }

  const handleSave = async (data: CreateProjectFormData, saveAsDraft: boolean) => {
    try {
      if (mode === 'create') {
        await createProject.mutateAsync({ ...data, saveAsDraft })
        navigate('/projects')
      }
      // TODO: Add update mutation for edit mode
    } catch (error) {
      console.error('Failed to save project:', error)
    }
  }

  const onSubmit = async (data: CreateProjectFormData) => {
    await handleSave(data, isSavingAsDraft)
  }

  const handleSaveAsDraft = async () => {
    setIsSavingAsDraft(true)
    handleSubmit(onSubmit)()
  }

  const handleCreate = async () => {
    setIsSavingAsDraft(false)
    handleSubmit(onSubmit)()
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center gap-4">
        <Button variant="ghost" size="icon" onClick={() => navigate(-1)}>
          <ArrowLeft className="size-5" />
        </Button>
        <h1 className="text-2xl font-bold">
          {mode === 'create' ? 'New Project' : 'Edit Project'}
        </h1>
      </div>

      <form onSubmit={handleSubmit(onSubmit)} className="space-y-8">
        {/* Title */}
        <div className="space-y-2">
          <Label htmlFor="title">
            Title <span className="text-destructive">*</span>
          </Label>
          <Input
            id="title"
            placeholder="What's this project about?"
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
            placeholder="Describe the project scope, outcomes, and success criteria..."
            rows={4}
            {...register('description')}
          />
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

        {/* Target end date */}
        <div className="space-y-2">
          <Label htmlFor="targetEndDate">Target End Date</Label>
          <Input
            id="targetEndDate"
            type="date"
            {...register('targetEndDate')}
            className="w-48"
          />
        </div>

        {/* Milestones */}
        <div className="space-y-4">
          <div className="flex items-center justify-between">
            <Label>Milestones</Label>
            <Button type="button" variant="outline" size="sm" onClick={addMilestone}>
              <Plus className="size-4 mr-1" />
              Add Milestone
            </Button>
          </div>

          {milestones.length > 0 ? (
            <div className="space-y-3">
              {milestones.map((_, index) => (
                <Card key={index} className="p-4">
                  <div className="flex items-start gap-3">
                    <div className="flex-1 space-y-3">
                      <Input
                        placeholder="Milestone title"
                        {...register(`milestones.${index}.title`)}
                      />
                      <div className="flex gap-3">
                        <Input
                          type="date"
                          placeholder="Target date"
                          {...register(`milestones.${index}.targetDate`)}
                          className="w-48"
                        />
                        <Input
                          placeholder="Notes (optional)"
                          {...register(`milestones.${index}.notes`)}
                        />
                      </div>
                    </div>
                    <Button
                      type="button"
                      variant="ghost"
                      size="icon"
                      onClick={() => removeMilestone(index)}
                      className="text-muted-foreground hover:text-destructive"
                    >
                      <Trash2 className="size-4" />
                    </Button>
                  </div>
                </Card>
              ))}
            </div>
          ) : (
            <p className="text-sm text-muted-foreground">
              No milestones yet. Add milestones to break down your project into phases.
            </p>
          )}
        </div>

        {/* Submit */}
        <div className="flex gap-3 pt-4">
          <Button
            type="button"
            variant="outline"
            onClick={() => navigate(-1)}
            disabled={isLoading}
          >
            Cancel
          </Button>
          {mode === 'create' && (
            <Button
              type="button"
              variant="outline"
              onClick={handleSaveAsDraft}
              disabled={isLoading}
            >
              {isLoading && isSavingAsDraft ? (
                <>
                  <Loader2 className="size-4 mr-2 animate-spin" />
                  Saving...
                </>
              ) : (
                <>
                  <FileText className="size-4 mr-2" />
                  Save as Draft
                </>
              )}
            </Button>
          )}
          <Button
            type="button"
            onClick={handleCreate}
            disabled={isLoading}
          >
            {isLoading && !isSavingAsDraft ? (
              <>
                <Loader2 className="size-4 mr-2 animate-spin" />
                Saving...
              </>
            ) : (
              <>
                <Save className="size-4 mr-2" />
                {mode === 'create' ? 'Create Project' : 'Save Changes'}
              </>
            )}
          </Button>
        </div>
      </form>
    </div>
  )
}
