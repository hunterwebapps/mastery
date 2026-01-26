import { Slider } from '@/components/ui/slider'
import { Label } from '@/components/ui/label'

interface RoleHoursInputProps {
  hours: number
  onChange: (hours: number) => void
  disabled?: boolean
}

export function RoleHoursInput({ hours, onChange, disabled }: RoleHoursInputProps) {
  return (
    <div className="space-y-2">
      <div className="flex items-center justify-between">
        <Label className="text-xs">Weekly hours target</Label>
        <span className="text-sm font-medium text-primary">{hours}h</span>
      </div>
      <Slider
        value={[hours]}
        onValueChange={([value]) => onChange(value)}
        min={0}
        max={60}
        step={1}
        disabled={disabled}
      />
      <div className="flex justify-between text-xs text-muted-foreground">
        <span>0h</span>
        <span>60h</span>
      </div>
    </div>
  )
}
