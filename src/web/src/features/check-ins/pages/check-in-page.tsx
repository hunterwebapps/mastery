import { useState, useMemo } from 'react'
import { Flame, Sun, Moon } from 'lucide-react'
import { useTodayCheckInState, useCheckIns, useSkipCheckIn } from '../hooks/use-check-ins'
import { useTodayTasks } from '@/features/tasks/hooks/use-tasks'
import { useTodayHabits } from '@/features/habits/hooks/use-habits'
import { useProjects } from '@/features/projects/hooks/use-projects'
import { MorningCheckInFlow } from '../components/morning/morning-check-in-flow'
import { EveningCheckInFlow } from '../components/evening/evening-check-in-flow'
import { CheckInStatusBanner } from '../components/common/check-in-status-banner'
import { CheckInHistoryList } from '../components/common/check-in-history-list'
import { Separator } from '@/components/ui/separator'

type ActiveFlow = 'none' | 'morning' | 'evening'

export function Component() {
  const [activeFlow, setActiveFlow] = useState<ActiveFlow>('none')
  const { data: todayState, isLoading } = useTodayCheckInState()
  const { data: history } = useCheckIns()
  const skipCheckIn = useSkipCheckIn()

  const { data: todayTasks } = useTodayTasks()
  const { data: todayHabits } = useTodayHabits()
  const { data: projects } = useProjects({ status: 'Active' })

  const morningDone = todayState?.morningStatus === 'Completed' || todayState?.morningStatus === 'Skipped'
  const eveningDone = todayState?.eveningStatus === 'Completed' || todayState?.eveningStatus === 'Skipped'

  const handleFlowComplete = () => {
    setActiveFlow('none')
  }

  const handleSkip = (type: 'Morning' | 'Evening') => {
    skipCheckIn.mutate({ type })
    setActiveFlow('none')
  }

  // Resolve morning Top 1 entity name for evening review
  const morningTop1Description = useMemo(() => {
    const morning = todayState?.morningCheckIn
    if (!morning?.top1Type) return undefined
    if (morning.top1Type === 'FreeText') return morning.top1FreeText

    const entityId = morning.top1EntityId
    if (!entityId) return undefined

    if (morning.top1Type === 'Task') {
      return todayTasks?.find(t => t.id === entityId)?.title
    }
    if (morning.top1Type === 'Habit') {
      return todayHabits?.find(h => h.id === entityId)?.title
    }
    if (morning.top1Type === 'Project') {
      return projects?.find(p => p.id === entityId)?.title
    }
    return undefined
  }, [todayState, todayTasks, todayHabits, projects])

  const morningTop1Type = todayState?.morningCheckIn?.top1Type

  if (isLoading) {
    return (
      <div className="min-h-screen bg-background">
        <div className="container max-w-2xl py-8 px-4 sm:px-6">
          <div className="animate-pulse space-y-4">
            <div className="h-8 w-48 bg-muted rounded" />
            <div className="h-24 bg-muted rounded-xl" />
            <div className="h-24 bg-muted rounded-xl" />
          </div>
        </div>
      </div>
    )
  }

  // Active flow view
  if (activeFlow === 'morning') {
    return (
      <div className="min-h-screen bg-background">
        <div className="container max-w-2xl py-8 px-4 sm:px-6">
          <div className="flex items-center gap-2 mb-6">
            <Sun className="size-5 text-orange-400" />
            <h1 className="text-xl font-semibold text-foreground">Morning Check-in</h1>
          </div>
          <MorningCheckInFlow
            onComplete={handleFlowComplete}
            onSkip={() => handleSkip('Morning')}
          />
        </div>
      </div>
    )
  }

  if (activeFlow === 'evening') {
    return (
      <div className="min-h-screen bg-background">
        <div className="container max-w-2xl py-8 px-4 sm:px-6">
          <div className="flex items-center gap-2 mb-6">
            <Moon className="size-5 text-indigo-400" />
            <h1 className="text-xl font-semibold text-foreground">Evening Check-in</h1>
          </div>
          <EveningCheckInFlow
            onComplete={handleFlowComplete}
            onSkip={() => handleSkip('Evening')}
            morningTop1Description={morningTop1Description}
            morningTop1Type={morningTop1Type}
          />
        </div>
      </div>
    )
  }

  // Default dashboard view
  return (
    <div className="min-h-screen bg-background">
      <div className="container max-w-2xl py-8 px-4 sm:px-6">
        {/* Header */}
        <div className="flex items-center gap-3 mb-6">
          <div className="flex size-10 items-center justify-center rounded-lg bg-primary/10">
            <Flame className="size-5 text-primary" />
          </div>
          <div>
            <h1 className="text-2xl font-bold text-foreground">Daily Check-in</h1>
            <p className="text-sm text-muted-foreground">
              {todayState?.checkInStreakDays
                ? `${todayState.checkInStreakDays} day streak`
                : 'Start your streak today'}
            </p>
          </div>
        </div>

        {/* Today's status */}
        <div className="space-y-3 mb-8">
          <CheckInStatusBanner
            type="morning"
            status={todayState?.morningStatus ?? 'Pending'}
            onStart={!morningDone ? () => setActiveFlow('morning') : undefined}
          />
          <CheckInStatusBanner
            type="evening"
            status={todayState?.eveningStatus ?? 'Pending'}
            onStart={!eveningDone ? () => setActiveFlow('evening') : undefined}
          />
        </div>

        {/* Today summary when both are done */}
        {morningDone && eveningDone && (
          <div className="rounded-xl border-2 border-green-500/30 bg-green-500/5 p-5 text-center mb-8 animate-in fade-in duration-300">
            <p className="text-lg font-semibold text-green-400">All done for today!</p>
            <p className="text-sm text-muted-foreground mt-1">
              Great job completing both check-ins. See you tomorrow.
            </p>
          </div>
        )}

        {/* History */}
        <Separator className="my-6" />
        <div className="space-y-4">
          <h2 className="text-lg font-semibold text-foreground">Recent history</h2>
          <CheckInHistoryList checkIns={history ?? []} />
        </div>
      </div>
    </div>
  )
}

Component.displayName = 'CheckInPage'
