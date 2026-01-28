import { useState, useCallback } from 'react'
import {
  Check,
  RotateCcw,
  Calendar,
  ChevronRight,
  Clock,
  AlertCircle,
  Lock,
} from 'lucide-react'
import { Link } from 'react-router-dom'
import { cn } from '@/lib/utils'
import { Button } from '@/components/ui/button'
import { Card } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import { Tooltip, TooltipContent, TooltipTrigger } from '@/components/ui/tooltip'
import type { TodayTaskDto, RescheduleReason } from '@/types/task'
import { dueTypeInfo } from '@/types/task'
import { EnergyIndicator } from '../common/energy-indicator'
import { ContextTags } from '../common/context-tag-badge'
import { PriorityBadge } from '../common/priority-badge'
import { RescheduleDialog } from './reschedule-dialog'
import { CompleteDialog } from './complete-dialog'

interface TodayTaskCardProps {
  task: TodayTaskDto
  onComplete: (data: {
    completedOn: string
    actualMinutes?: number
    note?: string
    enteredValue?: number
  }) => Promise<void>
  onUndo: () => Promise<void>
  onReschedule: (newDate: string, reason?: RescheduleReason) => Promise<void>
}

export function TodayTaskCard({
  task,
  onComplete,
  onUndo,
  onReschedule,
}: TodayTaskCardProps) {
  const [isCompleting, setIsCompleting] = useState(false)
  const [isUndoing, setIsUndoing] = useState(false)
  const [isRescheduling, setIsRescheduling] = useState(false)
  const [showCompleteDialog, setShowCompleteDialog] = useState(false)
  const [showRescheduleDialog, setShowRescheduleDialog] = useState(false)

  const isCompleted = task.status === 'Completed'
  const isCancelled = task.status === 'Cancelled'
  const isDisabled = isCompleting || isUndoing || isRescheduling

  // Calculate if task should show overdue warning
  const showOverdueWarning = task.isOverdue && !isCompleted
  const showBlockedWarning = task.isBlocked && !isCompleted

  const handleQuickComplete = useCallback(async () => {
    // If task requires value entry, show dialog
    if (task.requiresValueEntry) {
      setShowCompleteDialog(true)
      return
    }

    setIsCompleting(true)
    try {
      const today = new Date().toISOString().split('T')[0]
      await onComplete({ completedOn: today })
    } finally {
      setIsCompleting(false)
    }
  }, [task.requiresValueEntry, onComplete])

  const handleDialogComplete = useCallback(async (data: {
    completedOn: string
    actualMinutes?: number
    note?: string
    enteredValue?: number
  }) => {
    setIsCompleting(true)
    try {
      await onComplete(data)
      setShowCompleteDialog(false)
    } finally {
      setIsCompleting(false)
    }
  }, [onComplete])

  const handleUndo = useCallback(async () => {
    setIsUndoing(true)
    try {
      await onUndo()
    } finally {
      setIsUndoing(false)
    }
  }, [onUndo])

  const handleReschedule = useCallback(async (newDate: string, reason?: RescheduleReason) => {
    setIsRescheduling(true)
    try {
      await onReschedule(newDate, reason)
      setShowRescheduleDialog(false)
    } finally {
      setIsRescheduling(false)
    }
  }, [onReschedule])

  return (
    <>
      <Card
        className={cn(
          'relative overflow-hidden transition-all duration-200',
          'hover:shadow-md hover:border-primary/30',
          isCompleted && 'border-green-500/30 bg-green-500/5',
          isCancelled && 'border-muted bg-muted/30 opacity-75',
          showOverdueWarning && 'border-red-500/50 animate-pulse',
          showBlockedWarning && 'opacity-60',
          isCompleting && 'scale-[0.98]'
        )}
      >
        <div className="p-4">
          <div className="flex items-start gap-3">
            {/* Completion button - large tap target */}
            <CompletionButton
              isCompleted={isCompleted}
              isCancelled={isCancelled}
              isBlocked={showBlockedWarning}
              isLoading={isCompleting}
              disabled={isDisabled || showBlockedWarning}
              onClick={handleQuickComplete}
            />

            {/* Task info */}
            <div className="flex-1 min-w-0">
              <div className="flex items-start justify-between gap-2">
                <div className="flex-1 min-w-0">
                  <Link
                    to={`/tasks/${task.id}`}
                    className="group flex items-center gap-1"
                  >
                    <h3 className={cn(
                      'font-medium truncate transition-colors',
                      isCompleted && 'text-green-400 line-through',
                      isCancelled && 'text-muted-foreground line-through',
                      !isCompleted && !isCancelled && 'group-hover:text-primary'
                    )}>
                      {task.title}
                    </h3>
                    <ChevronRight className="size-4 opacity-0 -ml-1 transition-all group-hover:opacity-50 group-hover:ml-0" />
                  </Link>

                  {task.description && (
                    <p className="text-sm text-muted-foreground truncate mt-0.5">
                      {task.description}
                    </p>
                  )}
                </div>

                {/* Status badges */}
                <div className="flex items-center gap-1.5">
                  <PriorityBadge priority={task.priority} />
                  {showOverdueWarning && (
                    <Badge variant="destructive" className="text-xs">
                      Overdue
                    </Badge>
                  )}
                  {task.dueType === 'Hard' && !showOverdueWarning && (
                    <Badge className={cn('text-xs', dueTypeInfo.Hard.color)}>
                      Hard Due
                    </Badge>
                  )}
                </div>
              </div>

              {/* Meta row */}
              <div className="flex items-center justify-between mt-3">
                <div className="flex items-center gap-3">
                  {/* Time estimate */}
                  <div className="flex items-center gap-1 text-xs text-muted-foreground">
                    <Clock className="size-3.5" />
                    <span>{task.estimatedMinutes}m</span>
                  </div>

                  {/* Energy cost */}
                  <EnergyIndicator energyCost={task.energyCost} />

                  {/* Context tags */}
                  <ContextTags tags={task.contextTags} max={2} />

                  {/* Project/Goal link */}
                  {(task.projectTitle || task.goalTitle) && (
                    <Badge
                      variant="outline"
                      className="text-[10px] px-1.5 py-0 text-muted-foreground hidden sm:flex"
                    >
                      {task.projectTitle || task.goalTitle}
                    </Badge>
                  )}
                </div>

                {/* Action buttons */}
                <div className="flex gap-1">
                  {isCompleted && (
                    <Tooltip>
                      <TooltipTrigger asChild>
                        <Button
                          variant="ghost"
                          size="icon-xs"
                          onClick={handleUndo}
                          disabled={isDisabled}
                          className="text-muted-foreground hover:text-foreground"
                        >
                          <RotateCcw className="size-3.5" />
                        </Button>
                      </TooltipTrigger>
                      <TooltipContent>Undo completion</TooltipContent>
                    </Tooltip>
                  )}

                  {!isCompleted && !isCancelled && (
                    <Tooltip>
                      <TooltipTrigger asChild>
                        <Button
                          variant="ghost"
                          size="icon-xs"
                          onClick={() => setShowRescheduleDialog(true)}
                          disabled={isDisabled}
                          className="text-muted-foreground hover:text-foreground"
                        >
                          <Calendar className="size-3.5" />
                        </Button>
                      </TooltipTrigger>
                      <TooltipContent>Reschedule</TooltipContent>
                    </Tooltip>
                  )}
                </div>
              </div>

              {/* Reschedule count warning */}
              {task.rescheduleCount > 0 && !isCompleted && (
                <div className="flex items-center gap-1 mt-2 text-xs text-amber-500">
                  <AlertCircle className="size-3" />
                  <span>Rescheduled {task.rescheduleCount}x</span>
                </div>
              )}
            </div>
          </div>
        </div>

        {/* Bottom border indicator */}
        <div
          className={cn(
            'h-1 transition-all',
            isCompleted ? 'bg-green-500' : isCancelled ? 'bg-muted' : 'bg-primary/20'
          )}
          style={{
            width: isCompleted ? '100%' : '0%',
          }}
        />
      </Card>

      {/* Dialogs */}
      <CompleteDialog
        open={showCompleteDialog}
        onOpenChange={setShowCompleteDialog}
        taskTitle={task.title}
        estimatedMinutes={task.estimatedMinutes}
        requiresValueEntry={task.requiresValueEntry}
        onComplete={handleDialogComplete}
        isLoading={isCompleting}
      />

      <RescheduleDialog
        open={showRescheduleDialog}
        onOpenChange={setShowRescheduleDialog}
        taskTitle={task.title}
        onReschedule={handleReschedule}
        isLoading={isRescheduling}
      />
    </>
  )
}

