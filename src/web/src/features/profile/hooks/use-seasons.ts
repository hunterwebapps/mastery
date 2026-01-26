import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { seasonsApi } from '../api'
import { profileKeys } from './use-profile'
import type { CreateSeasonRequest, EndSeasonRequest } from '@/types'

export const seasonKeys = {
  all: ['seasons'] as const,
  list: () => [...seasonKeys.all, 'list'] as const,
}

export function useSeasons() {
  return useQuery({
    queryKey: seasonKeys.list(),
    queryFn: seasonsApi.getSeasons,
  })
}

export function useCreateSeason() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (request: CreateSeasonRequest) => seasonsApi.createSeason(request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: seasonKeys.list() })
      queryClient.invalidateQueries({ queryKey: profileKeys.current() })
    },
  })
}

export function useEndSeason() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({ seasonId, request }: { seasonId: string; request: EndSeasonRequest }) =>
      seasonsApi.endSeason(seasonId, request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: seasonKeys.list() })
      queryClient.invalidateQueries({ queryKey: profileKeys.current() })
    },
  })
}
