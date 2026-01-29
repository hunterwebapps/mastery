import { Link } from 'react-router-dom'
import { Loader2 } from 'lucide-react'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Card, CardContent } from '@/components/ui/card'
import { RoleBadge } from './role-badge'
import type { UserListDto } from '@/types'

interface UserListProps {
  users: UserListDto[]
  isLoading: boolean
}

export function UserList({ users, isLoading }: UserListProps) {
  if (isLoading) {
    return (
      <div className="flex items-center justify-center py-12">
        <Loader2 className="size-8 animate-spin text-muted-foreground" />
      </div>
    )
  }

  if (users.length === 0) {
    return (
      <div className="text-center py-12">
        <p className="text-muted-foreground">No users found</p>
      </div>
    )
  }

  return (
    <div className="space-y-3">
      {users.map((user) => (
        <Card key={user.id}>
          <CardContent className="p-4">
            <div className="flex items-center justify-between">
              <div className="space-y-1">
                <div className="flex items-center gap-2">
                  <span className="font-medium">{user.email}</span>
                  {user.isDisabled ? (
                    <Badge variant="destructive">Disabled</Badge>
                  ) : (
                    <Badge variant="secondary">Active</Badge>
                  )}
                </div>
                <div className="flex items-center gap-2 text-sm text-muted-foreground">
                  {user.displayName && <span>{user.displayName}</span>}
                  <span>-</span>
                  <Badge variant="outline">{user.authProvider}</Badge>
                </div>
                <div className="flex flex-wrap gap-1 mt-2">
                  {user.roles.map((role) => (
                    <RoleBadge key={role} role={role} />
                  ))}
                  {user.roles.length === 0 && (
                    <span className="text-muted-foreground text-sm">No roles</span>
                  )}
                </div>
              </div>
              <div className="flex items-center gap-4">
                <div className="text-sm text-muted-foreground text-right">
                  <div>Last login:</div>
                  <div>
                    {user.lastLoginAt
                      ? new Date(user.lastLoginAt).toLocaleDateString()
                      : 'Never'}
                  </div>
                </div>
                <Button variant="outline" size="sm" asChild>
                  <Link to={`/admin/users/${user.id}`}>Edit</Link>
                </Button>
              </div>
            </div>
          </CardContent>
        </Card>
      ))}
    </div>
  )
}
