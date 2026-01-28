import {
  AlertTriangle,
  Target,
  Activity,
} from 'lucide-react'
import { cn } from '@/lib/utils'
import type { SignalType } from '@/types'
import { signalTypeInfo } from '@/types'

const signalIcons: Record<string, React.ElementType> = {
  AlertTriangle,
  Target,
  Activity,
}

function getSeverityColor(severity: number): { text: string; bg: string } {
  if (severity > 70) return { text: 'text-red-400', bg: 'bg-red-500/10' }
  if (severity >= 40) return { text: 'text-orange-400', bg: 'bg-orange-500/10' }
  return { text: 'text-yellow-400', bg: 'bg-yellow-500/10' }
}

interface SignalBadgeProps {
  type: SignalType
  severity: number
}

export function SignalBadge({ type, severity }: SignalBadgeProps) {
  const info = signalTypeInfo[type]
  const Icon = signalIcons[info.icon] ?? Activity
  const severityColor = getSeverityColor(severity)

  return (
    <span
      className={cn(
        'inline-flex items-center gap-1.5 rounded-full px-2.5 py-1 text-xs font-medium',
        severityColor.bg,
        severityColor.text
      )}
    >
      <Icon className="size-3 shrink-0" />
      <span className="truncate">{info.label}</span>
      <span className={cn(
        'ml-0.5 font-bold tabular-nums',
        severityColor.text
      )}>
        {severity}
      </span>
    </span>
  )
}
