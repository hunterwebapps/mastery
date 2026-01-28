// Enums
export type TaskStatus = 'Inbox' | 'Ready' | 'Scheduled' | 'InProgress' | 'Completed' | 'Cancelled' | 'Archived'
export type DueType = 'Soft' | 'Hard'
export type TaskContributionType = 'BooleanAs1' | 'FixedValue' | 'UseActualMinutes' | 'UseEnteredValue'
export type RescheduleReason = 'NoTime' | 'TooTired' | 'Blocked' | 'Forgot' | 'ScopeTooBig' | 'WaitingOnSomeone' | 'Other'
export type ContextTag = 'Computer' | 'Phone' | 'Errands' | 'Home' | 'Office' | 'DeepWork' | 'LowEnergy' | 'Anywhere'

// Due DTO
export interface TaskDueDto {
  dueOn?: string
  dueAt?: string
  dueType: DueType
}

// Scheduling DTO
export interface TaskSchedulingDto {
  scheduledOn: string
  preferredTimeWindowStart?: string
  preferredTimeWindowEnd?: string
}

// Completion DTO
export interface TaskCompletionDto {
  completedAtUtc: string
  completedOn: string
  actualMinutes?: number
  completionNote?: string
  enteredValue?: number
}

// Metric Binding DTO
export interface TaskMetricBindingDto {
  id: string
  metricDefinitionId: string
  metricName?: string
  contributionType: TaskContributionType
  fixedValue?: number
  notes?: string
}

// Full Task DTO
export interface TaskDto {
  id: string
  userId: string
  projectId?: string
  projectTitle?: string
  goalId?: string
  goalTitle?: string
  title: string
  description?: string
  status: TaskStatus
  priority: number
  estimatedMinutes: number
  energyCost: number
  displayOrder: number
  due?: TaskDueDto
  scheduling?: TaskSchedulingDto
  completion?: TaskCompletionDto
  contextTags: ContextTag[]
  dependencyTaskIds: string[]
  roleIds: string[]
  valueIds: string[]
  metricBindings: TaskMetricBindingDto[]
  lastRescheduleReason?: RescheduleReason
  rescheduleCount: number
  isOverdue: boolean
  isBlocked: boolean
  isEligibleForNBA: boolean
  createdAt: string
  modifiedAt?: string
}

// Summary DTO for list views
export interface TaskSummaryDto {
  id: string
  title: string
  description?: string
  status: TaskStatus
  priority: number
  estimatedMinutes: number
  energyCost: number
  displayOrder: number
  dueOn?: string
  dueType?: DueType
  scheduledOn?: string
  contextTags: ContextTag[]
  isOverdue: boolean
  isBlocked: boolean
  hasDependencies: boolean
  rescheduleCount: number
  projectId?: string
  projectTitle?: string
  goalId?: string
  goalTitle?: string
  createdAt: string
}

// Today projection - optimized for daily loop
export interface TodayTaskDto {
  id: string
  title: string
  description?: string
  status: TaskStatus
  priority: number
  estimatedMinutes: number
  energyCost: number
  displayOrder: number
  dueOn?: string
  dueType?: DueType
  scheduledOn?: string
  contextTags: ContextTag[]
  isOverdue: boolean
  isBlocked: boolean
  requiresValueEntry: boolean
  rescheduleCount: number
  projectId?: string
  projectTitle?: string
  goalId?: string
  goalTitle?: string
  dependencyTaskIds: string[]
}

// Inbox DTO for capture view
export interface InboxTaskDto {
  id: string
  title: string
  description?: string
  estimatedMinutes: number
  energyCost: number
  contextTags: ContextTag[]
  createdAt: string
}

// Request types
export interface CreateTaskDueRequest {
  dueOn: string
  dueAt?: string
  dueType?: DueType
}

export interface CreateTaskSchedulingRequest {
  scheduledOn: string
  preferredTimeWindowStart?: string
  preferredTimeWindowEnd?: string
}

export interface CreateTaskMetricBindingRequest {
  metricDefinitionId: string
  contributionType: TaskContributionType
  fixedValue?: number
  notes?: string
}

export interface CreateTaskRequest {
  title: string
  description?: string
  estimatedMinutes?: number
  energyCost?: number
  priority?: number
  projectId?: string
  goalId?: string
  due?: CreateTaskDueRequest
  scheduling?: CreateTaskSchedulingRequest
  contextTags?: ContextTag[]
  dependencyTaskIds?: string[]
  roleIds?: string[]
  valueIds?: string[]
  metricBindings?: CreateTaskMetricBindingRequest[]
  startAsReady?: boolean
}

export interface UpdateTaskRequest {
  title?: string
  description?: string
  estimatedMinutes?: number
  energyCost?: number
  priority?: number
  projectId?: string
  goalId?: string
  due?: CreateTaskDueRequest
  contextTags?: ContextTag[]
  dependencyTaskIds?: string[]
  roleIds?: string[]
  valueIds?: string[]
}

export interface ScheduleTaskRequest {
  scheduledOn: string
  preferredTimeWindowStart?: string
  preferredTimeWindowEnd?: string
}

export interface RescheduleTaskRequest {
  newDate: string
  reason?: RescheduleReason
}

