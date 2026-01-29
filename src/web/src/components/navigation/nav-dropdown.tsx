import { Link, useLocation } from 'react-router-dom'
import { ChevronDown } from 'lucide-react'
import { cn } from '@/lib/utils'
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu'
import type { LucideIcon } from 'lucide-react'

export interface NavDropdownItem {
  href: string
  label: string
  icon?: LucideIcon
}

interface NavDropdownProps {
  label: string
  items: NavDropdownItem[]
}

export function NavDropdown({ label, items }: NavDropdownProps) {
  const location = useLocation()
  const isActive = items.some(
    (item) =>
      location.pathname === item.href ||
      (item.href !== '/' && location.pathname.startsWith(item.href))
  )

  return (
    <DropdownMenu>
      <DropdownMenuTrigger
        className={cn(
          'flex items-center gap-1 text-sm font-medium transition-colors hover:text-primary focus:outline-none',
          isActive ? 'text-primary' : 'text-muted-foreground'
        )}
      >
        {label}
        <ChevronDown className="h-4 w-4" />
      </DropdownMenuTrigger>
      <DropdownMenuContent align="start">
        {items.map((item) => {
          const Icon = item.icon
          const itemActive =
            location.pathname === item.href ||
            (item.href !== '/' && location.pathname.startsWith(item.href))

          return (
            <DropdownMenuItem key={item.href} asChild>
              <Link
                to={item.href}
                className={cn(
                  'flex w-full cursor-pointer items-center gap-2',
                  itemActive && 'text-primary'
                )}
              >
                {Icon && <Icon className="h-4 w-4" />}
                {item.label}
              </Link>
            </DropdownMenuItem>
          )
        })}
      </DropdownMenuContent>
    </DropdownMenu>
  )
}
