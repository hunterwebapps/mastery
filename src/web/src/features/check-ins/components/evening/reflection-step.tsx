import { Textarea } from '@/components/ui/textarea'

interface ReflectionStepProps {
  value?: string
  onChange: (reflection: string) => void
}

export function ReflectionStep({ value, onChange }: ReflectionStepProps) {
  return (
    <div className="space-y-6 animate-in fade-in duration-300">
      <div className="text-center space-y-2">
        <h2 className="text-2xl font-semibold text-foreground">
          Quick reflection
        </h2>
        <p className="text-sm text-muted-foreground">
          One sentence about your day (optional)
        </p>
      </div>

      <div className="space-y-2">
        <Textarea
          placeholder="e.g., Good deep work session but lost focus after lunch"
          value={value ?? ''}
          onChange={(e) => onChange(e.target.value)}
          maxLength={1000}
          rows={3}
          className="resize-none"
        />
        <p className="text-xs text-muted-foreground text-right">
          {(value?.length ?? 0)}/1000
        </p>
      </div>
    </div>
  )
}
