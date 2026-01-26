import { Calendar, Target, Flame } from 'lucide-react'
import { Badge } from '@/components/ui/badge'
import { Card, CardContent } from '@/components/ui/card'
import { Progress } from '@/components/ui/progress'
import type { SeasonDto } from '@/types'
import { seasonTypeInfo } from '@/types'

interface SeasonCardProps {
  season: SeasonDto
  isCurrent?: boolean
}

function formatDate(dateString: string): string {
  return new Date(dateString).toLocaleDateString('en-US', {
    month: 'short',
    day: 'numeric',
    year: 'numeric',
  })
}

function getProgress(season: SeasonDto): number {
  if (!season.expectedEndDate) return 0
  const start = new Date(season.startDate).getTime()
  const expected = new Date(season.expectedEndDate).getTime()
  const now = Date.now()
  if (now >= expected) return 100
  const total = expected - start
  const elapsed = now - start
  return Math.round((elapsed / total) * 100)
}

export function SeasonCard({ season, isCurrent = false }: SeasonCardProps) {
  const typeInfo = seasonTypeInfo[season.type]
  const progress = getProgress(season)

  return (
    <Card className={isCurrent ? 'ring-2 ring-primary' : ''}>
      <CardContent className="pt-6">
        <div className="space-y-4">
          <div className="flex items-start justify-between gap-4">
            <div className="space-y-1">
              <div className="flex items-center gap-2">
                <h4 className="font-semibold">{season.label}</h4>
                {isCurrent && (
                  <Badge variant="default" className="text-xs">
                    Current
                  </Badge>
                )}
              </div>
              <Badge variant="secondary" className={typeInfo.color}>
                {typeInfo.label}
              </Badge>
            </div>
            <div className="flex items-center gap-1">
              <Flame className="size-4 text-orange-400" />
              <span className="text-sm font-medium">{season.intensity}/10</span>
            </div>
          </div>

          <p className="text-sm text-muted-foreground">{typeInfo.description}</p>

          {season.successStatement && (
            <div className="flex items-start gap-2 text-sm">
              <Target className="size-4 shrink-0 mt-0.5 text-primary" />
              <span>{season.successStatement}</span>
            </div>
          )}

          <div className="space-y-2">
            <div className="flex items-center justify-between text-xs text-muted-foreground">
              <div className="flex items-center gap-1">
                <Calendar className="size-3" />
                <span>{formatDate(season.startDate)}</span>
              </div>
              {season.expectedEndDate && (
                <span>{formatDate(season.expectedEndDate)}</span>
              )}
            </div>
            {season.expectedEndDate && !season.isEnded && (
              <Progress value={progress} className="h-1" />
            )}
          </div>

          {season.nonNegotiables.length > 0 && (
            <div className="pt-2 border-t border-border">
              <p className="text-xs text-muted-foreground mb-2">Non-negotiables</p>
              <div className="flex flex-wrap gap-1">
                {season.nonNegotiables.map((item, index) => (
                  <Badge key={index} variant="outline" className="text-xs">
                    {item}
                  </Badge>
                ))}
              </div>
            </div>
          )}

          {season.isEnded && season.outcome && (
            <div className="pt-2 border-t border-border">
              <p className="text-xs text-muted-foreground mb-1">Outcome</p>
              <p className="text-sm">{season.outcome}</p>
            </div>
          )}
        </div>
      </CardContent>
    </Card>
  )
}