interface CompletionButtonProps {
  isCompleted: boolean
  isCancelled: boolean
  isBlocked: boolean
  isLoading: boolean
  disabled: boolean
  onClick: () => void
}

function CompletionButton({
  isCompleted,
  isCancelled,
  isBlocked,
  isLoading,
  disabled,
  onClick,
}: CompletionButtonProps) {
  return (
    <button
      type="button"
      onClick={onClick}
      disabled={disabled || isCompleted || isCancelled}
      className={cn(
        'relative flex-shrink-0 size-12 rounded-full border-2 transition-all',
        'flex items-center justify-center',
        'focus:outline-none focus:ring-2 focus:ring-primary/50 focus:ring-offset-2 focus:ring-offset-background',
        isCompleted && 'bg-green-500 border-green-500 cursor-default',
        isCancelled && 'bg-muted border-muted cursor-default',
        isBlocked && 'border-muted cursor-not-allowed',
        !isCompleted && !isCancelled && !isBlocked && [
          'border-muted-foreground/30 hover:border-primary hover:bg-primary/10',
          'active:scale-95',
        ],
        disabled && 'opacity-50 cursor-not-allowed'
      )}
    >
      {isLoading ? (
        <div className="size-5 border-2 border-current border-t-transparent rounded-full animate-spin" />
      ) : isCompleted ? (
        <Check className="size-6 text-white animate-in zoom-in duration-200" />
      ) : isBlocked ? (
        <Lock className="size-5 text-muted-foreground" />
      ) : (
        <Check className="size-5 text-muted-foreground/50" />
      )}
    </button>
  )
}
