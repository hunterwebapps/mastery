import { Trash2 } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Checkbox } from '@/components/ui/checkbox'
import type { BlockedWindowDto, DayOfWeek } from '@/types'

interface BlockedWindowItemProps {
  window: BlockedWindowDto
  onChange: (window: BlockedWindowDto) => void
  onRemove: () => void
  disabled?: boolean
}

const DAYS: { value: DayOfWeek; label: string; short: string }[] = [
  { value: 'Monday', label: 'Monday', short: 'M' },
  { value: 'Tuesday', label: 'Tuesday', short: 'T' },
  { value: 'Wednesday', label: 'Wednesday', short: 'W' },
  { value: 'Thursday', label: 'Thursday', short: 'T' },
  { value: 'Friday', label: 'Friday', short: 'F' },
  { value: 'Saturday', label: 'Saturday', short: 'S' },
  { value: 'Sunday', label: 'Sunday', short: 'S' },
]

export function BlockedWindowItem({
  window,
  onChange,
  onRemove,
  disabled = false,
}: BlockedWindowItemProps) {
  const toggleDay = (day: DayOfWeek) => {
    const isSelected = window.applicableDays.includes(day)
    const newDays = isSelected
      ? window.applicableDays.filter((d) => d !== day)
      : [...window.applicableDays, day]
    onChange({ ...window, applicableDays: newDays })
  }

  return (
    <div className="rounded-lg border border-border p-4 space-y-4">
      <div className="flex items-start justify-between gap-4">
        <div className="flex-1 space-y-4">
          <Input
            placeholder="Label (e.g., Family Dinner, Gym)"
            value={window.label || ''}
            onChange={(e) => onChange({ ...window, label: e.target.value || null })}
            disabled={disabled}
          />

          <div className="grid gap-4 sm:grid-cols-2">
            <div className="space-y-2">
              <Label className="text-xs">Start Time</Label>
              <Input
                type="time"
                value={window.timeWindow.start}
                onChange={(e) =>
                  onChange({
                    ...window,
                    timeWindow: { ...window.timeWindow, start: e.target.value },
                  })
                }
                disabled={disabled}
              />
            </div>
            <div className="space-y-2">
              <Label className="text-xs">End Time</Label>
              <Input
                type="time"
                value={window.timeWindow.end}
                onChange={(e) =>
                  onChange({
                    ...window,
                    timeWindow: { ...window.timeWindow, end: e.target.value },
                  })
                }
                disabled={disabled}
              />
            </div>
          </div>

          <div className="space-y-2">
            <Label className="text-xs">Applicable Days</Label>
            <div className="flex flex-wrap gap-2">
              {DAYS.map((day) => {
                const isSelected = window.applicableDays.includes(day.value)
                return (
                  <label
                    key={day.value}
                    className="flex items-center gap-1.5 cursor-pointer"
                  >
                    <Checkbox
                      checked={isSelected}
                      onCheckedChange={() => toggleDay(day.value)}
                      disabled={disabled}
                    />
                    <span className="text-xs">{day.label}</span>
                  </label>
                )
              })}
            </div>
          </div>
        </div>

        <Button
          variant="ghost"
          size="icon"
          className="text-destructive hover:bg-destructive/10 shrink-0"
          onClick={onRemove}
          disabled={disabled}
        >
          <Trash2 className="size-4" />
        </Button>
      </div>
    </div>
  )
}
