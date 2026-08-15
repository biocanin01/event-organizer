import ArrowBackRoundedIcon from '@mui/icons-material/ArrowBackRounded'
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
import { useState } from 'react'
import { Link, useParams } from 'react-router'
import { ApiError } from '../../api/ApiError'
import { StatusChip } from '../../shared/components/StatusChip'
import { formatDateTime } from '../../shared/format/dateTime'
import { getManageableEventById } from '../events/eventsApi'
import { useAuthenticatedRequest } from '../auth/useAuthenticatedRequest'
import {
  confirmRegistration,
  listEventRegistrations,
  rejectRegistration,
} from './registrationsApi'
import type { Registration, RegistrationStatus } from './types'

const statusOptions = [
  { value: 'Pending', label: 'Na čekanju' },
  { value: 'Confirmed', label: 'Potvrđen' },
  { value: 'Rejected', label: 'Odbijen' },
  { value: 'Cancelled', label: 'Otkazan' },
] satisfies ReadonlyArray<{ value: RegistrationStatus; label: string }>

function getErrorMessage(error: unknown) {
  return error instanceof ApiError
    ? error.message
    : 'Akcija trenutno nije uspela.'
}

export function EventRegistrationsPage() {
  const { eventId = '' } = useParams()
  const authenticatedRequest = useAuthenticatedRequest()
  const queryClient = useQueryClient()
  const [status, setStatus] = useState<RegistrationStatus>('Pending')
  const [registrationToReject, setRegistrationToReject] =
    useState<Registration | null>(null)
  const [rejectReason, setRejectReason] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [successMessage, setSuccessMessage] = useState<string | null>(null)

  const eventQuery = useQuery({
    queryKey: ['events', 'manage', eventId],
    queryFn: () => getManageableEventById(authenticatedRequest, eventId),
    enabled: eventId.length > 0,
  })
  const registrationsQuery = useQuery({
    queryKey: ['registrations', eventId, status],
    queryFn: () =>
      listEventRegistrations(authenticatedRequest, eventId, status),
    enabled: eventId.length > 0,
  })

  const refreshData = async () => {
    await queryClient.invalidateQueries({
      queryKey: ['registrations', eventId],
    })
    await queryClient.invalidateQueries({
      queryKey: ['events', 'manage', eventId],
    })
    await queryClient.invalidateQueries({ queryKey: ['events', 'manage'] })
    await queryClient.invalidateQueries({ queryKey: ['events', 'published'] })
  }

  const confirmMutation = useMutation({
    mutationFn: (registration: Registration) =>
      confirmRegistration(authenticatedRequest, registration.id, {
        version: registration.version,
      }),
    onSuccess: async () => {
      setError(null)
      setSuccessMessage('Prijava je potvrđena.')
      await refreshData()
    },
    onError: (mutationError) => {
      setSuccessMessage(null)
      setError(getErrorMessage(mutationError))
    },
  })

  const rejectMutation = useMutation({
    mutationFn: (registration: Registration) =>
      rejectRegistration(authenticatedRequest, registration.id, {
        version: registration.version,
        reason: rejectReason.trim(),
      }),
    onSuccess: async () => {
      setError(null)
      setSuccessMessage('Prijava je odbijena.')
      setRegistrationToReject(null)
      setRejectReason('')
      await refreshData()
    },
    onError: (mutationError) => {
      setSuccessMessage(null)
      setError(getErrorMessage(mutationError))
    },
  })

  const eventError = eventQuery.error
  const registrationsError = registrationsQuery.error
  const availableSpots = eventQuery.data
    ? Math.max(
        0,
        eventQuery.data.capacity -
          (eventQuery.data.confirmedRegistrationCount ?? 0),
      )
    : 0

  return (
    <Stack spacing={3}>
      <Button
        component={Link}
        to="/events"
        startIcon={<ArrowBackRoundedIcon />}
        sx={{ alignSelf: 'flex-start' }}
      >
        Nazad na događaje
      </Button>

      <Stack spacing={0.75}>
        <Typography component="h1" variant="h4">
          Učesnici
        </Typography>
        <Typography color="text.secondary">
          {eventQuery.data
            ? `${eventQuery.data.title} · ${availableSpots} od ${eventQuery.data.capacity} mesta je slobodno`
            : 'Pregled i upravljanje prijavama za događaj.'}
        </Typography>
      </Stack>

      {eventError && <Alert severity="error">{getErrorMessage(eventError)}</Alert>}
      {registrationsError && (
        <Alert severity="error">{getErrorMessage(registrationsError)}</Alert>
      )}
      {error && <Alert severity="error">{error}</Alert>}
      {successMessage && <Alert severity="success">{successMessage}</Alert>}

      <FormControl sx={{ width: { xs: '100%', sm: 240 } }}>
        <InputLabel id="registration-status-filter-label">Status</InputLabel>
        <Select
          labelId="registration-status-filter-label"
          label="Status"
          value={status}
          onChange={(event) =>
            setStatus(event.target.value as RegistrationStatus)
          }
        >
          {statusOptions.map((option) => (
            <MenuItem key={option.value} value={option.value}>
              {option.label}
            </MenuItem>
          ))}
        </Select>
      </FormControl>

      <TableContainer component={Paper} variant="outlined">
        <Table>
          <TableHead>
            <TableRow>
              <TableCell>Učesnik</TableCell>
              <TableCell>Prijavljen</TableCell>
              <TableCell>Status</TableCell>
              <TableCell>Razlog odbijanja</TableCell>
              <TableCell align="right">Akcije</TableCell>
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
                  <TableCell colSpan={5}>
                    Nema prijava za izabrani status.
                  </TableCell>
                </TableRow>
              )}
            {registrationsQuery.data?.map((registration) => (
              <TableRow key={registration.id} hover>
                <TableCell>
                  <Stack spacing={0.25}>
                    <Typography sx={{ fontWeight: 650 }}>
                      {registration.participantFullName}
                    </Typography>
                    <Typography variant="body2" color="text.secondary">
                      {registration.participantEmail}
                    </Typography>
                  </Stack>
                </TableCell>
                <TableCell>{formatDateTime(registration.createdAtUtc)}</TableCell>
                <TableCell>
                  <StatusChip status={registration.status} />
                </TableCell>
                <TableCell>{registration.rejectionReason ?? '-'}</TableCell>
                <TableCell align="right">
                  {registration.status === 'Pending' && (
                    <Stack
                      direction="row"
                      spacing={1}
                      sx={{ justifyContent: 'flex-end' }}
                    >
                      <Button
                        size="small"
                        variant="contained"
                        startIcon={<CheckRoundedIcon />}
                        loading={confirmMutation.isPending}
                        onClick={() => confirmMutation.mutate(registration)}
                      >
                        Potvrdi
                      </Button>
                      <Button
                        size="small"
                        color="error"
                        variant="outlined"
                        startIcon={<CloseRoundedIcon />}
                        onClick={() => setRegistrationToReject(registration)}
                      >
                        Odbij
                      </Button>
                    </Stack>
                  )}
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </TableContainer>

      <Dialog
        open={registrationToReject !== null}
        onClose={() => setRegistrationToReject(null)}
        fullWidth
        maxWidth="sm"
      >
        <DialogTitle>Odbij prijavu</DialogTitle>
        <DialogContent>
          <Box sx={{ pt: 1 }}>
            <TextField
              label="Razlog odbijanja"
              value={rejectReason}
              onChange={(event) => setRejectReason(event.target.value)}
              error={rejectReason.length > 500}
              helperText={`${rejectReason.length}/500`}
              slotProps={{ htmlInput: { maxLength: 500 } }}
              multiline
              minRows={3}
              required
              fullWidth
            />
          </Box>
        </DialogContent>
        <DialogActions>
          <Button
            onClick={() => {
              setRegistrationToReject(null)
              setRejectReason('')
            }}
          >
            Odustani
          </Button>
          <Button
            color="error"
            variant="contained"
            loading={rejectMutation.isPending}
            disabled={rejectReason.trim().length === 0}
            onClick={() => {
              if (registrationToReject) {
                rejectMutation.mutate(registrationToReject)
              }
            }}
          >
            Odbij prijavu
          </Button>
        </DialogActions>
      </Dialog>
    </Stack>
  )
}
