import { cn } from '@/lib/utils'
import { energyLevelInfo } from '@/types/check-in'

interface EveningEnergyStepProps {
  energyLevel?: number
  stressLevel?: number
  onEnergyChange: (level: number) => void
  onStressChange: (level: number) => void
}

const stressLabels: Record<number, string> = {
  1: 'Calm',
  2: 'Mild',
  3: 'Moderate',
  4: 'High',
  5: 'Intense',
}

const stressColors: Record<number, string> = {
  1: 'border-emerald-500/40 bg-emerald-500/10 hover:bg-emerald-500/20 text-emerald-400',
  2: 'border-green-500/40 bg-green-500/10 hover:bg-green-500/20 text-green-400',
  3: 'border-yellow-500/40 bg-yellow-500/10 hover:bg-yellow-500/20 text-yellow-400',
  4: 'border-orange-500/40 bg-orange-500/10 hover:bg-orange-500/20 text-orange-400',
  5: 'border-red-500/40 bg-red-500/10 hover:bg-red-500/20 text-red-400',
}

const stressSelectedColors: Record<number, string> = {
  1: 'border-emerald-500 bg-emerald-500/25 ring-2 ring-emerald-500/30 text-emerald-300',
  2: 'border-green-500 bg-green-500/25 ring-2 ring-green-500/30 text-green-300',
  3: 'border-yellow-500 bg-yellow-500/25 ring-2 ring-yellow-500/30 text-yellow-300',
  4: 'border-orange-500 bg-orange-500/25 ring-2 ring-orange-500/30 text-orange-300',
  5: 'border-red-500 bg-red-500/25 ring-2 ring-red-500/30 text-red-300',
}

const energySelectedColors: Record<number, string> = {
  1: 'border-red-500 bg-red-500/25 ring-2 ring-red-500/30 text-red-300',
  2: 'border-orange-500 bg-orange-500/25 ring-2 ring-orange-500/30 text-orange-300',
  3: 'border-yellow-500 bg-yellow-500/25 ring-2 ring-yellow-500/30 text-yellow-300',
  4: 'border-green-500 bg-green-500/25 ring-2 ring-green-500/30 text-green-300',
  5: 'border-emerald-500 bg-emerald-500/25 ring-2 ring-emerald-500/30 text-emerald-300',
}

const energyColors: Record<number, string> = {
  1: 'border-red-500/40 bg-red-500/10 hover:bg-red-500/20 text-red-400',
  2: 'border-orange-500/40 bg-orange-500/10 hover:bg-orange-500/20 text-orange-400',
  3: 'border-yellow-500/40 bg-yellow-500/10 hover:bg-yellow-500/20 text-yellow-400',
  4: 'border-green-500/40 bg-green-500/10 hover:bg-green-500/20 text-green-400',
  5: 'border-emerald-500/40 bg-emerald-500/10 hover:bg-emerald-500/20 text-emerald-400',
}

export function EveningEnergyStep({
  energyLevel,
  stressLevel,
  onEnergyChange,
  onStressChange,
}: EveningEnergyStepProps) {
  return (
    <div className="space-y-8 animate-in fade-in duration-300">
      <div className="text-center space-y-2">
        <h2 className="text-2xl font-semibold text-foreground">
          End-of-day check
        </h2>
        <p className="text-sm text-muted-foreground">
          How are you feeling now?
        </p>
      </div>

      {/* Energy PM */}
      <div className="space-y-3">
        <p className="text-sm font-medium text-foreground text-center">Energy level</p>
        <div className="grid grid-cols-5 gap-2">
          {[1, 2, 3, 4, 5].map((level) => {
            const info = energyLevelInfo[level]
            const isSelected = energyLevel === level

            return (
              <button
                key={`energy-${level}`}
                onClick={() => onEnergyChange(level)}
                className={cn(
                  'flex flex-col items-center gap-1 rounded-xl border-2 p-3 transition-all duration-200 cursor-pointer',
                  isSelected ? energySelectedColors[level] : energyColors[level]
                )}
              >
                <span className="text-lg">{info.emoji}</span>
                <span className="text-[10px] font-medium">{info.label}</span>
              </button>
            )
          })}
        </div>
      </div>

      {/* Stress */}
      <div className="space-y-3">
        <p className="text-sm font-medium text-foreground text-center">Stress level</p>
        <div className="grid grid-cols-5 gap-2">
          {[1, 2, 3, 4, 5].map((level) => {
            const isSelected = stressLevel === level

            return (
              <button
                key={`stress-${level}`}
                onClick={() => onStressChange(level)}
                className={cn(
                  'flex flex-col items-center gap-1 rounded-xl border-2 p-3 transition-all duration-200 cursor-pointer',
                  isSelected ? stressSelectedColors[level] : stressColors[level]
                )}
              >
                <span className="text-lg">{level}</span>
                <span className="text-[10px] font-medium">{stressLabels[level]}</span>
              </button>
            )
          })}
        </div>
      </div>
    </div>
  )
}
