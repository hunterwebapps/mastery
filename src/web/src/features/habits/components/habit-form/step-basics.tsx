import { useFormContext } from 'react-hook-form'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Textarea } from '@/components/ui/textarea'
import type { CreateHabitFormData } from '../../schemas/habit-schema'

export function StepBasics() {
  const {
    register,
    formState: { errors },
  } = useFormContext<CreateHabitFormData>()

  return (
    <div className="space-y-6">
      <div className="text-center mb-8">
        <h2 className="text-xl font-semibold">What habit do you want to build?</h2>
        <p className="text-muted-foreground mt-1">
          Start with a clear, actionable habit you want to practice regularly.
        </p>
      </div>

      <div className="space-y-2">
        <Label htmlFor="title">
          Habit name <span className="text-destructive">*</span>
        </Label>
        <Input
          id="title"
          placeholder="e.g., Morning meditation, Read 20 pages, Exercise"
          {...register('title')}
          className={errors.title ? 'border-destructive' : ''}
          autoFocus
        />
        {errors.title && (
          <p className="text-sm text-destructive">{errors.title.message}</p>
        )}
        <p className="text-xs text-muted-foreground">
          Keep it short and action-oriented.
        </p>
      </div>

      <div className="space-y-2">
        <Label htmlFor="description">Description</Label>
        <Textarea
          id="description"
          placeholder="What does this habit involve? Any specific details?"
          rows={3}
          {...register('description')}
        />
        <p className="text-xs text-muted-foreground">
          Optional. Add details about what completing this habit looks like.
        </p>
      </div>

      <div className="space-y-2">
        <Label htmlFor="why">Why is this habit important to you?</Label>
        <Textarea
          id="why"
          placeholder="How will this habit improve your life? What will it help you achieve?"
          rows={3}
          {...register('why')}
        />
        <p className="text-xs text-muted-foreground">
          Understanding your motivation helps you stick with it when things get tough.
        </p>
      </div>
    </div>
  )
}
