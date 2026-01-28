import { useState, useCallback } from 'react'
import { Link } from 'react-router-dom'
import { Plus, Sparkles, ListTodo, Calendar } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Tabs, TabsList, TabsTrigger, TabsContent } from '@/components/ui/tabs'
import {
  useTodayHabits,
  useHabits,
  useCompleteOccurrence,
  useUndoOccurrence,
  useSkipOccurrence,
} from '../hooks/use-habits'
import { TodayHabitsList } from '../components/today-view'
import { HabitsList } from '../components/habit-list'
import type { HabitStatus, HabitMode } from '@/types/habit'

type ViewTab = 'today' | 'all'
type StatusFilter = HabitStatus | 'all'

export function Component() {
  const [viewTab, setViewTab] = useState<ViewTab>('today')
  const [statusFilter, setStatusFilter] = useState<StatusFilter>('Active')

  // Today view data
  const {
    data: todayHabits,
    isLoading: isTodayLoading,
  } = useTodayHabits()

  // All habits data
  const {
    data: allHabits,
    isLoading: isAllLoading,
  } = useHabits(statusFilter === 'all' ? undefined : statusFilter)

  // Mutations
  const completeOccurrence = useCompleteOccurrence()
  const undoOccurrence = useUndoOccurrence()
  const skipOccurrence = useSkipOccurrence()

  // Get today's date in YYYY-MM-DD format
  const today = new Date().toISOString().split('T')[0]

  // Handlers
  const handleComplete = useCallback(
    async (habitId: string, data: { mode?: HabitMode; value?: number; note?: string }) => {
      await completeOccurrence.mutateAsync({
        habitId,
        date: today,
        request: {
          mode: data.mode,
          value: data.value,
          note: data.note,
        },
      })
    },
    [completeOccurrence, today]
  )

  const handleUndo = useCallback(
    async (habitId: string) => {
      await undoOccurrence.mutateAsync({
        habitId,
        date: today,
      })
    },
    [undoOccurrence, today]
  )

  const handleSkip = useCallback(
    async (habitId: string, reason?: string) => {
      await skipOccurrence.mutateAsync({
        habitId,
        date: today,
        request: reason ? { reason } : undefined,
      })
    },
    [skipOccurrence, today]
  )

  // Calculate today stats for header
  const todayStats = todayHabits
    ? {
        due: todayHabits.filter((h) => h.isDue).length,
        completed: todayHabits.filter((h) => h.todayOccurrence?.status === 'Completed').length,
      }
    : { due: 0, completed: 0 }

  return (
    <div className="min-h-screen bg-background">
      <div className="container max-w-4xl py-8 px-4 sm:px-6 lg:px-8">
        {/* Header */}
        <div className="flex items-center justify-between mb-8">
          <div className="flex items-center gap-3">
            <div className="p-2 rounded-lg bg-primary/10">
              <Sparkles className="size-6 text-primary" />
            </div>
            <div>
              <h1 className="text-2xl font-bold text-foreground">Habits</h1>
              <p className="text-sm text-muted-foreground">
                {viewTab === 'today' ? (
                  todayStats.due > 0 ? (
                    <>
                      <span className="text-foreground font-medium">{todayStats.completed}</span>
                      <span> of </span>
                      <span className="text-foreground font-medium">{todayStats.due}</span>
                      <span> complete today</span>
                    </>
                  ) : (
                    'No habits due today'
                  )
                ) : (
                  'Build consistency through daily practice'
                )}
              </p>
            </div>
          </div>
          <Button asChild>
            <Link to="/habits/new">
              <Plus className="size-4 mr-2" />
              New Habit
            </Link>
          </Button>
        </div>

        {/* View tabs */}
        <Tabs value={viewTab} onValueChange={(v) => setViewTab(v as ViewTab)}>
          <div className="flex items-center justify-between mb-6">
            <TabsList>
              <TabsTrigger value="today" className="gap-2">
                <Calendar className="size-4" />
                Today
              </TabsTrigger>
              <TabsTrigger value="all" className="gap-2">
                <ListTodo className="size-4" />
                All Habits
              </TabsTrigger>
            </TabsList>

            {/* Status filter for All view */}
            {viewTab === 'all' && (
              <Tabs
                value={statusFilter}
                onValueChange={(v) => setStatusFilter(v as StatusFilter)}
              >
                <TabsList>
                  <TabsTrigger value="Active">Active</TabsTrigger>
                  <TabsTrigger value="Paused">Paused</TabsTrigger>
                  <TabsTrigger value="Archived">Archived</TabsTrigger>
                  <TabsTrigger value="all">All</TabsTrigger>
                </TabsList>
              </Tabs>
            )}
          </div>

          {/* Today view */}
          <TabsContent value="today" className="mt-0">
            <TodayHabitsList
              habits={todayHabits ?? []}
              onComplete={handleComplete}
              onUndo={handleUndo}
              onSkip={handleSkip}
              isLoading={isTodayLoading}
            />
          </TabsContent>

          {/* All habits view */}
          <TabsContent value="all" className="mt-0">
            <HabitsList
              habits={allHabits ?? []}
              isLoading={isAllLoading}
            />
          </TabsContent>
        </Tabs>
      </div>
    </div>
  )
}
