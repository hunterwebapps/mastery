import { GoalWizard } from '../components/goal-form'

export function Component() {
  return (
    <div className="min-h-screen bg-background">
      <div className="container max-w-2xl py-8 px-4 sm:px-6 lg:px-8">
        <GoalWizard />
      </div>
    </div>
  )
}
