import { Plus } from 'lucide-react'
import { Alert, AlertDescription } from '@/components/ui/alert'
import { Button } from '@/components/ui/button'
import { Lightbulb, AlertCircle } from 'lucide-react'
import type { UserRoleDto } from '@/types'
import { RoleCard } from './role-card'

interface RoleListProps {
  roles: UserRoleDto[]
  onChange: (roles: UserRoleDto[]) => void
  minRecommended?: number
  maxAllowed?: number
}

export function RoleList({
  roles,
  onChange,
  minRecommended = 3,
  maxAllowed = 8,
}: RoleListProps) {
  const handleAdd = () => {
    if (roles.length >= maxAllowed) return
    const newRole: UserRoleDto = {
      id: crypto.randomUUID(),
      key: null,
      label: '',
      rank: roles.length + 1,
      seasonPriority: 3,
      minWeeklyMinutes: 0,
      targetWeeklyMinutes: 300, // 5 hours default
      tags: [],
      status: 'Active',
    }
    onChange([...roles, newRole])
  }

  const handleUpdate = (id: string, updates: Partial<UserRoleDto>) => {
    onChange(roles.map((r) => (r.id === id ? { ...r, ...updates } : r)))
  }

  const handleRemove = (id: string) => {
    const filtered = roles.filter((r) => r.id !== id)
    const reranked = filtered.map((r, index) => ({ ...r, rank: index + 1 }))
    onChange(reranked)
  }

  const totalHours = roles.reduce((sum, r) => sum + r.targetWeeklyMinutes / 60, 0)

  return (
    <div className="space-y-4">
      {/* Role cards */}
      <div className="space-y-3">
        {roles.map((role) => (
          <RoleCard
            key={role.id}
            role={role}
            onUpdate={(updates) => handleUpdate(role.id, updates)}
            onRemove={() => handleRemove(role.id)}
          />
        ))}
      </div>

      {/* Add button */}
      {roles.length < maxAllowed && (
        <Button variant="outline" onClick={handleAdd} className="w-full">
          <Plus className="size-4 mr-2" />
          Add Role
        </Button>
      )}

      {/* Total hours summary */}
      {roles.length > 0 && (
        <div className="flex items-center justify-between p-3 bg-muted rounded-lg">
          <span className="text-sm text-muted-foreground">Total weekly target</span>
          <span className="text-lg font-semibold text-primary">{Math.round(totalHours)}h</span>
        </div>
      )}

      {/* Soft validations */}
      {roles.length < minRecommended && roles.length > 0 && (
        <Alert className="border-amber-500/50 bg-amber-500/10">
          <Lightbulb className="size-4 text-amber-500" />
          <AlertDescription className="text-amber-700 dark:text-amber-300">
            We recommend at least {minRecommended} roles. You have {roles.length}.
          </AlertDescription>
        </Alert>
      )}

      {roles.length >= maxAllowed && (
        <Alert className="border-blue-500/50 bg-blue-500/10">
          <AlertCircle className="size-4 text-blue-500" />
          <AlertDescription className="text-blue-700 dark:text-blue-300">
            Maximum of {maxAllowed} roles reached.
          </AlertDescription>
        </Alert>
      )}

      {totalHours > 168 && (
        <Alert className="border-red-500/50 bg-red-500/10">
          <AlertCircle className="size-4 text-red-500" />
          <AlertDescription className="text-red-700 dark:text-red-300">
            Total hours ({Math.round(totalHours)}h) exceed available hours in a week (168h).
            Consider adjusting your targets.
          </AlertDescription>
        </Alert>
      )}
    </div>
  )
}
