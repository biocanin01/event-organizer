import { Navigate, Outlet } from 'react-router'
import { useAuth } from './useAuth'

export function PublicOnlyRoute() {
  const { status } = useAuth()

  if (status === 'authenticated') {
    return <Navigate to="/dashboard" replace />
  }

  return <Outlet />
}
