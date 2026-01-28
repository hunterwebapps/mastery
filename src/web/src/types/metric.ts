// Metric Types

export type MetricDataType = 'Number' | 'Boolean' | 'Duration' | 'Percentage' | 'Count' | 'Rating'
export type MetricDirection = 'Increase' | 'Decrease' | 'Maintain'

export interface MetricUnitDto {
  type: string
  label: string
}

export interface MetricDefinitionDto {
  id: string
  userId: string
  name: string
  description?: string
  dataType: MetricDataType
  unit?: MetricUnitDto
  direction: MetricDirection
  defaultCadence: string
  defaultAggregation: string
  isArchived: boolean
  tags: string[]
  createdAt: string
  modifiedAt?: string
}

export interface MetricDefinitionSummaryDto {
  id: string
  name: string
  description?: string
  dataType: MetricDataType
  direction: MetricDirection
  unit?: MetricUnitDto
}

export interface MetricObservationDto {
  id: string
  metricDefinitionId: string
  userId: string
  observedAt: string
  observedOn: string
  value: number
  source: string
  correlationId?: string
  note?: string
  createdAt: string
  isCorrected: boolean
}

export interface MetricDataPointDto {
  date: string
  value: number
  note?: string
}

export interface MetricTimeSeriesDto {
  metricDefinitionId: string
  metricName: string
  startDate: string
  endDate: string
  dataPoints: MetricDataPointDto[]
  aggregatedValue?: number
  count: number
}

// Request types
export interface CreateMetricUnitRequest {
  type: string
  label: string
}

export interface CreateMetricDefinitionRequest {
  name: string
  description?: string
  dataType?: MetricDataType
  unit?: CreateMetricUnitRequest
  direction?: MetricDirection
  defaultCadence?: string
  defaultAggregation?: string
  tags?: string[]
}

export interface UpdateMetricDefinitionRequest {
  name: string
  description?: string
  dataType?: MetricDataType
  unit?: CreateMetricUnitRequest
  direction?: MetricDirection
  defaultCadence?: string
  defaultAggregation?: string
  isArchived?: boolean
  tags?: string[]
}

export interface RecordObservationRequest {
  value: number
  observedOn?: string
  source?: string
  correlationId?: string
  note?: string
}

// UI helpers
export const metricDataTypeInfo: Record<
  MetricDataType,
  { label: string; description: string; icon: string }
> = {
  Number: {
    label: 'Number',
    description: 'Any numeric value (e.g., weight, revenue)',
    icon: '#',
  },
  Boolean: {
    label: 'Yes/No',
    description: 'Binary outcome (e.g., did workout)',
    icon: '✓',
  },
  Duration: {
    label: 'Duration',
    description: 'Time-based metric (e.g., deep work minutes)',
    icon: '⏱',
  },
  Percentage: {
    label: 'Percentage',
    description: 'Ratio or percentage (e.g., adherence rate)',
    icon: '%',
  },
  Count: {
    label: 'Count',
    description: 'Countable items (e.g., gym sessions)',
    icon: '№',
  },
  Rating: {
    label: 'Rating',
    description: 'Subjective scale (e.g., energy 1-5)',
    icon: '★',
  },
}

export const metricDirectionInfo: Record<
  MetricDirection,
  { label: string; description: string; icon: string }
> = {
  Increase: {
    label: 'Increase',
    description: 'Higher is better',
    icon: '↑',
  },
  Decrease: {
    label: 'Decrease',
    description: 'Lower is better',
    icon: '↓',
  },
  Maintain: {
    label: 'Maintain',
    description: 'Stay within range',
    icon: '↔',
  },
}
