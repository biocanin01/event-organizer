import {
  Alert,
  Button,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  Rating,
  Stack,
  TextField,
  Typography,
} from '@mui/material'
import { useEffect, useState } from 'react'
import { ApiError } from '../../api/ApiError'
import type { Review, ReviewFormValues } from './types'

interface ReviewFormDialogProps {
  open: boolean
  title: string
  review?: Review | null
  isSubmitting: boolean
  error?: unknown
  onClose: () => void
  onSubmit: (values: ReviewFormValues) => void
}

function getErrorMessage(error: unknown) {
  return error instanceof ApiError
    ? error.message
    : 'Recenzija trenutno nije sačuvana.'
}

export function ReviewFormDialog({
  open,
  title,
  review,
  isSubmitting,
  error,
  onClose,
  onSubmit,
}: ReviewFormDialogProps) {
  const [rating, setRating] = useState(5)
  const [comment, setComment] = useState('')

  useEffect(() => {
    if (open) {
      setRating(review?.rating ?? 5)
      setComment(review?.comment ?? '')
    }
  }, [open, review])

  const trimmedComment = comment.trim()
  const isValid = rating >= 1 && rating <= 5 && trimmedComment.length > 0

  return (
    <Dialog open={open} onClose={onClose} fullWidth maxWidth="sm">
      <DialogTitle>{title}</DialogTitle>
      <DialogContent>
        <Stack spacing={2.5} sx={{ pt: 1 }}>
          {Boolean(error) && (
            <Alert severity="error">{getErrorMessage(error)}</Alert>
          )}
          <Stack spacing={0.75}>
            <Typography component="legend">Ocena</Typography>
            <Rating
              value={rating}
              onChange={(_, value) => setRating(value ?? 0)}
            />
          </Stack>
          <TextField
            label="Komentar"
            value={comment}
            onChange={(event) => setComment(event.target.value)}
            multiline
            minRows={4}
            slotProps={{ htmlInput: { maxLength: 2000 } }}
            helperText={`${trimmedComment.length}/2000`}
            fullWidth
          />
        </Stack>
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>Otkaži</Button>
        <Button
          variant="contained"
          loading={isSubmitting}
          disabled={!isValid}
          onClick={() => onSubmit({ rating, comment: trimmedComment })}
        >
          Sačuvaj
        </Button>
      </DialogActions>
    </Dialog>
  )
}
