import { useMemo, useCallback } from 'react'
import { useSearchParams } from 'react-router-dom'
import { GoalWizard } from '../components/goal-form'
import { getPayload, clearPayload } from '@/features/recommendations/lib/payload-storage'
import { transformPayloadForForm } from '@/features/recommendations/lib/payload-transformers'
import type { CreateGoalFormData } from '../schemas'

export function Component() {
  const [searchParams] = useSearchParams()
  const recommendationId = searchParams.get('id')
  const isFromRecommendation = searchParams.get('from') === 'recommendation'

  // Get pre-filled data from recommendation payload
  const { initialData, showAiBanner } = useMemo(() => {
    if (!isFromRecommendation || !recommendationId) {
      return { initialData: undefined, showAiBanner: false }
    }

    const stored = getPayload(recommendationId)
    if (!stored || stored.targetKind !== 'Goal') {
      return { initialData: undefined, showAiBanner: false }
    }

    // Transform LLM payload field names to form field names
    const transformed = transformPayloadForForm(stored.payload, 'Goal')
    return {
      initialData: transformed as Partial<CreateGoalFormData>,
      showAiBanner: true,
    }
  }, [isFromRecommendation, recommendationId])

  const handleSuccess = useCallback(() => {
    if (recommendationId) {
      clearPayload(recommendationId)
    }
  }, [recommendationId])

  return (
    <div className="min-h-screen bg-background">
      <div className="container max-w-2xl py-8 px-4 sm:px-6 lg:px-8">
        <GoalWizard
          initialData={initialData}
          showAiBanner={showAiBanner}
          onSuccess={handleSuccess}
        />
      </div>
    </div>
  )
}
