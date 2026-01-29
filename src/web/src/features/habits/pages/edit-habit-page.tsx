import { useMemo, useCallback } from 'react'
import { useParams, useSearchParams, Link } from 'react-router-dom'
import { Button } from '@/components/ui/button'
import { Skeleton } from '@/components/ui/skeleton'
import { useHabit } from '../hooks/use-habits'
import { HabitWizard } from '../components/habit-form'
import { habitToFormData } from '../utils'
import { getPayload, clearPayload } from '@/features/recommendations/lib/payload-storage'
import { transformPayloadForForm } from '@/features/recommendations/lib/payload-transformers'

function EditHabitSkeleton() {
  return (
    <div className="min-h-screen bg-background">
      <div className="container max-w-2xl py-8 px-4 sm:px-6 lg:px-8">
        <div className="space-y-8">
          {/* Header skeleton */}
          <div className="space-y-4">
            <div className="flex items-center gap-4">
              <Skeleton className="size-10 rounded-md" />
              <div className="space-y-2">
                <Skeleton className="h-8 w-48" />
                <Skeleton className="h-4 w-32" />
              </div>
            </div>
            <Skeleton className="h-1 w-full" />
          </div>

          {/* Step indicators skeleton */}
          <div className="flex justify-center">
            <div className="flex items-center gap-2">
              {[1, 2, 3, 4].map((i) => (
                <div key={i} className="flex items-center">
                  <Skeleton className="h-8 w-20 rounded-full" />
                  {i < 4 && <Skeleton className="w-8 h-0.5 mx-1" />}
                </div>
              ))}
            </div>
          </div>

          {/* Content skeleton */}
          <div className="min-h-[400px] space-y-6">
            <div className="text-center space-y-2">
              <Skeleton className="h-6 w-64 mx-auto" />
              <Skeleton className="h-4 w-96 mx-auto" />
            </div>
            <div className="space-y-4">
              <Skeleton className="h-10 w-full" />
              <Skeleton className="h-24 w-full" />
              <Skeleton className="h-24 w-full" />
            </div>
          </div>

          {/* Navigation skeleton */}
          <div className="flex justify-between pt-4 border-t">
            <Skeleton className="h-10 w-24" />
            <Skeleton className="h-10 w-24" />
          </div>
        </div>
      </div>
    </div>
  )
}

export function Component() {
  const { id } = useParams<{ id: string }>()
  const [searchParams] = useSearchParams()
  const recommendationId = searchParams.get('id')
  const isFromRecommendation = searchParams.get('from') === 'recommendation'

  const { data: habit, isLoading, error } = useHabit(id ?? '')

  // Convert habit data to form data and merge with recommendation payload
  const { initialData, showAiBanner } = useMemo(() => {
    if (!habit) {
      return { initialData: undefined, showAiBanner: false }
    }

    const baseData = habitToFormData(habit)

    // Overlay fields from recommendation payload (if any)
    if (isFromRecommendation && recommendationId) {
      const stored = getPayload(recommendationId)
      if (stored?.targetKind === 'Habit') {
        // Transform LLM payload field names to form field names
        const transformed = transformPayloadForForm(stored.payload, 'Habit')
        // Only overlay fields that exist in transformed payload
        for (const [key, value] of Object.entries(transformed)) {
          if (value !== undefined && value !== null) {
            (baseData as unknown as Record<string, unknown>)[key] = value
          }
        }
        return { initialData: baseData, showAiBanner: true }
      }
    }

    return { initialData: baseData, showAiBanner: false }
  }, [habit, isFromRecommendation, recommendationId])

  const handleSuccess = useCallback(() => {
    if (recommendationId) {
      clearPayload(recommendationId)
    }
  }, [recommendationId])

  if (isLoading) {
    return <EditHabitSkeleton />
  }

  if (error || !habit) {
    return (
      <div className="min-h-screen bg-background">
        <div className="container max-w-2xl py-8 px-4 sm:px-6 lg:px-8">
          <div className="text-center py-12">
            <h1 className="text-2xl font-bold mb-4">Habit Not Found</h1>
            <p className="text-muted-foreground mb-6">
              The habit you're looking for doesn't exist or has been deleted.
            </p>
            <Button asChild>
              <Link to="/habits">Back to Habits</Link>
            </Button>
          </div>
        </div>
      </div>
    )
  }

  return (
    <div className="min-h-screen bg-background">
      <div className="container max-w-2xl py-8 px-4 sm:px-6 lg:px-8">
        <HabitWizard
          mode="edit"
          initialData={initialData}
          habitId={habit.id}
          cancelPath={`/habits/${habit.id}`}
          showAiBanner={showAiBanner}
          onSuccess={handleSuccess}
        />
      </div>
    </div>
  )
}
