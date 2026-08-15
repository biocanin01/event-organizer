import CancelRoundedIcon from '@mui/icons-material/CancelRounded'
import RateReviewRoundedIcon from '@mui/icons-material/RateReviewRounded'
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
import { ReviewFormDialog } from '../reviews/ReviewFormDialog'
import { createReview, listMyReviews, updateReview } from '../reviews/reviewsApi'
import type { Review, ReviewFormValues } from '../reviews/types'
import { cancelRegistration, listMyRegistrations } from './registrationsApi'
import type { Registration } from './types'
import { useState } from 'react'

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

function canReview(registration: Registration) {
  return (
    registration.status === 'Confirmed' &&
    registration.eventStatus === 'Completed'
  )
}

export function MyRegistrationsPage() {
  const authenticatedRequest = useAuthenticatedRequest()
  const queryClient = useQueryClient()
  const [reviewTarget, setReviewTarget] = useState<Registration | null>(null)
  const registrationsQuery = useQuery({
    queryKey: ['registrations', 'me'],
    queryFn: () => listMyRegistrations(authenticatedRequest),
  })
  const reviewsQuery = useQuery({
    queryKey: ['reviews', 'me'],
    queryFn: () => listMyReviews(authenticatedRequest),
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
  const saveReviewMutation = useMutation({
    mutationFn: (values: ReviewFormValues) => {
      if (!reviewTarget) {
        throw new Error('Događaj nije izabran.')
      }

      const existingReview = findReviewForRegistration(
        reviewsQuery.data,
        reviewTarget,
      )

      return existingReview
        ? updateReview(authenticatedRequest, existingReview.id, {
            ...values,
            version: existingReview.version,
          })
        : createReview(authenticatedRequest, reviewTarget.eventId, values)
    },
    onSuccess: async () => {
      setReviewTarget(null)
      await queryClient.invalidateQueries({ queryKey: ['reviews'] })
    },
  })
  const activeReview = reviewTarget
    ? findReviewForRegistration(reviewsQuery.data, reviewTarget)
    : null

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
      {reviewsQuery.error && (
        <Alert severity="error">{getErrorMessage(reviewsQuery.error)}</Alert>
      )}
      {saveReviewMutation.error && (
        <Alert severity="error">
          {getErrorMessage(saveReviewMutation.error)}
        </Alert>
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
                  <Stack
                    direction={{ xs: 'column', sm: 'row' }}
                    spacing={1}
                    sx={{ justifyContent: 'flex-end' }}
                  >
                    {canReview(registration) && (
                      <Button
                        size="small"
                        startIcon={<RateReviewRoundedIcon />}
                        onClick={() => setReviewTarget(registration)}
                      >
                        {findReviewForRegistration(
                          reviewsQuery.data,
                          registration,
                        )
                          ? 'Izmeni recenziju'
                          : 'Oceni'}
                      </Button>
                    )}
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
                  </Stack>
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </TableContainer>
      <ReviewFormDialog
        open={Boolean(reviewTarget)}
        title={activeReview ? 'Izmena recenzije' : 'Nova recenzija'}
        review={activeReview}
        isSubmitting={saveReviewMutation.isPending}
        error={saveReviewMutation.error}
        onClose={() => setReviewTarget(null)}
        onSubmit={(values) => saveReviewMutation.mutate(values)}
      />
    </Stack>
  )
}

function findReviewForRegistration(
  reviews: Review[] | undefined,
  registration: Registration,
) {
  return reviews?.find((review) => review.eventId === registration.eventId)
}
