import { apiClient } from '@/lib/api-client'
import type {
  AdminTraceListDto,
  AdminTraceDetailDto,
  AdminTraceFilterParams,
  PaginatedList,
} from '@/types'

export const recommendationsDebugApi = {
  getTraces: async (params: AdminTraceFilterParams = {}): Promise<PaginatedList<AdminTraceListDto>> => {
    const { data } = await apiClient.get<PaginatedList<AdminTraceListDto>>('/admin/recommendations/traces', {
      params,
    })
    return data
  },

  getTraceById: async (id: string): Promise<AdminTraceDetailDto> => {
    const { data } = await apiClient.get<AdminTraceDetailDto>(`/admin/recommendations/traces/${id}`)
    return data
  },
}
