import { useMutation, useQueryClient } from '@tanstack/react-query'
import { updateConstraintsApi } from '../api'
import { profileKeys } from './use-profile'
import type { ConstraintsDto } from '@/types'

export function useUpdateConstraints() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (constraints: ConstraintsDto) =>
      updateConstraintsApi.updateConstraints(constraints),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: profileKeys.current() })
    },
  })
}
