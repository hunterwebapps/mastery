import { useState, useMemo } from 'react'
import { cn } from '@/lib/utils'
import { Input } from '@/components/ui/input'
import {
  Sheet,
  SheetContent,
  SheetHeader,
  SheetTitle,
  SheetDescription,
} from '@/components/ui/sheet'
import {
  CheckSquare, Target, FolderKanban, Pencil,
  Search, Flame, Clock, AlertTriangle, Check, Plus,
} from 'lucide-react'
import { useTodayHabits } from '@/features/habits/hooks/use-habits'
import { useTodayTasks, taskKeys } from '@/features/tasks/hooks/use-tasks'
import { useQueryClient } from '@tanstack/react-query'
import { useProjects, projectKeys } from '@/features/projects/hooks/use-projects'
import { TaskForm } from '@/features/tasks/components/task-form/task-form'
import type { Top1Type } from '@/types/check-in'

interface Top1StepProps {
  top1Type?: Top1Type
  top1EntityId?: string
  top1FreeText?: string
  onTypeChange: (type: Top1Type) => void
  onEntityIdChange: (id: string) => void
  onFreeTextChange: (text: string) => void
}

interface TypeOption {
  value: Top1Type
  label: string
  icon: React.ReactNode
  color: string
  selectedColor: string
}

interface SuggestionItem {
  id: string
  title: string
  subtitle?: string
  badges: { label: string; className: string }[]
}

const typeOptions: TypeOption[] = [
  {
    value: 'Task',
    label: 'Task',
    icon: <CheckSquare className="size-5" />,
    color: 'border-blue-500/40 bg-blue-500/10 hover:bg-blue-500/20 text-blue-400',
    selectedColor: 'border-blue-500 bg-blue-500/25 ring-2 ring-blue-500/30 text-blue-300',
  },
  {
    value: 'Habit',
    label: 'Habit',
    icon: <Target className="size-5" />,
    color: 'border-green-500/40 bg-green-500/10 hover:bg-green-500/20 text-green-400',
    selectedColor: 'border-green-500 bg-green-500/25 ring-2 ring-green-500/30 text-green-300',
  },
  {
    value: 'Project',
    label: 'Project',
    icon: <FolderKanban className="size-5" />,
    color: 'border-purple-500/40 bg-purple-500/10 hover:bg-purple-500/20 text-purple-400',
    selectedColor: 'border-purple-500 bg-purple-500/25 ring-2 ring-purple-500/30 text-purple-300',
  },
  {
    value: 'FreeText',
    label: 'Custom',
    icon: <Pencil className="size-5" />,
    color: 'border-muted bg-muted/30 hover:bg-muted/50 text-muted-foreground',
    selectedColor: 'border-foreground/40 bg-foreground/10 ring-2 ring-foreground/20 text-foreground',
  },
]

