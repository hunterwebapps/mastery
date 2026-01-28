import { useState } from 'react'
import { Link } from 'react-router-dom'
import { Plus, FlaskConical } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Tabs, TabsList, TabsTrigger } from '@/components/ui/tabs'
import { useExperiments } from '../hooks'
import { ExperimentsList } from '../components/experiment-list'
import type { ExperimentStatus } from '@/types'

type StatusFilter = ExperimentStatus | 'all'

export function Component() {
  const [statusFilter, setStatusFilter] = useState<StatusFilter>('Active')
  const { data: experiments, isLoading } = useExperiments(
    statusFilter === 'all' ? undefined : statusFilter
  )

  return (
    <div className="min-h-screen bg-background">
      <div className="container max-w-6xl py-8 px-4 sm:px-6 lg:px-8">
        <div className="flex items-center justify-between mb-8">
          <div className="flex items-center gap-3">
            <div className="p-2 rounded-lg bg-primary/10">
              <FlaskConical className="size-6 text-primary" />
            </div>
            <div>
              <h1 className="text-2xl font-bold text-foreground">Experiments</h1>
              <p className="text-sm text-muted-foreground">
                Test what works for you with structured experiments
              </p>
            </div>
          </div>
          <Button asChild>
            <Link to="/experiments/new">
              <Plus className="size-4 mr-2" />
              New Experiment
            </Link>
          </Button>
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
            <TabsTrigger value="Abandoned">Abandoned</TabsTrigger>
            <TabsTrigger value="all">All</TabsTrigger>
          </TabsList>
        </Tabs>

        <ExperimentsList experiments={experiments ?? []} isLoading={isLoading} />
      </div>
    </div>
  )
}
