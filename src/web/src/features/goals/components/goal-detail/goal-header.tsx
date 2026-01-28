import { useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import {
  ArrowLeft,
  Calendar,
  MoreHorizontal,
  Play,
  Pause,
  Check,
  Archive,
  Pencil,
  Trash2,
} from 'lucide-react'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog'
import {
  Popover,
  PopoverContent,
  PopoverTrigger,
} from '@/components/ui/popover'
import type { GoalDto, GoalStatus } from '@/types'
import { goalStatusInfo } from '@/types'

interface GoalHeaderProps {
  goal: GoalDto
  onUpdateStatus: (status: GoalStatus, notes?: string) => void
  onDelete: () => void
  isUpdating?: boolean
}

function formatDate(dateString: string): string {
  return new Date(dateString).toLocaleDateString('en-US', {
    month: 'long',
    day: 'numeric',
    year: 'numeric',
  })
}

export function GoalHeader({ goal, onUpdateStatus, onDelete, isUpdating }: GoalHeaderProps) {
  const navigate = useNavigate()
  const [showDeleteDialog, setShowDeleteDialog] = useState(false)
  const [menuOpen, setMenuOpen] = useState(false)
  const statusInfo = goalStatusInfo[goal.status]

  const canActivate = goal.status === 'Draft' || goal.status === 'Paused'
  const canPause = goal.status === 'Active'
  const canComplete = goal.status === 'Active' || goal.status === 'Paused'
  const canArchive = goal.status !== 'Archived'

  return (
    <>
      <div className="space-y-4">
        <div className="flex items-center gap-4">
          <Button variant="ghost" size="icon" asChild>
            <Link to="/goals">
              <ArrowLeft className="size-4" />
            </Link>
          </Button>
          <div className="flex-1 min-w-0">
            <div className="flex items-center gap-3">
              <h1 className="text-2xl font-bold text-foreground truncate">{goal.title}</h1>
              <Badge className={`${statusInfo.color} ${statusInfo.bgColor}`}>
                {statusInfo.label}
              </Badge>
            </div>
            {goal.deadline && (
              <div className="flex items-center gap-2 text-sm text-muted-foreground mt-1">
                <Calendar className="size-4" />
                <span>Due {formatDate(goal.deadline)}</span>
              </div>
            )}
          </div>
          <div className="flex items-center gap-2">
            {canActivate && (
              <Button
                variant="default"
                size="sm"
                onClick={() => onUpdateStatus('Active')}
                disabled={isUpdating}
              >
                <Play className="size-4 mr-1" />
                Activate
              </Button>
            )}
            {canPause && (
              <Button
                variant="outline"
                size="sm"
                onClick={() => onUpdateStatus('Paused')}
                disabled={isUpdating}
              >
                <Pause className="size-4 mr-1" />
                Pause
              </Button>
            )}
            {canComplete && (
              <Button
                variant="outline"
                size="sm"
                onClick={() => onUpdateStatus('Completed')}
                disabled={isUpdating}
              >
                <Check className="size-4 mr-1" />
                Complete
              </Button>
            )}
            <Popover open={menuOpen} onOpenChange={setMenuOpen}>
              <PopoverTrigger asChild>
                <Button variant="ghost" size="icon">
                  <MoreHorizontal className="size-4" />
                </Button>
              </PopoverTrigger>
              <PopoverContent className="w-48 p-1" align="end">
                <div className="flex flex-col">
                  <button
                    className="flex items-center gap-2 px-3 py-2 text-sm rounded-md hover:bg-muted transition-colors text-left"
                    onClick={() => {
                      setMenuOpen(false)
                      navigate(`/goals/${goal.id}/edit`)
                    }}
                  >
                    <Pencil className="size-4" />
                    Edit Goal
                  </button>
                  {canArchive && (
                    <button
                      className="flex items-center gap-2 px-3 py-2 text-sm rounded-md hover:bg-muted transition-colors text-left"
                      onClick={() => {
                        setMenuOpen(false)
                        onUpdateStatus('Archived')
                      }}
                    >
                      <Archive className="size-4" />
                      Archive
                    </button>
                  )}
                  <div className="border-t border-border my-1" />
                  <button
                    className="flex items-center gap-2 px-3 py-2 text-sm rounded-md hover:bg-destructive/10 transition-colors text-left text-destructive"
                    onClick={() => {
                      setMenuOpen(false)
                      setShowDeleteDialog(true)
                    }}
                  >
                    <Trash2 className="size-4" />
                    Delete
                  </button>
                </div>
              </PopoverContent>
            </Popover>
          </div>
        </div>

        {goal.description && (
          <p className="text-muted-foreground">{goal.description}</p>
        )}

        {goal.why && (
          <div className="p-4 rounded-lg bg-primary/5 border border-primary/10">
            <p className="text-sm font-medium text-primary mb-1">Why this matters</p>
            <p className="text-sm text-foreground">{goal.why}</p>
          </div>
        )}
      </div>

      <Dialog open={showDeleteDialog} onOpenChange={setShowDeleteDialog}>
        <DialogContent showCloseButton={false}>
          <DialogHeader>
            <DialogTitle>Delete Goal</DialogTitle>
            <DialogDescription>
              Are you sure you want to delete "{goal.title}"? This action cannot be undone.
            </DialogDescription>
          </DialogHeader>
          <DialogFooter>
            <Button variant="outline" onClick={() => setShowDeleteDialog(false)}>
              Cancel
            </Button>
            <Button
              variant="destructive"
              onClick={() => {
                onDelete()
                navigate('/goals')
              }}
            >
              Delete
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </>
  )
}
