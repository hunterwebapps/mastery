import { Flame } from 'lucide-react'
import { cn } from '@/lib/utils'
import { Tooltip, TooltipContent, TooltipTrigger } from '@/components/ui/tooltip'

interface StreakBadgeProps {
  streak: number
  className?: string
  showTooltip?: boolean
}

function getStreakLevel(streak: number): { flames: number; color: string; label: string } {
  if (streak >= 100) {
    return { flames: 3, color: 'text-orange-500', label: 'Legendary streak!' }
  }
  if (streak >= 30) {
    return { flames: 2, color: 'text-orange-400', label: 'Amazing streak!' }
  }
  if (streak >= 7) {
    return { flames: 1, color: 'text-orange-400', label: 'Great streak!' }
  }
  if (streak >= 1) {
    return { flames: 1, color: 'text-amber-400', label: 'Keep it going!' }
  }
  return { flames: 0, color: 'text-muted-foreground', label: 'Start your streak!' }
}

export function StreakBadge({ streak, className, showTooltip = true }: StreakBadgeProps) {
  const { flames, color, label } = getStreakLevel(streak)

  if (streak === 0) {
    return null
  }

  const badge = (
    <div
      className={cn(
        'flex items-center gap-1 text-sm font-medium transition-transform',
        color,
        className
      )}
    >
      <div className="flex -space-x-1">
        {Array.from({ length: flames }).map((_, i) => (
          <Flame
            key={i}
            className={cn(
              'size-4 animate-pulse',
              i > 0 && 'opacity-80'
            )}
            style={{
              animationDelay: `${i * 150}ms`,
              animationDuration: '1.5s',
            }}
          />
        ))}
      </div>
      <span className="tabular-nums">{streak}</span>
    </div>
  )

  if (!showTooltip) {
    return badge
  }

  return (
    <Tooltip>
      <TooltipTrigger asChild>
        <div className="cursor-default">{badge}</div>
      </TooltipTrigger>
      <TooltipContent>
        <p className="font-medium">{streak} day streak</p>
        <p className="text-xs text-muted-foreground">{label}</p>
      </TooltipContent>
    </Tooltip>
  )
}
