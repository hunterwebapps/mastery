import { useState } from 'react'
import { Link } from 'react-router-dom'
import { Plus, Target, BarChart3 } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Tabs, TabsList, TabsTrigger } from '@/components/ui/tabs'
import { useGoals } from '../hooks'
import { GoalsList } from '../components/goal-list'
import { MetricLibraryDialog } from '../components/metric-library'
import type { GoalStatus } from '@/types'

type StatusFilter = GoalStatus | 'all'

export function Component() {
  const [statusFilter, setStatusFilter] = useState<StatusFilter>('Active')
  const [showMetricLibrary, setShowMetricLibrary] = useState(false)
  const { data: goals, isLoading } = useGoals(
    statusFilter === 'all' ? undefined : statusFilter
  )

  return (
    <div className="min-h-screen bg-background">
      <div className="container max-w-6xl py-8 px-4 sm:px-6 lg:px-8">
        <div className="flex items-center justify-between mb-8">
          <div className="flex items-center gap-3">
            <div className="p-2 rounded-lg bg-primary/10">
              <Target className="size-6 text-primary" />
            </div>
            <div>
              <h1 className="text-2xl font-bold text-foreground">Goals</h1>
              <p className="text-sm text-muted-foreground">
                Track your objectives with measurable outcomes
              </p>
            </div>
          </div>
          <div className="flex items-center gap-2">
            <Button variant="outline" onClick={() => setShowMetricLibrary(true)}>
              <BarChart3 className="size-4 mr-2" />
              Metric Library
            </Button>
            <Button asChild>
              <Link to="/goals/new">
                <Plus className="size-4 mr-2" />
                New Goal
              </Link>
            </Button>
          </div>
        </div>

        <Tabs
          value={statusFilter}
          onValueChange={(value) => setStatusFilter(value as StatusFilter)}
          className="mb-6"
        >
          <TabsList>
            <TabsTrigger value="Active">Active</TabsTrigger>
            <TabsTrigger value="Draft">Drafts</TabsTrigger>
            <TabsTrigger value="Paused">Paused</TabsTrigger>
            <TabsTrigger value="Completed">Completed</TabsTrigger>
            <TabsTrigger value="all">All</TabsTrigger>
          </TabsList>
        </Tabs>

        <GoalsList goals={goals ?? []} isLoading={isLoading} />
      </div>

      <MetricLibraryDialog
        open={showMetricLibrary}
        onOpenChange={setShowMetricLibrary}
      />
    </div>
  )
}
