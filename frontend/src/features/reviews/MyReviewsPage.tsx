import EditRoundedIcon from '@mui/icons-material/EditRounded'
import {
  Alert,
  Button,
  Paper,
  Rating,
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
import { useState } from 'react'
import { ApiError } from '../../api/ApiError'
import { formatDateTime } from '../../shared/format/dateTime'
import { useAuthenticatedRequest } from '../auth/useAuthenticatedRequest'
import { ReviewFormDialog } from './ReviewFormDialog'
import { listMyReviews, updateReview } from './reviewsApi'
import type { Review, ReviewFormValues } from './types'

function getErrorMessage(error: unknown) {
  return error instanceof ApiError
    ? error.message
    : 'Recenzije trenutno nisu dostupne.'
}

export function MyReviewsPage() {
  const authenticatedRequest = useAuthenticatedRequest()
  const queryClient = useQueryClient()
  const [selectedReview, setSelectedReview] = useState<Review | null>(null)
  const reviewsQuery = useQuery({
    queryKey: ['reviews', 'me'],
    queryFn: () => listMyReviews(authenticatedRequest),
  })
  const updateMutation = useMutation({
    mutationFn: (values: ReviewFormValues) => {
      if (!selectedReview) {
        throw new Error('Recenzija nije izabrana.')
      }

      return updateReview(authenticatedRequest, selectedReview.id, {
        ...values,
        version: selectedReview.version,
      })
    },
    onSuccess: async () => {
      setSelectedReview(null)
      await queryClient.invalidateQueries({ queryKey: ['reviews'] })
    },
  })

  return (
    <Stack spacing={3}>
      <Stack spacing={0.75}>
        <Typography component="h1" variant="h4">
          Moje recenzije
        </Typography>
        <Typography color="text.secondary">
          Pregled i izmena ocena koje ste ostavili nakon događaja.
        </Typography>
      </Stack>

      {reviewsQuery.error && (
        <Alert severity="error">{getErrorMessage(reviewsQuery.error)}</Alert>
      )}
      {updateMutation.error && (
        <Alert severity="error">{getErrorMessage(updateMutation.error)}</Alert>
      )}

      <TableContainer component={Paper} variant="outlined">
        <Table>
          <TableHead>
            <TableRow>
              <TableCell>Događaj</TableCell>
              <TableCell>Ocena</TableCell>
              <TableCell>Komentar</TableCell>
              <TableCell>Datum</TableCell>
              <TableCell align="right">Akcija</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {reviewsQuery.isLoading && (
              <TableRow>
                <TableCell colSpan={5}>Učitavanje recenzija...</TableCell>
              </TableRow>
            )}
            {!reviewsQuery.isLoading &&
              (reviewsQuery.data?.length ?? 0) === 0 && (
                <TableRow>
                  <TableCell colSpan={5}>Još nemate recenzije.</TableCell>
                </TableRow>
              )}
            {reviewsQuery.data?.map((review) => (
              <TableRow key={review.id} hover>
                <TableCell>
                  <Typography sx={{ fontWeight: 650 }}>
                    {review.eventTitle}
                  </Typography>
                </TableCell>
                <TableCell>
                  <Rating value={review.rating} readOnly size="small" />
                </TableCell>
                <TableCell>{review.comment}</TableCell>
                <TableCell>
                  {formatDateTime(review.updatedAtUtc ?? review.createdAtUtc)}
                </TableCell>
                <TableCell align="right">
                  <Button
                    size="small"
                    startIcon={<EditRoundedIcon />}
                    onClick={() => setSelectedReview(review)}
                  >
                    Izmeni
                  </Button>
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </TableContainer>

      <ReviewFormDialog
        open={Boolean(selectedReview)}
        title="Izmena recenzije"
        review={selectedReview}
        isSubmitting={updateMutation.isPending}
        error={updateMutation.error}
        onClose={() => setSelectedReview(null)}
        onSubmit={(values) => updateMutation.mutate(values)}
      />
    </Stack>
  )
}
