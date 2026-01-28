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
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Textarea } from '@/components/ui/textarea'
import type { HabitMode, HabitVariantDto } from '@/types/habit'
import { ModeSelector } from './mode-selector'

interface ValueEntryDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  habitTitle: string
  variants: HabitVariantDto[]
  defaultMode: HabitMode
  onComplete: (data: { mode: HabitMode; value?: number; note?: string }) => void
  isLoading?: boolean
}

export function ValueEntryDialog({
  open,
  onOpenChange,
  habitTitle,
  variants,
  defaultMode,
  onComplete,
  isLoading = false,
}: ValueEntryDialogProps) {
  const [selectedMode, setSelectedMode] = useState<HabitMode>(defaultMode)
  const [value, setValue] = useState<string>('')
  const [note, setNote] = useState<string>('')

  const selectedVariant = variants.find(v => v.mode === selectedMode)
  const defaultValue = selectedVariant?.defaultValue

  const handleComplete = () => {
    const numericValue = value ? parseFloat(value) : defaultValue
    onComplete({
      mode: selectedMode,
      value: numericValue,
      note: note || undefined,
    })
    // Reset form
    setValue('')
    setNote('')
    setSelectedMode(defaultMode)
  }

  const handleOpenChange = (newOpen: boolean) => {
    if (!newOpen) {
      // Reset on close
      setValue('')
      setNote('')
      setSelectedMode(defaultMode)
    }
    onOpenChange(newOpen)
  }

  return (
    <Dialog open={open} onOpenChange={handleOpenChange}>
      <DialogContent className="sm:max-w-md">
        <DialogHeader>
          <DialogTitle>Complete: {habitTitle}</DialogTitle>
          <DialogDescription>
            Enter a value to track your progress.
          </DialogDescription>
        </DialogHeader>

        <div className="space-y-4 py-4">
          {variants.length > 1 && (
            <div className="space-y-2">
              <Label>Mode</Label>
              <ModeSelector
                variants={variants}
                defaultMode={defaultMode}
                selectedMode={selectedMode}
                onModeChange={setSelectedMode}
                disabled={isLoading}
              />
            </div>
          )}

          <div className="space-y-2">
            <Label htmlFor="value">Value</Label>
            <Input
              id="value"
              type="number"
              step="any"
              placeholder={defaultValue ? `Default: ${defaultValue}` : 'Enter value'}
              value={value}
              onChange={(e) => setValue(e.target.value)}
              disabled={isLoading}
              autoFocus
            />
            {selectedVariant && (
              <p className="text-xs text-muted-foreground">
                {selectedVariant.label} ({selectedVariant.estimatedMinutes} min)
              </p>
            )}
          </div>

          <div className="space-y-2">
            <Label htmlFor="note">Note (optional)</Label>
            <Textarea
              id="note"
              placeholder="Add a note about this completion..."
              value={note}
              onChange={(e) => setNote(e.target.value)}
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
          <Button onClick={handleComplete} disabled={isLoading}>
            {isLoading ? 'Completing...' : 'Complete'}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}
