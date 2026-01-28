import { useMemo, useState } from 'react'
import { ChevronLeft, ChevronRight } from 'lucide-react'
import { cn } from '@/lib/utils'
import { Button } from '@/components/ui/button'
import {
  Tooltip,
  TooltipContent,
  TooltipProvider,
  TooltipTrigger,
} from '@/components/ui/tooltip'
import type { HabitOccurrenceDto, HabitOccurrenceStatus } from '@/types/habit'

interface HabitCalendarProps {
  occurrences: HabitOccurrenceDto[]
  fromDate: string
  toDate: string
  className?: string
}

interface DayData {
  date: Date
  dateString: string
  occurrence?: HabitOccurrenceDto
  isInRange: boolean
  isToday: boolean
}

const DAYS_OF_WEEK = ['Sun', 'Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat']
const MONTHS = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec']

function getStatusColor(status?: HabitOccurrenceStatus): string {
  if (!status) return 'bg-muted/30 hover:bg-muted/50'

  switch (status) {
    case 'Completed':
      return 'bg-green-500 hover:bg-green-400'
    case 'Skipped':
      return 'bg-yellow-500/60 hover:bg-yellow-500/80'
    case 'Missed':
      return 'bg-red-500/60 hover:bg-red-500/80'
    case 'Pending':
      return 'bg-blue-500/30 hover:bg-blue-500/50'
    case 'Rescheduled':
      return 'bg-purple-500/60 hover:bg-purple-500/80'
    default:
      return 'bg-muted/30 hover:bg-muted/50'
  }
}

function getStatusLabel(status?: HabitOccurrenceStatus): string {
  if (!status) return 'No data'

  switch (status) {
    case 'Completed':
      return 'Completed'
    case 'Skipped':
      return 'Skipped'
    case 'Missed':
      return 'Missed'
    case 'Pending':
      return 'Pending'
    case 'Rescheduled':
      return 'Rescheduled'
    default:
      return 'Unknown'
  }
}

function formatDateForDisplay(date: Date): string {
  return `${MONTHS[date.getMonth()]} ${date.getDate()}, ${date.getFullYear()}`
}

function formatDateString(date: Date): string {
  const year = date.getFullYear()
  const month = String(date.getMonth() + 1).padStart(2, '0')
  const day = String(date.getDate()).padStart(2, '0')
  return `${year}-${month}-${day}`
}

function getWeeksInRange(startDate: Date, endDate: Date): Date[][] {
  const weeks: Date[][] = []
  const current = new Date(startDate)

  // Start from Sunday of the week containing startDate
  current.setDate(current.getDate() - current.getDay())

  while (current <= endDate) {
    const week: Date[] = []
    for (let i = 0; i < 7; i++) {
      week.push(new Date(current))
      current.setDate(current.getDate() + 1)
    }
    weeks.push(week)
  }

  return weeks
}

