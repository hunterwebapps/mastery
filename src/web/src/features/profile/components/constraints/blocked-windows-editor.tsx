import { Plus } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Label } from '@/components/ui/label'
import { BlockedWindowItem } from './blocked-window-item'
import type { BlockedWindowDto } from '@/types'

interface BlockedWindowsEditorProps {
  windows: BlockedWindowDto[]
  onChange: (windows: BlockedWindowDto[]) => void
  disabled?: boolean
}

export function BlockedWindowsEditor({
  windows,
  onChange,
  disabled = false,
}: BlockedWindowsEditorProps) {
  const handleAdd = () => {
    const newWindow: BlockedWindowDto = {
      label: null,
      timeWindow: { start: '18:00', end: '19:00' },
      applicableDays: ['Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday'],
    }
    onChange([...windows, newWindow])
  }

  const handleUpdate = (index: number, updated: BlockedWindowDto) => {
    const newWindows = [...windows]
    newWindows[index] = updated
    onChange(newWindows)
  }

  const handleRemove = (index: number) => {
    onChange(windows.filter((_, i) => i !== index))
  }

  return (
    <div className="space-y-4">
      <div className="space-y-1">
        <Label className="text-sm font-medium">Blocked Time Windows</Label>
        <p className="text-xs text-muted-foreground">
          Times when tasks should not be scheduled (e.g., dinner, gym, commute)
        </p>
      </div>

      {windows.length > 0 && (
        <div className="space-y-3">
          {windows.map((window, index) => (
            <BlockedWindowItem
              key={index}
              window={window}
              onChange={(updated) => handleUpdate(index, updated)}
              onRemove={() => handleRemove(index)}
              disabled={disabled}
            />
          ))}
        </div>
      )}

      <Button
        variant="outline"
        onClick={handleAdd}
        disabled={disabled}
        className="w-full"
      >
        <Plus className="mr-2 size-4" />
        Add Blocked Window
      </Button>
    </div>
  )
}
