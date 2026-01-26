import { useState } from 'react'
import { Heart, Pencil } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Skeleton } from '@/components/ui/skeleton'
import { Alert, AlertDescription } from '@/components/ui/alert'
import type { UserValueDto } from '@/types'
import { ValuesList } from './values-list'
import { ValueEditDialog } from './value-edit-dialog'

interface ValuesSectionProps {
  values: UserValueDto[]
  isLoading: boolean
  onSave: (values: UserValueDto[]) => Promise<void>
  isSaving: boolean
}

export function ValuesSection({ values, isLoading, onSave, isSaving }: ValuesSectionProps) {
  const [isDialogOpen, setDialogOpen] = useState(false)

  if (isLoading) {
    return (
      <Card>
        <CardHeader>
          <Skeleton className="h-6 w-32" />
        </CardHeader>
        <CardContent>
          <div className="space-y-2">
            <Skeleton className="h-10 w-full" />
            <Skeleton className="h-10 w-full" />
            <Skeleton className="h-10 w-full" />
          </div>
        </CardContent>
      </Card>
    )
  }

  return (
    <Card>
      <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-4">
        <div className="flex items-center gap-2">
          <Heart className="size-5 text-primary" />
          <CardTitle>Your Values</CardTitle>
        </div>
        <Button variant="outline" size="sm" onClick={() => setDialogOpen(true)}>
          <Pencil className="mr-2 size-4" />
          Edit
        </Button>
      </CardHeader>
      <CardContent className="space-y-4">
        {values.length < 5 && (
          <Alert className="border-warning/50 bg-warning/10">
            <AlertDescription className="text-sm text-warning">
              We recommend defining at least 5 values for a complete profile.
            </AlertDescription>
          </Alert>
        )}
        <ValuesList values={values} />
        {values.length === 0 && (
          <p className="text-center text-muted-foreground py-4">
            No values defined yet. Click Edit to add your core values.
          </p>
        )}
      </CardContent>

      <ValueEditDialog
        open={isDialogOpen}
        onOpenChange={setDialogOpen}
        values={values}
        onSave={onSave}
        isSaving={isSaving}
      />
    </Card>
  )
}
