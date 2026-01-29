import { useState, useEffect } from 'react'
import { useParams, Link } from 'react-router-dom'
import { ArrowLeft, Loader2, User } from 'lucide-react'
import { toast } from 'sonner'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import { Switch } from '@/components/ui/switch'
import { Label } from '@/components/ui/label'
import { useUser, useUpdateUserRoles, useSetUserDisabled } from '../hooks'
import { RoleSelect, RoleBadge } from '../components'
import { useAuthStore } from '@/stores/auth-store'
import type { AppRole } from '@/types'

export function Component() {
  const { id } = useParams<{ id: string }>()
  const { user: currentUser } = useAuthStore()

  const { data: user, isLoading } = useUser(id!)
  const updateRoles = useUpdateUserRoles()
  const setDisabled = useSetUserDisabled()

  const [selectedRoles, setSelectedRoles] = useState<AppRole[]>([])
  const [isDisabled, setIsDisabled] = useState(false)

  useEffect(() => {
    if (user) {
      setSelectedRoles(user.roles)
      setIsDisabled(user.isDisabled)
    }
  }, [user])

  const handleSaveRoles = async () => {
    if (!id) return
    try {
      await updateRoles.mutateAsync({ id, request: { roles: selectedRoles } })
      toast.success('Roles updated', {
        description: 'User roles have been updated successfully.',
      })
    } catch {
      toast.error('Error', {
        description: 'Failed to update user roles.',
      })
    }
  }

  const handleToggleDisabled = async () => {
    if (!id) return
    try {
      await setDisabled.mutateAsync({ id, request: { disabled: !isDisabled } })
      setIsDisabled(!isDisabled)
      toast.success(isDisabled ? 'User enabled' : 'User disabled', {
        description: isDisabled
          ? 'User can now login again.'
          : 'User has been disabled and cannot login.',
      })
    } catch {
      toast.error('Error', {
        description: 'Failed to update user status.',
      })
    }
  }

  const isCurrentUser = currentUser?.id === id
  const hasRolesChanged =
    user && JSON.stringify([...selectedRoles].sort()) !== JSON.stringify([...user.roles].sort())

  if (isLoading) {
    return (
      <div className="flex items-center justify-center min-h-screen">
        <Loader2 className="size-8 animate-spin text-muted-foreground" />
      </div>
    )
  }

  if (!user) {
    return (
      <div className="container max-w-2xl py-8">
        <p className="text-muted-foreground">User not found</p>
        <Button asChild className="mt-4">
          <Link to="/admin/users">Back to Users</Link>
        </Button>
      </div>
    )
  }

  return (
    <div className="min-h-screen bg-background">
      <div className="container max-w-2xl py-8 px-4 sm:px-6 lg:px-8">
        <Button variant="ghost" asChild className="mb-6">
          <Link to="/admin/users">
            <ArrowLeft className="size-4 mr-2" />
            Back to Users
          </Link>
        </Button>

        <div className="flex items-center gap-3 mb-8">
          <div className="p-2 rounded-lg bg-primary/10">
            <User className="size-6 text-primary" />
          </div>
          <div>
            <h1 className="text-2xl font-bold text-foreground">{user.email}</h1>
            <p className="text-sm text-muted-foreground">
              {user.displayName || 'No display name'}
            </p>
          </div>
        </div>

        <div className="space-y-6">
          <Card>
            <CardHeader>
              <CardTitle>User Information</CardTitle>
              <CardDescription>Basic account details</CardDescription>
            </CardHeader>
            <CardContent className="space-y-4">
              <div className="grid grid-cols-2 gap-4">
                <div>
                  <Label className="text-muted-foreground">Auth Provider</Label>
                  <p className="font-medium">{user.authProvider}</p>
                </div>
                <div>
                  <Label className="text-muted-foreground">Email Confirmed</Label>
                  <p className="font-medium">{user.emailConfirmed ? 'Yes' : 'No'}</p>
                </div>
                <div>
                  <Label className="text-muted-foreground">Has Profile</Label>
                  <p className="font-medium">{user.hasProfile ? 'Yes' : 'No'}</p>
                </div>
                <div>
                  <Label className="text-muted-foreground">Created</Label>
                  <p className="font-medium">
                    {new Date(user.createdAt).toLocaleDateString()}
                  </p>
                </div>
                <div>
                  <Label className="text-muted-foreground">Last Login</Label>
                  <p className="font-medium">
                    {user.lastLoginAt
                      ? new Date(user.lastLoginAt).toLocaleString()
                      : 'Never'}
                  </p>
                </div>
                <div>
                  <Label className="text-muted-foreground">Current Roles</Label>
                  <div className="flex gap-1 mt-1">
                    {user.roles.map((role) => (
                      <RoleBadge key={role} role={role} />
                    ))}
                    {user.roles.length === 0 && (
                      <span className="text-muted-foreground text-sm">None</span>
                    )}
                  </div>
                </div>
              </div>
            </CardContent>
          </Card>

          <Card>
            <CardHeader>
              <CardTitle>Roles</CardTitle>
              <CardDescription>Manage user permissions</CardDescription>
            </CardHeader>
            <CardContent className="space-y-4">
              <RoleSelect
                selectedRoles={selectedRoles}
                onChange={setSelectedRoles}
                disabled={isCurrentUser}
              />
              {isCurrentUser && (
                <p className="text-sm text-muted-foreground">
                  You cannot modify your own roles.
                </p>
              )}
              <Button
                onClick={handleSaveRoles}
                disabled={!hasRolesChanged || isCurrentUser || updateRoles.isPending}
              >
                {updateRoles.isPending && <Loader2 className="size-4 mr-2 animate-spin" />}
                Save Roles
              </Button>
            </CardContent>
          </Card>

          <Card>
            <CardHeader>
              <CardTitle>Account Status</CardTitle>
              <CardDescription>Enable or disable this account</CardDescription>
            </CardHeader>
            <CardContent className="space-y-4">
              <div className="flex items-center justify-between">
                <div className="space-y-1">
                  <Label>Disable Account</Label>
                  <p className="text-sm text-muted-foreground">
                    Disabled users cannot login to the application
                  </p>
                </div>
                <div className="flex items-center gap-2">
                  <Switch
                    checked={isDisabled}
                    onCheckedChange={handleToggleDisabled}
                    disabled={isCurrentUser || setDisabled.isPending}
                  />
                  <Badge variant={isDisabled ? 'destructive' : 'secondary'}>
                    {isDisabled ? 'Disabled' : 'Active'}
                  </Badge>
                </div>
              </div>
              {isCurrentUser && (
                <p className="text-sm text-muted-foreground">
                  You cannot disable your own account.
                </p>
              )}
            </CardContent>
          </Card>
        </div>
      </div>
    </div>
  )
}
