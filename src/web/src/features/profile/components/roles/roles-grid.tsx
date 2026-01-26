import type { UserRoleDto } from '@/types'
import { RoleCard } from './role-card'

interface RolesGridProps {
  roles: UserRoleDto[]
}

export function RolesGrid({ roles }: RolesGridProps) {
  const sortedRoles = [...roles].sort((a, b) => {
    // Active roles first, then by rank
    if (a.status !== b.status) {
      return a.status === 'Active' ? -1 : 1
    }
    return a.rank - b.rank
  })

  return (
    <div className="grid gap-4 md:grid-cols-2">
      {sortedRoles.map((role) => (
        <RoleCard key={role.id} role={role} />
      ))}
    </div>
  )
}
