import {
  useCallback,
  useEffect,
  useMemo,
  useRef,
  useState,
  type PropsWithChildren,
} from 'react'
import * as authApi from './authApi'
import {
  AuthContext,
  type AuthContextValue,
  type AuthStatus,
} from './authContextValue'
import type { AuthSession, LoginRequest, RegisterRequest } from './types'

export function AuthProvider({ children }: PropsWithChildren) {
  const [status, setStatus] = useState<AuthStatus>('loading')
  const [session, setSession] = useState<AuthSession | null>(null)
  const restoredRef = useRef(false)

  const clearSession = useCallback(() => {
    setSession(null)
    setStatus('anonymous')
  }, [])

  const refresh = useCallback(async () => {
    const refreshedSession = await authApi.refreshSession()
    setSession(refreshedSession)
    setStatus('authenticated')

    return refreshedSession
  }, [])

  const restoreSession = useCallback(async () => {
    try {
      await refresh()
    } catch {
      clearSession()
    }
  }, [clearSession, refresh])

  useEffect(() => {
    if (restoredRef.current) {
      return
    }

    restoredRef.current = true
    void restoreSession()
  }, [restoreSession])

  const login = useCallback(async (request: LoginRequest) => {
    const nextSession = await authApi.login(request)
    setSession(nextSession)
    setStatus('authenticated')
  }, [])

  const register = useCallback(async (request: RegisterRequest) => {
    const nextSession = await authApi.register(request)
    setSession(nextSession)
    setStatus('authenticated')
  }, [])

  const logout = useCallback(async () => {
    try {
      await authApi.logout()
    } finally {
      clearSession()
    }
  }, [clearSession])

  const value = useMemo<AuthContextValue>(
    () => ({
      status,
      session,
      login,
      register,
      logout,
      refresh,
      clearSession,
    }),
    [status, session, login, register, logout, refresh, clearSession],
  )

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}
