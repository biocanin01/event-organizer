import CancelRoundedIcon from '@mui/icons-material/CancelRounded'
import {
  Alert,
  Button,
  Paper,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Typography,
} from '@mui/material'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { ApiError } from '../../api/ApiError'
import { StatusChip } from '../../shared/components/StatusChip'
import { formatDateTime } from '../../shared/format/dateTime'
import { useAuthenticatedRequest } from '../auth/useAuthenticatedRequest'
import { cancelRegistration, listMyRegistrations } from './registrationsApi'
import type { Registration } from './types'

function getErrorMessage(error: unknown) {
  return error instanceof ApiError
    ? error.message
    : 'Akcija trenutno nije uspela.'
}

function canCancel(registration: Registration) {
  return (
    (registration.status === 'Pending' ||
      registration.status === 'Confirmed') &&
    new Date(registration.eventStartsAtUtc) > new Date()
  )
}

export function MyRegistrationsPage() {
  const authenticatedRequest = useAuthenticatedRequest()
  const queryClient = useQueryClient()
  const registrationsQuery = useQuery({
    queryKey: ['registrations', 'me'],
    queryFn: () => listMyRegistrations(authenticatedRequest),
  })
  const cancelMutation = useMutation({
    mutationFn: (registration: Registration) =>
      cancelRegistration(authenticatedRequest, registration.id, {
        version: registration.version,
      }),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ['registrations', 'me'] })
      await queryClient.invalidateQueries({ queryKey: ['events', 'published'] })
    },
  })

  return (
    <Stack spacing={3}>
      <Stack spacing={0.75}>
        <Typography component="h1" variant="h4">
          Moje prijave
        </Typography>
        <Typography color="text.secondary">
          Pregled statusa prijava za događaje na kojima učestvujete.
        </Typography>
      </Stack>

      {registrationsQuery.error && (
        <Alert severity="error">
          {getErrorMessage(registrationsQuery.error)}
        </Alert>
      )}
      {cancelMutation.error && (
        <Alert severity="error">{getErrorMessage(cancelMutation.error)}</Alert>
      )}

      <TableContainer component={Paper} variant="outlined">
        <Table>
          <TableHead>
            <TableRow>
              <TableCell>Događaj</TableCell>
              <TableCell>Termin</TableCell>
              <TableCell>Status</TableCell>
              <TableCell>Napomena</TableCell>
              <TableCell align="right">Akcija</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {registrationsQuery.isLoading && (
              <TableRow>
                <TableCell colSpan={5}>Učitavanje prijava...</TableCell>
              </TableRow>
            )}
            {!registrationsQuery.isLoading &&
              (registrationsQuery.data?.length ?? 0) === 0 && (
                <TableRow>
                  <TableCell colSpan={5}>Još nemate prijave za događaje.</TableCell>
                </TableRow>
              )}
            {registrationsQuery.data?.map((registration) => (
              <TableRow key={registration.id} hover>
                <TableCell>
                  <Typography sx={{ fontWeight: 650 }}>
                    {registration.eventTitle}
                  </Typography>
                </TableCell>
                <TableCell>
                  <Stack spacing={0.25}>
                    <Typography variant="body2">
                      {formatDateTime(registration.eventStartsAtUtc)}
                    </Typography>
                    <Typography variant="body2" color="text.secondary">
                      do {formatDateTime(registration.eventEndsAtUtc)}
                    </Typography>
                  </Stack>
                </TableCell>
                <TableCell>
                  <StatusChip status={registration.status} />
                </TableCell>
                <TableCell>
                  {registration.rejectionReason ?? '-'}
                </TableCell>
                <TableCell align="right">
                  {canCancel(registration) && (
                    <Button
                      size="small"
                      color="error"
                      startIcon={<CancelRoundedIcon />}
                      loading={cancelMutation.isPending}
                      onClick={() => cancelMutation.mutate(registration)}
                    >
                      Otkaži prijavu
                    </Button>
                  )}
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </TableContainer>
    </Stack>
  )
}
