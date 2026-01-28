import { useEffect } from 'react'
import { useParams, useNavigate, Link } from 'react-router-dom'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { ArrowLeft, Loader2, Save } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Textarea } from '@/components/ui/textarea'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Skeleton } from '@/components/ui/skeleton'
import { useGoal, useUpdateGoal } from '../hooks'
import { updateGoalSchema, type UpdateGoalFormData } from '../schemas'

function EditGoalSkeleton() {
  return (
    <div className="min-h-screen bg-background">
      <div className="container max-w-2xl py-8 px-4 sm:px-6 lg:px-8">
        <div className="space-y-6">
          <div className="flex items-center gap-4">
            <Skeleton className="size-10" />
            <Skeleton className="h-8 w-48" />
          </div>
          <Card>
            <CardHeader>
              <Skeleton className="h-6 w-32" />
            </CardHeader>
            <CardContent className="space-y-4">
              <Skeleton className="h-10 w-full" />
              <Skeleton className="h-24 w-full" />
              <Skeleton className="h-24 w-full" />
              <div className="grid grid-cols-2 gap-4">
                <Skeleton className="h-10 w-full" />
                <Skeleton className="h-10 w-full" />
              </div>
            </CardContent>
          </Card>
        </div>
      </div>
    </div>
  )
}

export function Component() {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const { data: goal, isLoading } = useGoal(id!)
  const updateGoal = useUpdateGoal()

  const {
    register,
    handleSubmit,
    setValue,
    watch,
    reset,
    formState: { errors, isDirty },
  } = useForm<UpdateGoalFormData>({
    resolver: zodResolver(updateGoalSchema),
    defaultValues: {
      title: '',
      description: '',
      why: '',
      priority: 3,
      deadline: '',
    },
  })

  // Populate form when goal loads
  useEffect(() => {
    if (goal) {
      reset({
        title: goal.title,
        description: goal.description ?? '',
        why: goal.why ?? '',
        priority: goal.priority,
        deadline: goal.deadline ?? '',
      })
    }
  }, [goal, reset])

  const priority = watch('priority')

  const onSubmit = async (data: UpdateGoalFormData) => {
    try {
      await updateGoal.mutateAsync({
        id: id!,
        request: {
          ...data,
          deadline: data.deadline || undefined,
        },
      })
      navigate(`/goals/${id}`)
    } catch (error) {
      console.error('Failed to update goal:', error)
    }
  }

  if (isLoading || !goal) {
    return <EditGoalSkeleton />
  }

  return (
    <div className="min-h-screen bg-background">
      <div className="container max-w-2xl py-8 px-4 sm:px-6 lg:px-8">
        <form onSubmit={handleSubmit(onSubmit)} className="space-y-6">
          <div className="flex items-center justify-between">
            <div className="flex items-center gap-4">
              <Button type="button" variant="ghost" size="icon" asChild>
                <Link to={`/goals/${id}`}>
                  <ArrowLeft className="size-4" />
                </Link>
              </Button>
              <h1 className="text-2xl font-bold">Edit Goal</h1>
            </div>
            <div className="flex items-center gap-2">
              <Button type="button" variant="outline" asChild>
                <Link to={`/goals/${id}`}>Cancel</Link>
              </Button>
              <Button type="submit" disabled={!isDirty || updateGoal.isPending}>
                {updateGoal.isPending && <Loader2 className="size-4 mr-2 animate-spin" />}
                <Save className="size-4 mr-2" />
                Save Changes
              </Button>
            </div>
          </div>

          <Card>
            <CardHeader>
              <CardTitle>Goal Details</CardTitle>
            </CardHeader>
            <CardContent className="space-y-4">
              <div className="space-y-2">
                <Label htmlFor="title">
                  Title <span className="text-destructive">*</span>
                </Label>
                <Input
                  id="title"
                  placeholder="What do you want to achieve?"
                  {...register('title')}
                  className={errors.title ? 'border-destructive' : ''}
                />
                {errors.title && (
                  <p className="text-sm text-destructive">{errors.title.message}</p>
                )}
              </div>

              <div className="space-y-2">
                <Label htmlFor="description">Description</Label>
                <Textarea
                  id="description"
                  placeholder="Describe your goal in more detail..."
                  rows={3}
                  {...register('description')}
                />
              </div>

              <div className="space-y-2">
                <Label htmlFor="why">Why does this matter?</Label>
                <Textarea
                  id="why"
                  placeholder="What's your motivation? Why is this important to you?"
                  rows={3}
                  {...register('why')}
                />
              </div>

              <div className="grid gap-4 sm:grid-cols-2">
                <div className="space-y-2">
                  <Label>Priority</Label>
                  <Select
                    value={priority !== undefined ? String(priority) : undefined}
                    onValueChange={(value) => setValue('priority', parseInt(value), { shouldDirty: true })}
                  >
                    <SelectTrigger>
                      <SelectValue placeholder="Select priority" />
                    </SelectTrigger>
                    <SelectContent>
                      <SelectItem value="1">Critical</SelectItem>
                      <SelectItem value="2">High</SelectItem>
                      <SelectItem value="3">Medium</SelectItem>
                      <SelectItem value="4">Low</SelectItem>
                      <SelectItem value="5">Someday</SelectItem>
                    </SelectContent>
                  </Select>
                </div>

                <div className="space-y-2">
                  <Label htmlFor="deadline">Target Date</Label>
                  <Input
                    id="deadline"
                    type="date"
                    {...register('deadline')}
                  />
                </div>
              </div>
            </CardContent>
          </Card>
        </form>
      </div>
    </div>
  )
}
