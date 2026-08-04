import { Box, CircularProgress } from '@mui/material'
import { Navigate, Outlet, useLocation } from 'react-router'
import { useAuth } from './useAuth'
import type { ApplicationRole } from './types'

interface RequireAuthProps {
  roles?: ApplicationRole[]
}

export function RequireAuth({ roles }: RequireAuthProps) {
  const { status, session } = useAuth()
  const location = useLocation()

  if (status === 'loading') {
    return (
      <Box sx={{ minHeight: '100vh', display: 'grid', placeItems: 'center' }}>
        <CircularProgress />
      </Box>
    )
  }

  if (status !== 'authenticated' || session === null) {
    return <Navigate to="/login" replace state={{ from: location }} />
  }

  if (
    roles !== undefined &&
    !roles.some((role) => session.user.roles.includes(role))
  ) {
    return <Navigate to="/dashboard" replace />
  }

  return <Outlet />
}
