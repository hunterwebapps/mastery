import { Button } from '@/components/ui/button'
import { EnergyBadge } from '../common/energy-badge'
import { Sparkles, Sun, Target, Zap } from 'lucide-react'
import { energyLevelInfo, top1TypeInfo } from '@/types/check-in'
import type { Top1Type } from '@/types/check-in'
import { cn } from '@/lib/utils'

interface MorningSummaryProps {
  energyLevel: number
  selectedMode: string
  top1Type?: Top1Type
  top1FreeText?: string
  intention?: string
  onSubmit: () => void
  isSubmitting: boolean
}

export function MorningSummary({
  energyLevel,
  selectedMode,
  top1Type,
  top1FreeText,
  intention,
  onSubmit,
  isSubmitting,
}: MorningSummaryProps) {
  return (
    <div className="space-y-6 animate-in fade-in duration-300">
      <div className="text-center space-y-2">
        <div className="flex items-center justify-center gap-2 text-primary">
          <Sparkles className="size-5" />
          <h2 className="text-2xl font-semibold">Ready to go</h2>
        </div>
        <p className="text-sm text-muted-foreground">
          Here's your morning setup. Let's make it happen.
        </p>
      </div>

      <div className="space-y-3">
        {/* Energy */}
        <div className="flex items-center gap-3 rounded-xl border border-border/50 bg-card p-4">
          <div className={cn(
            'flex size-10 items-center justify-center rounded-lg',
            energyLevelInfo[energyLevel]?.bgColor
          )}>
            <Zap className={cn('size-5', energyLevelInfo[energyLevel]?.color)} />
          </div>
          <div className="flex-1">
            <p className="text-xs text-muted-foreground">Energy</p>
            <EnergyBadge level={energyLevel} size="sm" />
          </div>
        </div>

        {/* Mode */}
        <div className="flex items-center gap-3 rounded-xl border border-border/50 bg-card p-4">
          <div className="flex size-10 items-center justify-center rounded-lg bg-blue-500/10">
            <Sun className="size-5 text-blue-400" />
          </div>
          <div className="flex-1">
            <p className="text-xs text-muted-foreground">Day mode</p>
            <p className="text-sm font-medium text-foreground">{selectedMode}</p>
          </div>
        </div>

        {/* Top 1 */}
        {top1Type && (
          <div className="flex items-center gap-3 rounded-xl border border-border/50 bg-card p-4">
            <div className="flex size-10 items-center justify-center rounded-lg bg-primary/10">
              <Target className="size-5 text-primary" />
            </div>
            <div className="flex-1 min-w-0">
              <p className="text-xs text-muted-foreground">Top 1 ({top1TypeInfo[top1Type].label})</p>
              <p className="text-sm font-medium text-foreground truncate">
                {top1Type === 'FreeText' ? top1FreeText : `Selected ${top1Type.toLowerCase()}`}
              </p>
            </div>
          </div>
        )}

        {/* Intention */}
        {intention && (
          <div className="rounded-xl border border-border/50 bg-card p-4">
            <p className="text-xs text-muted-foreground mb-1">Intention</p>
            <p className="text-sm text-foreground italic">"{intention}"</p>
          </div>
        )}
      </div>

      <Button
        onClick={onSubmit}
        disabled={isSubmitting}
        className="w-full h-12 text-base font-semibold"
        size="lg"
      >
        {isSubmitting ? 'Saving...' : 'Start your day'}
      </Button>
    </div>
  )
}
