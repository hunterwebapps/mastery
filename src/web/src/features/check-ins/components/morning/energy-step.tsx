import { cn } from '@/lib/utils'
import { energyLevelInfo } from '@/types/check-in'

interface EnergyStepProps {
  value?: number
  onChange: (level: number) => void
}

const energyColors: Record<number, string> = {
  1: 'border-red-500/40 bg-red-500/10 hover:bg-red-500/20 text-red-400',
  2: 'border-orange-500/40 bg-orange-500/10 hover:bg-orange-500/20 text-orange-400',
  3: 'border-yellow-500/40 bg-yellow-500/10 hover:bg-yellow-500/20 text-yellow-400',
  4: 'border-green-500/40 bg-green-500/10 hover:bg-green-500/20 text-green-400',
  5: 'border-emerald-500/40 bg-emerald-500/10 hover:bg-emerald-500/20 text-emerald-400',
}

const energySelectedColors: Record<number, string> = {
  1: 'border-red-500 bg-red-500/25 ring-2 ring-red-500/30 text-red-300',
  2: 'border-orange-500 bg-orange-500/25 ring-2 ring-orange-500/30 text-orange-300',
  3: 'border-yellow-500 bg-yellow-500/25 ring-2 ring-yellow-500/30 text-yellow-300',
  4: 'border-green-500 bg-green-500/25 ring-2 ring-green-500/30 text-green-300',
  5: 'border-emerald-500 bg-emerald-500/25 ring-2 ring-emerald-500/30 text-emerald-300',
}

export function EnergyStep({ value, onChange }: EnergyStepProps) {
  return (
    <div className="space-y-6 animate-in fade-in duration-300">
      <div className="text-center space-y-2">
        <h2 className="text-2xl font-semibold text-foreground">
          How's your energy?
        </h2>
        <p className="text-sm text-muted-foreground">
          Tap the level that best describes how you feel right now
        </p>
      </div>

      <div className="grid grid-cols-5 gap-3">
        {[1, 2, 3, 4, 5].map((level) => {
          const info = energyLevelInfo[level]
          const isSelected = value === level

          return (
            <button
              key={level}
              onClick={() => onChange(level)}
              className={cn(
                'flex flex-col items-center gap-2 rounded-xl border-2 p-4 transition-all duration-200 cursor-pointer',
                isSelected
                  ? energySelectedColors[level]
                  : energyColors[level]
              )}
            >
              <span className="text-2xl sm:text-3xl">{info.emoji}</span>
              <span className="text-xs sm:text-sm font-medium">{info.label}</span>
            </button>
          )
        })}
      </div>
    </div>
  )
}
