import { Lightbulb, TrendingUp, HelpCircle } from 'lucide-react'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import type { HypothesisDto } from '@/types'

interface HypothesisCardProps {
  hypothesis: HypothesisDto
}

export function HypothesisCard({ hypothesis }: HypothesisCardProps) {
  return (
    <Card>
      <CardHeader className="pb-4">
        <CardTitle className="text-base font-semibold flex items-center gap-2">
          <Lightbulb className="size-4 text-yellow-400" />
          Hypothesis
        </CardTitle>
      </CardHeader>
      <CardContent className="space-y-3">
        {/* If I... */}
        <div className="relative rounded-lg border border-blue-500/20 bg-blue-500/5 p-4 pl-5">
          <div className="absolute left-0 top-0 bottom-0 w-1 rounded-l-lg bg-blue-500" />
          <div className="flex items-start gap-3">
            <div className="mt-0.5 shrink-0 rounded-md bg-blue-500/10 p-1.5">
              <Lightbulb className="size-3.5 text-blue-400" />
            </div>
            <div className="min-w-0">
              <p className="text-xs font-semibold uppercase tracking-wider text-blue-400 mb-1">
                If I...
              </p>
              <p className="text-sm text-foreground leading-relaxed">
                {hypothesis.change}
              </p>
            </div>
          </div>
        </div>

        {/* Then... */}
        <div className="relative rounded-lg border border-green-500/20 bg-green-500/5 p-4 pl-5">
          <div className="absolute left-0 top-0 bottom-0 w-1 rounded-l-lg bg-green-500" />
          <div className="flex items-start gap-3">
            <div className="mt-0.5 shrink-0 rounded-md bg-green-500/10 p-1.5">
              <TrendingUp className="size-3.5 text-green-400" />
            </div>
            <div className="min-w-0">
              <p className="text-xs font-semibold uppercase tracking-wider text-green-400 mb-1">
                Then...
              </p>
              <p className="text-sm text-foreground leading-relaxed">
                {hypothesis.expectedOutcome}
              </p>
            </div>
          </div>
        </div>

        {/* Because... (optional) */}
        {hypothesis.rationale && (
          <div className="relative rounded-lg border border-amber-500/20 bg-amber-500/5 p-4 pl-5">
            <div className="absolute left-0 top-0 bottom-0 w-1 rounded-l-lg bg-amber-500" />
            <div className="flex items-start gap-3">
              <div className="mt-0.5 shrink-0 rounded-md bg-amber-500/10 p-1.5">
                <HelpCircle className="size-3.5 text-amber-400" />
              </div>
              <div className="min-w-0">
                <p className="text-xs font-semibold uppercase tracking-wider text-amber-400 mb-1">
                  Because...
                </p>
                <p className="text-sm text-foreground leading-relaxed">
                  {hypothesis.rationale}
                </p>
              </div>
            </div>
          </div>
        )}
      </CardContent>
    </Card>
  )
}
