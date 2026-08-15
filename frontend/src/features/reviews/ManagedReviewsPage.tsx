import {
  Alert,
  FormControl,
  InputLabel,
  MenuItem,
  Paper,
  Rating,
  Select,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Typography,
} from '@mui/material'
import { useQuery } from '@tanstack/react-query'
import { useState } from 'react'
import { ApiError } from '../../api/ApiError'
import { formatDateTime } from '../../shared/format/dateTime'
import { applicationRoles } from '../auth/types'
import { useAuth } from '../auth/useAuth'
import { useAuthenticatedRequest } from '../auth/useAuthenticatedRequest'
import { listManageableEvents } from '../events/eventsApi'
import { listManagedReviews } from './reviewsApi'

function getErrorMessage(error: unknown) {
  return error instanceof ApiError
    ? error.message
    : 'Recenzije trenutno nisu dostupne.'
}

export function ManagedReviewsPage() {
  const authenticatedRequest = useAuthenticatedRequest()
  const { session } = useAuth()
  const isAdmin = Boolean(session?.user.roles.includes(applicationRoles.admin))
  const eventsQuery = useQuery({
    queryKey: ['events', 'manage'],
    queryFn: () => listManageableEvents(authenticatedRequest),
  })
  const [selectedEventId, setSelectedEventId] = useState('')
  const organizerDefaultEventId = isAdmin ? '' : eventsQuery.data?.[0]?.id ?? ''
  const effectiveEventId = selectedEventId || organizerDefaultEventId
  const reviewsQuery = useQuery({
    queryKey: ['reviews', 'manage', effectiveEventId || 'all'],
    queryFn: () =>
      listManagedReviews(
        authenticatedRequest,
        effectiveEventId.length > 0 ? effectiveEventId : undefined,
      ),
    enabled: isAdmin || effectiveEventId.length > 0,
  })

  return (
    <Stack spacing={3}>
      <Stack spacing={0.75}>
        <Typography component="h1" variant="h4">
          Izveštaji
        </Typography>
        <Typography color="text.secondary">
          Pregled recenzija po događaju i osnovni uvid u iskustvo učesnika.
        </Typography>
      </Stack>

      {(eventsQuery.error || reviewsQuery.error) && (
        <Alert severity="error">
          {getErrorMessage(eventsQuery.error ?? reviewsQuery.error)}
        </Alert>
      )}

      <FormControl sx={{ maxWidth: 420 }}>
        <InputLabel id="review-event-filter-label">Događaj</InputLabel>
        <Select
          labelId="review-event-filter-label"
          label="Događaj"
          value={effectiveEventId}
          onChange={(event) => setSelectedEventId(event.target.value)}
        >
          {isAdmin && <MenuItem value="">Svi događaji</MenuItem>}
          {eventsQuery.data?.map((eventItem) => (
            <MenuItem key={eventItem.id} value={eventItem.id}>
              {eventItem.title}
            </MenuItem>
          ))}
        </Select>
      </FormControl>

      <TableContainer component={Paper} variant="outlined">
        <Table>
          <TableHead>
            <TableRow>
              <TableCell>Događaj</TableCell>
              <TableCell>Učesnik</TableCell>
              <TableCell>Ocena</TableCell>
              <TableCell>Komentar</TableCell>
              <TableCell>Datum</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {(eventsQuery.isLoading || reviewsQuery.isLoading) && (
              <TableRow>
                <TableCell colSpan={5}>Učitavanje recenzija...</TableCell>
              </TableRow>
            )}
            {!eventsQuery.isLoading &&
              !reviewsQuery.isLoading &&
              (reviewsQuery.data?.length ?? 0) === 0 && (
                <TableRow>
                  <TableCell colSpan={5}>
                    Nema recenzija za izabrani pregled.
                  </TableCell>
                </TableRow>
              )}
            {reviewsQuery.data?.map((review) => (
              <TableRow key={review.id} hover>
                <TableCell>{review.eventTitle}</TableCell>
                <TableCell>{review.participantName}</TableCell>
                <TableCell>
                  <Rating value={review.rating} readOnly size="small" />
                </TableCell>
                <TableCell>{review.comment}</TableCell>
                <TableCell>
                  {formatDateTime(review.updatedAtUtc ?? review.createdAtUtc)}
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </TableContainer>
    </Stack>
  )
}
