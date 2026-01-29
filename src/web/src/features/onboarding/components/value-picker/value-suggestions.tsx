import { Check } from 'lucide-react'
import { cn } from '@/lib/utils'

// Preset value suggestions organized by category
const SUGGESTED_VALUES = [
  { category: 'Personal', values: ['Health', 'Family', 'Growth', 'Balance', 'Joy', 'Adventure'] },
  { category: 'Professional', values: ['Career', 'Excellence', 'Leadership', 'Learning', 'Impact', 'Creativity'] },
  { category: 'Social', values: ['Connection', 'Community', 'Generosity', 'Service', 'Friendship', 'Love'] },
  { category: 'Mindset', values: ['Integrity', 'Courage', 'Resilience', 'Mindfulness', 'Gratitude', 'Freedom'] },
]

interface ValueSuggestionsProps {
  selectedValues: string[]
  onSelect: (value: string) => void
  onDeselect: (value: string) => void
  maxReached?: boolean
}

export function ValueSuggestions({ selectedValues, onSelect, onDeselect, maxReached }: ValueSuggestionsProps) {
  return (
    <div className="space-y-4">
      <p className="text-sm text-muted-foreground">
        Click to select values that resonate with you:
      </p>
      {SUGGESTED_VALUES.map((group) => (
        <div key={group.category} className="space-y-2">
          <p className="text-xs font-medium text-muted-foreground uppercase tracking-wide">
            {group.category}
          </p>
          <div className="flex flex-wrap gap-2">
            {group.values.map((value) => {
              const isSelected = selectedValues.includes(value)
              const isDisabled = !isSelected && maxReached
              return (
                <button
                  key={value}
                  type="button"
                  onClick={() => isSelected ? onDeselect(value) : onSelect(value)}
                  disabled={isDisabled}
                  className={cn(
                    'inline-flex items-center gap-1.5 px-3 py-1.5 text-sm font-medium rounded-md border transition-all duration-200',
                    isSelected
                      ? 'bg-primary text-primary-foreground border-primary'
                      : 'bg-background text-foreground border-input hover:bg-accent hover:text-accent-foreground',
                    isDisabled && 'opacity-50 cursor-not-allowed'
                  )}
                >
                  {isSelected && <Check className="size-3.5" />}
                  {value}
                </button>
              )
            })}
          </div>
        </div>
      ))}
    </div>
  )
}

export { SUGGESTED_VALUES }
