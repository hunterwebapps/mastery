import { apiClient } from '@/lib/api-client'
import type { UserValueDto } from '@/types'

export interface UpdateValuesRequest {
  values: UserValueDto[]
}

export const updateValuesApi = {
  /**
   * Updates the current user's values.
   */
  updateValues: async (request: UpdateValuesRequest): Promise<void> => {
    await apiClient.put('/user-profile/values', request)
  },
}
