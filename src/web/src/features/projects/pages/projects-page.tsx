import { useState } from 'react'
import { Link } from 'react-router-dom'
import { Plus, Folder, Loader2 } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Card } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import { Tooltip, TooltipContent, TooltipTrigger } from '@/components/ui/tooltip'
import { Tabs, TabsList, TabsTrigger } from '@/components/ui/tabs'
import { cn } from '@/lib/utils'
import { useProjects } from '../hooks/use-projects'
import type { ProjectStatus } from '@/types/project'
import { projectStatusInfo } from '@/types/project'

type StatusFilter = ProjectStatus | 'all'

export function Component() {
  const [statusFilter, setStatusFilter] = useState<StatusFilter>('Active')

  const { data: projects, isLoading } = useProjects(
    statusFilter === 'all' ? undefined : { status: statusFilter }
  )

  return (
    <div className="min-h-screen bg-background">
      <div className="container max-w-4xl py-8 px-4 sm:px-6 lg:px-8">
        {/* Header */}
        <div className="flex items-center justify-between mb-8">
          <div className="flex items-center gap-3">
            <div className="p-2 rounded-lg bg-primary/10">
              <Folder className="size-6 text-primary" />
            </div>
            <div>
              <h1 className="text-2xl font-bold text-foreground">Projects</h1>
              <p className="text-sm text-muted-foreground">
                Execution containers for achieving goals
              </p>
            </div>
          </div>
          <Button asChild>
            <Link to="/projects/new">
              <Plus className="size-4 mr-2" />
              New Project
            </Link>
          </Button>
        </div>

        {/* Status filter */}
        <Tabs
          value={statusFilter}
          onValueChange={(v) => setStatusFilter(v as StatusFilter)}
          className="mb-6"
        >
          <TabsList>
            <TabsTrigger value="Active">Active</TabsTrigger>
            <TabsTrigger value="Draft">Draft</TabsTrigger>
            <TabsTrigger value="Paused">Paused</TabsTrigger>
            <TabsTrigger value="Completed">Completed</TabsTrigger>
            <TabsTrigger value="all">All</TabsTrigger>
          </TabsList>
        </Tabs>

        {/* Projects list */}
        {isLoading ? (
          <div className="flex items-center justify-center py-12">
            <Loader2 className="size-8 animate-spin text-muted-foreground" />
          </div>
        ) : projects && projects.length > 0 ? (
          <div className="grid gap-4">
            {projects.map((project) => {
              const statusInfo = projectStatusInfo[project.status]
              const progress = project.totalTasks > 0
                ? Math.round((project.completedTasks / project.totalTasks) * 100)
                : 0

              return (
                <Link key={project.id} to={`/projects/${project.id}`}>
                  <Card className="p-4 hover:shadow-md transition-shadow">
                    <div className="flex items-start justify-between">
                      <div className="flex-1 min-w-0">
                        <div className="flex items-center gap-2 mb-1">
                          <h3 className="font-medium truncate">{project.title}</h3>
                          <Badge className={cn('text-xs', statusInfo.bgColor, statusInfo.color)}>
                            {statusInfo.label}
                          </Badge>
                          {project.isStuck && (
                            <Tooltip>
                              <TooltipTrigger asChild>
                                <Badge variant="outline" className="text-amber-500 border-amber-500 cursor-help">
                                  Stuck
                                </Badge>
                              </TooltipTrigger>
                              <TooltipContent>
                                <p>No actionable tasks - add tasks or move them to Ready</p>
                              </TooltipContent>
                            </Tooltip>
                          )}
                        </div>
                        {project.description && (
                          <p className="text-sm text-muted-foreground truncate">
                            {project.description}
                          </p>
                        )}
                        <div className="flex items-center gap-4 mt-2 text-xs text-muted-foreground">
                          <span>{project.completedTasks}/{project.totalTasks} tasks</span>
                          {project.milestoneCount > 0 && (
                            <span>
                              {project.completedMilestones}/{project.milestoneCount} milestones
                            </span>
                          )}
                          {project.goalTitle && (
                            <span className="text-primary">{project.goalTitle}</span>
                          )}
                        </div>
                      </div>

                      {/* Progress indicator */}
                      <div className="w-16 text-right">
                        <span className="text-2xl font-bold">{progress}%</span>
                      </div>
                    </div>

                    {/* Progress bar */}
                    <div className="mt-3 h-1 bg-muted rounded-full overflow-hidden">
                      <div
                        className="h-full bg-primary transition-all"
                        style={{ width: `${progress}%` }}
                      />
                    </div>
                  </Card>
                </Link>
              )
            })}
          </div>
        ) : (
          <div className="text-center py-12">
            <Folder className="size-12 mx-auto mb-4 text-muted-foreground/50" />
            <h3 className="text-lg font-medium text-muted-foreground">No projects found</h3>
            <p className="text-sm text-muted-foreground mt-1">
              Create a project to organize your tasks toward a goal.
            </p>
            <Button asChild className="mt-4">
              <Link to="/projects/new">
                <Plus className="size-4 mr-2" />
                Create Project
              </Link>
            </Button>
          </div>
        )}
      </div>
    </div>
  )
}
