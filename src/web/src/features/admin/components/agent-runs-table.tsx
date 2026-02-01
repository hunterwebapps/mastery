import { CheckCircle, XCircle, Clock, Cpu, Zap } from 'lucide-react'
import { Badge } from '@/components/ui/badge'
import { cn } from '@/lib/utils'
import type { AgentRunDto } from '@/types'

interface AgentRunsTableProps {
  agentRuns: AgentRunDto[]
}

export function AgentRunsTable({ agentRuns }: AgentRunsTableProps) {
  if (agentRuns.length === 0) {
    return (
      <div className="text-center py-8 text-muted-foreground">
        <Cpu className="size-8 mx-auto mb-2 opacity-50" />
        <p>No LLM calls for this trace</p>
      </div>
    )
  }

  const totalTokens = agentRuns.reduce((sum, run) => sum + run.totalTokens, 0)
  const totalLatency = agentRuns.reduce((sum, run) => sum + run.latencyMs, 0)
  const hasErrors = agentRuns.some((run) => !run.isSuccess)

  return (
    <div className="space-y-4">
      {/* Summary stats */}
      <div className="flex flex-wrap gap-4 text-sm">
        <div className="flex items-center gap-2">
          <Cpu className="size-4 text-muted-foreground" />
          <span className="text-muted-foreground">Calls:</span>
          <span className="font-medium">{agentRuns.length}</span>
        </div>
        <div className="flex items-center gap-2">
          <Zap className="size-4 text-muted-foreground" />
          <span className="text-muted-foreground">Tokens:</span>
          <span className="font-medium">{totalTokens.toLocaleString()}</span>
        </div>
        <div className="flex items-center gap-2">
          <Clock className="size-4 text-muted-foreground" />
          <span className="text-muted-foreground">Total Latency:</span>
          <span className="font-medium">{totalLatency.toLocaleString()}ms</span>
        </div>
        {hasErrors && (
          <Badge variant="destructive" className="text-xs">
            Has Errors
          </Badge>
        )}
      </div>

      {/* Table */}
      <div className="border rounded-lg overflow-hidden">
        <div className="overflow-x-auto">
          <table className="w-full text-sm">
            <thead className="bg-muted/50">
              <tr className="border-b">
                <th className="text-left p-3 font-medium">Status</th>
                <th className="text-left p-3 font-medium">Stage</th>
                <th className="text-left p-3 font-medium">Model</th>
                <th className="text-right p-3 font-medium">Input</th>
                <th className="text-right p-3 font-medium">Output</th>
                <th className="text-right p-3 font-medium">Cached</th>
                <th className="text-right p-3 font-medium">Latency</th>
                <th className="text-left p-3 font-medium">Error</th>
              </tr>
            </thead>
            <tbody>
              {agentRuns.map((run) => (
                <tr key={run.id} className="border-b last:border-0 hover:bg-muted/30">
                  <td className="p-3">
                    {run.isSuccess ? (
                      <CheckCircle className="size-4 text-green-500" />
                    ) : (
                      <XCircle className="size-4 text-red-500" />
                    )}
                  </td>
                  <td className="p-3">
                    <Badge variant="outline" className="text-xs font-normal">
                      {run.stage}
                    </Badge>
                  </td>
                  <td className="p-3">
                    <div className="flex flex-col">
                      <span className="font-mono text-xs">{run.model}</span>
                      {run.provider && (
                        <span className="text-xs text-muted-foreground">{run.provider}</span>
                      )}
                    </div>
                  </td>
                  <td className="p-3 text-right font-mono text-xs">
                    {run.inputTokens.toLocaleString()}
                  </td>
                  <td className="p-3 text-right font-mono text-xs">
                    {run.outputTokens.toLocaleString()}
                  </td>
                  <td className="p-3 text-right font-mono text-xs">
                    {run.cachedInputTokens ? (
                      <span className="text-green-500">
                        {run.cachedInputTokens.toLocaleString()}
                      </span>
                    ) : (
                      <span className="text-muted-foreground">-</span>
                    )}
                  </td>
                  <td className="p-3 text-right font-mono text-xs">
                    <span
                      className={cn(
                        run.latencyMs > 5000
                          ? 'text-red-500'
                          : run.latencyMs > 2000
                          ? 'text-yellow-500'
                          : 'text-green-500'
                      )}
                    >
                      {run.latencyMs.toLocaleString()}ms
                    </span>
                  </td>
                  <td className="p-3 text-xs">
                    {run.errorType ? (
                      <div className="flex flex-col">
                        <span className="text-red-500 font-medium">{run.errorType}</span>
                        {run.errorMessage && (
                          <span className="text-muted-foreground truncate max-w-48" title={run.errorMessage}>
                            {run.errorMessage}
                          </span>
                        )}
                      </div>
                    ) : (
                      <span className="text-muted-foreground">-</span>
                    )}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  )
}
