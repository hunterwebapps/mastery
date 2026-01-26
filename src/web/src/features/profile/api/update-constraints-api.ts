import { apiClient } from '@/lib/api-client'
import type { ConstraintsDto } from '@/types'

export const updateConstraintsApi = {
  /**
   * Updates the current user's constraints.
   */
  updateConstraints: async (constraints: ConstraintsDto): Promise<void> => {
    await apiClient.put('/user-profile/constraints', constraints)
  },
}
