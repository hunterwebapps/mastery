import { Link } from 'react-router-dom'
import { Calendar, MoreHorizontal, Pause, Play, Archive, Pencil } from 'lucide-react'
import { Card, CardContent } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu'
import type { HabitSummaryDto } from '@/types/habit'
import { habitStatusInfo, habitModeInfo, scheduleTypeInfo } from '@/types/habit'
import { StreakBadge } from '../common/streak-badge'
import { cn } from '@/lib/utils'

interface HabitCardProps {
  habit: HabitSummaryDto
  onStatusChange?: (id: string, status: string) => void
}

export function HabitCard({ habit, onStatusChange }: HabitCardProps) {
  const statusInfo = habitStatusInfo[habit.status]
  const modeInfo = habitModeInfo[habit.defaultMode]
  const scheduleInfo = scheduleTypeInfo[habit.scheduleType]

  const handleStatusChange = (newStatus: string) => {
    onStatusChange?.(habit.id, newStatus)
  }

  return (
    <Card className="hover:border-primary/50 transition-colors h-full">
      <CardContent className="pt-6">
        <div className="space-y-4">
          {/* Header with title and actions */}
          <div className="flex items-start justify-between gap-3">
            <Link to={`/habits/${habit.id}`} className="flex-1 min-w-0 group">
              <h4 className="font-semibold text-foreground truncate group-hover:text-primary transition-colors">
                {habit.title}
              </h4>
              {habit.description && (
                <p className="text-sm text-muted-foreground truncate mt-0.5">
                  {habit.description}
                </p>
              )}
            </Link>

            <div className="flex items-center gap-2 shrink-0">
              <StreakBadge streak={habit.currentStreak} />

              <DropdownMenu>
                <DropdownMenuTrigger asChild>
                  <Button variant="ghost" size="icon-xs">
                    <MoreHorizontal className="size-4" />
                  </Button>
                </DropdownMenuTrigger>
                <DropdownMenuContent align="end">
                  <DropdownMenuItem asChild>
                    <Link to={`/habits/${habit.id}/edit`}>
                      <Pencil className="size-4 mr-2" />
                      Edit
                    </Link>
                  </DropdownMenuItem>
                  <DropdownMenuSeparator />
                  {habit.status !== 'Active' && (
                    <DropdownMenuItem onClick={() => handleStatusChange('Active')}>
                      <Play className="size-4 mr-2" />
                      Activate
                    </DropdownMenuItem>
                  )}
                  {habit.status === 'Active' && (
                    <DropdownMenuItem onClick={() => handleStatusChange('Paused')}>
                      <Pause className="size-4 mr-2" />
                      Pause
                    </DropdownMenuItem>
                  )}
                  {habit.status !== 'Archived' && (
                    <>
                      <DropdownMenuSeparator />
                      <DropdownMenuItem
                        onClick={() => handleStatusChange('Archived')}
                        className="text-destructive"
                      >
                        <Archive className="size-4 mr-2" />
                        Archive
                      </DropdownMenuItem>
                    </>
                  )}
                </DropdownMenuContent>
              </DropdownMenu>
            </div>
          </div>

          {/* Status and mode badges */}
          <div className="flex flex-wrap items-center gap-2">
            <Badge className={cn(statusInfo.color, statusInfo.bgColor)}>
              {statusInfo.label}
            </Badge>
            <Badge variant="outline" className={cn(modeInfo.color)}>
              {modeInfo.label}
            </Badge>
          </div>

          {/* Schedule info */}
          <div className="flex items-center gap-2 text-sm text-muted-foreground">
            <Calendar className="size-4" />
            <span>{habit.scheduleDescription || scheduleInfo.label}</span>
          </div>

          {/* Stats row */}
          <div className="flex items-center justify-between pt-2 border-t border-border">
            <div className="flex items-center gap-4 text-sm">
              {habit.variantCount > 0 && (
                <span className="text-muted-foreground">
                  {habit.variantCount} mode{habit.variantCount > 1 ? 's' : ''}
                </span>
              )}
              {habit.metricBindingCount > 0 && (
                <span className="text-muted-foreground">
                  {habit.metricBindingCount} metric{habit.metricBindingCount > 1 ? 's' : ''}
                </span>
              )}
            </div>

            {/* 7-day adherence */}
            <div className="text-sm">
              <span className={cn(
                'font-medium',
                habit.adherenceRate7Day >= 0.8 && 'text-green-400',
                habit.adherenceRate7Day >= 0.5 && habit.adherenceRate7Day < 0.8 && 'text-yellow-400',
                habit.adherenceRate7Day < 0.5 && 'text-orange-400'
              )}>
                {Math.round(habit.adherenceRate7Day * 100)}%
              </span>
              <span className="text-muted-foreground"> 7d</span>
            </div>
          </div>
        </div>
      </CardContent>
    </Card>
  )
}
