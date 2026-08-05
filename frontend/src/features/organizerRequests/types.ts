export type OrganizerRoleRequestStatus =
  | 'Pending'
  | 'Approved'
  | 'Rejected'
  | 'Withdrawn'

export interface OrganizerRoleRequest {
  id: string
  userId: string
  motivation: string
  status: OrganizerRoleRequestStatus
  reviewedByAdminUserId: string | null
  decisionReason: string | null
  submittedAtUtc: string
  reviewedAtUtc: string | null
  withdrawnAtUtc: string | null
  version: number
}

export interface SubmitOrganizerRoleRequest {
  motivation: string
}

export interface SubmitOrganizerRoleRequestResponse {
  requestId: string
}

export interface RejectOrganizerRoleRequest {
  decisionReason: string
  version: number
}

export interface VersionedOrganizerRoleRequestDecision {
  version: number
}
