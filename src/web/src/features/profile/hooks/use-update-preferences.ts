import { useMutation, useQueryClient } from '@tanstack/react-query'
import { updatePreferencesApi } from '../api'
import { profileKeys } from './use-profile'
import type { PreferencesDto } from '@/types'

export function useUpdatePreferences() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (preferences: PreferencesDto) =>
      updatePreferencesApi.updatePreferences(preferences),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: profileKeys.current() })
    },
  })
}
