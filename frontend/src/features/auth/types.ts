export const applicationRoles = {
  participant: 'Participant',
  organizer: 'Organizer',
  admin: 'Admin',
} as const

export type ApplicationRole =
  (typeof applicationRoles)[keyof typeof applicationRoles]

export interface AuthUser {
  userId: string
  fullName: string
  email: string
  roles: ApplicationRole[]
}

export interface AuthSession {
  user: AuthUser
  accessToken: string
  accessTokenExpiresAtUtc: string
}

export interface AuthResponse {
  userId: string
  fullName: string
  email: string
  roles: ApplicationRole[]
  accessToken: string
  accessTokenExpiresAtUtc: string
}

export interface LoginRequest {
  email: string
  password: string
}

export interface RegisterRequest {
  fullName: string
  email: string
  password: string
}
