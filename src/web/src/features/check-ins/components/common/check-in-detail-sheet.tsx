import { cn } from '@/lib/utils'
import {
  Sun, Moon, CheckSquare, Target, FolderKanban, Pencil,
  Check, X, Loader2,
} from 'lucide-react'
import { EnergyBadge } from './energy-badge'
import { useCheckIn } from '../../hooks/use-check-ins'
import { useTask } from '@/features/tasks/hooks/use-tasks'
import { useHabit } from '@/features/habits/hooks/use-habits'
import { useProject } from '@/features/projects/hooks/use-projects'
import { blockerCategoryInfo } from '@/types/check-in'
import type { CheckInDto, Top1Type } from '@/types/check-in'

interface CheckInDetailSheetProps {
  checkInId: string
}

const top1TypeIcons: Record<Top1Type, React.ReactNode> = {
  Task: <CheckSquare className="size-4 text-blue-400" />,
  Habit: <Target className="size-4 text-green-400" />,
  Project: <FolderKanban className="size-4 text-purple-400" />,
  FreeText: <Pencil className="size-4 text-muted-foreground" />,
}

function Top1EntityName({ type, entityId }: { type: Top1Type; entityId: string }) {
  const { data: task } = useTask(type === 'Task' ? entityId : '')
  const { data: habit } = useHabit(type === 'Habit' ? entityId : '')
  const { data: project } = useProject(type === 'Project' ? entityId : '')

  const name = type === 'Task' ? task?.title
    : type === 'Habit' ? habit?.title
    : type === 'Project' ? project?.title
    : null

  return <span>{name ?? 'Loading...'}</span>
}

function DetailRow({ label, children }: { label: string; children: React.ReactNode }) {
  return (
    <div className="flex items-start justify-between gap-4 py-3 border-b border-border/30 last:border-0">
      <span className="text-sm text-muted-foreground shrink-0">{label}</span>
      <div className="text-sm font-medium text-foreground text-right">
        {children}
      </div>
    </div>
  )
}

function MorningDetail({ checkIn }: { checkIn: CheckInDto }) {
  return (
    <div className="space-y-1">
      {checkIn.energyLevel != null && (
        <DetailRow label="Energy">
          <EnergyBadge level={checkIn.energyLevel} size="sm" />
        </DetailRow>
      )}

      {checkIn.selectedMode && (
        <DetailRow label="Day mode">
          <span className={cn(
            'inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-medium',
            checkIn.selectedMode === 'Full' && 'bg-green-500/15 text-green-400',
            checkIn.selectedMode === 'Maintenance' && 'bg-yellow-500/15 text-yellow-400',
            checkIn.selectedMode === 'Minimum' && 'bg-orange-500/15 text-orange-400',
          )}>
            {checkIn.selectedMode}
          </span>
        </DetailRow>
      )}

      {checkIn.top1Type && (
        <DetailRow label="#1 Priority">
          <div className="inline-flex items-center gap-1.5">
            {top1TypeIcons[checkIn.top1Type]}
            {checkIn.top1Type === 'FreeText' ? (
              <span>{checkIn.top1FreeText}</span>
            ) : checkIn.top1EntityId ? (
              <Top1EntityName type={checkIn.top1Type} entityId={checkIn.top1EntityId} />
            ) : null}
          </div>
        </DetailRow>
      )}

      {checkIn.intention && (
        <DetailRow label="Intention">
          <span className="italic text-muted-foreground">&ldquo;{checkIn.intention}&rdquo;</span>
        </DetailRow>
      )}
    </div>
  )
}

function EveningDetail({ checkIn }: { checkIn: CheckInDto }) {
  return (
    <div className="space-y-1">
      {checkIn.top1Completed != null && (
        <DetailRow label="#1 Completed">
          {checkIn.top1Completed ? (
            <span className="inline-flex items-center gap-1 text-green-400">
              <Check className="size-4" /> Yes
            </span>
          ) : (
            <span className="inline-flex items-center gap-1 text-orange-400">
              <X className="size-4" /> No
            </span>
          )}
        </DetailRow>
      )}

      {checkIn.energyLevelPm != null && (
        <DetailRow label="Energy (PM)">
          <EnergyBadge level={checkIn.energyLevelPm} size="sm" />
        </DetailRow>
      )}

      {checkIn.stressLevel != null && (
        <DetailRow label="Stress">
          <span className={cn(
            'text-sm font-medium',
            checkIn.stressLevel <= 2 ? 'text-green-400'
              : checkIn.stressLevel <= 3 ? 'text-yellow-400'
              : 'text-red-400'
          )}>
            {checkIn.stressLevel}/5
          </span>
        </DetailRow>
      )}

      {checkIn.blockerCategory && (
        <DetailRow label="Blocker">
          <div className="space-y-1 text-right">
            <span>
              {blockerCategoryInfo[checkIn.blockerCategory].emoji}{' '}
              {blockerCategoryInfo[checkIn.blockerCategory].label}
            </span>
            {checkIn.blockerNote && (
              <p className="text-xs text-muted-foreground">{checkIn.blockerNote}</p>
            )}
          </div>
        </DetailRow>
      )}

      {checkIn.reflection && (
        <DetailRow label="Reflection">
          <span className="italic text-muted-foreground">&ldquo;{checkIn.reflection}&rdquo;</span>
        </DetailRow>
      )}
    </div>
  )
}

export function CheckInDetailSheet({ checkInId }: CheckInDetailSheetProps) {
  const { data: checkIn, isLoading } = useCheckIn(checkInId)

  if (isLoading) {
    return (
      <div className="flex items-center justify-center py-12">
        <Loader2 className="size-6 animate-spin text-muted-foreground" />
      </div>
    )
  }

  if (!checkIn) {
    return (
      <div className="text-center py-12 text-muted-foreground">
        <p className="text-sm">Check-in not found.</p>
      </div>
    )
  }

  const isMorning = checkIn.type === 'Morning'

  return (
    <div className="space-y-4 px-4 pb-4">
      {/* Type header */}
      <div className="flex items-center gap-2.5 pb-2 border-b border-border/50">
        <div className={cn(
          'flex size-9 items-center justify-center rounded-lg',
          isMorning ? 'bg-orange-500/15' : 'bg-indigo-500/15'
        )}>
          {isMorning ? (
            <Sun className="size-5 text-orange-400" />
          ) : (
            <Moon className="size-5 text-indigo-400" />
          )}
        </div>
        <div>
          <p className="text-sm font-semibold text-foreground">
            {isMorning ? 'Morning' : 'Evening'} Check-in
          </p>
          <p className="text-xs text-muted-foreground">
            {formatDetailDate(checkIn.checkInDate)}
            {checkIn.completedAt && ` at ${formatTime(checkIn.completedAt)}`}
          </p>
        </div>
      </div>

      {/* Content */}
      {isMorning ? (
        <MorningDetail checkIn={checkIn} />
      ) : (
        <EveningDetail checkIn={checkIn} />
      )}
    </div>
  )
}

function formatDetailDate(dateStr: string): string {
  const date = new Date(dateStr + 'T00:00:00')
  return date.toLocaleDateString('en-US', {
    weekday: 'long',
    month: 'long',
    day: 'numeric',
    year: 'numeric',
  })
}

function formatTime(isoStr: string): string {
  const date = new Date(isoStr)
  return date.toLocaleTimeString('en-US', {
    hour: 'numeric',
    minute: '2-digit',
  })
}
