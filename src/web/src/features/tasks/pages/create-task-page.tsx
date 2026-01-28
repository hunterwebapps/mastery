import { useSearchParams } from 'react-router-dom'
import { TaskForm } from '../components/task-form'

export function Component() {
  const [searchParams] = useSearchParams()
  const projectId = searchParams.get('projectId') ?? undefined

  return (
    <div className="min-h-screen bg-background">
      <div className="container max-w-2xl py-8 px-4 sm:px-6 lg:px-8">
        <TaskForm mode="create" defaultProjectId={projectId} />
      </div>
    </div>
  )
}
