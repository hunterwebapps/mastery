import { Calendar, Target, BarChart3, Gauge, Shield } from 'lucide-react'
import { Link } from 'react-router-dom'
import { Badge } from '@/components/ui/badge'
import { Card, CardContent } from '@/components/ui/card'
import type { GoalSummaryDto } from '@/types'
import { goalStatusInfo } from '@/types'

interface GoalCardProps {
  goal: GoalSummaryDto
}

function formatDate(dateString: string): string {
  return new Date(dateString).toLocaleDateString('en-US', {
    month: 'short',
    day: 'numeric',
    year: 'numeric',
  })
}

function getPriorityLabel(priority: number): string {
  switch (priority) {
    case 1:
      return 'Critical'
    case 2:
      return 'High'
    case 3:
      return 'Medium'
    case 4:
      return 'Low'
    case 5:
      return 'Someday'
    default:
      return 'Medium'
  }
}

function getPriorityColor(priority: number): string {
  switch (priority) {
    case 1:
      return 'bg-red-500/20 text-red-400'
    case 2:
      return 'bg-orange-500/20 text-orange-400'
    case 3:
      return 'bg-yellow-500/20 text-yellow-400'
    case 4:
      return 'bg-blue-500/20 text-blue-400'
    case 5:
      return 'bg-zinc-500/20 text-zinc-400'
    default:
      return 'bg-muted text-muted-foreground'
  }
}

export function GoalCard({ goal }: GoalCardProps) {
  const statusInfo = goalStatusInfo[goal.status]

  return (
    <Link to={`/goals/${goal.id}`} className="block">
      <Card className="hover:border-primary/50 transition-colors cursor-pointer h-full">
        <CardContent className="pt-6">
          <div className="space-y-4">
            <div className="flex items-start justify-between gap-3">
              <div className="space-y-1 min-w-0 flex-1">
                <h4 className="font-semibold text-foreground truncate">{goal.title}</h4>
                <Badge className={`${statusInfo.color} ${statusInfo.bgColor}`}>
                  {statusInfo.label}
                </Badge>
              </div>
              <Badge className={`text-xs shrink-0 ${getPriorityColor(goal.priority)}`}>
                {getPriorityLabel(goal.priority)}
              </Badge>
            </div>

            {goal.deadline && (
              <div className="flex items-center gap-2 text-sm text-muted-foreground">
                <Calendar className="size-4" />
                <span>Due {formatDate(goal.deadline)}</span>
              </div>
            )}

            {goal.metricCount > 0 && (
              <div className="pt-2 border-t border-border">
                <p className="text-xs text-muted-foreground mb-2">Scoreboard</p>
                <div className="flex flex-wrap gap-2">
                  {goal.lagMetricCount > 0 && (
                    <div className="flex items-center gap-1 text-xs text-purple-400">
                      <Target className="size-3" />
                      <span>{goal.lagMetricCount} outcome{goal.lagMetricCount > 1 ? 's' : ''}</span>
                    </div>
                  )}
                  {goal.leadMetricCount > 0 && (
                    <div className="flex items-center gap-1 text-xs text-blue-400">
                      <BarChart3 className="size-3" />
                      <span>{goal.leadMetricCount} leading</span>
                    </div>
                  )}
                  {goal.constraintMetricCount > 0 && (
                    <div className="flex items-center gap-1 text-xs text-orange-400">
                      <Shield className="size-3" />
                      <span>{goal.constraintMetricCount} constraint{goal.constraintMetricCount > 1 ? 's' : ''}</span>
                    </div>
                  )}
                </div>
              </div>
            )}

            {goal.metricCount === 0 && (
              <div className="pt-2 border-t border-border">
                <div className="flex items-center gap-2 text-xs text-muted-foreground">
                  <Gauge className="size-3" />
                  <span>No metrics defined</span>
                </div>
              </div>
            )}
          </div>
        </CardContent>
      </Card>
    </Link>
  )
}
