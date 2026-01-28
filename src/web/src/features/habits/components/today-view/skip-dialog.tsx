import { useState } from 'react'
import { Button } from '@/components/ui/button'
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog'
import { Textarea } from '@/components/ui/textarea'
import { Label } from '@/components/ui/label'

interface SkipDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  habitTitle: string
  onSkip: (reason?: string) => void
  isLoading?: boolean
}

const QUICK_REASONS = [
  { emoji: '😴', label: 'Too tired' },
  { emoji: '⏰', label: 'No time' },
  { emoji: '🤒', label: 'Not feeling well' },
  { emoji: '🏠', label: 'Environment issue' },
  { emoji: '🗓️', label: 'Schedule conflict' },
]

export function SkipDialog({
  open,
  onOpenChange,
  habitTitle,
  onSkip,
  isLoading = false,
}: SkipDialogProps) {
  const [reason, setReason] = useState<string>('')
  const [selectedQuickReason, setSelectedQuickReason] = useState<string | null>(null)

  const handleSkip = () => {
    const finalReason = selectedQuickReason || reason || undefined
    onSkip(finalReason)
    // Reset form
    setReason('')
    setSelectedQuickReason(null)
  }

  const handleQuickReasonClick = (label: string) => {
    if (selectedQuickReason === label) {
      setSelectedQuickReason(null)
    } else {
      setSelectedQuickReason(label)
      setReason('')
    }
  }

  const handleOpenChange = (newOpen: boolean) => {
    if (!newOpen) {
      setReason('')
      setSelectedQuickReason(null)
    }
    onOpenChange(newOpen)
  }

  return (
    <Dialog open={open} onOpenChange={handleOpenChange}>
      <DialogContent className="sm:max-w-md">
        <DialogHeader>
          <DialogTitle>Skip: {habitTitle}</DialogTitle>
          <DialogDescription>
            Skipping won't break your streak, but tracking reasons helps identify patterns.
          </DialogDescription>
        </DialogHeader>

        <div className="space-y-4 py-4">
          <div className="space-y-2">
            <Label>Quick reason (optional)</Label>
            <div className="flex flex-wrap gap-2">
              {QUICK_REASONS.map(({ emoji, label }) => (
                <Button
                  key={label}
                  type="button"
                  variant={selectedQuickReason === label ? 'default' : 'outline'}
                  size="sm"
                  onClick={() => handleQuickReasonClick(label)}
                  disabled={isLoading}
                  className="gap-1.5"
                >
                  <span>{emoji}</span>
                  <span>{label}</span>
                </Button>
              ))}
            </div>
          </div>

          <div className="space-y-2">
            <Label htmlFor="custom-reason">Or add your own</Label>
            <Textarea
              id="custom-reason"
              placeholder="What's preventing you today?"
              value={reason}
              onChange={(e) => {
                setReason(e.target.value)
                setSelectedQuickReason(null)
              }}
              disabled={isLoading}
              className="min-h-[80px] resize-none"
            />
          </div>
        </div>

        <DialogFooter>
          <Button
            variant="outline"
            onClick={() => handleOpenChange(false)}
            disabled={isLoading}
          >
            Cancel
          </Button>
          <Button
            variant="secondary"
            onClick={handleSkip}
            disabled={isLoading}
          >
            {isLoading ? 'Skipping...' : 'Skip for today'}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}
