import { useFormContext } from 'react-hook-form'
import { Check, Calendar, Repeat, Zap } from 'lucide-react'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import { cn } from '@/lib/utils'
import type { CreateHabitFormData } from '../../schemas/habit-schema'
import { habitModeInfo, scheduleTypeInfo } from '@/types/habit'

const DAYS_OF_WEEK = ['Sun', 'Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat']

interface StepReviewProps {
  mode?: 'create' | 'edit'
}

export function StepReview({ mode = 'create' }: StepReviewProps) {
  const { watch } = useFormContext<CreateHabitFormData>()
  const isEditMode = mode === 'edit'

  const values = watch()

  const getScheduleDescription = () => {
    const { schedule } = values
    switch (schedule.type) {
      case 'Daily':
        return 'Every day'
      case 'DaysOfWeek':
        const days = schedule.daysOfWeek?.map(d => DAYS_OF_WEEK[d]).join(', ')
        return `On ${days}`
      case 'WeeklyFrequency':
        return `${schedule.frequencyPerWeek}x per week (flexible days)`
      case 'Interval':
        return `Every ${schedule.intervalDays} days`
      default:
        return 'Custom schedule'
    }
  }

  return (
    <div className="space-y-6">
      <div className="text-center mb-8">
        <h2 className="text-xl font-semibold">
          {isEditMode ? 'Review your changes' : 'Review your habit'}
        </h2>
        <p className="text-muted-foreground mt-1">
          {isEditMode
            ? 'Make sure everything looks good before saving.'
            : 'Make sure everything looks good before creating.'}
        </p>
      </div>

      {/* Habit Overview */}
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <Check className="size-5 text-primary" />
            {values.title}
          </CardTitle>
        </CardHeader>
        <CardContent className="space-y-4">
          {values.description && (
            <p className="text-muted-foreground">{values.description}</p>
          )}

          {values.why && (
            <div className="p-3 rounded-lg bg-muted/50">
              <p className="text-sm font-medium mb-1">Why this matters:</p>
              <p className="text-sm text-muted-foreground">{values.why}</p>
            </div>
          )}
        </CardContent>
      </Card>

      {/* Schedule */}
      <Card>
        <CardHeader className="pb-3">
          <CardTitle className="text-base flex items-center gap-2">
            <Calendar className="size-4" />
            Schedule
          </CardTitle>
        </CardHeader>
        <CardContent>
          <div className="flex items-center justify-between">
            <div>
              <p className="font-medium">{scheduleTypeInfo[values.schedule.type]?.label}</p>
              <p className="text-sm text-muted-foreground">{getScheduleDescription()}</p>
            </div>
            {values.schedule.startDate && (
              <Badge variant="outline">
                Starts {new Date(values.schedule.startDate).toLocaleDateString()}
              </Badge>
            )}
          </div>

          {values.schedule.type === 'DaysOfWeek' && values.schedule.daysOfWeek && (
            <div className="flex gap-1 mt-3">
              {DAYS_OF_WEEK.map((day, index) => (
                <div
                  key={day}
                  className={cn(
                    'size-8 rounded-full flex items-center justify-center text-xs font-medium',
                    values.schedule.daysOfWeek?.includes(index)
                      ? 'bg-primary text-primary-foreground'
                      : 'bg-muted text-muted-foreground'
                  )}
                >
                  {day[0]}
                </div>
              ))}
            </div>
          )}
        </CardContent>
      </Card>

      {/* Default Mode */}
      <Card>
        <CardHeader className="pb-3">
          <CardTitle className="text-base flex items-center gap-2">
            <Zap className="size-4" />
            Default Mode
          </CardTitle>
        </CardHeader>
        <CardContent>
          <Badge className={cn(
            habitModeInfo[values.defaultMode].bgColor,
            habitModeInfo[values.defaultMode].color
          )}>
            {habitModeInfo[values.defaultMode].label}
          </Badge>
          <p className="text-sm text-muted-foreground mt-2">
            {habitModeInfo[values.defaultMode].description}
          </p>
        </CardContent>
      </Card>

      {/* Variants */}
      {values.variants && values.variants.length > 0 && (
        <Card>
          <CardHeader className="pb-3">
            <CardTitle className="text-base flex items-center gap-2">
              <Repeat className="size-4" />
              Mode Variants ({values.variants.length})
            </CardTitle>
          </CardHeader>
          <CardContent>
            <div className="space-y-3">
              {values.variants.map((variant, index) => (
                <div
                  key={index}
                  className="flex items-center justify-between p-3 rounded-lg bg-muted/50"
                >
                  <div className="flex items-center gap-3">
                    <div
                      className={cn(
                        'size-3 rounded-full',
                        variant.mode === 'Full' && 'bg-blue-500',
                        variant.mode === 'Maintenance' && 'bg-yellow-500',
                        variant.mode === 'Minimum' && 'bg-orange-500'
                      )}
                    />
                    <div>
                      <p className="font-medium">{variant.label}</p>
                      <p className="text-xs text-muted-foreground">
                        {variant.estimatedMinutes} min · Energy: {variant.energyCost}/5
                      </p>
                    </div>
                  </div>
                  {variant.countsAsCompletion && (
                    <Badge variant="outline" className="text-xs">
                      Counts
                    </Badge>
                  )}
                </div>
              ))}
            </div>
          </CardContent>
        </Card>
      )}

      {/* Summary */}
      <div className="p-4 rounded-lg border border-primary/20 bg-primary/5">
        <p className="text-sm text-center">
          {isEditMode ? (
            <>Ready to save changes to <strong>{values.title}</strong>?</>
          ) : (
            <>Ready to start building <strong>{values.title}</strong>?</>
          )}
        </p>
        <p className="text-xs text-muted-foreground text-center mt-1">
          {isEditMode
            ? 'Your habit will be updated with these settings.'
            : 'You can always edit these settings later.'}
        </p>
      </div>
    </div>
  )
}