export interface CompleteTaskRequest {
  completedOn: string
  actualMinutes?: number
  note?: string
  enteredValue?: number
}

// Batch operations
export interface BatchCompleteTaskItem {
  taskId: string
  completedOn: string
  actualMinutes?: number
  enteredValue?: number
}

export interface BatchCompleteTasksRequest {
  items: BatchCompleteTaskItem[]
}

export interface BatchRescheduleTasksRequest {
  taskIds: string[]
  newDate: string
  reason?: RescheduleReason
}

// UI Helpers
export const taskStatusInfo: Record<TaskStatus, {
  label: string
  description: string
  color: string
  bgColor: string
}> = {
  Inbox: {
    label: 'Inbox',
    description: 'Captured, not triaged',
    color: 'text-gray-400',
    bgColor: 'bg-gray-500/10',
  },
  Ready: {
    label: 'Ready',
    description: 'Ready to work on',
    color: 'text-blue-400',
    bgColor: 'bg-blue-500/10',
  },
  Scheduled: {
    label: 'Scheduled',
    description: 'Assigned to a date',
    color: 'text-purple-400',
    bgColor: 'bg-purple-500/10',
  },
  InProgress: {
    label: 'In Progress',
    description: 'Currently working',
    color: 'text-yellow-400',
    bgColor: 'bg-yellow-500/10',
  },
  Completed: {
    label: 'Completed',
    description: 'Done',
    color: 'text-green-400',
    bgColor: 'bg-green-500/10',
  },
  Cancelled: {
    label: 'Cancelled',
    description: 'Not doing',
    color: 'text-red-400',
    bgColor: 'bg-red-500/10',
  },
  Archived: {
    label: 'Archived',
    description: 'Hidden',
    color: 'text-muted-foreground',
    bgColor: 'bg-muted/50',
  },
}

export const dueTypeInfo: Record<DueType, {
  label: string
  description: string
  color: string
}> = {
  Soft: {
    label: 'Soft',
    description: 'Guidance date',
    color: 'text-blue-400',
  },
  Hard: {
    label: 'Hard',
    description: 'Commitment date',
    color: 'text-red-400',
  },
}

export const contextTagInfo: Record<ContextTag, {
  label: string
  emoji: string
  color: string
}> = {
  Computer: { label: 'Computer', emoji: '💻', color: 'text-blue-400' },
  Phone: { label: 'Phone', emoji: '📱', color: 'text-green-400' },
  Errands: { label: 'Errands', emoji: '🚗', color: 'text-orange-400' },
  Home: { label: 'Home', emoji: '🏠', color: 'text-purple-400' },
  Office: { label: 'Office', emoji: '🏢', color: 'text-cyan-400' },
  DeepWork: { label: 'Deep Work', emoji: '🎯', color: 'text-red-400' },
  LowEnergy: { label: 'Low Energy', emoji: '🔋', color: 'text-yellow-400' },
  Anywhere: { label: 'Anywhere', emoji: '🌍', color: 'text-gray-400' },
}

export const rescheduleReasonInfo: Record<RescheduleReason, {
  label: string
  emoji: string
}> = {
  NoTime: { label: 'No time', emoji: '⏰' },
  TooTired: { label: 'Too tired', emoji: '😴' },
  Blocked: { label: 'Blocked', emoji: '🚧' },
  Forgot: { label: 'Forgot', emoji: '🤔' },
  ScopeTooBig: { label: 'Scope too big', emoji: '📏' },
  WaitingOnSomeone: { label: 'Waiting on someone', emoji: '👥' },
  Other: { label: 'Other', emoji: '❓' },
}

export const energyCostInfo: Record<number, {
  label: string
  color: string
  bgColor: string
}> = {
  1: { label: 'Very Low', color: 'text-green-400', bgColor: 'bg-green-500/20' },
  2: { label: 'Low', color: 'text-green-300', bgColor: 'bg-green-500/15' },
  3: { label: 'Medium', color: 'text-yellow-400', bgColor: 'bg-yellow-500/15' },
  4: { label: 'High', color: 'text-orange-400', bgColor: 'bg-orange-500/15' },
  5: { label: 'Very High', color: 'text-red-400', bgColor: 'bg-red-500/20' },
}

export const priorityInfo: Record<number, {
  label: string
  color: string
  bgColor: string
}> = {
  1: { label: 'Highest', color: 'text-red-400', bgColor: 'bg-red-500/20' },
  2: { label: 'High', color: 'text-orange-400', bgColor: 'bg-orange-500/15' },
  3: { label: 'Medium', color: 'text-yellow-400', bgColor: 'bg-yellow-500/15' },
  4: { label: 'Low', color: 'text-blue-400', bgColor: 'bg-blue-500/15' },
  5: { label: 'Lowest', color: 'text-gray-400', bgColor: 'bg-gray-500/15' },
}

export const contributionTypeInfo: Record<TaskContributionType, {
  label: string
  description: string
}> = {
  BooleanAs1: { label: 'Boolean (1)', description: 'Each completion adds 1' },
  FixedValue: { label: 'Fixed Value', description: 'Each completion adds a fixed amount' },
  UseActualMinutes: { label: 'Actual Minutes', description: 'Use actual time spent' },
  UseEnteredValue: { label: 'Entered Value', description: 'User enters value at completion' },
}
