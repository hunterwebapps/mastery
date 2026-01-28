import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { projectsApi, type GetProjectsParams } from '../api/projects-api'
import type {
  ProjectStatus,
  ProjectDto,
  ProjectSummaryDto,
  CreateProjectRequest,
  ChangeProjectStatusRequest,
  SetProjectNextActionRequest,
  CompleteProjectRequest,
  AddMilestoneRequest,
  UpdateMilestoneRequest,
} from '@/types/project'

export const projectKeys = {
  all: ['projects'] as const,
  lists: () => [...projectKeys.all, 'list'] as const,
  list: (params?: GetProjectsParams) => [...projectKeys.lists(), params] as const,
  details: () => [...projectKeys.all, 'detail'] as const,
  detail: (id: string) => [...projectKeys.details(), id] as const,
  byGoal: (goalId: string) => [...projectKeys.all, 'goal', goalId] as const,
}

export function useProjects(params?: GetProjectsParams) {
  return useQuery({
    queryKey: projectKeys.list(params),
    queryFn: () => projectsApi.getProjects(params),
  })
}

export function useProject(id: string) {
  return useQuery({
    queryKey: projectKeys.detail(id),
    queryFn: () => projectsApi.getProjectById(id),
    enabled: !!id,
  })
}

export function useProjectsByGoal(goalId: string) {
  return useQuery({
    queryKey: projectKeys.byGoal(goalId),
    queryFn: () => projectsApi.getProjects({ goalId }),
    enabled: !!goalId,
  })
}

export function useCreateProject() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (request: CreateProjectRequest) => projectsApi.createProject(request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: projectKeys.lists() })
    },
  })
}

export function useChangeProjectStatus() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({ id, request }: { id: string; request: ChangeProjectStatusRequest }) =>
      projectsApi.changeProjectStatus(id, request),

    // Optimistic update
    onMutate: async ({ id, request }) => {
      await queryClient.cancelQueries({ queryKey: projectKeys.lists() })
      const previousProjects = queryClient.getQueryData<ProjectSummaryDto[]>(projectKeys.list())

      queryClient.setQueryData<ProjectSummaryDto[]>(projectKeys.list(), (old) =>
        old?.map((p) =>
          p.id === id
            ? {
                ...p,
                status: request.newStatus as ProjectStatus,
              }
            : p
        )
      )

      return { previousProjects }
    },

    onError: (_err, _variables, context) => {
      if (context?.previousProjects) {
        queryClient.setQueryData(projectKeys.list(), context.previousProjects)
      }
    },

    onSettled: (_, __, { id }) => {
      queryClient.invalidateQueries({ queryKey: projectKeys.lists() })
      queryClient.invalidateQueries({ queryKey: projectKeys.detail(id) })
    },
  })
}

export function useSetProjectNextAction() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({ id, request }: { id: string; request: SetProjectNextActionRequest }) =>
      projectsApi.setProjectNextAction(id, request),
    onSuccess: (_, { id }) => {
      queryClient.invalidateQueries({ queryKey: projectKeys.lists() })
      queryClient.invalidateQueries({ queryKey: projectKeys.detail(id) })
    },
  })
}

/**
 * Complete a project with optimistic update.
 */
export function useCompleteProject() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({ id, request }: { id: string; request?: CompleteProjectRequest }) =>
      projectsApi.completeProject(id, request),

    onMutate: async ({ id }) => {
      await queryClient.cancelQueries({ queryKey: projectKeys.lists() })
      const previousProjects = queryClient.getQueryData<ProjectSummaryDto[]>(projectKeys.list())

      queryClient.setQueryData<ProjectSummaryDto[]>(projectKeys.list(), (old) =>
        old?.map((p) =>
          p.id === id
            ? {
                ...p,
                status: 'Completed' as ProjectStatus,
              }
            : p
        )
      )

      return { previousProjects }
    },

    onError: (_err, _variables, context) => {
      if (context?.previousProjects) {
        queryClient.setQueryData(projectKeys.list(), context.previousProjects)
      }
    },

    onSettled: (_, __, { id }) => {
      queryClient.invalidateQueries({ queryKey: projectKeys.lists() })
      queryClient.invalidateQueries({ queryKey: projectKeys.detail(id) })
    },
  })
}

