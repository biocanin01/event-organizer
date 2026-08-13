import type { ResourceStatus, ResourceType } from './types'

export const resourceTypeLabels: Record<ResourceType, string> = {
  Venue: 'Sala',
  Speaker: 'Predavač',
  EquipmentPackage: 'Paket opreme',
}

export const resourceStatusLabels: Record<ResourceStatus, string> = {
  Available: 'Dostupan',
  Unavailable: 'Nedostupan',
  Archived: 'Arhiviran',
}

export const resourceTypes: ResourceType[] = [
  'Venue',
  'Speaker',
  'EquipmentPackage',
]

export const resourceStatuses: ResourceStatus[] = [
  'Available',
  'Unavailable',
  'Archived',
]
