import { useState, useCallback } from 'react'
import { Check, RotateCcw, SkipForward, ChevronRight } from 'lucide-react'
import { Link } from 'react-router-dom'
import { cn } from '@/lib/utils'
import { Button } from '@/components/ui/button'
import { Card } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import { Tooltip, TooltipContent, TooltipTrigger } from '@/components/ui/tooltip'
import type { TodayHabitDto, HabitMode } from '@/types/habit'
import { habitModeInfo } from '@/types/habit'
import { StreakBadge } from '../common/streak-badge'
import { ModeSelector } from './mode-selector'
import { ValueEntryDialog } from './value-entry-dialog'
import { SkipDialog } from './skip-dialog'
import { CompletionCelebration, CheckmarkAnimation } from './completion-celebration'

interface TodayHabitCardProps {
  habit: TodayHabitDto
  onComplete: (data: { mode?: HabitMode; value?: number; note?: string }) => Promise<void>
  onUndo: () => Promise<void>
  onSkip: (reason?: string) => Promise<void>
}

export function TodayHabitCard({
  habit,
  onComplete,
  onUndo,
  onSkip,
}: TodayHabitCardProps) {
  const [selectedMode, setSelectedMode] = useState<HabitMode>(habit.defaultMode)
  const [isCompleting, setIsCompleting] = useState(false)
  const [isUndoing, setIsUndoing] = useState(false)
  const [isSkipping, setIsSkipping] = useState(false)
  const [showValueDialog, setShowValueDialog] = useState(false)
  const [showSkipDialog, setShowSkipDialog] = useState(false)
  const [showCelebration, setShowCelebration] = useState(false)
  const [celebrationMilestone, setCelebrationMilestone] = useState<number | null>(null)

  const isCompleted = habit.todayOccurrence?.status === 'Completed'
  const isSkipped = habit.todayOccurrence?.status === 'Skipped'
  const isPending = !habit.todayOccurrence || habit.todayOccurrence.status === 'Pending'
  const isDisabled = isCompleting || isUndoing || isSkipping

  // Check if this is a milestone streak
  const checkMilestone = useCallback((streak: number): number | null => {
    const milestones = [7, 14, 21, 30, 50, 100, 200, 365]
    return milestones.find(m => streak === m) || null
  }, [])

  const handleQuickComplete = useCallback(async () => {
    if (habit.requiresValueEntry) {
      setShowValueDialog(true)
      return
    }

    setIsCompleting(true)
    try {
      await onComplete({ mode: selectedMode })
      // Check for milestone
      const newStreak = habit.currentStreak + 1
      const milestone = checkMilestone(newStreak)
      setCelebrationMilestone(milestone)
      setShowCelebration(true)
    } finally {
      setIsCompleting(false)
    }
  }, [habit.requiresValueEntry, habit.currentStreak, selectedMode, onComplete, checkMilestone])

  const handleValueComplete = useCallback(async (data: { mode: HabitMode; value?: number; note?: string }) => {
    setIsCompleting(true)
    try {
      await onComplete(data)
      setShowValueDialog(false)
      // Check for milestone
      const newStreak = habit.currentStreak + 1
      const milestone = checkMilestone(newStreak)
      setCelebrationMilestone(milestone)
      setShowCelebration(true)
    } finally {
      setIsCompleting(false)
    }
  }, [habit.currentStreak, onComplete, checkMilestone])

  const handleUndo = useCallback(async () => {
    setIsUndoing(true)
    try {
      await onUndo()
    } finally {
      setIsUndoing(false)
    }
  }, [onUndo])

  const handleSkip = useCallback(async (reason?: string) => {
    setIsSkipping(true)
    try {
      await onSkip(reason)
      setShowSkipDialog(false)
    } finally {
      setIsSkipping(false)
    }
  }, [onSkip])

  return (
    <>
      <Card
        className={cn(
          'relative overflow-hidden transition-all duration-200',
          'hover:shadow-md hover:border-primary/30',
          isCompleted && 'border-green-500/30 bg-green-500/5',
          isSkipped && 'border-muted bg-muted/30 opacity-75',
          isCompleting && 'scale-[0.98]'
        )}
      >
        <div className="p-4">
          <div className="flex items-start gap-3">
            {/* Completion button - large tap target */}
            <CompletionButton
              isCompleted={isCompleted}
              isSkipped={isSkipped}
              isLoading={isCompleting}
              disabled={isDisabled}
              onClick={handleQuickComplete}
            />

            {/* Habit info */}
            <div className="flex-1 min-w-0">
              <div className="flex items-start justify-between gap-2">
                <div className="flex-1 min-w-0">
                  <Link
                    to={`/habits/${habit.id}`}
                    className="group flex items-center gap-1"
                  >
                    <h3 className={cn(
                      'font-medium truncate transition-colors',
                      isCompleted && 'text-green-400 line-through',
                      isSkipped && 'text-muted-foreground line-through',
                      !isCompleted && !isSkipped && 'group-hover:text-primary'
                    )}>
                      {habit.title}
                    </h3>
                    <ChevronRight className="size-4 opacity-0 -ml-1 transition-all group-hover:opacity-50 group-hover:ml-0" />
                  </Link>

                  {habit.description && (
                    <p className="text-sm text-muted-foreground truncate mt-0.5">
                      {habit.description}
                    </p>
                  )}
                </div>

                <StreakBadge streak={habit.currentStreak} />
              </div>

              {/* Mode selector and actions row */}
              <div className="flex items-center justify-between mt-3">
                <div className="flex items-center gap-2">
                  {isPending && habit.variants.length > 1 && (
                    <ModeSelector
                      variants={habit.variants}
                      defaultMode={habit.defaultMode}
                      selectedMode={selectedMode}
                      onModeChange={setSelectedMode}
                      disabled={isDisabled}
                    />
                  )}

                  {/* Goal impact tags */}
                  {habit.goalImpactTags.length > 0 && (
                    <div className="hidden sm:flex gap-1">
                      {habit.goalImpactTags.slice(0, 2).map((tag) => (
                        <Badge
                          key={tag}
                          variant="outline"
                          className="text-[10px] px-1.5 py-0 text-muted-foreground"
                        >
                          {tag}
                        </Badge>
                      ))}
                      {habit.goalImpactTags.length > 2 && (
                        <Badge
                          variant="outline"
                          className="text-[10px] px-1.5 py-0 text-muted-foreground"
                        >
                          +{habit.goalImpactTags.length - 2}
                        </Badge>
                      )}
                    </div>
                  )}

                  {/* Completed mode badge */}
                  {isCompleted && habit.todayOccurrence?.modeUsed && (
                    <Badge
                      className={cn(
                        'text-xs',
                        habitModeInfo[habit.todayOccurrence.modeUsed].bgColor,
                        habitModeInfo[habit.todayOccurrence.modeUsed].color
                      )}
                    >
                      {habitModeInfo[habit.todayOccurrence.modeUsed].label}
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

                  {isPending && (
                    <Tooltip>
                      <TooltipTrigger asChild>
                        <Button
                          variant="ghost"
                          size="icon-xs"
                          onClick={() => setShowSkipDialog(true)}
                          disabled={isDisabled}
                          className="text-muted-foreground hover:text-foreground"
                        >
                          <SkipForward className="size-3.5" />
                        </Button>
                      </TooltipTrigger>
                      <TooltipContent>Skip for today</TooltipContent>
                    </Tooltip>
                  )}
                </div>
              </div>
            </div>
          </div>
        </div>

        {/* Progress indicator for 7-day adherence */}
        <div
          className={cn(
            'h-1 transition-all',
            isCompleted ? 'bg-green-500' : isSkipped ? 'bg-muted' : 'bg-primary/20'
          )}
          style={{ width: `${habit.adherenceRate7Day * 100}%` }}
        />
      </Card>

      {/* Dialogs */}
      <ValueEntryDialog
        open={showValueDialog}
        onOpenChange={setShowValueDialog}
        habitTitle={habit.title}
        variants={habit.variants}
        defaultMode={habit.defaultMode}
        onComplete={handleValueComplete}
        isLoading={isCompleting}
      />

      <SkipDialog
        open={showSkipDialog}
        onOpenChange={setShowSkipDialog}
        habitTitle={habit.title}
        onSkip={handleSkip}
        isLoading={isSkipping}
      />

      {/* Celebration animation */}
      <CompletionCelebration
        show={showCelebration}
        streakMilestone={celebrationMilestone}
        onComplete={() => {
          setShowCelebration(false)
          setCelebrationMilestone(null)
        }}
      />
    </>
  )
}

interface CompletionButtonProps {
  isCompleted: boolean
  isSkipped: boolean
  isLoading: boolean
  disabled: boolean
  onClick: () => void
}

function CompletionButton({
  isCompleted,
  isSkipped,
  isLoading,
  disabled,
  onClick,
}: CompletionButtonProps) {
  return (
    <button
      type="button"
      onClick={onClick}
      disabled={disabled || isCompleted || isSkipped}
      className={cn(
        'relative flex-shrink-0 size-12 rounded-full border-2 transition-all',
        'flex items-center justify-center',
        'focus:outline-none focus:ring-2 focus:ring-primary/50 focus:ring-offset-2 focus:ring-offset-background',
        isCompleted && 'bg-green-500 border-green-500 cursor-default',
        isSkipped && 'bg-muted border-muted cursor-default',
        !isCompleted && !isSkipped && [
          'border-muted-foreground/30 hover:border-primary hover:bg-primary/10',
          'active:scale-95',
        ],
        disabled && 'opacity-50 cursor-not-allowed'
      )}
    >
      {isLoading ? (
        <div className="size-5 border-2 border-current border-t-transparent rounded-full animate-spin" />
      ) : isCompleted ? (
        <CheckmarkAnimation className="size-6 text-white" />
      ) : isSkipped ? (
        <SkipForward className="size-5 text-muted-foreground" />
      ) : (
        <Check className="size-5 text-muted-foreground/50" />
      )}
    </button>
  )
}
