import { apiClient } from '@/lib/api-client'
import type {
  TaskDto,
  TaskSummaryDto,
  TodayTaskDto,
  InboxTaskDto,
  CreateTaskRequest,
  UpdateTaskRequest,
  ScheduleTaskRequest,
  RescheduleTaskRequest,
  CompleteTaskRequest,
  BatchCompleteTasksRequest,
  BatchRescheduleTasksRequest,
} from '@/types/task'

export interface GetTasksParams {
  status?: string
  projectId?: string
  goalId?: string
  contextTag?: string
  isOverdue?: boolean
}

export const tasksApi = {
  /**
   * Gets all tasks for the current user with optional filters.
   */
  getTasks: async (params?: GetTasksParams): Promise<TaskSummaryDto[]> => {
    const { data } = await apiClient.get<TaskSummaryDto[]>('/tasks', { params })
    return data
  },

  /**
   * Gets tasks for today's daily loop (scheduled today + due today + overdue).
   */
  getTodayTasks: async (): Promise<TodayTaskDto[]> => {
    const { data } = await apiClient.get<TodayTaskDto[]>('/tasks/today')
    return data
  },

  /**
   * Gets tasks in Inbox status for capture/triage.
   */
  getInboxTasks: async (): Promise<InboxTaskDto[]> => {
    const { data } = await apiClient.get<InboxTaskDto[]>('/tasks/inbox')
    return data
  },

  /**
   * Gets a single task by ID with full details.
   */
  getTaskById: async (id: string): Promise<TaskDto> => {
    const { data } = await apiClient.get<TaskDto>(`/tasks/${id}`)
    return data
  },

  /**
   * Creates a new task.
   */
  createTask: async (request: CreateTaskRequest): Promise<string> => {
    const { data } = await apiClient.post<string>('/tasks', request)
    return data
  },

  /**
   * Updates task information.
   */
  updateTask: async (id: string, request: UpdateTaskRequest): Promise<void> => {
    await apiClient.put(`/tasks/${id}`, request)
  },

  /**
   * Schedules a task for a specific date.
   */
  scheduleTask: async (id: string, request: ScheduleTaskRequest): Promise<void> => {
    await apiClient.post(`/tasks/${id}/schedule`, request)
  },

  /**
   * Reschedules a task to a new date with optional reason.
   */
  rescheduleTask: async (id: string, request: RescheduleTaskRequest): Promise<void> => {
    await apiClient.post(`/tasks/${id}/reschedule`, request)
  },

  /**
   * Completes a task.
   */
  completeTask: async (id: string, request: CompleteTaskRequest): Promise<void> => {
    await apiClient.post(`/tasks/${id}/complete`, request)
  },

  /**
   * Undoes a task completion (returns to Ready status).
   */
  undoTaskCompletion: async (id: string): Promise<void> => {
    await apiClient.post(`/tasks/${id}/undo`)
  },

  /**
   * Cancels a task.
   */
  cancelTask: async (id: string): Promise<void> => {
    await apiClient.post(`/tasks/${id}/cancel`)
  },

  /**
   * Moves a task from Inbox to Ready status.
   */
  moveTaskToReady: async (id: string): Promise<void> => {
    await apiClient.post(`/tasks/${id}/ready`)
  },

  /**
   * Archives (soft-deletes) a task.
   */
  deleteTask: async (id: string): Promise<void> => {
    await apiClient.delete(`/tasks/${id}`)
  },

  /**
   * Batch complete multiple tasks at once.
   */
  batchCompleteTasks: async (request: BatchCompleteTasksRequest): Promise<void> => {
    await apiClient.post('/tasks/batch/complete', request)
  },

  /**
   * Batch reschedule multiple tasks at once.
   */
  batchRescheduleTasks: async (request: BatchRescheduleTasksRequest): Promise<void> => {
    await apiClient.post('/tasks/batch/reschedule', request)
  },
}
