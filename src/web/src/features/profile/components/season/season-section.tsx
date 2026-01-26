import { useState } from 'react'
import { Leaf, Plus, StopCircle, History } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Skeleton } from '@/components/ui/skeleton'
import {
  Collapsible,
  CollapsibleContent,
  CollapsibleTrigger,
} from '@/components/ui/collapsible'
import type { SeasonDto, CreateSeasonRequest } from '@/types'
import { SeasonCard } from './season-card'
import { EndSeasonDialog } from './end-season-dialog'
import { NewSeasonDialog } from './new-season-dialog'

interface SeasonSectionProps {
  seasons: SeasonDto[] | undefined
  isLoading: boolean
  onCreateSeason: (request: CreateSeasonRequest) => Promise<void>
  onEndSeason: (seasonId: string, outcome: string | undefined) => Promise<void>
  isCreating: boolean
  isEnding: boolean
}

export function SeasonSection({
  seasons,
  isLoading,
  onCreateSeason,
  onEndSeason,
  isCreating,
  isEnding,
}: SeasonSectionProps) {
  const [isNewDialogOpen, setNewDialogOpen] = useState(false)
  const [isEndDialogOpen, setEndDialogOpen] = useState(false)
  const [showHistory, setShowHistory] = useState(false)

  const currentSeason = seasons?.find((s) => !s.isEnded)
  const pastSeasons = seasons?.filter((s) => s.isEnded) || []

  if (isLoading) {
    return (
      <Card>
        <CardHeader>
          <Skeleton className="h-6 w-32" />
        </CardHeader>
        <CardContent>
          <Skeleton className="h-32 w-full" />
        </CardContent>
      </Card>
    )
  }

  return (
    <Card>
      <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-4">
        <div className="flex items-center gap-2">
          <Leaf className="size-5 text-primary" />
          <CardTitle>Season</CardTitle>
        </div>
        <div className="flex items-center gap-2">
          {currentSeason && (
            <Button
              variant="outline"
              size="sm"
              onClick={() => setEndDialogOpen(true)}
            >
              <StopCircle className="mr-2 size-4" />
              End Season
            </Button>
          )}
          <Button
            variant={currentSeason ? 'outline' : 'default'}
            size="sm"
            onClick={() => setNewDialogOpen(true)}
          >
            <Plus className="mr-2 size-4" />
            New Season
          </Button>
        </div>
      </CardHeader>
      <CardContent className="space-y-6">
        {currentSeason ? (
          <SeasonCard season={currentSeason} isCurrent />
        ) : (
          <div className="rounded-lg border border-dashed border-border p-8 text-center">
            <Leaf className="mx-auto size-10 text-muted-foreground/50 mb-4" />
            <h4 className="font-medium mb-2">No Active Season</h4>
            <p className="text-sm text-muted-foreground mb-4">
              Start a new season to define your current focus and intensity level.
            </p>
            <Button onClick={() => setNewDialogOpen(true)}>
              <Plus className="mr-2 size-4" />
              Start Your First Season
            </Button>
          </div>
        )}

        {pastSeasons.length > 0 && (
          <Collapsible open={showHistory} onOpenChange={setShowHistory}>
            <CollapsibleTrigger asChild>
              <Button variant="ghost" className="w-full justify-start gap-2">
                <History className="size-4" />
                {showHistory ? 'Hide' : 'Show'} Past Seasons ({pastSeasons.length})
              </Button>
            </CollapsibleTrigger>
            <CollapsibleContent className="space-y-4 pt-4">
              {pastSeasons.map((season) => (
                <SeasonCard key={season.id} season={season} />
              ))}
            </CollapsibleContent>
          </Collapsible>
        )}
      </CardContent>

      <NewSeasonDialog
        open={isNewDialogOpen}
        onOpenChange={setNewDialogOpen}
        onCreate={onCreateSeason}
        isCreating={isCreating}
      />

      {currentSeason && (
        <EndSeasonDialog
          open={isEndDialogOpen}
          onOpenChange={setEndDialogOpen}
          season={currentSeason}
          onConfirm={(outcome) => onEndSeason(currentSeason.id, outcome)}
          isEnding={isEnding}
        />
      )}
    </Card>
  )
}
