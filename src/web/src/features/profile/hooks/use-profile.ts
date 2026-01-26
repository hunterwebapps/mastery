import { useQuery } from '@tanstack/react-query'
import { profileApi } from '../api'

export const profileKeys = {
  all: ['profile'] as const,
  current: () => [...profileKeys.all, 'current'] as const,
}

export function useProfile() {
  return useQuery({
    queryKey: profileKeys.current(),
    queryFn: profileApi.getCurrentProfile,
    retry: (failureCount, error) => {
      // Don't retry on 404
      if (error instanceof Error && error.message.includes('404')) {
        return false
      }
      return failureCount < 3
    },
  })
}

export function useHasProfile() {
  return useQuery({
    queryKey: [...profileKeys.all, 'exists'] as const,
    queryFn: profileApi.hasProfile,
    staleTime: 1000 * 60 * 5, // 5 minutes
  })
}
