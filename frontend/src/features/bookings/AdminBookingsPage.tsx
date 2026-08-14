import CheckRoundedIcon from '@mui/icons-material/CheckRounded'
import CloseRoundedIcon from '@mui/icons-material/CloseRounded'
import HourglassBottomRoundedIcon from '@mui/icons-material/HourglassBottomRounded'
import VisibilityRoundedIcon from '@mui/icons-material/VisibilityRounded'
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
import { ApiError } from '../../api/ApiError'
import { StatusChip } from '../../shared/components/StatusChip'
import { formatDateTime } from '../../shared/format/dateTime'
import { formatMoney } from '../../shared/format/money'
import { useAuthenticatedRequest } from '../auth/useAuthenticatedRequest'
import {
  approveEventBooking,
  expireEventBookings,
  getAdminBookingById,
  listAdminBookings,
  rejectEventBooking,
} from './bookingsApi'
import type {
  EventBookingResource,
  EventResourceBooking,
  EventResourceBookingStatus,
} from './types'

const adminBookingStatusOptions = [
  { value: 'Submitted', label: 'Podnet' },
  { value: 'Approved', label: 'Odobren' },
  { value: 'Rejected', label: 'Odbijen' },
  { value: 'Expired', label: 'Istekao' },
] satisfies ReadonlyArray<{
  value: EventResourceBookingStatus
  label: string
}>

function getErrorMessage(error: unknown) {
  return error instanceof ApiError
    ? error.message
    : 'Akcija trenutno nije uspela.'
}

function ResourceLine({
  label,
  resource,
}: {
  label: string
  resource: EventBookingResource | null
}) {
  return (
    <Stack spacing={0.35}>
      <Typography variant="body2" color="text.secondary">
        {label}
      </Typography>
      <Typography sx={{ fontWeight: 650 }}>
        {resource ? resource.name : 'Nije izabrano'}
      </Typography>
      {resource && (
        <Typography variant="body2" color="text.secondary">
          {formatMoney(resource.cost)} · kvalitet {resource.qualityScore}
        </Typography>
      )}
    </Stack>
  )
}

function BookingDetailsDialog({
  booking,
  error,
  isLoading,
  open,
  onClose,
}: {
  booking: EventResourceBooking | null
  error: unknown
  isLoading: boolean
  open: boolean
  onClose: () => void
}) {
  return (
    <Dialog open={open} onClose={onClose} fullWidth maxWidth="md">
      <DialogTitle>Detalji booking zahteva</DialogTitle>
      <DialogContent>
        {isLoading && (
          <Typography color="text.secondary" sx={{ pt: 1 }}>
            Učitavanje detalja...
          </Typography>
        )}
        {error !== null && error !== undefined && (
          <Alert severity="error" sx={{ mt: 1 }}>
            {getErrorMessage(error)}
          </Alert>
        )}
        {booking && (
          <Stack spacing={2.5} sx={{ pt: 1 }}>
            <Stack direction="row" spacing={1.5} sx={{ alignItems: 'center' }}>
              <StatusChip status={booking.status} />
              <Typography color="text.secondary">
                Verzija {booking.version} · ukupno {formatMoney(booking.totalCost)}
              </Typography>
            </Stack>
            <Box
              sx={{
                display: 'grid',
                gap: 2,
                gridTemplateColumns: { xs: '1fr', md: 'repeat(3, 1fr)' },
              }}
            >
              <ResourceLine label="Sala" resource={booking.venue} />
              <Stack spacing={0.35}>
                <Typography variant="body2" color="text.secondary">
                  Predavači
                </Typography>
                <Typography sx={{ fontWeight: 650 }}>
                  {booking.speakers.length > 0
                    ? booking.speakers.map((speaker) => speaker.name).join(', ')
                    : 'Nije izabrano'}
                </Typography>
                <Typography variant="body2" color="text.secondary">
                  {booking.speakers.length} izabrano
                </Typography>
              </Stack>
              <ResourceLine
                label="Paket opreme"
                resource={booking.equipmentPackage}
              />
            </Box>
            <Box
              sx={{
                display: 'grid',
                gap: 2,
                gridTemplateColumns: { xs: '1fr', md: 'repeat(3, 1fr)' },
              }}
            >
              <Stack spacing={0.35}>
                <Typography variant="body2" color="text.secondary">
                  Poslato
                </Typography>
                <Typography>{formatDateTime(booking.submittedAtUtc)}</Typography>
              </Stack>
              <Stack spacing={0.35}>
                <Typography variant="body2" color="text.secondary">
                  Hold ističe
                </Typography>
                <Typography>{formatDateTime(booking.holdExpiresAtUtc)}</Typography>
              </Stack>
              <Stack spacing={0.35}>
                <Typography variant="body2" color="text.secondary">
                  Odluka
                </Typography>
                <Typography>{formatDateTime(booking.decidedAtUtc)}</Typography>
              </Stack>
            </Box>
            {booking.decisionReason && (
              <Alert severity="info">
                Razlog odluke: {booking.decisionReason}
              </Alert>
            )}
          </Stack>
        )}
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>Zatvori</Button>
      </DialogActions>
    </Dialog>
  )
}

