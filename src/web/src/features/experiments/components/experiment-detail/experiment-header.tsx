import { useState } from 'react'
import { Link } from 'react-router-dom'
import {
  ArrowLeft,
  Play,
  Pause,
  CheckCircle,
  XCircle,
  Archive,
  Pencil,
  MoreHorizontal,
  Loader2,
} from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu'
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from '@/components/ui/alert-dialog'
import { Textarea } from '@/components/ui/textarea'
import { cn } from '@/lib/utils'
import type { ExperimentDto } from '@/types'
import {
  experimentStatusInfo,
  experimentCategoryInfo,
} from '@/types'

interface ExperimentHeaderProps {
  experiment: ExperimentDto
  onStart: () => void
  onPause: () => void
  onResume: () => void
  onComplete: () => void
  onAbandon: (reason?: string) => void
  isActionPending?: boolean
}

export function ExperimentHeader({
  experiment,
  onStart,
  onPause,
  onResume,
  onComplete,
  onAbandon,
  isActionPending,
}: ExperimentHeaderProps) {
  const [showAbandonDialog, setShowAbandonDialog] = useState(false)
  const [abandonReason, setAbandonReason] = useState('')
  const statusInfo = experimentStatusInfo[experiment.status]
  const categoryInfo = experimentCategoryInfo[experiment.category]

  const handleAbandon = () => {
    onAbandon(abandonReason || undefined)
    setShowAbandonDialog(false)
    setAbandonReason('')
  }

  return (
    <>
      <div className="space-y-4">
        {/* Top bar: back + actions */}
        <div className="flex items-center justify-between">
          <Button variant="ghost" size="sm" asChild>
            <Link to="/experiments" className="gap-2">
              <ArrowLeft className="size-4" />
              Experiments
            </Link>
          </Button>

          <div className="flex items-center gap-2">
            {/* Status-specific primary actions */}
            {experiment.status === 'Draft' && (
              <>
                <Button variant="outline" size="sm" asChild>
                  <Link to={`/experiments/${experiment.id}/edit`}>
                    <Pencil className="size-4 mr-1.5" />
                    Edit
                  </Link>
                </Button>
                <Button size="sm" onClick={onStart} disabled={isActionPending}>
                  {isActionPending ? (
                    <Loader2 className="size-4 mr-1.5 animate-spin" />
                  ) : (
                    <Play className="size-4 mr-1.5" />
                  )}
                  Start Experiment
                </Button>
              </>
            )}

            {experiment.status === 'Active' && (
              <>
                <Button variant="outline" size="sm" onClick={onPause} disabled={isActionPending}>
                  <Pause className="size-4 mr-1.5" />
                  Pause
                </Button>
                <Button size="sm" onClick={onComplete} disabled={isActionPending}>
                  {isActionPending ? (
                    <Loader2 className="size-4 mr-1.5 animate-spin" />
                  ) : (
                    <CheckCircle className="size-4 mr-1.5" />
                  )}
                  Complete
                </Button>
                <DropdownMenu>
                  <DropdownMenuTrigger asChild>
                    <Button variant="ghost" size="icon" className="size-8">
                      <MoreHorizontal className="size-4" />
                    </Button>
                  </DropdownMenuTrigger>
                  <DropdownMenuContent align="end">
                    <DropdownMenuItem
                      onClick={() => setShowAbandonDialog(true)}
                      className="text-destructive focus:text-destructive"
                    >
                      <XCircle className="size-4 mr-2" />
                      Abandon
                    </DropdownMenuItem>
                  </DropdownMenuContent>
                </DropdownMenu>
              </>
            )}

            {experiment.status === 'Paused' && (
              <>
                <Button variant="outline" size="sm" onClick={onResume} disabled={isActionPending}>
                  {isActionPending ? (
                    <Loader2 className="size-4 mr-1.5 animate-spin" />
                  ) : (
                    <Play className="size-4 mr-1.5" />
                  )}
                  Resume
                </Button>
                <Button size="sm" onClick={onComplete} disabled={isActionPending}>
                  <CheckCircle className="size-4 mr-1.5" />
                  Complete
                </Button>
                <DropdownMenu>
                  <DropdownMenuTrigger asChild>
                    <Button variant="ghost" size="icon" className="size-8">
                      <MoreHorizontal className="size-4" />
                    </Button>
                  </DropdownMenuTrigger>
                  <DropdownMenuContent align="end">
                    <DropdownMenuItem
                      onClick={() => setShowAbandonDialog(true)}
                      className="text-destructive focus:text-destructive"
                    >
                      <XCircle className="size-4 mr-2" />
                      Abandon
                    </DropdownMenuItem>
                  </DropdownMenuContent>
                </DropdownMenu>
              </>
            )}

            {(experiment.status === 'Completed' || experiment.status === 'Abandoned') && (
              <DropdownMenu>
                <DropdownMenuTrigger asChild>
                  <Button variant="ghost" size="icon" className="size-8">
                    <MoreHorizontal className="size-4" />
                  </Button>
                </DropdownMenuTrigger>
                <DropdownMenuContent align="end">
                  <DropdownMenuItem>
                    <Archive className="size-4 mr-2" />
                    Archive
                  </DropdownMenuItem>
                </DropdownMenuContent>
              </DropdownMenu>
            )}
          </div>
        </div>

        {/* Title + badges + description */}
        <div className="space-y-3">
          <div className="flex items-center gap-3 flex-wrap">
            <Badge
              className={cn(
                'text-xs font-medium px-2.5 py-1',
                statusInfo.color,
                statusInfo.bgColor
              )}
            >
              {statusInfo.label}
            </Badge>
            <Badge
              variant="outline"
              className={cn(
                'text-xs font-normal px-2.5 py-1 border-border/50',
                categoryInfo.color
              )}
            >
              {categoryInfo.label}
            </Badge>
          </div>
          <h1 className="text-2xl font-bold text-foreground">{experiment.title}</h1>
          {experiment.description && (
            <p className="text-muted-foreground leading-relaxed max-w-2xl">
              {experiment.description}
            </p>
          )}
        </div>
      </div>

      {/* Abandon dialog */}
      <AlertDialog open={showAbandonDialog} onOpenChange={setShowAbandonDialog}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Abandon Experiment</AlertDialogTitle>
            <AlertDialogDescription>
              Are you sure you want to abandon this experiment? This action cannot be undone.
              The experiment will be marked as abandoned and no results will be recorded.
            </AlertDialogDescription>
          </AlertDialogHeader>
          <div className="py-2">
            <Textarea
              placeholder="Reason for abandoning (optional)..."
              value={abandonReason}
              onChange={(e) => setAbandonReason(e.target.value)}
              rows={3}
            />
          </div>
          <AlertDialogFooter>
            <AlertDialogCancel>Cancel</AlertDialogCancel>
            <AlertDialogAction
              onClick={handleAbandon}
              className="bg-destructive text-destructive-foreground hover:bg-destructive/90"
            >
              Abandon Experiment
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </>
  )
}
