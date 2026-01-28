import { cn } from '@/lib/utils'
import { Badge } from '@/components/ui/badge'
import type { ContextTag } from '@/types/task'
import { contextTagInfo } from '@/types/task'

interface ContextTagBadgeProps {
  tag: ContextTag
  showEmoji?: boolean
  className?: string
}

export function ContextTagBadge({ tag, showEmoji = true, className }: ContextTagBadgeProps) {
  const info = contextTagInfo[tag]

  return (
    <Badge
      variant="outline"
      className={cn('text-xs px-1.5 py-0', info.color, className)}
    >
      {showEmoji && <span className="mr-1">{info.emoji}</span>}
      {info.label}
    </Badge>
  )
}

interface ContextTagsProps {
  tags: ContextTag[]
  max?: number
  showEmoji?: boolean
  className?: string
}

export function ContextTags({ tags, max = 3, showEmoji = false, className }: ContextTagsProps) {
  if (!tags || tags.length === 0) return null

  const visibleTags = tags.slice(0, max)
  const hiddenCount = tags.length - max

  return (
    <div className={cn('flex flex-wrap gap-1', className)}>
      {visibleTags.map((tag) => (
        <ContextTagBadge key={tag} tag={tag} showEmoji={showEmoji} />
      ))}
      {hiddenCount > 0 && (
        <Badge variant="outline" className="text-xs px-1.5 py-0 text-muted-foreground">
          +{hiddenCount}
        </Badge>
      )}
    </div>
  )
}
