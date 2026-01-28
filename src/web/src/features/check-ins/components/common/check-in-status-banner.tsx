import { cn } from '@/lib/utils'
import { Sun, Moon, Check, SkipForward } from 'lucide-react'
import { Button } from '@/components/ui/button'

interface CheckInStatusBannerProps {
  type: 'morning' | 'evening'
  status: string
  onStart?: () => void
}

export function CheckInStatusBanner({ type, status, onStart }: CheckInStatusBannerProps) {
  const isMorning = type === 'morning'
  const isDone = status === 'Completed' || status === 'Skipped'
  const isSkipped = status === 'Skipped'

  return (
    <div
      className={cn(
        'flex items-center gap-3 rounded-xl border p-4 transition-all',
        isDone
          ? 'border-border/30 bg-muted/30'
          : isMorning
            ? 'border-orange-500/30 bg-gradient-to-r from-orange-500/10 to-orange-500/5'
            : 'border-indigo-500/30 bg-gradient-to-r from-indigo-500/10 to-indigo-500/5'
      )}
    >
      <div
        className={cn(
          'flex size-10 items-center justify-center rounded-lg',
          isDone ? 'bg-muted' : isMorning ? 'bg-orange-500/20' : 'bg-indigo-500/20'
        )}
      >
        {isDone ? (
          isSkipped ? (
            <SkipForward className="size-5 text-muted-foreground" />
          ) : (
            <Check className="size-5 text-green-400" />
          )
        ) : isMorning ? (
          <Sun className="size-5 text-orange-400" />
        ) : (
          <Moon className="size-5 text-indigo-400" />
        )}
      </div>

      <div className="flex-1 min-w-0">
        <p className={cn(
          'text-sm font-medium',
          isDone ? 'text-muted-foreground' : 'text-foreground'
        )}>
          {isMorning ? 'Morning' : 'Evening'} check-in
        </p>
        <p className="text-xs text-muted-foreground">
          {isDone
            ? isSkipped ? 'Skipped' : 'Completed'
            : isMorning
              ? 'Start your day with intention'
              : 'Reflect on your day'
          }
        </p>
      </div>

      {!isDone && onStart && (
        <Button
          onClick={onStart}
          size="sm"
          className={cn(
            isMorning
              ? 'bg-orange-500 hover:bg-orange-600'
              : 'bg-indigo-500 hover:bg-indigo-600'
          )}
        >
          Start
        </Button>
      )}
    </div>
  )
}
