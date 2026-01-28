import { apiClient } from '@/lib/api-client'
import type {
  ProjectDto,
  ProjectSummaryDto,
  CreateProjectRequest,
  ChangeProjectStatusRequest,
  SetProjectNextActionRequest,
  CompleteProjectRequest,
  AddMilestoneRequest,
  UpdateMilestoneRequest,
} from '@/types/project'

export interface GetProjectsParams {
  status?: string
  goalId?: string
}

export const projectsApi = {
  /**
   * Gets all projects for the current user with optional filters.
   */
  getProjects: async (params?: GetProjectsParams): Promise<ProjectSummaryDto[]> => {
    const { data } = await apiClient.get<ProjectSummaryDto[]>('/projects', { params })
    return data
  },

  /**
   * Gets a single project by ID with full details including milestones.
   */
  getProjectById: async (id: string): Promise<ProjectDto> => {
    const { data } = await apiClient.get<ProjectDto>(`/projects/${id}`)
    return data
  },

  /**
   * Creates a new project.
   */
  createProject: async (request: CreateProjectRequest): Promise<string> => {
    const { data } = await apiClient.post<string>('/projects', request)
    return data
  },

  /**
   * Changes the status of a project (activate, pause, archive).
   */
  changeProjectStatus: async (id: string, request: ChangeProjectStatusRequest): Promise<void> => {
    await apiClient.put(`/projects/${id}/status`, request)
  },

  /**
   * Sets or clears the next action (task) for a project.
   */
  setProjectNextAction: async (id: string, request: SetProjectNextActionRequest): Promise<void> => {
    await apiClient.put(`/projects/${id}/next-action`, request)
  },

  /**
   * Completes a project with optional outcome notes.
   */
  completeProject: async (id: string, request?: CompleteProjectRequest): Promise<void> => {
    await apiClient.post(`/projects/${id}/complete`, request ?? {})
  },

  /**
   * Archives (soft-deletes) a project.
   */
  deleteProject: async (id: string): Promise<void> => {
    await apiClient.delete(`/projects/${id}`)
  },

  // Milestone operations

  /**
   * Adds a new milestone to a project.
   */
  addMilestone: async (projectId: string, request: AddMilestoneRequest): Promise<string> => {
    const { data } = await apiClient.post<string>(`/projects/${projectId}/milestones`, request)
    return data
  },

  /**
   * Updates an existing milestone.
   */
  updateMilestone: async (
    projectId: string,
    milestoneId: string,
    request: UpdateMilestoneRequest
  ): Promise<void> => {
    await apiClient.put(`/projects/${projectId}/milestones/${milestoneId}`, request)
  },

  /**
   * Marks a milestone as completed.
   */
  completeMilestone: async (projectId: string, milestoneId: string): Promise<void> => {
    await apiClient.post(`/projects/${projectId}/milestones/${milestoneId}/complete`)
  },

  /**
   * Removes a milestone from a project.
   */
  removeMilestone: async (projectId: string, milestoneId: string): Promise<void> => {
    await apiClient.delete(`/projects/${projectId}/milestones/${milestoneId}`)
  },
}
