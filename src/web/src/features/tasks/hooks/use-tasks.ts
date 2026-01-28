import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { tasksApi, type GetTasksParams } from '../api/tasks-api'
import { projectKeys } from '@/features/projects/hooks/use-projects'
import type {
  TaskStatus,
  TodayTaskDto,
  TaskSummaryDto,
  CreateTaskRequest,
  UpdateTaskRequest,
  ScheduleTaskRequest,
  RescheduleTaskRequest,
  CompleteTaskRequest,
  BatchCompleteTasksRequest,
  BatchRescheduleTasksRequest,
} from '@/types/task'

export const taskKeys = {
  all: ['tasks'] as const,
  lists: () => [...taskKeys.all, 'list'] as const,
  list: (params?: GetTasksParams) => [...taskKeys.lists(), params] as const,
  details: () => [...taskKeys.all, 'detail'] as const,
  detail: (id: string) => [...taskKeys.details(), id] as const,
  today: () => [...taskKeys.all, 'today'] as const,
  inbox: () => [...taskKeys.all, 'inbox'] as const,
  byProject: (projectId: string) => [...taskKeys.all, 'project', projectId] as const,
}

export function useTasks(params?: GetTasksParams) {
  return useQuery({
    queryKey: taskKeys.list(params),
    queryFn: () => tasksApi.getTasks(params),
  })
}

export function useTask(id: string) {
  return useQuery({
    queryKey: taskKeys.detail(id),
    queryFn: () => tasksApi.getTaskById(id),
    enabled: !!id,
  })
}

export function useTodayTasks() {
  return useQuery({
    queryKey: taskKeys.today(),
    queryFn: () => tasksApi.getTodayTasks(),
    refetchOnWindowFocus: true,
  })
}

export function useInboxTasks() {
  return useQuery({
    queryKey: taskKeys.inbox(),
    queryFn: () => tasksApi.getInboxTasks(),
    refetchOnWindowFocus: true,
  })
}

export function useTasksByProject(projectId: string) {
  return useQuery({
    queryKey: taskKeys.byProject(projectId),
    queryFn: () => tasksApi.getTasks({ projectId }),
    enabled: !!projectId,
  })
}

export function useCreateTask() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (request: CreateTaskRequest) => tasksApi.createTask(request),
    onSuccess: (_, request) => {
      queryClient.invalidateQueries({ queryKey: taskKeys.lists() })
      queryClient.invalidateQueries({ queryKey: taskKeys.today() })
      queryClient.invalidateQueries({ queryKey: taskKeys.inbox() })
      // Also invalidate project-specific queries if task belongs to a project
      if (request.projectId) {
        queryClient.invalidateQueries({ queryKey: taskKeys.byProject(request.projectId) })
      }
    },
  })
}

export function useUpdateTask() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({ id, request }: { id: string; request: UpdateTaskRequest }) =>
      tasksApi.updateTask(id, request),
    onSuccess: (_, { id }) => {
      queryClient.invalidateQueries({ queryKey: taskKeys.lists() })
      queryClient.invalidateQueries({ queryKey: taskKeys.detail(id) })
      queryClient.invalidateQueries({ queryKey: taskKeys.today() })
    },
  })
}

export function useScheduleTask() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({ id, request }: { id: string; request: ScheduleTaskRequest }) =>
      tasksApi.scheduleTask(id, request),
    onSuccess: (_, { id }) => {
      queryClient.invalidateQueries({ queryKey: taskKeys.lists() })
      queryClient.invalidateQueries({ queryKey: taskKeys.detail(id) })
      queryClient.invalidateQueries({ queryKey: taskKeys.today() })
      queryClient.invalidateQueries({ queryKey: taskKeys.inbox() })
    },
  })
}

export function useRescheduleTask() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({ id, request }: { id: string; request: RescheduleTaskRequest }) =>
      tasksApi.rescheduleTask(id, request),
    onSuccess: (_, { id }) => {
      queryClient.invalidateQueries({ queryKey: taskKeys.lists() })
      queryClient.invalidateQueries({ queryKey: taskKeys.detail(id) })
      queryClient.invalidateQueries({ queryKey: taskKeys.today() })
    },
  })
}

/**
 * Complete a task with optimistic update for instant feedback.
 */
export function useCompleteTask() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({ id, request }: { id: string; request: CompleteTaskRequest }) =>
      tasksApi.completeTask(id, request),

    // Optimistic update for instant feedback
    onMutate: async ({ id }) => {
      // Cancel any outgoing refetches
      await queryClient.cancelQueries({ queryKey: taskKeys.today() })
      await queryClient.cancelQueries({ queryKey: ['tasks', 'project'] })

      // Snapshot the previous values
      const previousTasks = queryClient.getQueryData<TodayTaskDto[]>(taskKeys.today())

      // Optimistically update today tasks
      queryClient.setQueryData<TodayTaskDto[]>(taskKeys.today(), (old) =>
        old?.map((t) =>
          t.id === id
            ? {
                ...t,
                status: 'Completed' as TaskStatus,
              }
            : t
        )
      )

      // Optimistically update any byProject queries that contain this task
      queryClient.setQueriesData<TaskSummaryDto[]>(
        { queryKey: ['tasks', 'project'] },
        (old) =>
          old?.map((t) =>
            t.id === id
              ? {
                  ...t,
                  status: 'Completed' as TaskStatus,
                }
              : t
          )
      )

      return { previousTasks }
    },

    onError: (_err, _variables, context) => {
      // Rollback on error
      if (context?.previousTasks) {
        queryClient.setQueryData(taskKeys.today(), context.previousTasks)
      }
    },

    onSettled: (_, __, { id }) => {
      // Refetch after success or error
      queryClient.invalidateQueries({ queryKey: taskKeys.today() })
      queryClient.invalidateQueries({ queryKey: taskKeys.detail(id) })
      queryClient.invalidateQueries({ queryKey: taskKeys.lists() })
      // Invalidate project task lists and project details (for Stuck indicator)
      queryClient.invalidateQueries({ queryKey: ['tasks', 'project'] })
      queryClient.invalidateQueries({ queryKey: projectKeys.all })
    },
  })
}

