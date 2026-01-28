import { cn } from '@/lib/utils'
import { Textarea } from '@/components/ui/textarea'
import { blockerCategoryInfo } from '@/types/check-in'
import type { BlockerCategory } from '@/types/check-in'

interface BlockerStepProps {
  category?: BlockerCategory
  note?: string
  onCategoryChange: (category: BlockerCategory | undefined) => void
  onNoteChange: (note: string) => void
}

const categories = Object.entries(blockerCategoryInfo) as [BlockerCategory, { label: string; emoji: string }][]

export function BlockerStep({ category, note, onCategoryChange, onNoteChange }: BlockerStepProps) {
  return (
    <div className="space-y-6 animate-in fade-in duration-300">
      <div className="text-center space-y-2">
        <h2 className="text-2xl font-semibold text-foreground">
          Any blockers today?
        </h2>
        <p className="text-sm text-muted-foreground">
          What got in the way? (optional - helps spot patterns)
        </p>
      </div>

      <div className="grid grid-cols-2 sm:grid-cols-4 gap-2">
        {categories.map(([key, info]) => {
          const isSelected = category === key

          return (
            <button
              key={key}
              onClick={() => onCategoryChange(isSelected ? undefined : key)}
              className={cn(
                'flex flex-col items-center gap-1.5 rounded-xl border-2 p-3 transition-all duration-200 cursor-pointer',
                isSelected
                  ? 'border-primary bg-primary/15 ring-1 ring-primary/30 text-primary'
                  : 'border-border/50 bg-card hover:bg-muted/50 text-muted-foreground hover:text-foreground'
              )}
            >
              <span className="text-xl">{info.emoji}</span>
              <span className="text-xs font-medium">{info.label}</span>
            </button>
          )
        })}
      </div>

      {category && (
        <div className="space-y-2 animate-in slide-in-from-bottom-4 duration-300">
          <Textarea
            placeholder="Any details? (optional)"
            value={note ?? ''}
            onChange={(e) => onNoteChange(e.target.value)}
            maxLength={500}
            rows={2}
            className="resize-none"
          />
          <p className="text-xs text-muted-foreground text-right">
            {(note?.length ?? 0)}/500
          </p>
        </div>
      )}
    </div>
  )
}
