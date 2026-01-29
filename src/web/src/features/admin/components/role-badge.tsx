import { cn } from '@/lib/utils'
import type { AppRole } from '@/types'

interface RoleBadgeProps {
  role: AppRole
  className?: string
}

const roleColors: Record<AppRole, string> = {
  Super: 'bg-red-100 text-red-800 dark:bg-red-900 dark:text-red-200',
  Admin: 'bg-blue-100 text-blue-800 dark:bg-blue-900 dark:text-blue-200',
  User: 'bg-gray-100 text-gray-800 dark:bg-gray-700 dark:text-gray-200',
}

export function RoleBadge({ role, className }: RoleBadgeProps) {
  return (
    <span
      className={cn(
        'inline-flex items-center px-2 py-0.5 rounded text-xs font-medium',
        roleColors[role],
        className
      )}
    >
      {role}
    </span>
  )
}
