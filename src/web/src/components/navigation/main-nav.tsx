import { Link } from 'react-router-dom'
import {
  CalendarCheck,
  FlaskConical,
  FolderKanban,
  LayoutDashboard,
  Lightbulb,
  ListTodo,
  Sparkles,
  Target,
} from 'lucide-react'
import { NavLink } from './nav-link'
import { NavDropdown } from './nav-dropdown'
import { UserMenu } from './user-menu'

const planningItems = [
  { href: '/goals', label: 'Goals', icon: Target },
  { href: '/projects', label: 'Projects', icon: FolderKanban },
  { href: '/tasks', label: 'Tasks', icon: ListTodo },
]

const behaviorsItems = [
  { href: '/habits', label: 'Habits', icon: Sparkles },
  { href: '/experiments', label: 'Experiments', icon: FlaskConical },
]

export function MainNav() {
  return (
    <nav className="mx-auto flex max-w-7xl items-center justify-between px-4 py-4">
      <div className="flex items-center gap-8">
        <Link to="/" className="text-xl font-bold text-primary">
          Mastery
        </Link>
        <div className="flex items-center gap-6">
          <NavLink href="/" label="Dashboard" icon={LayoutDashboard} />
          <NavLink href="/check-in" label="Check-in" icon={CalendarCheck} />
          <NavDropdown label="Planning" items={planningItems} />
          <NavDropdown label="Behaviors" items={behaviorsItems} />
          <NavLink href="/recommendations" label="Insights" icon={Lightbulb} />
        </div>
      </div>
      <UserMenu />
    </nav>
  )
}
