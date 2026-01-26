import type { UserValueDto } from '@/types'
import { ValueItem } from './value-item'

interface ValuesListProps {
  values: UserValueDto[]
}

export function ValuesList({ values }: ValuesListProps) {
  const sortedValues = [...values].sort((a, b) => a.rank - b.rank)

  return (
    <div className="space-y-2">
      {sortedValues.map((value) => (
        <ValueItem key={value.id} value={value} />
      ))}
    </div>
  )
}
