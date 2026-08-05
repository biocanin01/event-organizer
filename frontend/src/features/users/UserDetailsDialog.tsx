import {
  Alert,
  Button,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  Divider,
  Stack,
  Typography,
} from '@mui/material'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { ApiError } from '../../api/ApiError'
import { StatusChip } from '../../shared/components/StatusChip'
import { formatDateTime } from '../../shared/format/dateTime'
import { useAuthenticatedRequest } from '../auth/useAuthenticatedRequest'
import { useAuth } from '../auth/useAuth'
import { getUserById, reactivateUser, suspendUser } from './usersApi'
import type { UserSummary } from './types'

interface UserDetailsDialogProps {
  user: UserSummary | null
  onClose: () => void
}

function getErrorMessage(error: unknown) {
  return error instanceof ApiError
    ? error.message
    : 'Akcija trenutno nije uspela.'
}

export function UserDetailsDialog({ user, onClose }: UserDetailsDialogProps) {
  const { session } = useAuth()
  const authenticatedRequest = useAuthenticatedRequest()
  const queryClient = useQueryClient()
  const userId = user?.id

  const {
    data: details,
    isLoading,
    error,
  } = useQuery({
    queryKey: ['admin-users', 'details', userId],
    queryFn: () => getUserById(authenticatedRequest, userId ?? ''),
    enabled: Boolean(session?.accessToken && userId),
  })

  const suspendMutation = useMutation({
    mutationFn: () => suspendUser(authenticatedRequest, userId ?? ''),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ['admin-users'] })
    },
  })

  const reactivateMutation = useMutation({
    mutationFn: () => reactivateUser(authenticatedRequest, userId ?? ''),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ['admin-users'] })
    },
  })

  const actionError =
    suspendMutation.error ?? reactivateMutation.error ?? error ?? null

  return (
    <Dialog open={user !== null} onClose={onClose} fullWidth maxWidth="sm">
      <DialogTitle>Detalji korisnika</DialogTitle>
      <DialogContent>
        <Stack spacing={2.5} sx={{ pt: 1 }}>
          {actionError && (
            <Alert severity="error">{getErrorMessage(actionError)}</Alert>
          )}

          {isLoading && (
            <Typography color="text.secondary">Ucitavanje korisnika...</Typography>
          )}

          {details && (
            <>
              <Stack spacing={1}>
                <Typography variant="h6">{details.fullName}</Typography>
                <Typography color="text.secondary">{details.email}</Typography>
                <Stack direction="row" spacing={1} sx={{ flexWrap: 'wrap' }}>
                  <StatusChip status={details.status} />
                  {details.roles.map((role) => (
                    <StatusChip key={role} status={role} />
                  ))}
                </Stack>
              </Stack>

              <Divider />

              <Stack spacing={1}>
                <Typography>
                  Kreiran nalog: {formatDateTime(details.createdAtUtc)}
                </Typography>
                <Typography>
                  Verifikovan: {formatDateTime(details.verifiedAtUtc)}
                </Typography>
                <Typography>
                  Broj kreiranih dogadjaja: {details.createdEventCount}
                </Typography>
              </Stack>
            </>
          )}
        </Stack>
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>Zatvori</Button>
        {details?.status === 'Active' && (
          <Button
            color="warning"
            variant="outlined"
            loading={suspendMutation.isPending}
            onClick={() => suspendMutation.mutate()}
          >
            Suspenduj
          </Button>
        )}
        {details?.status === 'Suspended' && (
          <Button
            color="success"
            variant="contained"
            loading={reactivateMutation.isPending}
            onClick={() => reactivateMutation.mutate()}
          >
            Reaktiviraj
          </Button>
        )}
      </DialogActions>
    </Dialog>
  )
}