export function HabitCalendar({ occurrences, fromDate, toDate, className }: HabitCalendarProps) {
  const [viewOffset, setViewOffset] = useState(0)
  const weeksToShow = 13 // About 3 months

  const { weeks, monthLabels } = useMemo(() => {
    const today = new Date()
    today.setHours(0, 0, 0, 0)

    // Calculate the end date based on offset
    const endDate = new Date(today)
    endDate.setDate(endDate.getDate() - (viewOffset * 7 * weeksToShow))

    // Calculate start date (weeksToShow weeks before end)
    const startDate = new Date(endDate)
    startDate.setDate(startDate.getDate() - (weeksToShow * 7))

    const rangeStart = new Date(fromDate)
    const rangeEnd = new Date(toDate)

    // Create occurrence lookup map
    const occurrenceMap = new Map<string, HabitOccurrenceDto>()
    occurrences.forEach((occ) => {
      occurrenceMap.set(occ.scheduledOn, occ)
    })

    // Generate weeks with data
    const weeks = getWeeksInRange(startDate, endDate)
    const weeksWithData: DayData[][] = weeks.map((week) =>
      week.map((date) => {
        const dateString = formatDateString(date)
        return {
          date,
          dateString,
          occurrence: occurrenceMap.get(dateString),
          isInRange: date >= rangeStart && date <= rangeEnd,
          isToday: formatDateString(date) === formatDateString(today),
        }
      })
    )

    // Generate month labels
    const monthLabels: { month: string; startWeek: number }[] = []
    let currentMonth = -1
    weeksWithData.forEach((week, weekIndex) => {
      const firstDayOfWeek = week[0]
      if (firstDayOfWeek.date.getMonth() !== currentMonth) {
        currentMonth = firstDayOfWeek.date.getMonth()
        monthLabels.push({
          month: MONTHS[currentMonth],
          startWeek: weekIndex,
        })
      }
    })

    return { weeks: weeksWithData, monthLabels }
  }, [occurrences, fromDate, toDate, viewOffset, weeksToShow])

  const canGoNewer = viewOffset > 0
  const canGoOlder = true // Can always go older (though may have no data)

  return (
    <div className={cn('', className)}>
      {/* Navigation */}
      <div className="flex items-center justify-between mb-4">
        <div className="flex items-center gap-2">
          <Button
            variant="ghost"
            size="icon"
            onClick={() => setViewOffset(viewOffset + 1)}
            disabled={!canGoOlder}
          >
            <ChevronLeft className="size-4" />
          </Button>
          <Button
            variant="ghost"
            size="icon"
            onClick={() => setViewOffset(viewOffset - 1)}
            disabled={!canGoNewer}
          >
            <ChevronRight className="size-4" />
          </Button>
          {viewOffset > 0 && (
            <Button
              variant="ghost"
              size="sm"
              onClick={() => setViewOffset(0)}
              className="text-xs"
            >
              Today
            </Button>
          )}
        </div>

        {/* Legend */}
        <div className="flex items-center gap-3 text-xs text-muted-foreground">
          <div className="flex items-center gap-1">
            <div className="size-3 rounded-sm bg-muted/30" />
            <span>No data</span>
          </div>
          <div className="flex items-center gap-1">
            <div className="size-3 rounded-sm bg-green-500" />
            <span>Done</span>
          </div>
          <div className="flex items-center gap-1">
            <div className="size-3 rounded-sm bg-yellow-500/60" />
            <span>Skip</span>
          </div>
          <div className="flex items-center gap-1">
            <div className="size-3 rounded-sm bg-red-500/60" />
            <span>Miss</span>
          </div>
        </div>
      </div>

      {/* Month labels */}
      <div className="flex mb-1 pl-8">
        {monthLabels.map((label, idx) => (
          <div
            key={`${label.month}-${idx}`}
            className="text-xs text-muted-foreground"
            style={{
              marginLeft: idx === 0 ? `${label.startWeek * 14}px` : undefined,
              width: monthLabels[idx + 1]
                ? `${(monthLabels[idx + 1].startWeek - label.startWeek) * 14}px`
                : 'auto',
            }}
          >
            {label.month}
          </div>
        ))}
      </div>

      {/* Calendar grid */}
      <div className="flex">
        {/* Day labels */}
        <div className="flex flex-col gap-0.5 mr-1 text-xs text-muted-foreground">
          {DAYS_OF_WEEK.map((day, idx) => (
            <div
              key={day}
              className="h-3 flex items-center justify-end pr-1"
              style={{ visibility: idx % 2 === 1 ? 'visible' : 'hidden' }}
            >
              {day}
            </div>
          ))}
        </div>

        {/* Grid */}
        <div className="flex gap-0.5">
          <TooltipProvider delayDuration={100}>
            {weeks.map((week, weekIdx) => (
              <div key={weekIdx} className="flex flex-col gap-0.5">
                {week.map((day) => (
                  <Tooltip key={day.dateString}>
                    <TooltipTrigger asChild>
                      <button
                        className={cn(
                          'size-3 rounded-sm transition-colors',
                          getStatusColor(day.occurrence?.status),
                          day.isToday && 'ring-1 ring-primary ring-offset-1 ring-offset-background',
                          !day.isInRange && 'opacity-30'
                        )}
                        aria-label={`${formatDateForDisplay(day.date)}: ${getStatusLabel(day.occurrence?.status)}`}
                      />
                    </TooltipTrigger>
                    <TooltipContent side="top" className="text-xs">
                      <div className="font-medium">{formatDateForDisplay(day.date)}</div>
                      <div className={cn(
                        day.occurrence?.status === 'Completed' && 'text-green-400',
                        day.occurrence?.status === 'Missed' && 'text-red-400',
                        day.occurrence?.status === 'Skipped' && 'text-yellow-400',
                        day.occurrence?.status === 'Pending' && 'text-blue-400'
                      )}>
                        {getStatusLabel(day.occurrence?.status)}
                      </div>
                      {day.occurrence?.note && (
                        <div className="text-muted-foreground mt-1 max-w-48 truncate">
                          {day.occurrence.note}
                        </div>
                      )}
                    </TooltipContent>
                  </Tooltip>
                ))}
              </div>
            ))}
          </TooltipProvider>
        </div>
      </div>
    </div>
  )
}
