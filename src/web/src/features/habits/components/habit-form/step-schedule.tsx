import { useFormContext } from 'react-hook-form'
import { Label } from '@/components/ui/label'
import { Input } from '@/components/ui/input'
import { Button } from '@/components/ui/button'
import { Slider } from '@/components/ui/slider'
import { cn } from '@/lib/utils'
import type { CreateHabitFormData } from '../../schemas/habit-schema'
import type { ScheduleType } from '@/types/habit'
import { scheduleTypeInfo } from '@/types/habit'

const DAYS_OF_WEEK = [
  { value: 0, label: 'Sun', short: 'S' },
  { value: 1, label: 'Mon', short: 'M' },
  { value: 2, label: 'Tue', short: 'T' },
  { value: 3, label: 'Wed', short: 'W' },
  { value: 4, label: 'Thu', short: 'T' },
  { value: 5, label: 'Fri', short: 'F' },
  { value: 6, label: 'Sat', short: 'S' },
]

export function StepSchedule() {
  const {
    watch,
    setValue,
    register,
    formState: { errors },
  } = useFormContext<CreateHabitFormData>()

  const scheduleType = watch('schedule.type')
  const daysOfWeek = watch('schedule.daysOfWeek') || []
  const frequencyPerWeek = watch('schedule.frequencyPerWeek') || 3
  const intervalDays = watch('schedule.intervalDays') || 2

  const handleScheduleTypeChange = (type: ScheduleType) => {
    setValue('schedule.type', type)
    // Reset type-specific fields
    if (type === 'Daily') {
      setValue('schedule.daysOfWeek', undefined)
      setValue('schedule.frequencyPerWeek', undefined)
      setValue('schedule.intervalDays', undefined)
    } else if (type === 'DaysOfWeek') {
      setValue('schedule.frequencyPerWeek', undefined)
      setValue('schedule.intervalDays', undefined)
    } else if (type === 'WeeklyFrequency') {
      setValue('schedule.daysOfWeek', undefined)
      setValue('schedule.intervalDays', undefined)
      setValue('schedule.frequencyPerWeek', 3)
    } else if (type === 'Interval') {
      setValue('schedule.daysOfWeek', undefined)
      setValue('schedule.frequencyPerWeek', undefined)
      setValue('schedule.intervalDays', 2)
    }
  }

  const toggleDayOfWeek = (day: number) => {
    const current = daysOfWeek || []
    if (current.includes(day)) {
      setValue('schedule.daysOfWeek', current.filter(d => d !== day))
    } else {
      setValue('schedule.daysOfWeek', [...current, day].sort())
    }
  }

  return (
    <div className="space-y-6">
      <div className="text-center mb-8">
        <h2 className="text-xl font-semibold">How often do you want to do this?</h2>
        <p className="text-muted-foreground mt-1">
          Choose a schedule that fits your lifestyle. You can always adjust it later.
        </p>
      </div>

      {/* Schedule Type Selection */}
      <div className="space-y-3">
        <Label>Schedule type</Label>
        <div className="grid grid-cols-2 gap-3">
          {(Object.keys(scheduleTypeInfo) as ScheduleType[]).map((type) => (
            <Button
              key={type}
              type="button"
              variant={scheduleType === type ? 'default' : 'outline'}
              className={cn(
                'h-auto py-4 flex flex-col items-start gap-1',
                scheduleType === type && 'ring-2 ring-primary'
              )}
              onClick={() => handleScheduleTypeChange(type)}
            >
              <span className="font-medium">{scheduleTypeInfo[type].label}</span>
              <span className="text-xs opacity-70 font-normal">
                {scheduleTypeInfo[type].description}
              </span>
            </Button>
          ))}
        </div>
      </div>

      {/* Type-specific configuration */}
      {scheduleType === 'DaysOfWeek' && (
        <div className="space-y-3">
          <Label>Which days?</Label>
          <div className="flex gap-2 justify-center">
            {DAYS_OF_WEEK.map((day) => (
              <Button
                key={day.value}
                type="button"
                variant={daysOfWeek.includes(day.value) ? 'default' : 'outline'}
                size="icon"
                className={cn(
                  'size-12 rounded-full font-medium',
                  daysOfWeek.includes(day.value) && 'bg-primary'
                )}
                onClick={() => toggleDayOfWeek(day.value)}
              >
                {day.short}
              </Button>
            ))}
          </div>
          {daysOfWeek.length === 0 && (
            <p className="text-sm text-destructive text-center">
              Please select at least one day
            </p>
          )}
          {daysOfWeek.length > 0 && (
            <p className="text-sm text-muted-foreground text-center">
              {daysOfWeek.length} day{daysOfWeek.length > 1 ? 's' : ''} per week
            </p>
          )}
        </div>
      )}

      {scheduleType === 'WeeklyFrequency' && (
        <div className="space-y-4">
          <Label>How many times per week?</Label>
          <div className="px-4">
            <Slider
              value={[frequencyPerWeek]}
              onValueChange={([value]) => setValue('schedule.frequencyPerWeek', value)}
              min={1}
              max={7}
              step={1}
            />
          </div>
          <p className="text-center text-2xl font-bold text-primary">
            {frequencyPerWeek}x per week
          </p>
          <p className="text-sm text-muted-foreground text-center">
            Complete on any {frequencyPerWeek} day{frequencyPerWeek > 1 ? 's' : ''} each week
          </p>
        </div>
      )}

      {scheduleType === 'Interval' && (
        <div className="space-y-4">
          <Label>Every how many days?</Label>
          <div className="flex items-center justify-center gap-4">
            <Button
              type="button"
              variant="outline"
              size="icon"
              onClick={() => setValue('schedule.intervalDays', Math.max(2, intervalDays - 1))}
              disabled={intervalDays <= 2}
            >
              -
            </Button>
            <div className="text-center">
              <p className="text-3xl font-bold text-primary">{intervalDays}</p>
              <p className="text-sm text-muted-foreground">days</p>
            </div>
            <Button
              type="button"
              variant="outline"
              size="icon"
              onClick={() => setValue('schedule.intervalDays', Math.min(30, intervalDays + 1))}
              disabled={intervalDays >= 30}
            >
              +
            </Button>
          </div>
          <p className="text-sm text-muted-foreground text-center">
            Once every {intervalDays} days ({Math.round(365 / intervalDays)} times per year)
          </p>
        </div>
      )}

      {scheduleType === 'Daily' && (
        <div className="text-center py-4">
          <p className="text-lg font-medium text-primary">Every single day</p>
          <p className="text-sm text-muted-foreground mt-1">
            This habit will show up in your daily loop 7 days a week.
          </p>
        </div>
      )}

      {/* Optional start date */}
      <div className="space-y-2 pt-4 border-t">
        <Label htmlFor="startDate">Start date (optional)</Label>
        <Input
          id="startDate"
          type="date"
          {...register('schedule.startDate')}
        />
        <p className="text-xs text-muted-foreground">
          Leave empty to start today.
        </p>
      </div>

      {errors.schedule && (
        <p className="text-sm text-destructive">{errors.schedule.message}</p>
      )}
    </div>
  )
}