export function Top1Step({
  top1Type,
  top1EntityId,
  top1FreeText,
  onTypeChange,
  onEntityIdChange,
  onFreeTextChange,
}: Top1StepProps) {
  const queryClient = useQueryClient()
  const [searchQuery, setSearchQuery] = useState('')
  const [showCreateTask, setShowCreateTask] = useState(false)

  // Fetch real data
  const { data: todayHabits } = useTodayHabits()
  const { data: todayTasks } = useTodayTasks()
  const { data: projects } = useProjects({ status: 'Active' })

  // Map data to suggestion items
  const suggestions = useMemo((): SuggestionItem[] => {
    if (!top1Type || top1Type === 'FreeText') return []

    const query = searchQuery.toLowerCase().trim()

    if (top1Type === 'Task') {
      const items: SuggestionItem[] = (todayTasks ?? [])
        .filter(t => t.status !== 'Completed' && t.status !== 'Cancelled')
        .map(t => {
          const badges: SuggestionItem['badges'] = []
          if (t.isOverdue) badges.push({ label: 'Overdue', className: 'bg-destructive/15 text-destructive' })
          if (t.dueOn) badges.push({ label: `Due ${t.dueOn}`, className: 'bg-warning/15 text-warning' })
          if (t.estimatedMinutes) badges.push({ label: `${t.estimatedMinutes}m`, className: 'bg-muted text-muted-foreground' })
          return {
            id: t.id,
            title: t.title,
            subtitle: t.projectTitle,
            badges,
          }
        })
      if (!query) return items
      return items.filter(i => i.title.toLowerCase().includes(query))
    }

    if (top1Type === 'Habit') {
      const items: SuggestionItem[] = (todayHabits ?? [])
        .filter(h => h.isDue && h.todayOccurrence?.status !== 'Completed')
        .map(h => {
          const badges: SuggestionItem['badges'] = []
          if (h.currentStreak > 0) badges.push({ label: `${h.currentStreak}d streak`, className: 'bg-primary/15 text-primary' })
          if (h.adherenceRate7Day > 0) badges.push({ label: `${Math.round(h.adherenceRate7Day * 100)}% 7d`, className: 'bg-muted text-muted-foreground' })
          return {
            id: h.id,
            title: h.title,
            subtitle: h.description,
            badges,
          }
        })
      if (!query) return items
      return items.filter(i => i.title.toLowerCase().includes(query))
    }

    if (top1Type === 'Project') {
      const items: SuggestionItem[] = (projects ?? []).map(p => {
        const badges: SuggestionItem['badges'] = []
        const progress = p.totalTasks > 0 ? Math.round((p.completedTasks / p.totalTasks) * 100) : 0
        if (progress > 0) badges.push({ label: `${progress}%`, className: 'bg-muted text-muted-foreground' })
        if (p.isStuck) badges.push({ label: 'Stuck', className: 'bg-destructive/15 text-destructive' })
        if (p.isNearingDeadline) badges.push({ label: 'Deadline soon', className: 'bg-warning/15 text-warning' })
        if (p.goalTitle) badges.push({ label: p.goalTitle, className: 'bg-primary/15 text-primary' })
        return {
          id: p.id,
          title: p.title,
          subtitle: p.nextTaskTitle ? `Next: ${p.nextTaskTitle}` : undefined,
          badges,
        }
      })
      if (!query) return items
      return items.filter(i => i.title.toLowerCase().includes(query))
    }

    return []
  }, [top1Type, searchQuery, todayTasks, todayHabits, projects])

  const isLoading = (top1Type === 'Task' && !todayTasks)
    || (top1Type === 'Habit' && !todayHabits)
    || (top1Type === 'Project' && !projects)

  const handleTaskCreated = (taskId: string) => {
    onEntityIdChange(taskId)
    setShowCreateTask(false)
    setSearchQuery('')
    queryClient.invalidateQueries({ queryKey: taskKeys.today() })
    queryClient.invalidateQueries({ queryKey: projectKeys.all })
  }

  return (
    <div className="space-y-6 animate-in fade-in duration-300">
      <div className="text-center space-y-2">
        <h2 className="text-2xl font-semibold text-foreground">
          What's your #1 priority?
        </h2>
        <p className="text-sm text-muted-foreground">
          Pick the one thing that would make today a success
        </p>
      </div>

      <div className="grid grid-cols-2 gap-3">
        {typeOptions.map((option) => {
          const isSelected = top1Type === option.value

          return (
            <button
              key={option.value}
              onClick={() => {
                onTypeChange(option.value)
                setSearchQuery('')
              }}
              className={cn(
                'flex flex-col items-center gap-2 rounded-xl border-2 p-4 transition-all duration-200 cursor-pointer',
                isSelected ? option.selectedColor : option.color
              )}
            >
              {option.icon}
              <span className="text-sm font-medium">{option.label}</span>
            </button>
          )
        })}
      </div>

      {/* Entity suggestions for Task/Habit/Project */}
      {top1Type && top1Type !== 'FreeText' && (
        <div className="space-y-3 animate-in slide-in-from-bottom-4 duration-300">
          {/* Search */}
          <div className="relative">
            <Search className="absolute left-3 top-1/2 -translate-y-1/2 size-4 text-muted-foreground" />
            <Input
              placeholder={`Search ${top1Type.toLowerCase()}s...`}
              value={searchQuery}
              onChange={(e) => setSearchQuery(e.target.value)}
              className="pl-9"
            />
          </div>

          {/* Create new task button */}
          {top1Type === 'Task' && (
            <button
              onClick={() => setShowCreateTask(true)}
              className="w-full flex items-center gap-2.5 rounded-lg border border-dashed border-blue-500/40 p-3 text-left transition-all duration-150 cursor-pointer hover:bg-blue-500/5 hover:border-blue-500/60"
            >
              <div className="flex size-8 shrink-0 items-center justify-center rounded-md bg-blue-500/10">
                <Plus className="size-4 text-blue-400" />
              </div>
              <div className="flex-1 min-w-0">
                <p className="text-sm font-medium text-blue-400">Create a new task</p>
                <p className="text-xs text-muted-foreground">
                  Full form with project, goal, and more
                </p>
              </div>
            </button>
          )}

          {/* Suggestion list */}
          <div className="max-h-56 overflow-y-auto space-y-1.5 pr-1">
            {isLoading ? (
              <div className="space-y-2">
                {[1, 2, 3].map(i => (
                  <div key={i} className="h-14 rounded-lg bg-muted/50 animate-pulse" />
                ))}
              </div>
            ) : suggestions.length === 0 ? (
              <div className="text-center py-6 space-y-1">
                <p className="text-sm text-muted-foreground">
                  {searchQuery
                    ? `No ${top1Type.toLowerCase()}s match "${searchQuery}"`
                    : top1Type === 'Task'
                      ? 'No tasks scheduled for today'
                      : top1Type === 'Habit'
                        ? 'No habits due today'
                        : 'No active projects'
                  }
                </p>
                {top1Type !== 'Task' && (
                  <button
                    onClick={() => {
                      onTypeChange('FreeText')
                      setSearchQuery('')
                    }}
                    className="text-xs text-primary hover:underline"
                  >
                    Write a custom priority instead
                  </button>
                )}
              </div>
            ) : (
              suggestions.map((item) => {
                const isSelected = top1EntityId === item.id

                return (
                  <button
                    key={item.id}
                    onClick={() => onEntityIdChange(item.id)}
                    className={cn(
                      'w-full flex items-start gap-3 rounded-lg border p-3 text-left transition-all duration-150 cursor-pointer',
                      isSelected
                        ? 'border-primary bg-primary/10 ring-1 ring-primary/30'
                        : 'border-border/50 bg-card hover:bg-muted/50 hover:border-border'
                    )}
                  >
                    <div className={cn(
                      'flex size-8 shrink-0 items-center justify-center rounded-md mt-0.5',
                      isSelected ? 'bg-primary/20' : 'bg-muted/50'
                    )}>
                      {isSelected ? (
                        <Check className="size-4 text-primary" />
                      ) : top1Type === 'Task' ? (
                        <CheckSquare className="size-4 text-blue-400" />
                      ) : top1Type === 'Habit' ? (
                        <Target className="size-4 text-green-400" />
                      ) : (
                        <FolderKanban className="size-4 text-purple-400" />
                      )}
                    </div>

                    <div className="flex-1 min-w-0">
                      <p className={cn(
                        'text-sm font-medium truncate',
                        isSelected ? 'text-primary' : 'text-foreground'
                      )}>
                        {item.title}
                      </p>
                      {item.subtitle && (
                        <p className="text-xs text-muted-foreground truncate mt-0.5">
                          {item.subtitle}
                        </p>
                      )}
                      {item.badges.length > 0 && (
                        <div className="flex flex-wrap gap-1 mt-1.5">
                          {item.badges.map((badge, i) => (
                            <span
                              key={i}
                              className={cn(
                                'inline-flex items-center gap-0.5 rounded-full px-1.5 py-0.5 text-[10px] font-medium',
                                badge.className
                              )}
                            >
                              {badge.label === 'Overdue' && <AlertTriangle className="size-2.5" />}
                              {badge.label === 'Stuck' && <AlertTriangle className="size-2.5" />}
                              {badge.label.includes('streak') && <Flame className="size-2.5" />}
                              {badge.label.includes('Due') && <Clock className="size-2.5" />}
                              {badge.label.includes('Deadline') && <Clock className="size-2.5" />}
                              {badge.label}
                            </span>
                          ))}
                        </div>
                      )}
                    </div>
                  </button>
                )
              })
            )}
          </div>
        </div>
      )}

      {/* Free text input */}
      {top1Type === 'FreeText' && (
        <div className="space-y-2 animate-in slide-in-from-bottom-4 duration-300">
          <Input
            placeholder="e.g., Finish the quarterly report"
            value={top1FreeText ?? ''}
            onChange={(e) => onFreeTextChange(e.target.value)}
            maxLength={200}
            autoFocus
          />
          <p className="text-xs text-muted-foreground text-right">
            {(top1FreeText?.length ?? 0)}/200
          </p>
        </div>
      )}

      {/* Create task sheet */}
      <Sheet open={showCreateTask} onOpenChange={setShowCreateTask}>
        <SheetContent side="right" className="w-full sm:max-w-lg overflow-y-auto">
          <SheetHeader>
            <SheetTitle>New Task</SheetTitle>
            <SheetDescription>
              Create a task and set it as your #1 priority
            </SheetDescription>
          </SheetHeader>
          <div className="mt-6 px-4 pb-4">
            <TaskForm
              mode="create"
              embedded
              onCreated={handleTaskCreated}
              onCancel={() => setShowCreateTask(false)}
            />
          </div>
        </SheetContent>
      </Sheet>
    </div>
  )
}
