import { useState } from 'react'
import { Clock, Loader2 } from 'lucide-react'
import { format } from 'date-fns'
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Textarea } from '@/components/ui/textarea'
import { Label } from '@/components/ui/label'

interface CompleteDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  taskTitle: string
  estimatedMinutes?: number
  requiresValueEntry?: boolean
  onComplete: (data: {
    completedOn: string
    actualMinutes?: number
    note?: string
    enteredValue?: number
  }) => Promise<void>
  isLoading?: boolean
}

export function CompleteDialog({
  open,
  onOpenChange,
  taskTitle,
  estimatedMinutes,
  requiresValueEntry = false,
  onComplete,
  isLoading = false,
}: CompleteDialogProps) {
  const [actualMinutes, setActualMinutes] = useState<number | undefined>(estimatedMinutes)
  const [note, setNote] = useState('')
  const [enteredValue, setEnteredValue] = useState<number | undefined>()

  const handleComplete = async () => {
    await onComplete({
      completedOn: format(new Date(), 'yyyy-MM-dd'),
      actualMinutes,
      note: note || undefined,
      enteredValue,
    })
    onOpenChange(false)
    // Reset form
    setActualMinutes(estimatedMinutes)
    setNote('')
    setEnteredValue(undefined)
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-md">
        <DialogHeader>
          <DialogTitle>Complete Task</DialogTitle>
          <DialogDescription className="truncate">
            {taskTitle}
          </DialogDescription>
        </DialogHeader>

        <div className="space-y-4 py-4">
          {/* Time tracking */}
          <div className="space-y-2">
            <Label htmlFor="actual-minutes" className="flex items-center gap-2">
              <Clock className="size-4" />
              Time spent (minutes)
            </Label>
            <div className="flex gap-2">
              <Input
                id="actual-minutes"
                type="number"
                min={0}
                max={480}
                placeholder={estimatedMinutes?.toString() || '30'}
                value={actualMinutes ?? ''}
                onChange={(e) => setActualMinutes(e.target.value ? parseInt(e.target.value) : undefined)}
                className="w-24"
              />
              {estimatedMinutes && (
                <span className="text-sm text-muted-foreground self-center">
                  (estimated: {estimatedMinutes}m)
                </span>
              )}
            </div>
          </div>

          {/* Value entry for metric binding */}
          {requiresValueEntry && (
            <div className="space-y-2">
              <Label htmlFor="entered-value">
                Value achieved
              </Label>
              <Input
                id="entered-value"
                type="number"
                min={0}
                placeholder="Enter value"
                value={enteredValue ?? ''}
                onChange={(e) => setEnteredValue(e.target.value ? parseFloat(e.target.value) : undefined)}
                className="w-32"
              />
            </div>
          )}

          {/* Completion note */}
          <div className="space-y-2">
            <Label htmlFor="note">
              Note <span className="text-xs text-muted-foreground">(optional)</span>
            </Label>
            <Textarea
              id="note"
              placeholder="Any learnings or observations?"
              value={note}
              onChange={(e) => setNote(e.target.value)}
              rows={2}
              className="resize-none"
            />
          </div>

          <Button
            onClick={handleComplete}
            disabled={isLoading || (requiresValueEntry && enteredValue === undefined)}
            className="w-full"
          >
            {isLoading ? (
              <>
                <Loader2 className="size-4 mr-2 animate-spin" />
                Completing...
              </>
            ) : (
              'Complete Task'
            )}
          </Button>
        </div>
      </DialogContent>
    </Dialog>
  )
}
