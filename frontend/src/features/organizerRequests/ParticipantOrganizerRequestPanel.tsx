import { zodResolver } from '@hookform/resolvers/zod'
import {
  Alert,
  Box,
  Button,
  Divider,
  Paper,
  Stack,
  TextField,
  Typography,
} from '@mui/material'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { z } from 'zod'
import { ApiError } from '../../api/ApiError'
import { applicationRoles } from '../auth/types'
import { useAuth } from '../auth/useAuth'
import {
  getMyOrganizerRoleRequest,
  submitOrganizerRoleRequest,
  withdrawOrganizerRoleRequest,
} from './organizerRequestApi'
import { StatusChip } from '../../shared/components/StatusChip'
import { formatDateTime } from '../../shared/format/dateTime'

const organizerRequestQueryKey = ['organizer-role-requests', 'me'] as const

const organizerRequestSchema = z.object({
  motivation: z
    .string()
    .trim()
    .min(20, 'Motivacija mora imati najmanje 20 karaktera.')
    .max(1000, 'Motivacija moze imati najvise 1000 karaktera.'),
})

type OrganizerRequestFormValues = z.infer<typeof organizerRequestSchema>

function getErrorMessage(error: unknown) {
  return error instanceof ApiError
    ? error.message
    : 'Akcija trenutno nije uspela.'
}

export function ParticipantOrganizerRequestPanel() {
  const { session } = useAuth()
  const queryClient = useQueryClient()
  const [error, setError] = useState<string | null>(null)
  const accessToken = session?.accessToken ?? ''
  const userRoles = session?.user.roles ?? []
  const isOrganizer = userRoles.includes(applicationRoles.organizer)
  const isAdmin = userRoles.includes(applicationRoles.admin)

  const { data: request, isLoading } = useQuery({
    queryKey: organizerRequestQueryKey,
    queryFn: () => getMyOrganizerRoleRequest(accessToken),
    enabled: Boolean(accessToken) && !isAdmin,
  })

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors, isSubmitting },
  } = useForm<OrganizerRequestFormValues>({
    resolver: zodResolver(organizerRequestSchema),
    defaultValues: {
      motivation: '',
    },
  })

  const submitMutation = useMutation({
    mutationFn: (values: OrganizerRequestFormValues) =>
      submitOrganizerRoleRequest(accessToken, values),
    onSuccess: async () => {
      reset()
      setError(null)
      await queryClient.invalidateQueries({ queryKey: organizerRequestQueryKey })
    },
    onError: (mutationError) => setError(getErrorMessage(mutationError)),
  })

  const withdrawMutation = useMutation({
    mutationFn: () => {
      if (!request) {
        throw new Error('Organizer request is not available.')
      }

      return withdrawOrganizerRoleRequest(accessToken, request.id, {
        version: request.version,
      })
    },
    onSuccess: async () => {
      setError(null)
      await queryClient.invalidateQueries({ queryKey: organizerRequestQueryKey })
    },
    onError: (mutationError) => setError(getErrorMessage(mutationError)),
  })

  const canSubmit =
    !isOrganizer &&
    !isAdmin &&
    (!request || request.status === 'Rejected' || request.status === 'Withdrawn')

  const onSubmit = handleSubmit((values) => submitMutation.mutate(values))

  if (isAdmin) {
    return null
  }

  return (
    <Paper variant="outlined" sx={{ p: 3 }}>
      <Stack spacing={2.5}>
        <Stack spacing={0.75}>
          <Typography component="h2" variant="h6">
            Organizer rola
          </Typography>
          <Typography color="text.secondary">
            Ucesnici mogu da zatraze Organizer privilegije za kreiranje i
            upravljanje dogadjajima.
          </Typography>
        </Stack>

        {error && <Alert severity="error">{error}</Alert>}

        {isOrganizer && (
          <Alert severity="success">
            Vas nalog vec ima Organizer privilegije.
          </Alert>
        )}

        {!isOrganizer && isLoading && (
          <Typography color="text.secondary">Ucitavanje zahteva...</Typography>
        )}

        {!isOrganizer && request && (
          <Stack spacing={1.5}>
            <Stack
              direction={{ xs: 'column', sm: 'row' }}
              spacing={1.5}
              sx={{ alignItems: { sm: 'center' } }}
            >
              <StatusChip status={request.status} />
              <Typography color="text.secondary">
                Poslato: {formatDateTime(request.submittedAtUtc)}
              </Typography>
            </Stack>
            <Typography>{request.motivation}</Typography>
            {request.decisionReason && (
              <Alert severity="info">Razlog odluke: {request.decisionReason}</Alert>
            )}
            {request.status === 'Pending' && (
              <Button
                variant="outlined"
                color="warning"
                loading={withdrawMutation.isPending}
                onClick={() => withdrawMutation.mutate()}
                sx={{ alignSelf: 'flex-start' }}
              >
                Povuci zahtev
              </Button>
            )}
          </Stack>
        )}

        {canSubmit && (
          <>
            {request && <Divider />}
            <Stack component="form" spacing={2} onSubmit={onSubmit}>
              <TextField
                label="Motivacija"
                multiline
                minRows={4}
                {...register('motivation')}
                error={Boolean(errors.motivation)}
                helperText={
                  errors.motivation?.message ??
                  'Objasnite zasto zelite da organizujete dogadjaje.'
                }
              />
              <Box>
                <Button
                  type="submit"
                  variant="contained"
                  loading={isSubmitting || submitMutation.isPending}
                >
                  Posalji zahtev
                </Button>
              </Box>
            </Stack>
          </>
        )}
      </Stack>
    </Paper>
  )
}
