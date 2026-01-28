import { cn } from '@/lib/utils'
import { Flame, Shield, Battery } from 'lucide-react'
import type { ReactNode } from 'react'

interface ModeStepProps {
  value?: string
  onChange: (mode: string) => void
  suggestedMode?: string
}

interface ModeOption {
  value: string
  label: string
  description: string
  icon: ReactNode
  color: string
  selectedColor: string
}

const modes: ModeOption[] = [
  {
    value: 'Full',
    label: 'Full',
    description: 'Tackle everything. Full capacity today.',
    icon: <Flame className="size-6" />,
    color: 'border-blue-500/40 bg-blue-500/10 hover:bg-blue-500/20 text-blue-400',
    selectedColor: 'border-blue-500 bg-blue-500/25 ring-2 ring-blue-500/30 text-blue-300',
  },
  {
    value: 'Maintenance',
    label: 'Maintenance',
    description: 'Keep the streak alive. Core habits only.',
    icon: <Shield className="size-6" />,
    color: 'border-yellow-500/40 bg-yellow-500/10 hover:bg-yellow-500/20 text-yellow-400',
    selectedColor: 'border-yellow-500 bg-yellow-500/25 ring-2 ring-yellow-500/30 text-yellow-300',
  },
  {
    value: 'Minimum',
    label: 'Minimum',
    description: 'Rest day. Only the bare essentials.',
    icon: <Battery className="size-6" />,
    color: 'border-orange-500/40 bg-orange-500/10 hover:bg-orange-500/20 text-orange-400',
    selectedColor: 'border-orange-500 bg-orange-500/25 ring-2 ring-orange-500/30 text-orange-300',
  },
]

export function ModeStep({ value, onChange, suggestedMode }: ModeStepProps) {
  return (
    <div className="space-y-6 animate-in fade-in duration-300">
      <div className="text-center space-y-2">
        <h2 className="text-2xl font-semibold text-foreground">
          Choose your mode
        </h2>
        <p className="text-sm text-muted-foreground">
          How much do you want to take on today?
        </p>
      </div>

      <div className="grid gap-3">
        {modes.map((mode) => {
          const isSelected = value === mode.value
          const isSuggested = suggestedMode === mode.value && !value

          return (
            <button
              key={mode.value}
              onClick={() => onChange(mode.value)}
              className={cn(
                'flex items-center gap-4 rounded-xl border-2 p-4 text-left transition-all duration-200 cursor-pointer',
                isSelected ? mode.selectedColor : mode.color,
                isSuggested && !isSelected && 'ring-1 ring-primary/30'
              )}
            >
              <div className="flex-shrink-0">{mode.icon}</div>
              <div className="flex-1 min-w-0">
                <div className="flex items-center gap-2">
                  <span className="font-semibold text-base">{mode.label}</span>
                  {isSuggested && (
                    <span className="text-[10px] uppercase tracking-wider font-medium px-1.5 py-0.5 rounded-full bg-primary/20 text-primary">
                      Suggested
                    </span>
                  )}
                </div>
                <p className="text-xs text-muted-foreground mt-0.5">
                  {mode.description}
                </p>
              </div>
            </button>
          )
        })}
      </div>
    </div>
  )
}
