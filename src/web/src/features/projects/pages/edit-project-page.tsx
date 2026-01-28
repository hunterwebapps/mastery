import { useParams } from 'react-router-dom'
import { Loader2 } from 'lucide-react'
import { ProjectForm } from '../components/project-form'
import { useProject } from '../hooks/use-projects'

export function Component() {
  const { id } = useParams<{ id: string }>()
  const { data: project, isLoading, error } = useProject(id ?? '')

  if (isLoading) {
    return (
      <div className="min-h-screen bg-background flex items-center justify-center">
        <Loader2 className="size-8 animate-spin text-muted-foreground" />
      </div>
    )
  }

  if (error || !project) {
    return (
      <div className="min-h-screen bg-background flex items-center justify-center">
        <div className="text-center">
          <h2 className="text-xl font-semibold text-destructive">Project not found</h2>
          <p className="text-muted-foreground mt-2">
            The project you're looking for doesn't exist or has been deleted.
          </p>
        </div>
      </div>
    )
  }

  return (
    <div className="min-h-screen bg-background">
      <div className="container max-w-2xl py-8 px-4 sm:px-6 lg:px-8">
        <ProjectForm mode="edit" initialData={project} />
      </div>
    </div>
  )
}
