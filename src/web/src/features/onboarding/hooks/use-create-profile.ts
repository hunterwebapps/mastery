import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useNavigate } from 'react-router-dom'
import { createProfileApi, type CreateProfileRequest } from '../api'
import { profileKeys } from '@/features/profile'

export function useCreateProfile() {
  const queryClient = useQueryClient()
  const navigate = useNavigate()

  return useMutation({
    mutationFn: (request: CreateProfileRequest) =>
      createProfileApi.createProfile(request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: profileKeys.current() })
      navigate('/')
    },
  })
}
