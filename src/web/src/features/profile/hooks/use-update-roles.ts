import { useMutation, useQueryClient } from '@tanstack/react-query'
import { updateRolesApi, type UpdateRolesRequest } from '../api'
import { profileKeys } from './use-profile'

export function useUpdateRoles() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (request: UpdateRolesRequest) => updateRolesApi.updateRoles(request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: profileKeys.current() })
    },
  })
}
