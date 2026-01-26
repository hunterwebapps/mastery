import { useState } from 'react'
import { Settings, Pencil } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Skeleton } from '@/components/ui/skeleton'
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog'
import type { PreferencesDto } from '@/types'
import { PreferencesForm } from './preferences-form'

interface PreferencesSectionProps {
  preferences: PreferencesDto | undefined
  isLoading: boolean
  onSave: (preferences: PreferencesDto) => Promise<void>
  isSaving: boolean
}

export function PreferencesSection({
  preferences,
  isLoading,
  onSave,
  isSaving,
}: PreferencesSectionProps) {
  const [isDialogOpen, setDialogOpen] = useState(false)

  if (isLoading || !preferences) {
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
          <Settings className="size-5 text-primary" />
          <CardTitle>Preferences</CardTitle>
        </div>
        <Button variant="outline" size="sm" onClick={() => setDialogOpen(true)}>
          <Pencil className="mr-2 size-4" />
          Edit
        </Button>
      </CardHeader>
      <CardContent>
        <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
          <PreferenceItem label="Coaching Style" value={preferences.coachingStyle} />
          <PreferenceItem label="Explanation Detail" value={preferences.explanationVerbosity} />
          <PreferenceItem label="Nudge Level" value={preferences.nudgeLevel} />
          <PreferenceItem
            label="Morning Check-in"
            value={preferences.morningCheckInTime}
          />
          <PreferenceItem
            label="Evening Check-in"
            value={preferences.eveningCheckInTime}
          />
          <PreferenceItem
            label="Default Task Duration"
            value={`${preferences.planningDefaults.defaultTaskDurationMinutes} min`}
          />
        </div>
      </CardContent>

      <Dialog open={isDialogOpen} onOpenChange={setDialogOpen}>
        <DialogContent className="max-h-[85vh] overflow-y-auto sm:max-w-2xl">
          <DialogHeader>
            <DialogTitle>Edit Preferences</DialogTitle>
            <DialogDescription>
              Customize how Mastery coaches and communicates with you.
            </DialogDescription>
          </DialogHeader>
          <PreferencesForm
            preferences={preferences}
            onSave={async (prefs) => {
              await onSave(prefs)
              setDialogOpen(false)
            }}
            isSaving={isSaving}
            onCancel={() => setDialogOpen(false)}
          />
        </DialogContent>
      </Dialog>
    </Card>
  )
}

function PreferenceItem({ label, value }: { label: string; value: string }) {
  return (
    <div className="space-y-1">
      <p className="text-xs text-muted-foreground">{label}</p>
      <p className="font-medium">{value}</p>
    </div>
  )
}
