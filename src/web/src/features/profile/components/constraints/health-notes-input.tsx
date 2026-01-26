import { Textarea } from '@/components/ui/textarea'
import { Label } from '@/components/ui/label'

interface HealthNotesInputProps {
  value: string | null
  onChange: (value: string | null) => void
  disabled?: boolean
}

export function HealthNotesInput({
  value,
  onChange,
  disabled = false,
}: HealthNotesInputProps) {
  return (
    <div className="space-y-2">
      <Label className="text-sm font-medium">Health Notes</Label>
      <Textarea
        placeholder="Any health considerations the AI should know about (e.g., chronic fatigue, ADHD, recovering from injury...)"
        value={value || ''}
        onChange={(e) => onChange(e.target.value || null)}
        disabled={disabled}
        rows={3}
        className="resize-none"
      />
      <p className="text-xs text-muted-foreground">
        This helps the coaching engine provide more personalized recommendations
      </p>
    </div>
  )
}
