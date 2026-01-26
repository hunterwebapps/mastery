import { StepIndicator } from './step-indicator'

interface OnboardingLayoutProps {
  currentStep: number
  totalSteps: number
  children: React.ReactNode
}

export function OnboardingLayout({
  currentStep,
  totalSteps,
  children,
}: OnboardingLayoutProps) {
  return (
    <div className="min-h-screen bg-background flex flex-col">
      {/* Header */}
      <header className="border-b border-border bg-card">
        <div className="container max-w-4xl py-6 px-4 sm:px-6 lg:px-8">
          <div className="text-center mb-6">
            <h1 className="text-2xl font-bold text-primary">Welcome to Mastery</h1>
            <p className="text-muted-foreground mt-1">
              Let's set up your personal mastery system
            </p>
          </div>
          <StepIndicator currentStep={currentStep} totalSteps={totalSteps} />
        </div>
      </header>

      {/* Main content */}
      <main className="flex-1 container max-w-2xl py-8 px-4 sm:px-6 lg:px-8">
        <div className="bg-card rounded-lg border border-border p-6 sm:p-8">
          {children}
        </div>
      </main>

      {/* Footer */}
      <footer className="border-t border-border py-4">
        <div className="container max-w-4xl px-4 sm:px-6 lg:px-8">
          <p className="text-xs text-center text-muted-foreground">
            Your data is encrypted and never shared. You can change these settings anytime.
          </p>
        </div>
      </footer>
    </div>
  )
}
