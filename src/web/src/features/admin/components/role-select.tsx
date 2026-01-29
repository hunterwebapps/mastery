import { Checkbox } from '@/components/ui/checkbox'
import { Label } from '@/components/ui/label'
import { useAuthStore } from '@/stores/auth-store'
import type { AppRole } from '@/types'

interface RoleSelectProps {
  selectedRoles: AppRole[]
  onChange: (roles: AppRole[]) => void
  disabled?: boolean
}

const allRoles: AppRole[] = ['Super', 'Admin', 'User']

export function RoleSelect({ selectedRoles, onChange, disabled }: RoleSelectProps) {
  const { isSuper } = useAuthStore()
  const canAssignSuper = isSuper()

  const handleRoleToggle = (role: AppRole, checked: boolean) => {
    if (checked) {
      onChange([...selectedRoles, role])
    } else {
      onChange(selectedRoles.filter((r) => r !== role))
    }
  }

  return (
    <div className="space-y-3">
      <Label className="text-sm font-medium">Roles</Label>
      <div className="space-y-2">
        {allRoles.map((role) => {
          const isRoleDisabled = disabled || (role === 'Super' && !canAssignSuper)
          return (
            <div key={role} className="flex items-center gap-2">
              <Checkbox
                id={`role-${role}`}
                checked={selectedRoles.includes(role)}
                onCheckedChange={(checked) => handleRoleToggle(role, checked as boolean)}
                disabled={isRoleDisabled}
              />
              <Label
                htmlFor={`role-${role}`}
                className={isRoleDisabled ? 'text-muted-foreground' : ''}
              >
                {role}
                {role === 'Super' && !canAssignSuper && (
                  <span className="text-xs text-muted-foreground ml-2">(Super only)</span>
                )}
              </Label>
            </div>
          )
        })}
      </div>
    </div>
  )
}
