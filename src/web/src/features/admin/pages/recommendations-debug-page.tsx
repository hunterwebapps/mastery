import { useState } from 'react'
import { Cpu, ChevronDown, ChevronUp } from 'lucide-react'
import { Button } from '@/components/ui/button'
import {
  Collapsible,
  CollapsibleContent,
  CollapsibleTrigger,
} from '@/components/ui/collapsible'
import { useRecommendationTraces } from '../hooks'
import { TraceFilters, TraceList, TraceDetailSheet } from '../components'
import type { AdminTraceFilterParams } from '@/types'

export function Component() {
  const [filters, setFilters] = useState<AdminTraceFilterParams>({ page: 1, pageSize: 20 })
  const [selectedTraceId, setSelectedTraceId] = useState<string | null>(null)
  const [showFilters, setShowFilters] = useState(false)

  const { data, isLoading } = useRecommendationTraces(filters)

  const handleFiltersChange = (newFilters: AdminTraceFilterParams) => {
    setFilters(newFilters)
  }

  const handlePageChange = (newPage: number) => {
    setFilters((f) => ({ ...f, page: newPage }))
  }

  return (
    <div className="min-h-screen bg-background">
      <div className="container max-w-6xl py-8 px-4 sm:px-6 lg:px-8">
        {/* Header */}
        <div className="flex items-center justify-between mb-8">
          <div className="flex items-center gap-3">
            <div className="p-2 rounded-lg bg-purple-500/10">
              <Cpu className="size-6 text-purple-500" />
            </div>
            <div>
              <h1 className="text-2xl font-bold text-foreground">Recommendations Debug</h1>
              <p className="text-sm text-muted-foreground">
                Inspect recommendation pipeline traces and LLM calls
              </p>
            </div>
          </div>
        </div>

        {/* Filters (collapsible) */}
        <Collapsible open={showFilters} onOpenChange={setShowFilters} className="mb-6">
          <CollapsibleTrigger asChild>
            <Button variant="outline" className="w-full justify-between mb-2">
              <span>Filters</span>
              {showFilters ? <ChevronUp className="size-4" /> : <ChevronDown className="size-4" />}
            </Button>
          </CollapsibleTrigger>
          <CollapsibleContent>
            <TraceFilters filters={filters} onFiltersChange={handleFiltersChange} />
          </CollapsibleContent>
        </Collapsible>

        {/* Trace list */}
        <TraceList
          traces={data?.items ?? []}
          isLoading={isLoading}
          onSelectTrace={setSelectedTraceId}
        />

        {/* Pagination */}
        {data && data.totalPages > 1 && (
          <div className="flex items-center justify-between mt-6">
            <p className="text-sm text-muted-foreground">
              Page {data.page} of {data.totalPages} ({data.totalCount.toLocaleString()} traces)
            </p>
            <div className="flex gap-2">
              <Button
                variant="outline"
                size="sm"
                onClick={() => handlePageChange(data.page - 1)}
                disabled={!data.hasPreviousPage}
              >
                Previous
              </Button>
              <Button
                variant="outline"
                size="sm"
                onClick={() => handlePageChange(data.page + 1)}
                disabled={!data.hasNextPage}
              >
                Next
              </Button>
            </div>
          </div>
        )}

        {/* Detail sheet */}
        <TraceDetailSheet
          traceId={selectedTraceId}
          open={!!selectedTraceId}
          onOpenChange={(open) => {
            if (!open) setSelectedTraceId(null)
          }}
        />
      </div>
    </div>
  )
}
