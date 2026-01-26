import { useState } from 'react'
import { Shield, Pencil } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Skeleton } from '@/components/ui/skeleton'
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog'
import type { ConstraintsDto } from '@/types'
import { CapacityLimits } from './capacity-limits'
import { BlockedWindowsEditor } from './blocked-windows-editor'
import { HealthNotesInput } from './health-notes-input'
import { ContentBoundariesInput } from './content-boundaries-input'

interface ConstraintsSectionProps {
  constraints: ConstraintsDto | undefined
  isLoading: boolean
  onSave: (constraints: ConstraintsDto) => Promise<void>
  isSaving: boolean
}

function formatHours(minutes: number): string {
  const hours = Math.floor(minutes / 60)
  const mins = minutes % 60
  if (mins === 0) return `${hours}h`
  return `${hours}h ${mins}m`
}

export function ConstraintsSection({
  constraints,
  isLoading,
  onSave,
  isSaving,
}: ConstraintsSectionProps) {
  const [isDialogOpen, setDialogOpen] = useState(false)
  const [editedConstraints, setEditedConstraints] = useState<ConstraintsDto | null>(null)

  const handleOpenDialog = () => {
    if (constraints) {
      setEditedConstraints({ ...constraints })
      setDialogOpen(true)
    }
  }

  const handleSave = async () => {
    if (editedConstraints) {
      await onSave(editedConstraints)
      setDialogOpen(false)
    }
  }

  if (isLoading || !constraints) {
    return (
      <Card>
        <CardHeader>
          <Skeleton className="h-6 w-32" />
        </CardHeader>
        <CardContent>
          <div className="space-y-4">
            <Skeleton className="h-8 w-full" />
            <Skeleton className="h-8 w-full" />
          </div>
        </CardContent>
      </Card>
    )
  }

  return (
    <Card>
      <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-4">
        <div className="flex items-center gap-2">
          <Shield className="size-5 text-primary" />
          <CardTitle>Constraints</CardTitle>
        </div>
        <Button variant="outline" size="sm" onClick={handleOpenDialog}>
          <Pencil className="mr-2 size-4" />
          Edit
        </Button>
      </CardHeader>
      <CardContent>
        <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
          <ConstraintItem
            label="Weekday Capacity"
            value={formatHours(constraints.maxPlannedMinutesWeekday)}
          />
          <ConstraintItem
            label="Weekend Capacity"
            value={formatHours(constraints.maxPlannedMinutesWeekend)}
          />
          <ConstraintItem
            label="Blocked Windows"
            value={`${constraints.blockedTimeWindows.length} defined`}
          />
          <ConstraintItem
            label="Content Boundaries"
            value={`${constraints.contentBoundaries.length} topics`}
          />
        </div>

        {constraints.healthNotes && (
          <div className="mt-4 pt-4 border-t border-border">
            <p className="text-xs text-muted-foreground mb-1">Health Notes</p>
            <p className="text-sm">{constraints.healthNotes}</p>
          </div>
        )}
      </CardContent>

      <Dialog open={isDialogOpen} onOpenChange={setDialogOpen}>
        <DialogContent className="max-h-[85vh] overflow-y-auto sm:max-w-2xl">
          <DialogHeader>
            <DialogTitle>Edit Constraints</DialogTitle>
            <DialogDescription>
              Set your capacity limits and blocked time windows.
            </DialogDescription>
          </DialogHeader>

          {editedConstraints && (
            <div className="space-y-6 py-4">
              <CapacityLimits
                weekdayMinutes={editedConstraints.maxPlannedMinutesWeekday}
                weekendMinutes={editedConstraints.maxPlannedMinutesWeekend}
                onChange={(weekday, weekend) =>
                  setEditedConstraints({
                    ...editedConstraints,
                    maxPlannedMinutesWeekday: weekday,
                    maxPlannedMinutesWeekend: weekend,
                  })
                }
                disabled={isSaving}
              />

              <div className="border-t border-border pt-6">
                <BlockedWindowsEditor
                  windows={editedConstraints.blockedTimeWindows}
                  onChange={(windows) =>
                    setEditedConstraints({
                      ...editedConstraints,
                      blockedTimeWindows: windows,
                    })
                  }
                  disabled={isSaving}
                />
              </div>

              <div className="border-t border-border pt-6">
                <HealthNotesInput
                  value={editedConstraints.healthNotes}
                  onChange={(notes) =>
                    setEditedConstraints({
                      ...editedConstraints,
                      healthNotes: notes,
                    })
                  }
                  disabled={isSaving}
                />
              </div>

              <div className="border-t border-border pt-6">
                <ContentBoundariesInput
                  values={editedConstraints.contentBoundaries}
                  onChange={(boundaries) =>
                    setEditedConstraints({
                      ...editedConstraints,
                      contentBoundaries: boundaries,
                    })
                  }
                  disabled={isSaving}
                />
              </div>
            </div>
          )}

          <DialogFooter>
            <Button variant="outline" onClick={() => setDialogOpen(false)}>
              Cancel
            </Button>
            <Button onClick={handleSave} disabled={isSaving}>
              {isSaving ? 'Saving...' : 'Save Changes'}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </Card>
  )
}

function ConstraintItem({ label, value }: { label: string; value: string }) {
  return (
    <div className="space-y-1">
      <p className="text-xs text-muted-foreground">{label}</p>
      <p className="font-medium">{value}</p>
    </div>
  )
}
