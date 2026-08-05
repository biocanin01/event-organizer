import CheckRoundedIcon from '@mui/icons-material/CheckRounded'
import CloseRoundedIcon from '@mui/icons-material/CloseRounded'
import {
  Alert,
  Box,
  Button,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  FormControl,
  InputLabel,
  MenuItem,
  Paper,
  Select,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  TextField,
  Typography,
} from '@mui/material'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useMemo, useState } from 'react'
import { ApiError } from '../../api/ApiError'
import { StatusChip } from '../../shared/components/StatusChip'
import { formatDateTime } from '../../shared/format/dateTime'
import { useAuth } from '../auth/useAuth'
import { listUsers } from '../users/usersApi'
import {
  approveOrganizerRoleRequest,
  listOrganizerRoleRequests,
  rejectOrganizerRoleRequest,
} from './organizerRequestApi'
import type {
  OrganizerRoleRequest,
  OrganizerRoleRequestStatus,
} from './types'

const statuses: OrganizerRoleRequestStatus[] = [
  'Pending',
  'Approved',
  'Rejected',
  'Withdrawn',
]

function getErrorMessage(error: unknown) {
  return error instanceof ApiError
    ? error.message
    : 'Akcija trenutno nije uspela.'
}

export function AdminOrganizerRequestsPage() {
  const { session } = useAuth()
  const queryClient = useQueryClient()
  const accessToken = session?.accessToken ?? ''
  const [status, setStatus] = useState<OrganizerRoleRequestStatus>('Pending')
  const [error, setError] = useState<string | null>(null)
  const [requestToReject, setRequestToReject] =
    useState<OrganizerRoleRequest | null>(null)
  const [decisionReason, setDecisionReason] = useState('')

  const requestsQueryKey = ['organizer-role-requests', 'admin', status]

  const { data: requests = [], isLoading } = useQuery({
    queryKey: requestsQueryKey,
    queryFn: () => listOrganizerRoleRequests(accessToken, status),
    enabled: Boolean(accessToken),
  })

  const { data: users = [] } = useQuery({
    queryKey: ['admin-users', 'lookup'],
    queryFn: () => listUsers(accessToken),
    enabled: Boolean(accessToken),
  })

  const usersById = useMemo(
    () => new Map(users.map((user) => [user.id, user])),
    [users],
  )

  const approveMutation = useMutation({
    mutationFn: (request: OrganizerRoleRequest) =>
      approveOrganizerRoleRequest(accessToken, request.id, {
        version: request.version,
      }),
    onSuccess: async () => {
      setError(null)
      await queryClient.invalidateQueries({
        queryKey: ['organizer-role-requests', 'admin'],
      })
      await queryClient.invalidateQueries({ queryKey: ['admin-users'] })
    },
    onError: (mutationError) => setError(getErrorMessage(mutationError)),
  })

  const rejectMutation = useMutation({
    mutationFn: (request: OrganizerRoleRequest) =>
      rejectOrganizerRoleRequest(accessToken, request.id, {
        decisionReason,
        version: request.version,
      }),
    onSuccess: async () => {
      setError(null)
      setRequestToReject(null)
      setDecisionReason('')
      await queryClient.invalidateQueries({
        queryKey: ['organizer-role-requests', 'admin'],
      })
    },
    onError: (mutationError) => setError(getErrorMessage(mutationError)),
  })

  return (
    <Stack spacing={3}>
      <Stack
        direction={{ xs: 'column', md: 'row' }}
        spacing={2}
        sx={{ justifyContent: 'space-between' }}
      >
        <Stack spacing={0.75}>
          <Typography component="h1" variant="h4">
            Zahtevi za organizatore
          </Typography>
          <Typography color="text.secondary">
            Pregled i odlucivanje o zahtevima za Organizer rolu.
          </Typography>
        </Stack>
        <FormControl sx={{ minWidth: 220 }}>
          <InputLabel id="organizer-request-status-label">Status</InputLabel>
          <Select
            labelId="organizer-request-status-label"
            label="Status"
            value={status}
            onChange={(event) =>
              setStatus(event.target.value as OrganizerRoleRequestStatus)
            }
          >
            {statuses.map((statusOption) => (
              <MenuItem key={statusOption} value={statusOption}>
                {statusOption}
              </MenuItem>
            ))}
          </Select>
        </FormControl>
      </Stack>

      {error && <Alert severity="error">{error}</Alert>}

      <TableContainer component={Paper} variant="outlined">
        <Table>
          <TableHead>
            <TableRow>
              <TableCell>Korisnik</TableCell>
              <TableCell>Motivacija</TableCell>
              <TableCell>Status</TableCell>
              <TableCell>Poslato</TableCell>
              <TableCell align="right">Akcije</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {isLoading && (
              <TableRow>
                <TableCell colSpan={5}>Ucitavanje zahteva...</TableCell>
              </TableRow>
            )}
            {!isLoading && requests.length === 0 && (
              <TableRow>
                <TableCell colSpan={5}>
                  Nema zahteva za izabrani status.
                </TableCell>
              </TableRow>
            )}
            {requests.map((request) => {
              const user = usersById.get(request.userId)

              return (
                <TableRow key={request.id} hover>
                  <TableCell>
                    <Stack spacing={0.25}>
                      <Typography sx={{ fontWeight: 650 }}>
                        {user?.fullName ?? request.userId}
                      </Typography>
                      <Typography variant="body2" color="text.secondary">
                        {user?.email ?? 'Korisnik nije ucitan'}
                      </Typography>
                    </Stack>
                  </TableCell>
                  <TableCell sx={{ maxWidth: 420 }}>{request.motivation}</TableCell>
                  <TableCell>
                    <StatusChip status={request.status} />
                  </TableCell>
                  <TableCell>{formatDateTime(request.submittedAtUtc)}</TableCell>
                  <TableCell align="right">
                    {request.status === 'Pending' ? (
                      <Stack
                        direction="row"
                        spacing={1}
                        sx={{ justifyContent: 'flex-end' }}
                      >
                        <Button
                          size="small"
                          variant="contained"
                          startIcon={<CheckRoundedIcon />}
                          loading={approveMutation.isPending}
                          onClick={() => approveMutation.mutate(request)}
                        >
                          Odobri
                        </Button>
                        <Button
                          size="small"
                          variant="outlined"
                          color="error"
                          startIcon={<CloseRoundedIcon />}
                          onClick={() => setRequestToReject(request)}
                        >
                          Odbij
                        </Button>
                      </Stack>
                    ) : (
                      <Typography variant="body2" color="text.secondary">
                        {request.decisionReason ?? '-'}
                      </Typography>
                    )}
                  </TableCell>
                </TableRow>
              )
            })}
          </TableBody>
        </Table>
      </TableContainer>

      <Dialog
        open={requestToReject !== null}
        onClose={() => setRequestToReject(null)}
        fullWidth
        maxWidth="sm"
      >
        <DialogTitle>Odbij zahtev</DialogTitle>
        <DialogContent>
          <Box sx={{ pt: 1 }}>
            <TextField
              label="Razlog odbijanja"
              value={decisionReason}
              onChange={(event) => setDecisionReason(event.target.value)}
              multiline
              minRows={3}
              fullWidth
            />
          </Box>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setRequestToReject(null)}>Odustani</Button>
          <Button
            color="error"
            variant="contained"
            disabled={decisionReason.trim().length === 0}
            loading={rejectMutation.isPending}
            onClick={() => {
              if (requestToReject) {
                rejectMutation.mutate(requestToReject)
              }
            }}
          >
            Odbij zahtev
          </Button>
        </DialogActions>
      </Dialog>
    </Stack>
  )
}