/**
 * Undo a task completion with optimistic update.
 */
export function useUndoTaskCompletion() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (id: string) => tasksApi.undoTaskCompletion(id),

    onMutate: async (id) => {
      await queryClient.cancelQueries({ queryKey: taskKeys.today() })
      const previousTasks = queryClient.getQueryData<TodayTaskDto[]>(taskKeys.today())

      queryClient.setQueryData<TodayTaskDto[]>(taskKeys.today(), (old) =>
        old?.map((t) =>
          t.id === id
            ? {
                ...t,
                status: 'Ready' as TaskStatus,
              }
            : t
        )
      )

      return { previousTasks }
    },

    onError: (_err, _variables, context) => {
      if (context?.previousTasks) {
        queryClient.setQueryData(taskKeys.today(), context.previousTasks)
      }
    },

    onSettled: (_, __, id) => {
      queryClient.invalidateQueries({ queryKey: taskKeys.today() })
      queryClient.invalidateQueries({ queryKey: taskKeys.detail(id) })
      queryClient.invalidateQueries({ queryKey: taskKeys.lists() })
    },
  })
}

export function useCancelTask() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (id: string) => tasksApi.cancelTask(id),

    onMutate: async (id) => {
      await queryClient.cancelQueries({ queryKey: taskKeys.today() })
      const previousTasks = queryClient.getQueryData<TodayTaskDto[]>(taskKeys.today())

      queryClient.setQueryData<TodayTaskDto[]>(taskKeys.today(), (old) =>
        old?.map((t) =>
          t.id === id
            ? {
                ...t,
                status: 'Cancelled' as TaskStatus,
              }
            : t
        )
      )

      return { previousTasks }
    },

    onError: (_err, _variables, context) => {
      if (context?.previousTasks) {
        queryClient.setQueryData(taskKeys.today(), context.previousTasks)
      }
    },

    onSettled: (_, __, id) => {
      queryClient.invalidateQueries({ queryKey: taskKeys.today() })
      queryClient.invalidateQueries({ queryKey: taskKeys.detail(id) })
      queryClient.invalidateQueries({ queryKey: taskKeys.lists() })
    },
  })
}

export function useMoveTaskToReady() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (id: string) => tasksApi.moveTaskToReady(id),
    onSuccess: (_, id) => {
      queryClient.invalidateQueries({ queryKey: taskKeys.inbox() })
      queryClient.invalidateQueries({ queryKey: taskKeys.detail(id) })
      queryClient.invalidateQueries({ queryKey: taskKeys.lists() })
    },
  })
}

export function useDeleteTask() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (id: string) => tasksApi.deleteTask(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: taskKeys.lists() })
      queryClient.invalidateQueries({ queryKey: taskKeys.today() })
      queryClient.invalidateQueries({ queryKey: taskKeys.inbox() })
    },
  })
}

/**
 * Batch complete multiple tasks at once.
 */
export function useBatchCompleteTasks() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (request: BatchCompleteTasksRequest) => tasksApi.batchCompleteTasks(request),

    onMutate: async ({ items }) => {
      await queryClient.cancelQueries({ queryKey: taskKeys.today() })
      const previousTasks = queryClient.getQueryData<TodayTaskDto[]>(taskKeys.today())

      const completedIds = new Set(items.map((i) => i.taskId))
      queryClient.setQueryData<TodayTaskDto[]>(taskKeys.today(), (old) =>
        old?.map((t) =>
          completedIds.has(t.id)
            ? {
                ...t,
                status: 'Completed' as TaskStatus,
              }
            : t
        )
      )

      return { previousTasks }
    },

    onError: (_err, _variables, context) => {
      if (context?.previousTasks) {
        queryClient.setQueryData(taskKeys.today(), context.previousTasks)
      }
    },

    onSettled: () => {
      queryClient.invalidateQueries({ queryKey: taskKeys.today() })
      queryClient.invalidateQueries({ queryKey: taskKeys.lists() })
    },
  })
}

/**
 * Batch reschedule multiple tasks at once.
 */
export function useBatchRescheduleTasks() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (request: BatchRescheduleTasksRequest) => tasksApi.batchRescheduleTasks(request),

    onMutate: async ({ taskIds }) => {
      await queryClient.cancelQueries({ queryKey: taskKeys.today() })
      const previousTasks = queryClient.getQueryData<TodayTaskDto[]>(taskKeys.today())

      // Remove rescheduled tasks from today view
      const rescheduledIds = new Set(taskIds)
      queryClient.setQueryData<TodayTaskDto[]>(taskKeys.today(), (old) =>
        old?.filter((t) => !rescheduledIds.has(t.id))
      )

      return { previousTasks }
    },

    onError: (_err, _variables, context) => {
      if (context?.previousTasks) {
        queryClient.setQueryData(taskKeys.today(), context.previousTasks)
      }
    },

    onSettled: () => {
      queryClient.invalidateQueries({ queryKey: taskKeys.today() })
      queryClient.invalidateQueries({ queryKey: taskKeys.lists() })
    },
  })
}
