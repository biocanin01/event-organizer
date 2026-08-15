import ArrowBackRoundedIcon from '@mui/icons-material/ArrowBackRounded'
import HowToRegRoundedIcon from '@mui/icons-material/HowToRegRounded'
import {
  Alert,
  Box,
  Button,
  Container,
  Divider,
  Paper,
  Stack,
  Typography,
} from '@mui/material'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Link, useParams } from 'react-router'
import { ApiError } from '../../api/ApiError'
import { apiRequest } from '../../api/apiClient'
import { StatusChip } from '../../shared/components/StatusChip'
import { formatDateTime } from '../../shared/format/dateTime'
import { applicationRoles } from '../auth/types'
import { useAuth } from '../auth/useAuth'
import { useAuthenticatedRequest } from '../auth/useAuthenticatedRequest'
import {
  createRegistration,
  listMyRegistrations,
} from '../registrations/registrationsApi'
import { getPublishedEventById } from './eventsApi'
import { getAvailableSpots } from './eventAvailability'
import { PublicEventsHeader } from './PublicEventsHeader'

function getErrorMessage(error: unknown) {
  return error instanceof ApiError
    ? error.message
    : 'Akcija trenutno nije uspela.'
}

export function PublicEventDetailsPage() {
  const { eventId = '' } = useParams()
  const { session, status } = useAuth()
  const authenticatedRequest = useAuthenticatedRequest()
  const queryClient = useQueryClient()
  const [eventItem, myRegistrations] = [
    useQuery({
      queryKey: ['events', 'published', eventId],
      queryFn: () => getPublishedEventById(apiRequest, eventId),
      enabled: eventId.length > 0,
    }),
    useQuery({
      queryKey: ['registrations', 'me'],
      queryFn: () => listMyRegistrations(authenticatedRequest),
      enabled:
        status === 'authenticated' &&
        Boolean(session?.user.roles.includes(applicationRoles.participant)),
    }),
  ]
  const existingRegistration = myRegistrations.data?.find(
    (registration) => registration.eventId === eventId,
  )

  const registerMutation = useMutation({
    mutationFn: () => createRegistration(authenticatedRequest, eventId),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ['registrations', 'me'] })
      await queryClient.invalidateQueries({ queryKey: ['events', 'published'] })
    },
  })

  if (eventItem.isLoading) {
    return (
      <Box sx={{ minHeight: '100vh' }}>
        <PublicEventsHeader />
        <Container maxWidth="lg" sx={{ py: 6 }}>
          <Typography>Učitavanje događaja...</Typography>
        </Container>
      </Box>
    )
  }

  if (eventItem.error || !eventItem.data) {
    return (
      <Box sx={{ minHeight: '100vh' }}>
        <PublicEventsHeader />
        <Container maxWidth="lg" sx={{ py: 6 }}>
          <Alert severity="error">
            {getErrorMessage(eventItem.error ?? new Error())}
          </Alert>
        </Container>
      </Box>
    )
  }

  const event = eventItem.data
  const availableSpots = getAvailableSpots(event)
  const hasStarted = new Date(event.startsAtUtc) <= new Date()
  const isParticipant = Boolean(
    session?.user.roles.includes(applicationRoles.participant),
  )

  return (
    <Box sx={{ minHeight: '100vh' }}>
      <PublicEventsHeader />
      <Container component="main" maxWidth="md" sx={{ py: { xs: 4, md: 6 } }}>
        <Stack spacing={3}>
          <Button
            component={Link}
            to="/discover"
            startIcon={<ArrowBackRoundedIcon />}
            sx={{ alignSelf: 'flex-start' }}
          >
            Svi događaji
          </Button>

          <Stack spacing={1}>
            <Typography component="h1" variant="h3">
              {event.title}
            </Typography>
            <Typography color="text.secondary">{event.area}</Typography>
          </Stack>

          <Paper variant="outlined" sx={{ p: { xs: 2, md: 3 } }}>
            <Stack spacing={2.5}>
              <Typography>{event.description}</Typography>
              <Divider />
              <Box
                sx={{
                  display: 'grid',
                  gap: 2,
                  gridTemplateColumns: { xs: '1fr', sm: 'repeat(2, 1fr)' },
                }}
              >
                <Stack spacing={0.35}>
                  <Typography variant="body2" color="text.secondary">
                    Početak
                  </Typography>
                  <Typography>{formatDateTime(event.startsAtUtc)}</Typography>
                </Stack>
                <Stack spacing={0.35}>
                  <Typography variant="body2" color="text.secondary">
                    Kraj
                  </Typography>
                  <Typography>{formatDateTime(event.endsAtUtc)}</Typography>
                </Stack>
                <Stack spacing={0.35}>
                  <Typography variant="body2" color="text.secondary">
                    Kapacitet
                  </Typography>
                  <Typography>
                    {availableSpots} od {event.capacity} mesta je slobodno
                  </Typography>
                </Stack>
                <Stack spacing={0.35}>
                  <Typography variant="body2" color="text.secondary">
                    Oblast
                  </Typography>
                  <Typography>{event.area}</Typography>
                </Stack>
              </Box>
            </Stack>
          </Paper>

          {registerMutation.error && (
            <Alert severity="error">
              {getErrorMessage(registerMutation.error)}
            </Alert>
          )}

          {myRegistrations.error ? (
            <Alert severity="error">
              {getErrorMessage(myRegistrations.error)}
            </Alert>
          ) : status === 'loading' ? (
            <Typography color="text.secondary">Provera naloga...</Typography>
          ) : existingRegistration ? (
            <Alert
              severity={
                existingRegistration.status === 'Rejected' ? 'error' : 'info'
              }
              action={<StatusChip status={existingRegistration.status} />}
            >
              Već imate prijavu za ovaj događaj.
              {existingRegistration.rejectionReason
                ? ` Razlog odbijanja: ${existingRegistration.rejectionReason}`
                : ''}
            </Alert>
          ) : status === 'anonymous' ? (
            <Button
              component={Link}
              to="/login"
              variant="contained"
              startIcon={<HowToRegRoundedIcon />}
              sx={{ alignSelf: 'flex-start' }}
            >
              Prijavi se na nalog
            </Button>
          ) : isParticipant && !hasStarted && availableSpots > 0 ? (
            <Button
              variant="contained"
              startIcon={<HowToRegRoundedIcon />}
              loading={registerMutation.isPending}
              onClick={() => registerMutation.mutate()}
              sx={{ alignSelf: 'flex-start' }}
            >
              Prijavi se
            </Button>
          ) : status === 'authenticated' && !isParticipant ? (
            <Alert severity="info">
              Samo korisnici sa Participant rolom mogu da se prijave.
            </Alert>
          ) : (
            <Alert severity="info">
              {hasStarted
                ? 'Prijave su zatvorene jer je događaj počeo.'
                : 'Događaj trenutno nema slobodnih mesta.'}
            </Alert>
          )}
        </Stack>
      </Container>
    </Box>
  )
}
