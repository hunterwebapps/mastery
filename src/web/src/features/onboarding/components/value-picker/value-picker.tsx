import { Alert, AlertDescription } from '@/components/ui/alert'
import { AlertCircle, Lightbulb } from 'lucide-react'
import { cn } from '@/lib/utils'
import type { UserValueDto } from '@/types'
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
  const hasEnoughValues = values.length >= minRecommended
  const maxReached = values.length >= maxAllowed

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

  const handleRemoveValue = (label: string) => {
    const filtered = values.filter((v) => v.label !== label)
    const reranked = filtered.map((v, index) => ({ ...v, rank: index + 1 }))
    onChange(reranked)
  }

  return (
    <div className="space-y-6">
      {/* Info alert - smoothly collapses after minimum reached */}
      <div
        className={cn(
          'grid transition-all duration-300 ease-in-out',
          hasEnoughValues ? 'grid-rows-[0fr] opacity-0' : 'grid-rows-[1fr] opacity-100'
        )}
      >
        <div className="overflow-hidden">
          <Alert className="border-muted bg-muted/50">
            <Lightbulb className="size-4 text-muted-foreground" />
            <AlertDescription className="text-muted-foreground">
              Select at least {minRecommended} values for personalized recommendations.
              {values.length > 0 && ` (${values.length} selected)`}
            </AlertDescription>
          </Alert>
        </div>
      </div>

      {/* Selection count */}
      <p className="text-sm font-medium text-muted-foreground">
        {values.length} of {maxAllowed} values selected
      </p>

      {/* Max reached alert */}
      {maxReached && (
        <Alert className="border-blue-500/50 bg-blue-500/10">
          <AlertCircle className="size-4 text-blue-500" />
          <AlertDescription className="text-blue-700 dark:text-blue-300">
            Maximum of {maxAllowed} values reached. Deselect some to choose others.
          </AlertDescription>
        </Alert>
      )}

      {/* Suggestions */}
      <ValueSuggestions
        selectedValues={selectedLabels}
        onSelect={handleAddValue}
        onDeselect={handleRemoveValue}
        maxReached={maxReached}
      />

      {/* Custom input */}
      {!maxReached && (
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
