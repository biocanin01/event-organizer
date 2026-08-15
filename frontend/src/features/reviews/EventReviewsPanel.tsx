import RateReviewRoundedIcon from '@mui/icons-material/RateReviewRounded'
import {
  Alert,
  Button,
  Divider,
  Paper,
  Rating,
  Stack,
  Typography,
} from '@mui/material'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { ApiError } from '../../api/ApiError'
import { apiRequest } from '../../api/apiClient'
import { formatDateTime } from '../../shared/format/dateTime'
import type { Registration } from '../registrations/types'
import { useAuthenticatedRequest } from '../auth/useAuthenticatedRequest'
import { createReview, listEventReviews, updateReview } from './reviewsApi'
import { ReviewFormDialog } from './ReviewFormDialog'
import type { Review, ReviewFormValues } from './types'
import { useState } from 'react'

interface EventReviewsPanelProps {
  eventId: string
  eventStatus: string
  currentUserId?: string
  registration?: Registration
  canUseReviewActions: boolean
}

function getErrorMessage(error: unknown) {
  return error instanceof ApiError
    ? error.message
    : 'Recenzije trenutno nisu dostupne.'
}

export function EventReviewsPanel({
  eventId,
  eventStatus,
  currentUserId,
  registration,
  canUseReviewActions,
}: EventReviewsPanelProps) {
  const authenticatedRequest = useAuthenticatedRequest()
  const queryClient = useQueryClient()
  const [dialogOpen, setDialogOpen] = useState(false)
  const reviewsQuery = useQuery({
    queryKey: ['reviews', 'event', eventId],
    queryFn: () => listEventReviews(apiRequest, eventId),
    enabled: eventId.length > 0,
  })
  const ownReview = reviewsQuery.data?.find(
    (review) => review.participantUserId === currentUserId,
  )
  const canReview =
    canUseReviewActions &&
    eventStatus === 'Completed' &&
    registration?.status === 'Confirmed'

  const saveMutation = useMutation({
    mutationFn: (values: ReviewFormValues) =>
      ownReview
        ? updateReview(authenticatedRequest, ownReview.id, {
            ...values,
            version: ownReview.version,
          })
        : createReview(authenticatedRequest, eventId, values),
    onSuccess: async () => {
      setDialogOpen(false)
      await queryClient.invalidateQueries({ queryKey: ['reviews'] })
    },
  })

  return (
    <Paper variant="outlined" sx={{ p: { xs: 2, md: 3 } }}>
      <Stack spacing={2}>
        <Stack
          direction={{ xs: 'column', sm: 'row' }}
          spacing={1.5}
          sx={{ justifyContent: 'space-between', alignItems: { sm: 'center' } }}
        >
          <Stack spacing={0.25}>
            <Typography component="h2" variant="h5">
              Recenzije
            </Typography>
            <Typography variant="body2" color="text.secondary">
              Iskustva učesnika nakon završenog događaja.
            </Typography>
          </Stack>
          {canReview && (
            <Button
              variant={ownReview ? 'outlined' : 'contained'}
              startIcon={<RateReviewRoundedIcon />}
              onClick={() => setDialogOpen(true)}
              sx={{ alignSelf: { xs: 'flex-start', sm: 'center' } }}
            >
              {ownReview ? 'Izmeni recenziju' : 'Ostavi recenziju'}
            </Button>
          )}
        </Stack>

        {reviewsQuery.error && (
          <Alert severity="error">{getErrorMessage(reviewsQuery.error)}</Alert>
        )}

        {saveMutation.error && (
          <Alert severity="error">{getErrorMessage(saveMutation.error)}</Alert>
        )}

        {reviewsQuery.isLoading ? (
          <Typography color="text.secondary">Učitavanje recenzija...</Typography>
        ) : (reviewsQuery.data?.length ?? 0) === 0 ? (
          <Typography color="text.secondary">
            Još nema recenzija za ovaj događaj.
          </Typography>
        ) : (
          <Stack divider={<Divider flexItem />} spacing={2}>
            {reviewsQuery.data?.map((review) => (
              <ReviewSummary key={review.id} review={review} />
            ))}
          </Stack>
        )}
      </Stack>
      <ReviewFormDialog
        open={dialogOpen}
        title={ownReview ? 'Izmena recenzije' : 'Nova recenzija'}
        review={ownReview}
        isSubmitting={saveMutation.isPending}
        error={saveMutation.error}
        onClose={() => setDialogOpen(false)}
        onSubmit={(values) => saveMutation.mutate(values)}
      />
    </Paper>
  )
}

function ReviewSummary({ review }: { review: Review }) {
  return (
    <Stack spacing={0.75}>
      <Stack
        direction={{ xs: 'column', sm: 'row' }}
        spacing={1}
        sx={{ justifyContent: 'space-between' }}
      >
        <Typography sx={{ fontWeight: 700 }}>{review.participantName}</Typography>
        <Typography variant="body2" color="text.secondary">
          {formatDateTime(review.updatedAtUtc ?? review.createdAtUtc)}
        </Typography>
      </Stack>
      <Rating value={review.rating} readOnly size="small" />
      <Typography>{review.comment}</Typography>
    </Stack>
  )
}
