import { useEffect, useState } from 'react'
import { CheckCircle2, PartyPopper, Sparkles } from 'lucide-react'
import { cn } from '@/lib/utils'

interface CompletionCelebrationProps {
  show: boolean
  onComplete?: () => void
  streakMilestone?: number | null
}

export function CompletionCelebration({
  show,
  onComplete,
  streakMilestone,
}: CompletionCelebrationProps) {
  const [isAnimating, setIsAnimating] = useState(false)

  useEffect(() => {
    if (show) {
      setIsAnimating(true)
      const timer = setTimeout(() => {
        setIsAnimating(false)
        onComplete?.()
      }, 1000)
      return () => clearTimeout(timer)
    }
  }, [show, onComplete])

  if (!isAnimating) return null

  // Special celebration for milestones
  if (streakMilestone && streakMilestone > 0) {
    return (
      <div className="fixed inset-0 pointer-events-none z-50 flex items-center justify-center">
        <div className="animate-in zoom-in-50 fade-in duration-300 flex flex-col items-center gap-2">
          <div className="relative">
            <PartyPopper className="size-16 text-yellow-400 animate-bounce" />
            <Sparkles className="absolute -top-2 -right-2 size-6 text-yellow-300 animate-pulse" />
            <Sparkles className="absolute -bottom-1 -left-2 size-5 text-orange-400 animate-pulse" style={{ animationDelay: '150ms' }} />
          </div>
          <div className="text-center animate-in slide-in-from-bottom-2 duration-500">
            <p className="text-2xl font-bold text-yellow-400">{streakMilestone} Day Streak!</p>
            <p className="text-sm text-muted-foreground">Amazing consistency!</p>
          </div>
        </div>
        {/* Confetti particles */}
        <ConfettiParticles />
      </div>
    )
  }

  // Regular completion celebration
  return (
    <div className="fixed inset-0 pointer-events-none z-50 flex items-center justify-center">
      <div
        className={cn(
          'animate-in zoom-in-75 fade-in duration-200',
          'animate-out zoom-out-50 fade-out'
        )}
      >
        <CheckCircle2 className="size-20 text-green-500 drop-shadow-lg" />
      </div>
    </div>
  )
}

function ConfettiParticles() {
  const particles = Array.from({ length: 20 }).map((_, i) => ({
    id: i,
    x: Math.random() * 100,
    delay: Math.random() * 500,
    duration: 800 + Math.random() * 400,
    color: ['#fbbf24', '#f97316', '#22c55e', '#3b82f6', '#a855f7'][Math.floor(Math.random() * 5)],
  }))

  return (
    <>
      {particles.map((particle) => (
        <div
          key={particle.id}
          className="absolute w-2 h-2 rounded-full animate-in fade-in slide-in-from-top-4"
          style={{
            left: `${particle.x}%`,
            top: '40%',
            backgroundColor: particle.color,
            animationDelay: `${particle.delay}ms`,
            animationDuration: `${particle.duration}ms`,
          }}
        />
      ))}
    </>
  )
}

interface CheckmarkAnimationProps {
  className?: string
}

export function CheckmarkAnimation({ className }: CheckmarkAnimationProps) {
  return (
    <div className={cn('relative', className)}>
      <svg
        className="size-full animate-in zoom-in-50 duration-300"
        viewBox="0 0 24 24"
        fill="none"
        stroke="currentColor"
        strokeWidth="3"
        strokeLinecap="round"
        strokeLinejoin="round"
      >
        <path
          d="M5 12l5 5L19 7"
          className="text-green-500"
          style={{
            strokeDasharray: 24,
            strokeDashoffset: 24,
            animation: 'checkmark-draw 0.4s ease-out forwards',
          }}
        />
      </svg>
      <style>{`
        @keyframes checkmark-draw {
          to {
            stroke-dashoffset: 0;
          }
        }
      `}</style>
    </div>
  )
}
