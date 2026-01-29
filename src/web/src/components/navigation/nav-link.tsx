import { Link, useLocation } from 'react-router-dom'
import { cn } from '@/lib/utils'
import type { LucideIcon } from 'lucide-react'

interface NavLinkProps {
  href: string
  label: string
  icon?: LucideIcon
}

export function NavLink({ href, label, icon: Icon }: NavLinkProps) {
  const location = useLocation()
  const isActive = location.pathname === href ||
    (href !== '/' && location.pathname.startsWith(href))

  return (
    <Link
      to={href}
      className={cn(
        'flex items-center gap-2 text-sm font-medium transition-colors hover:text-primary',
        isActive ? 'text-primary' : 'text-muted-foreground'
      )}
    >
      {Icon && <Icon className="h-4 w-4" />}
      {label}
    </Link>
  )
}
