import { CheckCircle, Loader2 } from 'lucide-react'
import { Button } from '@/components/ui/button'

interface CompletionScreenProps {
  isSubmitting: boolean
  error?: string
  onRetry: () => void
}

export function CompletionScreen({ isSubmitting, error, onRetry }: CompletionScreenProps) {
  if (isSubmitting) {
    return (
      <div className="flex flex-col items-center justify-center py-12 space-y-6">
        <Loader2 className="size-16 text-primary animate-spin" />
        <div className="text-center">
          <h2 className="text-xl font-semibold mb-2">Setting up your account...</h2>
          <p className="text-muted-foreground">
            We're creating your personalized mastery system.
          </p>
        </div>
      </div>
    )
  }

  if (error) {
    return (
      <div className="flex flex-col items-center justify-center py-12 space-y-6">
        <div className="size-16 rounded-full bg-destructive/10 flex items-center justify-center">
          <span className="text-3xl">⚠️</span>
        </div>
        <div className="text-center">
          <h2 className="text-xl font-semibold mb-2">Something went wrong</h2>
          <p className="text-muted-foreground mb-4">{error}</p>
          <Button onClick={onRetry}>Try Again</Button>
        </div>
      </div>
    )
  }

  return (
    <div className="flex flex-col items-center justify-center py-12 space-y-6">
      <div className="size-16 rounded-full bg-primary/10 flex items-center justify-center">
        <CheckCircle className="size-10 text-primary" />
      </div>
      <div className="text-center">
        <h2 className="text-xl font-semibold mb-2">You're all set!</h2>
        <p className="text-muted-foreground">
          Your personalized mastery system is ready. Let's get started.
        </p>
      </div>
    </div>
  )
}
