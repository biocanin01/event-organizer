export type ResourceType = 'Venue' | 'Speaker' | 'EquipmentPackage'

export type ResourceStatus = 'Available' | 'Unavailable' | 'Archived'

export interface ResourceItem {
  id: string
  name: string
  description: string
  type: ResourceType
  status: ResourceStatus
  cost: number
  qualityScore: number
  version: number
  capacity: number | null
  expertiseArea: string | null
  providerName: string | null
  supportedCapacity: number | null
  serviceArea: string | null
  includesTechnicalSupport: boolean | null
  contentsSummary: string | null
  createdAtUtc: string
  updatedAtUtc: string | null
}

export interface ResourceFormValues {
  name: string
  description: string
  type: ResourceType
  cost: number
  qualityScore: number
  capacity: number | null
  expertiseArea: string | null
  providerName: string | null
  supportedCapacity: number | null
  serviceArea: string | null
  includesTechnicalSupport: boolean | null
  contentsSummary: string | null
}

export type CreateResourceRequest = ResourceFormValues

export type UpdateResourceRequest = ResourceFormValues
