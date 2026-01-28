import { ProjectForm } from '../components/project-form'

export function Component() {
  return (
    <div className="min-h-screen bg-background">
      <div className="container max-w-2xl py-8 px-4 sm:px-6 lg:px-8">
        <ProjectForm mode="create" />
      </div>
    </div>
  )
}