export function useDeleteProject() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (id: string) => projectsApi.deleteProject(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: projectKeys.lists() })
    },
  })
}

// Milestone hooks

/**
 * Add a new milestone to a project.
 */
export function useAddMilestone() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({ projectId, request }: { projectId: string; request: AddMilestoneRequest }) =>
      projectsApi.addMilestone(projectId, request),
    onSuccess: (_, { projectId }) => {
      queryClient.invalidateQueries({ queryKey: projectKeys.detail(projectId) })
      queryClient.invalidateQueries({ queryKey: projectKeys.lists() })
    },
  })
}

/**
 * Update an existing milestone.
 */
export function useUpdateMilestone() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({
      projectId,
      milestoneId,
      request,
    }: {
      projectId: string
      milestoneId: string
      request: UpdateMilestoneRequest
    }) => projectsApi.updateMilestone(projectId, milestoneId, request),
    onSuccess: (_, { projectId }) => {
      queryClient.invalidateQueries({ queryKey: projectKeys.detail(projectId) })
      queryClient.invalidateQueries({ queryKey: projectKeys.lists() })
    },
  })
}

/**
 * Complete a milestone with optimistic update.
 */
export function useCompleteMilestone() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({ projectId, milestoneId }: { projectId: string; milestoneId: string }) =>
      projectsApi.completeMilestone(projectId, milestoneId),

    // Optimistic update
    onMutate: async ({ projectId, milestoneId }) => {
      await queryClient.cancelQueries({ queryKey: projectKeys.detail(projectId) })
      const previousProject = queryClient.getQueryData<ProjectDto>(projectKeys.detail(projectId))

      queryClient.setQueryData<ProjectDto>(projectKeys.detail(projectId), (old) => {
        if (!old) return old
        return {
          ...old,
          milestones: old.milestones.map((m) =>
            m.id === milestoneId
              ? { ...m, status: 'Completed' as const, completedAtUtc: new Date().toISOString() }
              : m
          ),
        }
      })

      return { previousProject }
    },

    onError: (_err, { projectId }, context) => {
      if (context?.previousProject) {
        queryClient.setQueryData(projectKeys.detail(projectId), context.previousProject)
      }
    },

    onSettled: (_, __, { projectId }) => {
      queryClient.invalidateQueries({ queryKey: projectKeys.detail(projectId) })
      queryClient.invalidateQueries({ queryKey: projectKeys.lists() })
    },
  })
}

/**
 * Remove a milestone from a project.
 */
export function useRemoveMilestone() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({ projectId, milestoneId }: { projectId: string; milestoneId: string }) =>
      projectsApi.removeMilestone(projectId, milestoneId),

    // Optimistic update
    onMutate: async ({ projectId, milestoneId }) => {
      await queryClient.cancelQueries({ queryKey: projectKeys.detail(projectId) })
      const previousProject = queryClient.getQueryData<ProjectDto>(projectKeys.detail(projectId))

      queryClient.setQueryData<ProjectDto>(projectKeys.detail(projectId), (old) => {
        if (!old) return old
        return {
          ...old,
          milestones: old.milestones.filter((m) => m.id !== milestoneId),
        }
      })

      return { previousProject }
    },

    onError: (_err, { projectId }, context) => {
      if (context?.previousProject) {
        queryClient.setQueryData(projectKeys.detail(projectId), context.previousProject)
      }
    },

    onSettled: (_, __, { projectId }) => {
      queryClient.invalidateQueries({ queryKey: projectKeys.detail(projectId) })
      queryClient.invalidateQueries({ queryKey: projectKeys.lists() })
    },
  })
}
