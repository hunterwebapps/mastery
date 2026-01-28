import { useState } from 'react'
import { Calendar, ChevronRight, Loader2 } from 'lucide-react'
import { format, addDays, nextSaturday, nextMonday } from 'date-fns'
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog'
import { Button } from '@/components/ui/button'
import { Calendar as CalendarComponent } from '@/components/ui/calendar'
import { Popover, PopoverContent, PopoverTrigger } from '@/components/ui/popover'
import { cn } from '@/lib/utils'
import type { RescheduleReason } from '@/types/task'
import { rescheduleReasonInfo } from '@/types/task'

interface RescheduleDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  taskTitle: string
  onReschedule: (newDate: string, reason?: RescheduleReason) => Promise<void>
  isLoading?: boolean
}

const quickOptions = [
  { label: 'Tomorrow', getDate: () => addDays(new Date(), 1) },
  { label: 'This Weekend', getDate: () => nextSaturday(new Date()) },
  { label: 'Next Week', getDate: () => nextMonday(addDays(new Date(), 7)) },
]

const reasons: RescheduleReason[] = [
  'NoTime',
  'TooTired',
  'Blocked',
  'ScopeTooBig',
  'WaitingOnSomeone',
  'Other',
]

export function RescheduleDialog({
  open,
  onOpenChange,
  taskTitle,
  onReschedule,
  isLoading = false,
}: RescheduleDialogProps) {
  const [selectedDate, setSelectedDate] = useState<Date>()
  const [selectedReason, setSelectedReason] = useState<RescheduleReason>()
  const [showCalendar, setShowCalendar] = useState(false)

  const handleQuickSelect = async (getDate: () => Date) => {
    const date = getDate()
    setSelectedDate(date)
    await onReschedule(format(date, 'yyyy-MM-dd'), selectedReason)
    onOpenChange(false)
  }

  const handleCustomDate = async () => {
    if (!selectedDate) return
    await onReschedule(format(selectedDate, 'yyyy-MM-dd'), selectedReason)
    onOpenChange(false)
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-md">
        <DialogHeader>
          <DialogTitle>Reschedule Task</DialogTitle>
          <DialogDescription className="truncate">
            {taskTitle}
          </DialogDescription>
        </DialogHeader>

        <div className="space-y-6 py-4">
          {/* Quick date options */}
          <div className="space-y-2">
            <label className="text-sm font-medium text-muted-foreground">Quick options</label>
            <div className="flex flex-wrap gap-2">
              {quickOptions.map((option) => (
                <Button
                  key={option.label}
                  variant="outline"
                  size="sm"
                  onClick={() => handleQuickSelect(option.getDate)}
                  disabled={isLoading}
                  className="flex-1 min-w-[100px]"
                >
                  {option.label}
                  <ChevronRight className="size-4 ml-1" />
                </Button>
              ))}
            </div>
          </div>

          {/* Custom date picker */}
          <div className="space-y-2">
            <label className="text-sm font-medium text-muted-foreground">Or pick a date</label>
            <Popover open={showCalendar} onOpenChange={setShowCalendar}>
              <PopoverTrigger asChild>
                <Button
                  variant="outline"
                  className={cn(
                    'w-full justify-start text-left font-normal',
                    !selectedDate && 'text-muted-foreground'
                  )}
                >
                  <Calendar className="mr-2 size-4" />
                  {selectedDate ? format(selectedDate, 'PPP') : 'Select date'}
                </Button>
              </PopoverTrigger>
              <PopoverContent className="w-auto p-0" align="start">
                <CalendarComponent
                  mode="single"
                  selected={selectedDate}
                  onSelect={(date) => {
                    setSelectedDate(date)
                    setShowCalendar(false)
                  }}
                  disabled={(date) => date < new Date()}
                  initialFocus
                />
              </PopoverContent>
            </Popover>
          </div>

          {/* Reason selection (optional) */}
          <div className="space-y-2">
            <label className="text-sm font-medium text-muted-foreground">
              Why are you rescheduling? <span className="text-xs">(optional)</span>
            </label>
            <div className="flex flex-wrap gap-2">
              {reasons.map((reason) => {
                const info = rescheduleReasonInfo[reason]
                return (
                  <Button
                    key={reason}
                    variant={selectedReason === reason ? 'secondary' : 'outline'}
                    size="sm"
                    onClick={() => setSelectedReason(
                      selectedReason === reason ? undefined : reason
                    )}
                    className="text-xs"
                  >
                    <span className="mr-1">{info.emoji}</span>
                    {info.label}
                  </Button>
                )
              })}
            </div>
          </div>

          {/* Submit button for custom date */}
          {selectedDate && (
            <Button
              onClick={handleCustomDate}
              disabled={isLoading}
              className="w-full"
            >
              {isLoading ? (
                <>
                  <Loader2 className="size-4 mr-2 animate-spin" />
                  Rescheduling...
                </>
              ) : (
                <>
                  Reschedule to {format(selectedDate, 'MMM d')}
                </>
              )}
            </Button>
          )}
        </div>
      </DialogContent>
    </Dialog>
  )
}
