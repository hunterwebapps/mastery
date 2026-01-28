import { Sparkles, Loader2 } from 'lucide-react'
import { Button } from '@/components/ui/button'
import type { RecommendationContext } from '@/types'
import { useGenerateRecommendations } from '../hooks'

interface GenerateButtonProps {
  context: RecommendationContext
  onGenerated?: () => void
}

export function GenerateButton({ context, onGenerated }: GenerateButtonProps) {
  const generateMutation = useGenerateRecommendations()

  const handleGenerate = () => {
    generateMutation.mutate(context, {
      onSuccess: () => {
        onGenerated?.()
      },
    })
  }

  return (
    <Button
      variant="outline"
      onClick={handleGenerate}
      disabled={generateMutation.isPending}
      className="gap-2"
    >
      {generateMutation.isPending ? (
        <Loader2 className="size-4 animate-spin" />
      ) : (
        <Sparkles className="size-4" />
      )}
      {generateMutation.isPending ? 'Generating...' : 'Generate Recommendations'}
    </Button>
  )
}
