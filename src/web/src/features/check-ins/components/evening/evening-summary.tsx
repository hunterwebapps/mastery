import { Button } from '@/components/ui/button'
import { EnergyBadge } from '../common/energy-badge'
import { Moon, Target, AlertTriangle, MessageSquare, Zap, Brain } from 'lucide-react'
import { blockerCategoryInfo } from '@/types/check-in'
import type { BlockerCategory } from '@/types/check-in'
import { cn } from '@/lib/utils'

interface EveningSummaryProps {
  top1Completed?: boolean
  energyLevelPm?: number
  stressLevel?: number
  reflection?: string
  blockerCategory?: BlockerCategory
  blockerNote?: string
  onSubmit: () => void
  isSubmitting: boolean
}

const stressLabels: Record<number, string> = {
  1: 'Calm', 2: 'Mild', 3: 'Moderate', 4: 'High', 5: 'Intense',
}

export function EveningSummary({
  top1Completed,
  energyLevelPm,
  stressLevel,
  reflection,
  blockerCategory,
  blockerNote,
  onSubmit,
  isSubmitting,
}: EveningSummaryProps) {
  return (
    <div className="space-y-6 animate-in fade-in duration-300">
      <div className="text-center space-y-2">
        <div className="flex items-center justify-center gap-2 text-primary">
          <Moon className="size-5" />
          <h2 className="text-2xl font-semibold">Day complete</h2>
        </div>
        <p className="text-sm text-muted-foreground">
          Here's your evening wrap-up. Rest well.
        </p>
      </div>

      <div className="space-y-3">
        {/* Top 1 result */}
        {top1Completed !== undefined && (
          <div className="flex items-center gap-3 rounded-xl border border-border/50 bg-card p-4">
            <div className={cn(
              'flex size-10 items-center justify-center rounded-lg',
              top1Completed ? 'bg-green-500/10' : 'bg-orange-500/10'
            )}>
              <Target className={cn(
                'size-5',
                top1Completed ? 'text-green-400' : 'text-orange-400'
              )} />
            </div>
            <div>
              <p className="text-xs text-muted-foreground">Top 1</p>
              <p className={cn(
                'text-sm font-medium',
                top1Completed ? 'text-green-400' : 'text-orange-400'
              )}>
                {top1Completed ? 'Completed' : 'Not completed'}
              </p>
            </div>
          </div>
        )}

        {/* Energy PM */}
        {energyLevelPm && (
          <div className="flex items-center gap-3 rounded-xl border border-border/50 bg-card p-4">
            <div className="flex size-10 items-center justify-center rounded-lg bg-blue-500/10">
              <Zap className="size-5 text-blue-400" />
            </div>
            <div>
              <p className="text-xs text-muted-foreground">Evening energy</p>
              <EnergyBadge level={energyLevelPm} size="sm" />
            </div>
          </div>
        )}

        {/* Stress */}
        {stressLevel && (
          <div className="flex items-center gap-3 rounded-xl border border-border/50 bg-card p-4">
            <div className="flex size-10 items-center justify-center rounded-lg bg-purple-500/10">
              <Brain className="size-5 text-purple-400" />
            </div>
            <div>
              <p className="text-xs text-muted-foreground">Stress</p>
              <p className="text-sm font-medium text-foreground">
                {stressLabels[stressLevel]} ({stressLevel}/5)
              </p>
            </div>
          </div>
        )}

        {/* Blocker */}
        {blockerCategory && (
          <div className="flex items-center gap-3 rounded-xl border border-border/50 bg-card p-4">
            <div className="flex size-10 items-center justify-center rounded-lg bg-warning/10">
              <AlertTriangle className="size-5 text-warning" />
            </div>
            <div className="flex-1 min-w-0">
              <p className="text-xs text-muted-foreground">Blocker</p>
              <p className="text-sm font-medium text-foreground">
                {blockerCategoryInfo[blockerCategory].emoji} {blockerCategoryInfo[blockerCategory].label}
              </p>
              {blockerNote && (
                <p className="text-xs text-muted-foreground mt-0.5 truncate">{blockerNote}</p>
              )}
            </div>
          </div>
        )}

        {/* Reflection */}
        {reflection && (
          <div className="rounded-xl border border-border/50 bg-card p-4">
            <div className="flex items-center gap-2 mb-1">
              <MessageSquare className="size-3.5 text-muted-foreground" />
              <p className="text-xs text-muted-foreground">Reflection</p>
            </div>
            <p className="text-sm text-foreground italic">"{reflection}"</p>
          </div>
        )}
      </div>

      <Button
        onClick={onSubmit}
        disabled={isSubmitting}
        className="w-full h-12 text-base font-semibold"
        size="lg"
      >
        {isSubmitting ? 'Saving...' : 'Wrap up your day'}
      </Button>
    </div>
  )
}
