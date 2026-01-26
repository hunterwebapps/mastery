import { Trash2 } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Badge } from '@/components/ui/badge'
import type { UserRoleDto } from '@/types'
import { RoleHoursInput } from './role-hours-input'

interface RoleCardProps {
  role: UserRoleDto
  onUpdate: (updates: Partial<UserRoleDto>) => void
  onRemove: () => void
}

// Preset role suggestions
const ROLE_SUGGESTIONS = [
  'Engineer',
  'Manager',
  'Parent',
  'Partner',
  'Friend',
  'Student',
  'Creator',
  'Athlete',
  'Volunteer',
  'Caregiver',
]

export function RoleCard({ role, onUpdate, onRemove }: RoleCardProps) {
  const unusedSuggestions = ROLE_SUGGESTIONS.filter(
    (s) => s.toLowerCase() !== role.label.toLowerCase()
  )

  return (
    <div className="rounded-lg border border-border p-4 space-y-4 bg-card">
      <div className="flex items-start justify-between gap-3">
        <div className="flex-1">
          <div className="flex items-center gap-2 mb-2">
            <Badge variant="outline" className="shrink-0">
              #{role.rank}
            </Badge>
            <Input
              placeholder="Role name (e.g., Engineer, Parent)"
              value={role.label}
              onChange={(e) => onUpdate({ label: e.target.value })}
              className="h-9"
            />
          </div>

          {!role.label && (
            <div className="flex flex-wrap gap-1 mt-2">
              <span className="text-xs text-muted-foreground mr-1">Suggestions:</span>
              {unusedSuggestions.slice(0, 5).map((suggestion) => (
                <Button
                  key={suggestion}
                  variant="ghost"
                  size="sm"
                  className="h-6 px-2 text-xs"
                  onClick={() => onUpdate({ label: suggestion })}
                >
                  {suggestion}
                </Button>
              ))}
            </div>
          )}
        </div>
        <Button
          variant="ghost"
          size="icon"
          className="size-8 text-muted-foreground hover:text-destructive shrink-0"
          onClick={onRemove}
        >
          <Trash2 className="size-4" />
        </Button>
      </div>

      <RoleHoursInput
        hours={Math.round(role.targetWeeklyMinutes / 60)}
        onChange={(hours) => onUpdate({ targetWeeklyMinutes: hours * 60 })}
      />
    </div>
  )
}
