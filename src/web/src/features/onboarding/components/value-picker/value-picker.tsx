import { Alert, AlertDescription } from '@/components/ui/alert'
import { AlertCircle, Lightbulb } from 'lucide-react'
import type { UserValueDto } from '@/types'
import { ValueItem } from './value-item'
import { ValueSuggestions } from './value-suggestions'
import { ValueInput } from './value-input'

interface ValuePickerProps {
  values: UserValueDto[]
  onChange: (values: UserValueDto[]) => void
  minRecommended?: number
  maxAllowed?: number
}

export function ValuePicker({
  values,
  onChange,
  minRecommended = 5,
  maxAllowed = 10,
}: ValuePickerProps) {
  const selectedLabels = values.map((v) => v.label)

  const handleAddValue = (label: string) => {
    if (values.length >= maxAllowed) return
    const newValue: UserValueDto = {
      id: crypto.randomUUID(),
      label,
      key: null,
      rank: values.length + 1,
      weight: null,
      notes: null,
      source: 'onboarding',
    }
    onChange([...values, newValue])
  }

  const handleRemoveValue = (id: string) => {
    const filtered = values.filter((v) => v.id !== id)
    const reranked = filtered.map((v, index) => ({ ...v, rank: index + 1 }))
    onChange(reranked)
  }

  return (
    <div className="space-y-6">
      {/* Selected values list */}
      {values.length > 0 && (
        <div className="space-y-2">
          <p className="text-sm font-medium">
            Your values ({values.length}/{maxAllowed})
          </p>
          <div className="space-y-2">
            {values.map((value) => (
              <ValueItem
                key={value.id}
                label={value.label}
                rank={value.rank}
                onRemove={() => handleRemoveValue(value.id)}
              />
            ))}
          </div>
          <p className="text-xs text-muted-foreground">
            Drag to reorder by importance (most important first)
          </p>
        </div>
      )}

      {/* Soft validation */}
      {values.length < minRecommended && values.length > 0 && (
        <Alert className="border-amber-500/50 bg-amber-500/10">
          <Lightbulb className="size-4 text-amber-500" />
          <AlertDescription className="text-amber-700 dark:text-amber-300">
            We recommend at least {minRecommended} values for best results. You have {values.length}.
          </AlertDescription>
        </Alert>
      )}

      {values.length >= maxAllowed && (
        <Alert className="border-blue-500/50 bg-blue-500/10">
          <AlertCircle className="size-4 text-blue-500" />
          <AlertDescription className="text-blue-700 dark:text-blue-300">
            Maximum of {maxAllowed} values reached. Remove some to add more.
          </AlertDescription>
        </Alert>
      )}

      {/* Suggestions */}
      {values.length < maxAllowed && (
        <ValueSuggestions
          selectedValues={selectedLabels}
          onSelect={handleAddValue}
        />
      )}

      {/* Custom input */}
      {values.length < maxAllowed && (
        <div className="pt-4 border-t border-border">
          <p className="text-sm font-medium mb-2">Or add your own:</p>
          <ValueInput
            onAdd={handleAddValue}
            existingValues={selectedLabels}
          />
        </div>
      )}
    </div>
  )
}
