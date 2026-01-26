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
import type { SeasonDto } from '@/types'

interface EndSeasonDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  season: SeasonDto
  onConfirm: (outcome: string | undefined) => Promise<void>
  isEnding: boolean
}

export function EndSeasonDialog({
  open,
  onOpenChange,
  season,
  onConfirm,
  isEnding,
}: EndSeasonDialogProps) {
  const [outcome, setOutcome] = useState('')

  const handleConfirm = async () => {
    await onConfirm(outcome.trim() || undefined)
    setOutcome('')
    onOpenChange(false)
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>End Season</DialogTitle>
          <DialogDescription>
            Are you sure you want to end "{season.label}"? This action cannot be undone.
          </DialogDescription>
        </DialogHeader>

        <div className="py-4 space-y-4">
          <div className="space-y-2">
            <Label>Reflection (optional)</Label>
            <Textarea
              placeholder="How did this season go? What did you learn?"
              value={outcome}
              onChange={(e) => setOutcome(e.target.value)}
              rows={4}
              className="resize-none"
            />
            <p className="text-xs text-muted-foreground">
              This reflection will be saved and can help inform future seasons.
            </p>
          </div>
        </div>

        <DialogFooter>
          <Button variant="outline" onClick={() => onOpenChange(false)}>
            Cancel
          </Button>
          <Button onClick={handleConfirm} disabled={isEnding}>
            {isEnding ? 'Ending...' : 'End Season'}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}
