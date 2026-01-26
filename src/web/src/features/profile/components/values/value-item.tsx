import { Badge } from '@/components/ui/badge'
import type { UserValueDto } from '@/types'

interface ValueItemProps {
  value: UserValueDto
}

export function ValueItem({ value }: ValueItemProps) {
  return (
    <div className="flex items-center justify-between rounded-lg border border-border bg-card/50 px-4 py-3 transition-colors hover:bg-card">
      <div className="flex items-center gap-3">
        <Badge
          variant="outline"
          className="size-7 items-center justify-center rounded-full text-xs font-semibold"
        >
          {value.rank}
        </Badge>
        <div>
          <p className="font-medium text-foreground">{value.label}</p>
          {value.notes && (
            <p className="text-sm text-muted-foreground">{value.notes}</p>
          )}
        </div>
      </div>
      {value.source && (
        <span className="text-xs text-muted-foreground">{value.source}</span>
      )}
    </div>
  )
}
