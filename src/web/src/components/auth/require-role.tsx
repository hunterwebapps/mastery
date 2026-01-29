import { Navigate } from 'react-router-dom'
import { useAuthStore } from '@/stores/auth-store'
import type { AppRole } from '@/types'

interface RequireRoleProps {
  children: React.ReactNode
  roles: AppRole[]
}

export function RequireRole({ children, roles }: RequireRoleProps) {
  const { hasAnyRole } = useAuthStore()

  if (!hasAnyRole(roles)) {
    return <Navigate to="/" replace />
  }

  return <>{children}</>
}
