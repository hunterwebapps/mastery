import { Check } from 'lucide-react'
import { cn } from '@/lib/utils'
import { Button } from '@/components/ui/button'
import type { HabitMode, HabitVariantDto } from '@/types/habit'
import { habitModeInfo } from '@/types/habit'

interface ModeSelectorProps {
  variants: HabitVariantDto[]
  defaultMode: HabitMode
  selectedMode: HabitMode
  onModeChange: (mode: HabitMode) => void
  disabled?: boolean
}

export function ModeSelector({
  variants,
  defaultMode,
  selectedMode,
  onModeChange,
  disabled = false,
}: ModeSelectorProps) {
  // Only show modes that have variants defined
  const availableModes = variants.length > 0
    ? variants.map(v => v.mode)
    : [defaultMode]

  // Sort modes in order: Full > Maintenance > Minimum
  const modeOrder: HabitMode[] = ['Full', 'Maintenance', 'Minimum']
  const sortedModes = modeOrder.filter(m => availableModes.includes(m))

  if (sortedModes.length <= 1) {
    return null
  }

  return (
    <div className="flex gap-1">
      {sortedModes.map((mode) => {
        const variant = variants.find(v => v.mode === mode)
        const info = habitModeInfo[mode]
        const isSelected = selectedMode === mode

        return (
          <Button
            key={mode}
            variant={isSelected ? 'default' : 'ghost'}
            size="xs"
            disabled={disabled}
            onClick={(e) => {
              e.stopPropagation()
              onModeChange(mode)
            }}
            className={cn(
              'gap-1 transition-all',
              isSelected && info.bgColor,
              isSelected && info.color,
              !isSelected && 'opacity-60 hover:opacity-100'
            )}
          >
            {isSelected && <Check className="size-3" />}
            <span>{variant?.label || info.label}</span>
            {variant && variant.estimatedMinutes > 0 && (
              <span className="text-[10px] opacity-70">
                {variant.estimatedMinutes}m
              </span>
            )}
          </Button>
        )
      })}
    </div>
  )
}
