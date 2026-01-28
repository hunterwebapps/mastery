import { useMemo } from 'react'
import { useParams, Link } from 'react-router-dom'
import { ArrowLeft, Pencil, Loader2 } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import { useHabit, useHabitHistory } from '../hooks/use-habits'
import { StreakBadge } from '../components/common/streak-badge'
import { HabitCalendar } from '../components/habit-detail'
import { habitStatusInfo, habitModeInfo, scheduleTypeInfo } from '@/types/habit'
import { cn } from '@/lib/utils'

function formatDateString(date: Date): string {
  const year = date.getFullYear()
  const month = String(date.getMonth() + 1).padStart(2, '0')
  const day = String(date.getDate()).padStart(2, '0')
  return `${year}-${month}-${day}`
}

export function Component() {
  const { id } = useParams<{ id: string }>()
  const { data: habit, isLoading, error } = useHabit(id ?? '')

  // Calculate date range for history (last 6 months)
  const { fromDate, toDate } = useMemo(() => {
    const today = new Date()
    const sixMonthsAgo = new Date(today)
    sixMonthsAgo.setMonth(sixMonthsAgo.getMonth() - 6)
    return {
      fromDate: formatDateString(sixMonthsAgo),
      toDate: formatDateString(today),
    }
  }, [])

  // Fetch habit history for calendar
  const { data: history, isLoading: historyLoading } = useHabitHistory(id ?? '', fromDate, toDate)

  if (isLoading) {
    return (
      <div className="min-h-screen bg-background flex items-center justify-center">
        <Loader2 className="size-8 animate-spin text-muted-foreground" />
      </div>
    )
  }

  if (error || !habit) {
    return (
      <div className="min-h-screen bg-background">
        <div className="container max-w-4xl py-8 px-4 sm:px-6 lg:px-8">
          <div className="text-center py-12">
            <h1 className="text-2xl font-bold mb-4">Habit Not Found</h1>
            <p className="text-muted-foreground mb-6">
              The habit you're looking for doesn't exist or has been deleted.
            </p>
            <Button asChild>
              <Link to="/habits">Back to Habits</Link>
            </Button>
          </div>
        </div>
      </div>
    )
  }

  const statusInfo = habitStatusInfo[habit.status]
  const modeInfo = habitModeInfo[habit.defaultMode]

  return (
    <div className="min-h-screen bg-background">
      <div className="container max-w-4xl py-8 px-4 sm:px-6 lg:px-8">
        {/* Header */}
        <div className="flex items-center justify-between mb-6">
          <Button variant="ghost" size="sm" asChild>
            <Link to="/habits">
              <ArrowLeft className="size-4 mr-2" />
              Back to Habits
            </Link>
          </Button>

          <Button variant="outline" size="sm" asChild>
            <Link to={`/habits/${habit.id}/edit`}>
              <Pencil className="size-4 mr-2" />
              Edit
            </Link>
          </Button>
        </div>

        {/* Hero section */}
        <div className="mb-8">
          <div className="flex items-start justify-between gap-4">
            <div>
              <h1 className="text-3xl font-bold mb-2">{habit.title}</h1>
              {habit.description && (
                <p className="text-muted-foreground text-lg mb-4">
                  {habit.description}
                </p>
              )}
              <div className="flex flex-wrap items-center gap-2">
                <Badge className={cn(statusInfo.color, statusInfo.bgColor)}>
                  {statusInfo.label}
                </Badge>
                <Badge variant="outline" className={cn(modeInfo.color)}>
                  {modeInfo.label} mode
                </Badge>
              </div>
            </div>
            <StreakBadge streak={habit.currentStreak} className="text-xl" />
          </div>
        </div>

        {/* Stats grid */}
        <div className="grid gap-4 sm:grid-cols-3 mb-8">
          <Card>
            <CardHeader className="pb-2">
              <CardTitle className="text-sm font-medium text-muted-foreground">
                Current Streak
              </CardTitle>
            </CardHeader>
            <CardContent>
              <div className="text-2xl font-bold">{habit.currentStreak} days</div>
            </CardContent>
          </Card>

          <Card>
            <CardHeader className="pb-2">
              <CardTitle className="text-sm font-medium text-muted-foreground">
                7-Day Adherence
              </CardTitle>
            </CardHeader>
            <CardContent>
              <div className={cn(
                'text-2xl font-bold',
                habit.adherenceRate7Day >= 0.8 && 'text-green-400',
                habit.adherenceRate7Day >= 0.5 && habit.adherenceRate7Day < 0.8 && 'text-yellow-400',
                habit.adherenceRate7Day < 0.5 && 'text-orange-400'
              )}>
                {Math.round(habit.adherenceRate7Day * 100)}%
              </div>
            </CardContent>
          </Card>

          <Card>
            <CardHeader className="pb-2">
              <CardTitle className="text-sm font-medium text-muted-foreground">
                Schedule
              </CardTitle>
            </CardHeader>
            <CardContent>
              <div className="text-lg font-medium">
                {scheduleTypeInfo[habit.schedule.type]?.label || habit.schedule.type}
              </div>
            </CardContent>
          </Card>
        </div>

        {/* Why section */}
        {habit.why && (
          <Card className="mb-8">
            <CardHeader>
              <CardTitle>Why This Habit?</CardTitle>
            </CardHeader>
            <CardContent>
              <p className="text-muted-foreground">{habit.why}</p>
            </CardContent>
          </Card>
        )}

        {/* Variants section */}
        {habit.variants.length > 0 && (
          <Card className="mb-8">
            <CardHeader>
              <CardTitle>Mode Variants</CardTitle>
            </CardHeader>
            <CardContent>
              <div className="grid gap-4 sm:grid-cols-3">
                {habit.variants.map((variant) => (
                  <div
                    key={variant.id}
                    className={cn(
                      'p-4 rounded-lg border',
                      habitModeInfo[variant.mode].bgColor
                    )}
                  >
                    <div className={cn('font-medium', habitModeInfo[variant.mode].color)}>
                      {variant.label}
                    </div>
                    <div className="text-sm text-muted-foreground mt-1">
                      {variant.estimatedMinutes} min · Energy: {variant.energyCost}/5
                    </div>
                  </div>
                ))}
              </div>
            </CardContent>
          </Card>
        )}

        {/* Metric bindings */}
        {habit.metricBindings.length > 0 && (
          <Card className="mb-8">
            <CardHeader>
              <CardTitle>Linked Metrics</CardTitle>
            </CardHeader>
            <CardContent>
              <div className="space-y-2">
                {habit.metricBindings.map((binding) => (
                  <div
                    key={binding.id}
                    className="flex items-center justify-between p-3 rounded-lg bg-muted/50"
                  >
                    <span className="font-medium">
                      {binding.metricName || binding.metricDefinitionId}
                    </span>
                    <Badge variant="outline">{binding.contributionType}</Badge>
                  </div>
                ))}
              </div>
            </CardContent>
          </Card>
        )}

        {/* History Calendar */}
        <Card>
          <CardHeader>
            <CardTitle>History</CardTitle>
          </CardHeader>
          <CardContent>
            {historyLoading ? (
              <div className="flex items-center justify-center py-8">
                <Loader2 className="size-6 animate-spin text-muted-foreground" />
              </div>
            ) : history ? (
              <>
                {/* Summary stats */}
                <div className="grid grid-cols-4 gap-4 mb-6">
                  <div className="text-center">
                    <div className="text-2xl font-bold text-green-400">{history.totalCompleted}</div>
                    <div className="text-xs text-muted-foreground">Completed</div>
                  </div>
                  <div className="text-center">
                    <div className="text-2xl font-bold text-red-400">{history.totalMissed}</div>
                    <div className="text-xs text-muted-foreground">Missed</div>
                  </div>
                  <div className="text-center">
                    <div className="text-2xl font-bold text-yellow-400">{history.totalSkipped}</div>
                    <div className="text-xs text-muted-foreground">Skipped</div>
                  </div>
                  <div className="text-center">
                    <div className="text-2xl font-bold text-muted-foreground">{history.totalDue}</div>
                    <div className="text-xs text-muted-foreground">Total Due</div>
                  </div>
                </div>
                {/* Calendar */}
                <HabitCalendar
                  occurrences={history.occurrences}
                  fromDate={fromDate}
                  toDate={toDate}
                />
              </>
            ) : (
              <p className="text-muted-foreground text-center py-8">
                No history data available.
              </p>
            )}
          </CardContent>
        </Card>
      </div>
    </div>
  )
}
