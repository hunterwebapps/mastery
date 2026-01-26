import { useState } from 'react'
import { Users, Pencil } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Skeleton } from '@/components/ui/skeleton'
import { Alert, AlertDescription } from '@/components/ui/alert'
import type { UserRoleDto } from '@/types'
import { RolesGrid } from './roles-grid'
import { RoleEditDialog } from './role-edit-dialog'

interface RolesSectionProps {
  roles: UserRoleDto[]
  isLoading: boolean
  onSave: (roles: UserRoleDto[]) => Promise<void>
  isSaving: boolean
}

export function RolesSection({ roles, isLoading, onSave, isSaving }: RolesSectionProps) {
  const [isDialogOpen, setDialogOpen] = useState(false)

  if (isLoading) {
    return (
      <Card>
        <CardHeader>
          <Skeleton className="h-6 w-32" />
        </CardHeader>
        <CardContent>
          <div className="grid gap-4 md:grid-cols-2">
            <Skeleton className="h-32" />
            <Skeleton className="h-32" />
          </div>
        </CardContent>
      </Card>
    )
  }

  const activeRoles = roles.filter((r) => r.status === 'Active')

  return (
    <Card>
      <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-4">
        <div className="flex items-center gap-2">
          <Users className="size-5 text-primary" />
          <CardTitle>Your Roles</CardTitle>
        </div>
        <Button variant="outline" size="sm" onClick={() => setDialogOpen(true)}>
          <Pencil className="mr-2 size-4" />
          Edit
        </Button>
      </CardHeader>
      <CardContent className="space-y-4">
        {activeRoles.length < 3 && (
          <Alert className="border-warning/50 bg-warning/10">
            <AlertDescription className="text-sm text-warning">
              We recommend defining at least 3 active roles for balanced life planning.
            </AlertDescription>
          </Alert>
        )}
        <RolesGrid roles={roles} />
        {roles.length === 0 && (
          <p className="text-center text-muted-foreground py-4">
            No roles defined yet. Click Edit to add your life roles.
          </p>
        )}
      </CardContent>

      <RoleEditDialog
        open={isDialogOpen}
        onOpenChange={setDialogOpen}
        roles={roles}
        onSave={onSave}
        isSaving={isSaving}
      />
    </Card>
  )
}
