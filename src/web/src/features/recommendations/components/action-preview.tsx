import { Plus, Pencil, Trash2, Calendar, ArrowRight, MessageSquare, BookOpen } from 'lucide-react'
import { Badge } from '@/components/ui/badge'
import { cn } from '@/lib/utils'
import type { RecommendationActionKind, RecommendationTargetKind } from '@/types'

interface ActionPreviewProps {
  actionKind: RecommendationActionKind
  targetKind: RecommendationTargetKind
  targetTitle?: string
  actionSummary?: string
}

const actionConfig: Record<
  RecommendationActionKind,
  { icon: React.ElementType; label: string; color: string }
> = {
  Create: {
    icon: Plus,
    label: 'Create',
    color: 'border-l-green-500 bg-green-50 dark:bg-green-950/20',
  },
  Update: {
    icon: Pencil,
    label: 'Edit',
    color: 'border-l-blue-500 bg-blue-50 dark:bg-blue-950/20',
  },
  Remove: {
    icon: Trash2,
    label: 'Archive',
    color: 'border-l-red-500 bg-red-50 dark:bg-red-950/20',
  },
  ExecuteToday: {
    icon: Calendar,
    label: 'Schedule Today',
    color: 'border-l-amber-500 bg-amber-50 dark:bg-amber-950/20',
  },
  Defer: {
    icon: ArrowRight,
    label: 'Defer',
    color: 'border-l-slate-500 bg-slate-50 dark:bg-slate-950/20',
  },
  ReflectPrompt: {
    icon: MessageSquare,
    label: 'Reflect',
    color: 'border-l-purple-500 bg-purple-50 dark:bg-purple-950/20',
  },
  LearnPrompt: {
    icon: BookOpen,
    label: 'Learn',
    color: 'border-l-cyan-500 bg-cyan-50 dark:bg-cyan-950/20',
  },
}

const targetKindLabels: Record<RecommendationTargetKind, string> = {
  Goal: 'goal',
  Metric: 'metric',
  Habit: 'habit',
  HabitOccurrence: 'habit occurrence',
  Task: 'task',
  Project: 'project',
  Experiment: 'experiment',
  UserProfile: 'profile',
}

function getFallbackSummary(
  actionKind: RecommendationActionKind,
  targetKind: RecommendationTargetKind,
  targetTitle?: string
): string {
  const action = actionConfig[actionKind]?.label ?? actionKind
  const targetLabel = targetKindLabels[targetKind] ?? targetKind
  const target = targetTitle ? `'${targetTitle}'` : targetLabel
  return `${action} ${target}`
}

export function ActionPreview({
  actionKind,
  targetKind,
  targetTitle,
  actionSummary,
}: ActionPreviewProps) {
  const config = actionConfig[actionKind] ?? actionConfig.Create
  const Icon = config.icon
  const displaySummary = actionSummary ?? getFallbackSummary(actionKind, targetKind, targetTitle)

  return (
    <div className={cn('rounded-lg border-l-4 p-3', config.color)}>
      <div className="flex items-center gap-2">
        <Icon className="size-4" />
        <span className="font-medium text-sm">{config.label}</span>
        <Badge variant="outline" className="text-xs">
          {targetKind}
        </Badge>
      </div>
      <p className="mt-1.5 text-sm text-muted-foreground">{displaySummary}</p>
    </div>
  )
}
