import { useMutation, useQueryClient } from '@tanstack/react-query'
import { updateValuesApi, type UpdateValuesRequest } from '../api'
import { profileKeys } from './use-profile'

export function useUpdateValues() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (request: UpdateValuesRequest) => updateValuesApi.updateValues(request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: profileKeys.current() })
    },
  })
}
