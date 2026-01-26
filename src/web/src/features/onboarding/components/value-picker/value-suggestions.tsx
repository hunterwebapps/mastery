import { Plus } from 'lucide-react'
import { Button } from '@/components/ui/button'

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
}

export function ValueSuggestions({ selectedValues, onSelect }: ValueSuggestionsProps) {
  return (
    <div className="space-y-4">
      <p className="text-sm text-muted-foreground">
        Click to add values that resonate with you:
      </p>
      {SUGGESTED_VALUES.map((group) => (
        <div key={group.category} className="space-y-2">
          <p className="text-xs font-medium text-muted-foreground uppercase tracking-wide">
            {group.category}
          </p>
          <div className="flex flex-wrap gap-2">
            {group.values.map((value) => {
              const isSelected = selectedValues.includes(value)
              return (
                <Button
                  key={value}
                  variant={isSelected ? 'secondary' : 'outline'}
                  size="sm"
                  onClick={() => onSelect(value)}
                  disabled={isSelected}
                  className="gap-1"
                >
                  {!isSelected && <Plus className="size-3" />}
                  {value}
                </Button>
              )
            })}
          </div>
        </div>
      ))}
    </div>
  )
}

export { SUGGESTED_VALUES }
