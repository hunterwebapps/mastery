import { Target, BarChart3, Shield } from 'lucide-react'
import { Label } from '@/components/ui/label'
import { RadioGroup, RadioGroupItem } from '@/components/ui/radio-group'
import type { MetricKind } from '@/types'
import { metricKindInfo } from '@/types'

interface MetricKindSelectProps {
  value: MetricKind
  onValueChange: (value: MetricKind) => void
  disabled?: boolean
}

const kindIcons: Record<MetricKind, React.ElementType> = {
  Lag: Target,
  Lead: BarChart3,
  Constraint: Shield,
}

const kinds: MetricKind[] = ['Lag', 'Lead', 'Constraint']

export function MetricKindSelect({
  value,
  onValueChange,
  disabled,
}: MetricKindSelectProps) {
  return (
    <RadioGroup
      value={value}
      onValueChange={(v) => onValueChange(v as MetricKind)}
      disabled={disabled}
      className="grid grid-cols-1 gap-2"
    >
      {kinds.map((kind) => {
        const info = metricKindInfo[kind]
        const Icon = kindIcons[kind]
        const isSelected = value === kind

        return (
          <Label
            key={kind}
            htmlFor={`kind-${kind}`}
            className={`
              flex items-center gap-3 p-3 rounded-lg border cursor-pointer transition-colors
              ${isSelected
                ? 'border-primary bg-primary/5'
                : 'border-border hover:border-primary/50 hover:bg-muted/50'
              }
              ${disabled ? 'opacity-50 cursor-not-allowed' : ''}
            `}
          >
            <RadioGroupItem value={kind} id={`kind-${kind}`} className="sr-only" />
            <div className={`p-1.5 rounded ${info.color.replace('text-', 'bg-')}/10`}>
              <Icon className={`size-4 ${info.color}`} />
            </div>
            <div className="flex-1">
              <div className="font-medium text-sm">{info.label}</div>
              <div className="text-xs text-muted-foreground">{info.description}</div>
            </div>
            {isSelected && (
              <div className={`size-2 rounded-full ${info.color.replace('text-', 'bg-')}`} />
            )}
          </Label>
        )
      })}
    </RadioGroup>
  )
}
