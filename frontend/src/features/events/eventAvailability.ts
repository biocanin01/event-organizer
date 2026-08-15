import type { EventItem } from './types'

export function getAvailableSpots(eventItem: EventItem) {
  return Math.max(
    0,
    eventItem.capacity - (eventItem.confirmedRegistrationCount ?? 0),
  )
}
