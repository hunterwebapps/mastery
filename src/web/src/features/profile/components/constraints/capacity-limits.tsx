import { Slider } from '@/components/ui/slider'
import { Label } from '@/components/ui/label'

interface CapacityLimitsProps {
  weekdayMinutes: number
  weekendMinutes: number
  onChange: (weekday: number, weekend: number) => void
  disabled?: boolean
}

function formatHours(minutes: number): string {
  const hours = Math.floor(minutes / 60)
  const mins = minutes % 60
  if (mins === 0) return `${hours}h`
  return `${hours}h ${mins}m`
}

export function CapacityLimits({
  weekdayMinutes,
  weekendMinutes,
  onChange,
  disabled = false,
}: CapacityLimitsProps) {
  return (
    <div className="space-y-6">
      <div className="space-y-3">
        <div className="flex items-center justify-between">
          <Label className="text-sm font-medium">Weekday Capacity</Label>
          <span className="text-sm font-medium text-primary">
            {formatHours(weekdayMinutes)}
          </span>
        </div>
        <Slider
          value={[weekdayMinutes]}
          onValueChange={([value]) => onChange(value, weekendMinutes)}
          min={60}
          max={720}
          step={30}
          disabled={disabled}
        />
        <p className="text-xs text-muted-foreground">
          Maximum time for planned activities on weekdays (Mon-Fri)
        </p>
      </div>

      <div className="space-y-3">
        <div className="flex items-center justify-between">
          <Label className="text-sm font-medium">Weekend Capacity</Label>
          <span className="text-sm font-medium text-primary">
            {formatHours(weekendMinutes)}
          </span>
        </div>
        <Slider
          value={[weekendMinutes]}
          onValueChange={([value]) => onChange(weekdayMinutes, value)}
          min={0}
          max={720}
          step={30}
          disabled={disabled}
        />
        <p className="text-xs text-muted-foreground">
          Maximum time for planned activities on weekends (Sat-Sun)
        </p>
      </div>
    </div>
  )
}
