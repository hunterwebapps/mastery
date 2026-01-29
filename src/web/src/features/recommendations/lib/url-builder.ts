import type { RecommendationActionKind, RecommendationTargetKind } from '@/types'

/**
 * Maps target entity kinds to their URL path segments.
 */
const TARGET_KIND_TO_PATH: Record<RecommendationTargetKind, string> = {
  Task: 'tasks',
  Habit: 'habits',
  Goal: 'goals',
  Project: 'projects',
  Experiment: 'experiments',
  Metric: 'metrics',
  HabitOccurrence: 'habits',
  UserProfile: 'profile',
}

/**
 * Builds a redirect URL for client-side recommendation actions.
 *
 * @param recommendationId - The recommendation ID (used for payload lookup)
 * @param actionKind - The type of action (Create, Update, Remove)
 * @param targetKind - The target entity type
 * @param targetEntityId - The target entity ID (for Update/Remove actions)
 * @returns The URL to redirect to
 */
export function buildRedirectUrl(
  recommendationId: string,
  actionKind: RecommendationActionKind,
  targetKind: RecommendationTargetKind,
  targetEntityId?: string
): string {
  const basePath = TARGET_KIND_TO_PATH[targetKind]
  const params = new URLSearchParams({
    from: 'recommendation',
    id: recommendationId,
  })

  switch (actionKind) {
    case 'Create':
      return `/${basePath}/new?${params}`

    case 'Update':
      if (!targetEntityId) {
        console.warn('Update action missing target entity ID')
        return `/${basePath}?${params}`
      }
      return `/${basePath}/${targetEntityId}/edit?${params}`

    case 'Remove':
      if (!targetEntityId) {
        console.warn('Remove action missing target entity ID')
        return `/${basePath}?${params}`
      }
      // Redirect to entity page with archive action
      return `/${basePath}/${targetEntityId}?${params}&action=archive`

    default:
      // For ExecuteToday, Defer, etc. - these are server-side, shouldn't reach here
      // Fall back to entity detail or list page
      if (targetEntityId) {
        return `/${basePath}/${targetEntityId}`
      }
      return `/${basePath}`
  }
}
