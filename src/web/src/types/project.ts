// Enums
export type ProjectStatus = 'Draft' | 'Active' | 'Paused' | 'Completed' | 'Archived'
export type MilestoneStatus = 'NotStarted' | 'InProgress' | 'Completed'

// Milestone DTO
export interface MilestoneDto {
  id: string
  projectId: string
  title: string
  targetDate?: string
  status: MilestoneStatus
  notes?: string
  displayOrder: number
  completedAtUtc?: string
  createdAt: string
}

// Task Counts DTO
export interface ProjectTaskCountsDto {
  inbox: number
  ready: number
  scheduled: number
  inProgress: number
  completed: number
  cancelled: number
  total: number
}

// Full Project DTO
export interface ProjectDto {
  id: string
  userId: string
  title: string
  description?: string
  status: ProjectStatus
  priority: number
  goalId?: string
  goalTitle?: string
  seasonId?: string
  targetEndDate?: string
  nextTaskId?: string
  nextTaskTitle?: string
  roleIds: string[]
  valueIds: string[]
  milestones: MilestoneDto[]
  outcomeNotes?: string
  completedAtUtc?: string
  totalTasks: number
  completedTasks: number
  inProgressTasks: number
  isStuck: boolean
  createdAt: string
  modifiedAt?: string
}

// Summary DTO for list views
export interface ProjectSummaryDto {
  id: string
  title: string
  description?: string
  status: ProjectStatus
  priority: number
  goalId?: string
  goalTitle?: string
  targetEndDate?: string
  nextTaskId?: string
  nextTaskTitle?: string
  totalTasks: number
  completedTasks: number
  milestoneCount: number
  completedMilestones: number
  isStuck: boolean
  isNearingDeadline: boolean
  createdAt: string
}

// Detail DTO with task counts
export interface ProjectDetailDto {
  id: string
  title: string
  description?: string
  status: ProjectStatus
  priority: number
  goalId?: string
  goalTitle?: string
  seasonId?: string
  targetEndDate?: string
  nextTaskId?: string
  nextTaskTitle?: string
  milestones: MilestoneDto[]
  taskCounts: ProjectTaskCountsDto
  outcomeNotes?: string
  isStuck: boolean
  createdAt: string
  modifiedAt?: string
}

// Progress DTO for dashboard
export interface ProjectProgressDto {
  id: string
  title: string
  status: ProjectStatus
  totalTasks: number
  completedTasks: number
  totalMilestones: number
  completedMilestones: number
  completionPercentage: number
  targetEndDate?: string
  daysUntilDeadline?: number
}

// Stuck project DTO
export interface StuckProjectDto {
  id: string
  title: string
  priority: number
  totalReadyTasks: number
  suggestedNextTaskTitle?: string
  suggestedNextTaskId?: string
  lastUpdated: string
}

// Request types
export interface CreateMilestoneRequest {
  title: string
  targetDate?: string
  notes?: string
}

export interface CreateProjectRequest {
  title: string
  description?: string
  priority?: number
  goalId?: string
  seasonId?: string
  targetEndDate?: string
  roleIds?: string[]
  valueIds?: string[]
  milestones?: CreateMilestoneRequest[]
  saveAsDraft?: boolean
}

export interface UpdateProjectRequest {
  title?: string
  description?: string
  priority?: number
  goalId?: string
  seasonId?: string
  targetEndDate?: string
  roleIds?: string[]
  valueIds?: string[]
}

export interface ChangeProjectStatusRequest {
  newStatus: ProjectStatus
}

export interface SetProjectNextActionRequest {
  taskId?: string
}

export interface CompleteProjectRequest {
  outcomeNotes?: string
}

export interface AddMilestoneRequest {
  title: string
  targetDate?: string
  notes?: string
}

export interface UpdateMilestoneRequest {
  title?: string
  targetDate?: string
  notes?: string
  displayOrder?: number
}

// UI Helpers
export const projectStatusInfo: Record<ProjectStatus, {
  label: string
  description: string
  color: string
  bgColor: string
}> = {
  Draft: {
    label: 'Draft',
    description: 'Being defined',
    color: 'text-gray-400',
    bgColor: 'bg-gray-500/10',
  },
  Active: {
    label: 'Active',
    description: 'In progress',
    color: 'text-green-400',
    bgColor: 'bg-green-500/10',
  },
  Paused: {
    label: 'Paused',
    description: 'On hold',
    color: 'text-yellow-400',
    bgColor: 'bg-yellow-500/10',
  },
  Completed: {
    label: 'Completed',
    description: 'Successfully done',
    color: 'text-blue-400',
    bgColor: 'bg-blue-500/10',
  },
  Archived: {
    label: 'Archived',
    description: 'Hidden',
    color: 'text-muted-foreground',
    bgColor: 'bg-muted/50',
  },
}

export const milestoneStatusInfo: Record<MilestoneStatus, {
  label: string
  color: string
  bgColor: string
}> = {
  NotStarted: {
    label: 'Not Started',
    color: 'text-gray-400',
    bgColor: 'bg-gray-500/10',
  },
  InProgress: {
    label: 'In Progress',
    color: 'text-yellow-400',
    bgColor: 'bg-yellow-500/10',
  },
  Completed: {
    label: 'Completed',
    color: 'text-green-400',
    bgColor: 'bg-green-500/10',
  },
}
