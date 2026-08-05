import type { ApplicationRole } from '../auth/types'

export type UserStatus =
  | 'PendingVerification'
  | 'Active'
  | 'Suspended'
  | 'Deleted'

export interface UserSummary {
  id: string
  fullName: string
  email: string
  status: UserStatus
  createdAtUtc: string
  verifiedAtUtc: string | null
  roles: ApplicationRole[]
}

export interface UserDetails extends UserSummary {
  createdEventCount: number
}

export interface UserListFilters {
  search?: string
  status?: UserStatus | ''
  role?: ApplicationRole | ''
}
