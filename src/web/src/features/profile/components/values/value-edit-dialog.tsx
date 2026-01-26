import { useState, useEffect } from 'react'
import { Plus, Trash2, GripVertical } from 'lucide-react'
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
import { Textarea } from '@/components/ui/textarea'
import type { UserValueDto } from '@/types'

interface ValueEditDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  values: UserValueDto[]
  onSave: (values: UserValueDto[]) => Promise<void>
  isSaving: boolean
}

export function ValueEditDialog({
  open,
  onOpenChange,
  values,
  onSave,
  isSaving,
}: ValueEditDialogProps) {
  const [editedValues, setEditedValues] = useState<UserValueDto[]>([])

  useEffect(() => {
    if (open) {
      setEditedValues([...values].sort((a, b) => a.rank - b.rank))
    }
  }, [open, values])

  const handleAdd = () => {
    const newValue: UserValueDto = {
      id: crypto.randomUUID(),
      key: null,
      label: '',
      rank: editedValues.length + 1,
      weight: null,
      notes: null,
      source: 'manual',
    }
    setEditedValues([...editedValues, newValue])
  }

  const handleRemove = (id: string) => {
    const filtered = editedValues.filter((v) => v.id !== id)
    // Rerank remaining values
    const reranked = filtered.map((v, index) => ({ ...v, rank: index + 1 }))
    setEditedValues(reranked)
  }

  const handleUpdate = (id: string, field: keyof UserValueDto, value: string) => {
    setEditedValues(
      editedValues.map((v) => (v.id === id ? { ...v, [field]: value } : v))
    )
  }

  const handleMoveUp = (index: number) => {
    if (index === 0) return
    const newValues = [...editedValues]
    ;[newValues[index - 1], newValues[index]] = [newValues[index], newValues[index - 1]]
    setEditedValues(newValues.map((v, i) => ({ ...v, rank: i + 1 })))
  }

  const handleMoveDown = (index: number) => {
    if (index === editedValues.length - 1) return
    const newValues = [...editedValues]
    ;[newValues[index], newValues[index + 1]] = [newValues[index + 1], newValues[index]]
    setEditedValues(newValues.map((v, i) => ({ ...v, rank: i + 1 })))
  }

  const handleSave = async () => {
    const validValues = editedValues.filter((v) => v.label.trim() !== '')
    await onSave(validValues)
    onOpenChange(false)
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-h-[85vh] overflow-y-auto sm:max-w-2xl">
        <DialogHeader>
          <DialogTitle>Edit Your Values</DialogTitle>
          <DialogDescription>
            Define 5-10 core values that guide your decisions. Drag to reorder by priority.
          </DialogDescription>
        </DialogHeader>

        <div className="space-y-4 py-4">
          {editedValues.map((value, index) => (
            <div
              key={value.id}
              className="flex items-start gap-2 rounded-lg border border-border p-3"
            >
              <div className="flex flex-col gap-0.5 pt-2">
                <Button
                  variant="ghost"
                  size="icon"
                  className="size-6"
                  onClick={() => handleMoveUp(index)}
                  disabled={index === 0}
                >
                  <GripVertical className="size-4 rotate-90" />
                </Button>
                <Button
                  variant="ghost"
                  size="icon"
                  className="size-6"
                  onClick={() => handleMoveDown(index)}
                  disabled={index === editedValues.length - 1}
                >
                  <GripVertical className="size-4 -rotate-90" />
                </Button>
              </div>
              <div className="flex-1 space-y-3">
                <div className="flex items-center gap-2">
                  <span className="flex size-6 items-center justify-center rounded-full bg-primary/10 text-xs font-semibold text-primary">
                    {value.rank}
                  </span>
                  <Input
                    placeholder="Value name (e.g., Family, Health)"
                    value={value.label}
                    onChange={(e) => handleUpdate(value.id, 'label', e.target.value)}
                    className="flex-1"
                  />
                </div>
                <Textarea
                  placeholder="Brief description (optional)"
                  value={value.notes || ''}
                  onChange={(e) => handleUpdate(value.id, 'notes', e.target.value)}
                  rows={2}
                />
              </div>
              <Button
                variant="ghost"
                size="icon"
                className="text-destructive hover:bg-destructive/10"
                onClick={() => handleRemove(value.id)}
              >
                <Trash2 className="size-4" />
              </Button>
            </div>
          ))}

          <Button variant="outline" onClick={handleAdd} className="w-full">
            <Plus className="mr-2 size-4" />
            Add Value
          </Button>
        </div>

        <DialogFooter>
          <Button variant="outline" onClick={() => onOpenChange(false)}>
            Cancel
          </Button>
          <Button onClick={handleSave} disabled={isSaving}>
            {isSaving ? 'Saving...' : 'Save Changes'}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}
