import { useState } from 'react'
import { cn } from '@/lib/utils'
import { Sun, Moon, Check, SkipForward } from 'lucide-react'
import {
  Sheet,
  SheetContent,
  SheetHeader,
  SheetTitle,
  SheetDescription,
} from '@/components/ui/sheet'
import { EnergyBadge } from './energy-badge'
import { CheckInDetailSheet } from './check-in-detail-sheet'
import type { CheckInSummaryDto } from '@/types/check-in'

interface CheckInHistoryListProps {
  checkIns: CheckInSummaryDto[]
}

export function CheckInHistoryList({ checkIns }: CheckInHistoryListProps) {
  const [selectedId, setSelectedId] = useState<string | null>(null)

  if (checkIns.length === 0) {
    return (
      <div className="text-center py-8 text-muted-foreground">
        <p className="text-sm">No check-ins yet. Start your first one!</p>
      </div>
    )
  }

  // Group by date
  const grouped = checkIns.reduce<Record<string, CheckInSummaryDto[]>>((acc, ci) => {
    const date = ci.checkInDate
    if (!acc[date]) acc[date] = []
    acc[date].push(ci)
    return acc
  }, {})

  return (
    <>
      <div className="space-y-4">
        {Object.entries(grouped).map(([date, dayCheckIns]) => {
          const morning = dayCheckIns.find(c => c.type === 'Morning')
          const evening = dayCheckIns.find(c => c.type === 'Evening')

          return (
            <div key={date} className="rounded-xl border border-border/50 bg-card p-4 space-y-3">
              <p className="text-sm font-medium text-foreground">
                {formatDate(date)}
              </p>

              <div className="grid grid-cols-2 gap-3">
                {/* Morning */}
                <button
                  disabled={!morning}
                  onClick={() => morning && setSelectedId(morning.id)}
                  className={cn(
                    'flex items-center gap-2 rounded-lg p-2 text-left transition-colors',
                    morning
                      ? 'bg-orange-500/5 hover:bg-orange-500/10 cursor-pointer'
                      : 'bg-muted/30 cursor-default'
                  )}
                >
                  <Sun className={cn(
                    'size-4',
                    morning ? 'text-orange-400' : 'text-muted-foreground/50'
                  )} />
                  <div className="flex-1 min-w-0">
                    <p className="text-xs text-muted-foreground">Morning</p>
                    {morning ? (
                      <div className="flex items-center gap-1.5">
                        {morning.status === 'Completed' ? (
                          <>
                            <Check className="size-3 text-green-400" />
                            {morning.energyLevel && (
                              <EnergyBadge level={morning.energyLevel} size="sm" showLabel={false} />
                            )}
                            {morning.selectedMode && (
                              <span className="text-xs text-muted-foreground">{morning.selectedMode}</span>
                            )}
                          </>
                        ) : (
                          <span className="text-xs text-muted-foreground flex items-center gap-1">
                            <SkipForward className="size-3" /> Skipped
                          </span>
                        )}
                      </div>
                    ) : (
                      <span className="text-xs text-muted-foreground/50">--</span>
                    )}
                  </div>
                </button>

                {/* Evening */}
                <button
                  disabled={!evening}
                  onClick={() => evening && setSelectedId(evening.id)}
                  className={cn(
                    'flex items-center gap-2 rounded-lg p-2 text-left transition-colors',
                    evening
                      ? 'bg-indigo-500/5 hover:bg-indigo-500/10 cursor-pointer'
                      : 'bg-muted/30 cursor-default'
                  )}
                >
                  <Moon className={cn(
                    'size-4',
                    evening ? 'text-indigo-400' : 'text-muted-foreground/50'
                  )} />
                  <div className="flex-1 min-w-0">
                    <p className="text-xs text-muted-foreground">Evening</p>
                    {evening ? (
                      <div className="flex items-center gap-1.5">
                        {evening.status === 'Completed' ? (
                          <>
                            <Check className="size-3 text-green-400" />
                            {evening.top1Completed !== undefined && (
                              <span className={cn(
                                'text-xs',
                                evening.top1Completed ? 'text-green-400' : 'text-orange-400'
                              )}>
                                Top 1: {evening.top1Completed ? 'Done' : 'Missed'}
                              </span>
                            )}
                          </>
                        ) : (
                          <span className="text-xs text-muted-foreground flex items-center gap-1">
                            <SkipForward className="size-3" /> Skipped
                          </span>
                        )}
                      </div>
                    ) : (
                      <span className="text-xs text-muted-foreground/50">--</span>
                    )}
                  </div>
                </button>
              </div>
            </div>
          )
        })}
      </div>

      <Sheet open={!!selectedId} onOpenChange={(open) => { if (!open) setSelectedId(null) }}>
        <SheetContent side="right" className="w-full sm:max-w-md overflow-y-auto">
          <SheetHeader>
            <SheetTitle>Check-in Details</SheetTitle>
            <SheetDescription>
              Review your check-in responses
            </SheetDescription>
          </SheetHeader>
          {selectedId && (
            <div className="mt-4">
              <CheckInDetailSheet checkInId={selectedId} />
            </div>
          )}
        </SheetContent>
      </Sheet>
    </>
  )
}

function formatDate(dateStr: string): string {
  const date = new Date(dateStr + 'T00:00:00')
  const today = new Date()
  today.setHours(0, 0, 0, 0)
  const yesterday = new Date(today)
  yesterday.setDate(yesterday.getDate() - 1)

  const dateOnly = new Date(date)
  dateOnly.setHours(0, 0, 0, 0)

  if (dateOnly.getTime() === today.getTime()) return 'Today'
  if (dateOnly.getTime() === yesterday.getTime()) return 'Yesterday'

  return date.toLocaleDateString('en-US', {
    weekday: 'short',
    month: 'short',
    day: 'numeric',
  })
}
