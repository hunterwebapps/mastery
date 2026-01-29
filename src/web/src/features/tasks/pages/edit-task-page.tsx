import { useMemo, useCallback } from 'react'
import { useParams, useSearchParams } from 'react-router-dom'
import { Loader2 } from 'lucide-react'
import { TaskForm } from '../components/task-form'
import { useTask } from '../hooks/use-tasks'
import { getPayload, clearPayload } from '@/features/recommendations/lib/payload-storage'
import { transformPayloadForForm } from '@/features/recommendations/lib/payload-transformers'
import type { TaskDto } from '@/types/task'

export function Component() {
  const { id } = useParams<{ id: string }>()
  const [searchParams] = useSearchParams()
  const recommendationId = searchParams.get('id')
  const isFromRecommendation = searchParams.get('from') === 'recommendation'

  const { data: task, isLoading, error } = useTask(id ?? '')

  // Merge existing task data with recommendation payload (if any)
  const { initialData, showAiBanner } = useMemo(() => {
    if (!task) {
      return { initialData: undefined, showAiBanner: false }
    }

    // Start with existing task data
    const data: TaskDto = { ...task }

    // Overlay fields from recommendation payload (only fields that exist in payload)
    if (isFromRecommendation && recommendationId) {
      const stored = getPayload(recommendationId)
      if (stored?.targetKind === 'Task') {
        // Transform LLM payload field names to form field names
        const transformed = transformPayloadForForm(stored.payload, 'Task')
        // Only overlay fields that exist in transformed payload
        for (const [key, value] of Object.entries(transformed)) {
          if (value !== undefined && value !== null) {
            (data as unknown as Record<string, unknown>)[key] = value
          }
        }
        return { initialData: data, showAiBanner: true }
      }
    }

    return { initialData: data, showAiBanner: false }
  }, [task, isFromRecommendation, recommendationId])

  const handleSuccess = useCallback(() => {
    if (recommendationId) {
      clearPayload(recommendationId)
    }
  }, [recommendationId])

  if (isLoading) {
    return (
      <div className="min-h-screen bg-background flex items-center justify-center">
        <Loader2 className="size-8 animate-spin text-muted-foreground" />
      </div>
    )
  }

  if (error || !task) {
    return (
      <div className="min-h-screen bg-background flex items-center justify-center">
        <div className="text-center">
          <h2 className="text-xl font-semibold text-destructive">Task not found</h2>
          <p className="text-muted-foreground mt-2">
            The task you're looking for doesn't exist or has been deleted.
          </p>
        </div>
      </div>
    )
  }

  return (
    <div className="min-h-screen bg-background">
      <div className="container max-w-2xl py-8 px-4 sm:px-6 lg:px-8">
        <TaskForm
          mode="edit"
          initialData={initialData}
          showAiBanner={showAiBanner}
          onSuccess={handleSuccess}
        />
      </div>
    </div>
  )
}
