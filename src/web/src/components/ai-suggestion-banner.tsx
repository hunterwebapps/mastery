import { Sparkles } from 'lucide-react'

interface AiSuggestionBannerProps {
  className?: string
}

/**
 * Banner displayed on forms when values are pre-filled from an AI recommendation.
 * Informs users they can review and modify before saving.
 */
export function AiSuggestionBanner({ className }: AiSuggestionBannerProps) {
  return (
    <div
      className={`rounded-lg border border-violet-500/30 bg-violet-500/10 p-4 ${className ?? ''}`}
    >
      <div className="flex items-center gap-3">
        <Sparkles className="size-5 text-violet-400 shrink-0" />
        <div>
          <p className="text-sm font-medium text-violet-100">
            Pre-filled from AI Recommendation
          </p>
          <p className="text-xs text-muted-foreground">
            Review and modify the values below before saving.
          </p>
        </div>
      </div>
    </div>
  )
}
