import { BarChart3 } from 'lucide-react'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Skeleton } from '@/components/ui/skeleton'
import { ObservationInput } from './observation-input'
import { useMetrics, useRecordObservation } from '../../hooks'
import type { MetricDefinitionDto } from '@/types'

interface QuickEntryPanelProps {
  metricIds?: string[]
  title?: string
}

function QuickEntrySkeleton() {
  return (
    <div className="space-y-4">
      {Array.from({ length: 3 }).map((_, i) => (
        <div key={i} className="space-y-2">
          <div className="flex items-center gap-2">
            <Skeleton className="size-10 rounded-lg" />
            <div className="flex-1 space-y-1">
              <Skeleton className="h-4 w-24" />
              <Skeleton className="h-3 w-32" />
            </div>
          </div>
          <Skeleton className="h-10 w-full" />
        </div>
      ))}
    </div>
  )
}

export function QuickEntryPanel({ metricIds, title = 'Quick Entry' }: QuickEntryPanelProps) {
  const { data: allMetrics, isLoading } = useMetrics()
  const recordObservation = useRecordObservation()

  // Filter to only specified metrics, or show all non-archived
  const metrics = metricIds
    ? allMetrics?.filter((m) => metricIds.includes(m.id) && !m.isArchived)
    : allMetrics?.filter((m) => !m.isArchived).slice(0, 5)

  const handleRecordObservation = async (metric: MetricDefinitionDto, value: number, note?: string) => {
    await recordObservation.mutateAsync({
      metricId: metric.id,
      request: {
        value,
        note,
        source: 'Manual',
      },
    })
  }

  return (
    <Card>
      <CardHeader className="pb-2">
        <CardTitle className="text-lg flex items-center gap-2">
          <BarChart3 className="size-5" />
          {title}
        </CardTitle>
      </CardHeader>
      <CardContent>
        {isLoading ? (
          <QuickEntrySkeleton />
        ) : !metrics || metrics.length === 0 ? (
          <div className="text-center py-4">
            <p className="text-sm text-muted-foreground">
              No metrics to display. Create metrics in the Metric Library.
            </p>
          </div>
        ) : (
          <div className="space-y-6">
            {metrics.map((metric) => (
              <ObservationInput
                key={metric.id}
                metric={metric}
                onSubmit={(value, note) => handleRecordObservation(metric, value, note)}
                isSubmitting={recordObservation.isPending}
              />
            ))}
          </div>
        )}
      </CardContent>
    </Card>
  )
}
