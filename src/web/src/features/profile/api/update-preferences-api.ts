import { apiClient } from '@/lib/api-client'
import type { PreferencesDto } from '@/types'

export const updatePreferencesApi = {
  /**
   * Updates the current user's preferences.
   */
  updatePreferences: async (preferences: PreferencesDto): Promise<void> => {
    await apiClient.put('/user-profile/preferences', preferences)
  },
}
