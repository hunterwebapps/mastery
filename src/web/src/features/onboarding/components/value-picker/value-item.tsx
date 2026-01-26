import { X, GripVertical } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'

interface ValueItemProps {
  label: string
  rank: number
  onRemove: () => void
  isDragging?: boolean
}

export function ValueItem({ label, rank, onRemove, isDragging }: ValueItemProps) {
  return (
    <div
      className={`flex items-center gap-3 p-3 bg-card border border-border rounded-lg transition-shadow ${
        isDragging ? 'shadow-lg ring-2 ring-primary' : ''
      }`}
    >
      <GripVertical className="size-4 text-muted-foreground cursor-grab shrink-0" />
      <Badge variant="outline" className="shrink-0">
        #{rank}
      </Badge>
      <span className="flex-1 font-medium">{label}</span>
      <Button
        variant="ghost"
        size="icon"
        className="size-8 text-muted-foreground hover:text-destructive"
        onClick={onRemove}
      >
        <X className="size-4" />
      </Button>
    </div>
  )
}
