import { Badge } from '@/components/ui/badge'
import { cn } from '@/lib/utils'
import type { UserRoleDto } from '@/types'

interface RoleCardProps {
  role: UserRoleDto
}

function formatHours(minutes: number): string {
  const hours = Math.round(minutes / 60)
  return `${hours}h`
}

function getPriorityColor(priority: number): string {
  if (priority >= 4) return 'bg-primary/20 text-primary'
  if (priority >= 3) return 'bg-blue-500/20 text-blue-400'
  return 'bg-muted text-muted-foreground'
}

export function RoleCard({ role }: RoleCardProps) {
  const isActive = role.status === 'Active'

  return (
    <div
      className={cn(
        'rounded-lg border border-border p-4 transition-colors',
        isActive ? 'bg-card/50' : 'bg-muted/30 opacity-60'
      )}
    >
      <div className="flex items-start justify-between">
        <div className="space-y-1">
          <div className="flex items-center gap-2">
            <h4 className="font-semibold text-foreground">{role.label}</h4>
            {!isActive && (
              <Badge variant="outline" className="text-xs">
                Inactive
              </Badge>
            )}
          </div>
          <p className="text-sm text-muted-foreground">
            {formatHours(role.targetWeeklyMinutes)}/week target
            {role.minWeeklyMinutes > 0 && (
              <span className="text-xs"> (min {formatHours(role.minWeeklyMinutes)})</span>
            )}
          </p>
        </div>
        <Badge className={cn('text-xs', getPriorityColor(role.seasonPriority))}>
          P{role.seasonPriority}
        </Badge>
      </div>
      {role.tags.length > 0 && (
        <div className="mt-3 flex flex-wrap gap-1">
          {role.tags.map((tag) => (
            <Badge key={tag} variant="secondary" className="text-xs">
              {tag}
            </Badge>
          ))}
        </div>
      )}
    </div>
  )
}
