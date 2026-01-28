import { cn } from '@/lib/utils'
import { Check, X, CheckSquare, Target, FolderKanban, Pencil } from 'lucide-react'
import type { Top1Type } from '@/types/check-in'

interface Top1ReviewStepProps {
  value?: boolean
  onChange: (completed: boolean) => void
  top1Description?: string
  top1Type?: Top1Type
}

const typeIcons: Record<Top1Type, React.ReactNode> = {
  Task: <CheckSquare className="size-4 text-blue-400" />,
  Habit: <Target className="size-4 text-green-400" />,
  Project: <FolderKanban className="size-4 text-purple-400" />,
  FreeText: <Pencil className="size-4 text-muted-foreground" />,
}

export function Top1ReviewStep({ value, onChange, top1Description, top1Type }: Top1ReviewStepProps) {
  return (
    <div className="space-y-6 animate-in fade-in duration-300">
      <div className="text-center space-y-3">
        <h2 className="text-2xl font-semibold text-foreground">
          Did you complete your #1?
        </h2>
        {top1Description && (
          <div className="inline-flex items-center gap-2 rounded-lg border border-border/50 bg-muted/30 px-3 py-2">
            {top1Type && typeIcons[top1Type]}
            <span className="text-sm font-medium text-foreground">
              {top1Description}
            </span>
          </div>
        )}
      </div>

      <div className="grid grid-cols-2 gap-4">
        <button
          onClick={() => onChange(true)}
          className={cn(
            'flex flex-col items-center gap-3 rounded-xl border-2 p-6 transition-all duration-200 cursor-pointer',
            value === true
              ? 'border-green-500 bg-green-500/25 ring-2 ring-green-500/30 text-green-300'
              : 'border-green-500/40 bg-green-500/10 hover:bg-green-500/20 text-green-400'
          )}
        >
          <div className="flex size-12 items-center justify-center rounded-full bg-green-500/20">
            <Check className="size-6" />
          </div>
          <span className="text-base font-semibold">Yes!</span>
        </button>

        <button
          onClick={() => onChange(false)}
          className={cn(
            'flex flex-col items-center gap-3 rounded-xl border-2 p-6 transition-all duration-200 cursor-pointer',
            value === false
              ? 'border-orange-500 bg-orange-500/25 ring-2 ring-orange-500/30 text-orange-300'
              : 'border-orange-500/40 bg-orange-500/10 hover:bg-orange-500/20 text-orange-400'
          )}
        >
          <div className="flex size-12 items-center justify-center rounded-full bg-orange-500/20">
            <X className="size-6" />
          </div>
          <span className="text-base font-semibold">Not yet</span>
        </button>
      </div>
    </div>
  )
}
