import { cn } from '@/lib/utils'
import { energyLevelInfo } from '@/types/check-in'

interface EnergyBadgeProps {
  level: number
  size?: 'sm' | 'md' | 'lg'
  showLabel?: boolean
}

export function EnergyBadge({ level, size = 'md', showLabel = true }: EnergyBadgeProps) {
  const info = energyLevelInfo[level]
  if (!info) return null

  return (
    <span
      className={cn(
        'inline-flex items-center gap-1.5 rounded-full font-medium',
        info.bgColor,
        info.color,
        size === 'sm' && 'px-2 py-0.5 text-xs',
        size === 'md' && 'px-2.5 py-1 text-sm',
        size === 'lg' && 'px-3 py-1.5 text-base'
      )}
    >
      <span>{info.emoji}</span>
      {showLabel && <span>{info.label}</span>}
    </span>
  )
}
