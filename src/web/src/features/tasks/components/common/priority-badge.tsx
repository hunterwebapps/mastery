import { Flag } from 'lucide-react'
import { cn } from '@/lib/utils'
import { Badge } from '@/components/ui/badge'
import { Tooltip, TooltipContent, TooltipTrigger } from '@/components/ui/tooltip'
import { priorityInfo } from '@/types/task'

interface PriorityBadgeProps {
  priority: number
  showLabel?: boolean
  className?: string
}

export function PriorityBadge({ priority, showLabel = false, className }: PriorityBadgeProps) {
  const info = priorityInfo[priority] || priorityInfo[3]

  // Only show badge for high priority items (1-2)
  if (priority > 2 && !showLabel) return null

  const badge = (
    <Badge
      variant="outline"
      className={cn('px-1.5 py-0', info.color, info.bgColor, className)}
    >
      <Flag className="size-3 mr-1" />
      {showLabel && <span className="text-xs">{info.label}</span>}
    </Badge>
  )

  if (showLabel) {
    return badge
  }

  return (
    <Tooltip>
      <TooltipTrigger asChild>
        {badge}
      </TooltipTrigger>
      <TooltipContent>
        <span>Priority: {info.label}</span>
      </TooltipContent>
    </Tooltip>
  )
}
