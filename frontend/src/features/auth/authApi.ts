import { apiRequest } from '../../api/apiClient'
import type {
  AuthResponse,
  AuthSession,
  LoginRequest,
  RegisterRequest,
} from './types'

function toAuthSession(response: AuthResponse): AuthSession {
  return {
    user: {
      userId: response.userId,
      fullName: response.fullName,
      email: response.email,
      roles: response.roles,
    },
    accessToken: response.accessToken,
    accessTokenExpiresAtUtc: response.accessTokenExpiresAtUtc,
  }
}

export async function login(request: LoginRequest): Promise<AuthSession> {
  const response = await apiRequest<AuthResponse>('/auth/login', {
    method: 'POST',
    body: JSON.stringify(request),
  })

  return toAuthSession(response)
}

export async function register(request: RegisterRequest): Promise<AuthSession> {
  const response = await apiRequest<AuthResponse>('/auth/register', {
    method: 'POST',
    body: JSON.stringify(request),
  })

  return toAuthSession(response)
}

export async function refreshSession(): Promise<AuthSession> {
  const response = await apiRequest<AuthResponse>('/auth/refresh', {
    method: 'POST',
  })

  return toAuthSession(response)
}

export async function logout(): Promise<void> {
  await apiRequest<void>('/auth/logout', {
    method: 'POST',
  })
}