export function AdminBookingsPage() {
  const authenticatedRequest = useAuthenticatedRequest()
  const queryClient = useQueryClient()
  const [status, setStatus] =
    useState<EventResourceBookingStatus>('Submitted')
  const [error, setError] = useState<string | null>(null)
  const [successMessage, setSuccessMessage] = useState<string | null>(null)
  const [bookingToReject, setBookingToReject] =
    useState<EventResourceBooking | null>(null)
  const [rejectReason, setRejectReason] = useState('')
  const [detailsOpen, setDetailsOpen] = useState(false)
  const [selectedBookingId, setSelectedBookingId] = useState<string | null>(null)

  const bookingsQueryKey = ['admin-bookings', status]

  const {
    data: bookings = [],
    error: bookingsError,
    isLoading,
  } = useQuery({
    queryKey: bookingsQueryKey,
    queryFn: () => listAdminBookings(authenticatedRequest, status),
  })

  const {
    data: selectedBooking = null,
    error: selectedBookingError,
    isLoading: isBookingDetailsLoading,
  } = useQuery({
    queryKey: ['admin-booking', selectedBookingId],
    queryFn: () =>
      getAdminBookingById(authenticatedRequest, selectedBookingId ?? ''),
    enabled: selectedBookingId !== null,
  })

  const refreshBookings = async () => {
    await queryClient.invalidateQueries({ queryKey: ['admin-bookings'] })
    if (selectedBookingId) {
      await queryClient.invalidateQueries({
        queryKey: ['admin-booking', selectedBookingId],
      })
    }
  }

  const handleError = (mutationError: unknown) => {
    setSuccessMessage(null)
    setError(getErrorMessage(mutationError))
  }

  const approveMutation = useMutation({
    mutationFn: (booking: EventResourceBooking) =>
      approveEventBooking(authenticatedRequest, booking.id, {
        version: booking.version,
      }),
    onSuccess: async () => {
      setError(null)
      setSuccessMessage('Booking je odobren.')
      await refreshBookings()
    },
    onError: handleError,
  })

  const rejectMutation = useMutation({
    mutationFn: (booking: EventResourceBooking) =>
      rejectEventBooking(authenticatedRequest, booking.id, {
        version: booking.version,
        reason: rejectReason.trim() || null,
      }),
    onSuccess: async () => {
      setError(null)
      setSuccessMessage('Booking je odbijen.')
      setBookingToReject(null)
      setRejectReason('')
      await refreshBookings()
    },
    onError: handleError,
  })

  const expireMutation = useMutation({
    mutationFn: () => expireEventBookings(authenticatedRequest),
    onSuccess: async (response) => {
      setError(null)
      setSuccessMessage(`Isteklo booking zahteva: ${response.expiredCount}.`)
      await refreshBookings()
    },
    onError: handleError,
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
            Booking zahtevi
          </Typography>
          <Typography color="text.secondary">
            Pregled, odobravanje i odbijanje podnetih booking zahteva.
          </Typography>
        </Stack>
        <Button
          variant="outlined"
          startIcon={<HourglassBottomRoundedIcon />}
          loading={expireMutation.isPending}
          onClick={() => expireMutation.mutate()}
        >
          Označi istekle
        </Button>
      </Stack>

      {error && <Alert severity="error">{error}</Alert>}
      {bookingsError && (
        <Alert severity="error">{getErrorMessage(bookingsError)}</Alert>
      )}
      {successMessage && <Alert severity="success">{successMessage}</Alert>}

      <FormControl sx={{ maxWidth: 260 }}>
        <InputLabel id="admin-booking-status-label">Status</InputLabel>
        <Select
          labelId="admin-booking-status-label"
          label="Status"
          value={status}
          onChange={(event) =>
            setStatus(event.target.value as EventResourceBookingStatus)
          }
        >
          {adminBookingStatusOptions.map((statusOption) => (
            <MenuItem key={statusOption.value} value={statusOption.value}>
              {statusOption.label}
            </MenuItem>
          ))}
        </Select>
      </FormControl>

      <TableContainer component={Paper} variant="outlined">
        <Table>
          <TableHead>
            <TableRow>
              <TableCell>Booking</TableCell>
              <TableCell>Resursi</TableCell>
              <TableCell>Ukupno</TableCell>
              <TableCell>Hold ističe</TableCell>
              <TableCell>Status</TableCell>
              <TableCell align="right">Akcije</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {isLoading && (
              <TableRow>
                <TableCell colSpan={6}>Učitavanje booking zahteva...</TableCell>
              </TableRow>
            )}
            {!isLoading && bookings.length === 0 && (
              <TableRow>
                <TableCell colSpan={6}>
                  Nema booking zahteva za izabrani status.
                </TableCell>
              </TableRow>
            )}
            {bookings.map((booking) => (
              <TableRow key={booking.id} hover>
                <TableCell>
                  <Stack spacing={0.35}>
                    <Typography sx={{ fontWeight: 650 }}>
                      {booking.id}
                    </Typography>
                    <Typography variant="body2" color="text.secondary">
                      Event: {booking.eventId} · verzija {booking.version}
                    </Typography>
                  </Stack>
                </TableCell>
                <TableCell>
                  <Stack spacing={0.35}>
                    <Typography variant="body2">
                      Sala: {booking.venue?.name ?? '-'}
                    </Typography>
                    <Typography variant="body2" color="text.secondary">
                      Predavači: {booking.speakers.length}
                      {booking.equipmentPackage
                        ? ` · oprema: ${booking.equipmentPackage.name}`
                        : ''}
                    </Typography>
                  </Stack>
                </TableCell>
                <TableCell>{formatMoney(booking.totalCost)}</TableCell>
                <TableCell>{formatDateTime(booking.holdExpiresAtUtc)}</TableCell>
                <TableCell>
                  <StatusChip status={booking.status} />
                </TableCell>
                <TableCell align="right">
                  <Stack
                    direction="row"
                    spacing={1}
                    sx={{ justifyContent: 'flex-end', flexWrap: 'wrap' }}
                  >
                    <Button
                      size="small"
                      startIcon={<VisibilityRoundedIcon />}
                      onClick={() => {
                        setSelectedBookingId(booking.id)
                        setDetailsOpen(true)
                      }}
                    >
                      Detalji
                    </Button>
                    {booking.status === 'Submitted' && (
                      <>
                        <Button
                          size="small"
                          variant="contained"
                          startIcon={<CheckRoundedIcon />}
                          loading={approveMutation.isPending}
                          onClick={() => approveMutation.mutate(booking)}
                        >
                          Odobri
                        </Button>
                        <Button
                          size="small"
                          color="error"
                          variant="outlined"
                          startIcon={<CloseRoundedIcon />}
                          onClick={() => setBookingToReject(booking)}
                        >
                          Odbij
                        </Button>
                      </>
                    )}
                  </Stack>
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </TableContainer>

      <BookingDetailsDialog
        booking={selectedBooking}
        error={selectedBookingError}
        isLoading={isBookingDetailsLoading}
        open={detailsOpen}
        onClose={() => {
          setDetailsOpen(false)
          setSelectedBookingId(null)
        }}
      />

      <Dialog
        open={bookingToReject !== null}
        onClose={() => setBookingToReject(null)}
        fullWidth
        maxWidth="sm"
      >
        <DialogTitle>Odbij booking zahtev</DialogTitle>
        <DialogContent>
          <Box sx={{ pt: 1 }}>
            <TextField
              label="Razlog odbijanja"
              value={rejectReason}
              onChange={(event) => setRejectReason(event.target.value)}
              multiline
              minRows={3}
              fullWidth
            />
          </Box>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setBookingToReject(null)}>Odustani</Button>
          <Button
            color="error"
            variant="contained"
            loading={rejectMutation.isPending}
            onClick={() => {
              if (bookingToReject) {
                rejectMutation.mutate(bookingToReject)
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
