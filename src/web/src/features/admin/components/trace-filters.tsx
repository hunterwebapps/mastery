import { useState } from 'react'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import type { AdminTraceFilterParams } from '@/types'

interface TraceFiltersProps {
  filters: AdminTraceFilterParams
  onFiltersChange: (filters: AdminTraceFilterParams) => void
}

export function TraceFilters({ filters, onFiltersChange }: TraceFiltersProps) {
  const [localFilters, setLocalFilters] = useState<AdminTraceFilterParams>(filters)

  const handleApply = () => {
    onFiltersChange({ ...localFilters, page: 1 })
  }

  const handleClear = () => {
    const clearedFilters: AdminTraceFilterParams = { page: 1, pageSize: filters.pageSize }
    setLocalFilters(clearedFilters)
    onFiltersChange(clearedFilters)
  }

  return (
    <div className="space-y-4 p-4 border rounded-lg bg-muted/30">
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
        {/* Date Range */}
        <div className="space-y-1.5">
          <Label htmlFor="dateFrom" className="text-xs">From Date</Label>
          <Input
            id="dateFrom"
            type="date"
            value={localFilters.dateFrom?.split('T')[0] ?? ''}
            onChange={(e) =>
              setLocalFilters((f) => ({ ...f, dateFrom: e.target.value ? `${e.target.value}T00:00:00Z` : undefined }))
            }
            className="h-9"
          />
        </div>
        <div className="space-y-1.5">
          <Label htmlFor="dateTo" className="text-xs">To Date</Label>
          <Input
            id="dateTo"
            type="date"
            value={localFilters.dateTo?.split('T')[0] ?? ''}
            onChange={(e) =>
              setLocalFilters((f) => ({ ...f, dateTo: e.target.value ? `${e.target.value}T23:59:59Z` : undefined }))
            }
            className="h-9"
          />
        </div>

        {/* Context */}
        <div className="space-y-1.5">
          <Label className="text-xs">Context</Label>
          <Select
            value={localFilters.context ?? 'all'}
            onValueChange={(v) => setLocalFilters((f) => ({ ...f, context: v === 'all' ? undefined : v }))}
          >
            <SelectTrigger className="h-9">
              <SelectValue placeholder="All contexts" />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="all">All contexts</SelectItem>
              <SelectItem value="MorningCheckIn">Morning Check-in</SelectItem>
              <SelectItem value="EveningCheckIn">Evening Check-in</SelectItem>
              <SelectItem value="WeeklyReview">Weekly Review</SelectItem>
              <SelectItem value="DriftAlert">Drift Alert</SelectItem>
              <SelectItem value="ProactiveCheck">Proactive Check</SelectItem>
            </SelectContent>
          </Select>
        </div>

        {/* Status */}
        <div className="space-y-1.5">
          <Label className="text-xs">Status</Label>
          <Select
            value={localFilters.status ?? 'all'}
            onValueChange={(v) => setLocalFilters((f) => ({ ...f, status: v === 'all' ? undefined : v }))}
          >
            <SelectTrigger className="h-9">
              <SelectValue placeholder="All statuses" />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="all">All statuses</SelectItem>
              <SelectItem value="Pending">Pending</SelectItem>
              <SelectItem value="Accepted">Accepted</SelectItem>
              <SelectItem value="Dismissed">Dismissed</SelectItem>
              <SelectItem value="Snoozed">Snoozed</SelectItem>
              <SelectItem value="Expired">Expired</SelectItem>
              <SelectItem value="Executed">Executed</SelectItem>
            </SelectContent>
          </Select>
        </div>

        {/* Tier */}
        <div className="space-y-1.5">
          <Label className="text-xs">Processing Tier</Label>
          <Select
            value={localFilters.finalTier?.toString() ?? 'all'}
            onValueChange={(v) => setLocalFilters((f) => ({ ...f, finalTier: v === 'all' ? undefined : parseInt(v) }))}
          >
            <SelectTrigger className="h-9">
              <SelectValue placeholder="All tiers" />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="all">All tiers</SelectItem>
              <SelectItem value="0">Tier 0 (Rules)</SelectItem>
              <SelectItem value="1">Tier 1 (Quick)</SelectItem>
              <SelectItem value="2">Tier 2 (LLM)</SelectItem>
            </SelectContent>
          </Select>
        </div>

        {/* Selection Method */}
        <div className="space-y-1.5">
          <Label className="text-xs">Selection Method</Label>
          <Select
            value={localFilters.selectionMethod ?? 'all'}
            onValueChange={(v) => setLocalFilters((f) => ({ ...f, selectionMethod: v === 'all' ? undefined : v }))}
          >
            <SelectTrigger className="h-9">
              <SelectValue placeholder="All methods" />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="all">All methods</SelectItem>
              <SelectItem value="Tier0-Rules">Tier0-Rules</SelectItem>
              <SelectItem value="LLM-Selection">LLM-Selection</SelectItem>
              <SelectItem value="Deterministic">Deterministic</SelectItem>
            </SelectContent>
          </Select>
        </div>

        {/* User ID */}
        <div className="space-y-1.5 col-span-1 sm:col-span-2">
          <Label htmlFor="userId" className="text-xs">User ID</Label>
          <Input
            id="userId"
            placeholder="Filter by user ID..."
            value={localFilters.userId ?? ''}
            onChange={(e) => setLocalFilters((f) => ({ ...f, userId: e.target.value || undefined }))}
            className="h-9"
          />
        </div>
      </div>

      <div className="flex gap-2 justify-end">
        <Button variant="outline" size="sm" onClick={handleClear}>
          Clear
        </Button>
        <Button size="sm" onClick={handleApply}>
          Apply Filters
        </Button>
      </div>
    </div>
  )
}
