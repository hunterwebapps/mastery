import { Battery, BatteryFull, BatteryLow, BatteryMedium, BatteryWarning } from 'lucide-react'
import { cn } from '@/lib/utils'
import { Tooltip, TooltipContent, TooltipTrigger } from '@/components/ui/tooltip'
import { energyCostInfo } from '@/types/task'

interface EnergyIndicatorProps {
  energyCost: number
  showLabel?: boolean
  className?: string
}

export function EnergyIndicator({ energyCost, showLabel = false, className }: EnergyIndicatorProps) {
  const info = energyCostInfo[energyCost] || energyCostInfo[3]

  const Icon = (() => {
    switch (energyCost) {
      case 1:
        return BatteryFull
      case 2:
        return BatteryMedium
      case 3:
        return Battery
      case 4:
        return BatteryLow
      case 5:
        return BatteryWarning
      default:
        return Battery
    }
  })()

  const indicator = (
    <div className={cn('flex items-center gap-1', className)}>
      <Icon className={cn('size-4', info.color)} />
      {showLabel && <span className={cn('text-xs', info.color)}>{info.label}</span>}
    </div>
  )

  if (showLabel) {
    return indicator
  }

  return (
    <Tooltip>
      <TooltipTrigger asChild>
        {indicator}
      </TooltipTrigger>
      <TooltipContent>
        <span>Energy: {info.label}</span>
      </TooltipContent>
    </Tooltip>
  )
}
