import { useFormContext } from 'react-hook-form'
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
import type { CreateGoalFormData } from '../../schemas'

export function StepBasics() {
  const {
    register,
    formState: { errors },
    setValue,
    watch,
  } = useFormContext<CreateGoalFormData>()

  const priority = watch('priority')

  return (
    <div className="space-y-6">
      <div className="space-y-2">
        <Label htmlFor="title">
          What do you want to achieve? <span className="text-destructive">*</span>
        </Label>
        <Input
          id="title"
          placeholder="e.g., Lose 10 pounds, Launch MVP, Learn Spanish"
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
        <p className="text-xs text-muted-foreground">
          Optional. Add context or clarification about what success looks like.
        </p>
      </div>

      <div className="space-y-2">
        <Label htmlFor="why">Why does this matter?</Label>
        <Textarea
          id="why"
          placeholder="What will achieving this goal mean for you? What's at stake?"
          rows={3}
          {...register('why')}
        />
        <p className="text-xs text-muted-foreground">
          Understanding your "why" helps maintain motivation when things get hard.
        </p>
      </div>

      <div className="grid gap-6 sm:grid-cols-2">
        <div className="space-y-2">
          <Label htmlFor="priority">Priority</Label>
          <Select
            value={priority?.toString() ?? '3'}
            onValueChange={(value) => setValue('priority', parseInt(value))}
          >
            <SelectTrigger>
              <SelectValue placeholder="Select priority" />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="1">1 - Critical</SelectItem>
              <SelectItem value="2">2 - High</SelectItem>
              <SelectItem value="3">3 - Medium</SelectItem>
              <SelectItem value="4">4 - Low</SelectItem>
              <SelectItem value="5">5 - Someday</SelectItem>
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
          <p className="text-xs text-muted-foreground">
            Optional. When do you want to achieve this?
          </p>
        </div>
      </div>
    </div>
  )
}
