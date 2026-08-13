import AddRoundedIcon from '@mui/icons-material/AddRounded'
import CancelRoundedIcon from '@mui/icons-material/CancelRounded'
import CheckCircleRoundedIcon from '@mui/icons-material/CheckCircleRounded'
import EditRoundedIcon from '@mui/icons-material/EditRounded'
import PublishRoundedIcon from '@mui/icons-material/PublishRounded'
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
import { useMemo, useState } from 'react'
import { ApiError } from '../../api/ApiError'
import { StatusChip } from '../../shared/components/StatusChip'
import { formatDateTime } from '../../shared/format/dateTime'
import { applicationRoles } from '../auth/types'
import { useAuthenticatedRequest } from '../auth/useAuthenticatedRequest'
import { useAuth } from '../auth/useAuth'
import {
  cancelEvent,
  completeEvent,
  createEvent,
  listManageableEvents,
  listPublishedEvents,
  publishEvent,
  updateEvent,
} from './eventsApi'
import { EventFormDialog } from './EventFormDialog'
import type { EventFormValues, EventItem } from './types'

function getErrorMessage(error: unknown) {
  return error instanceof ApiError
    ? error.message
    : 'Akcija trenutno nije uspela.'
}

function formatMoney(value: number) {
  return new Intl.NumberFormat('sr-RS', {
    style: 'currency',
    currency: 'RSD',
    maximumFractionDigits: 0,
  }).format(value)
}

function hasEnded(eventItem: EventItem) {
  return new Date(eventItem.endsAtUtc) <= new Date()
}

export function EventsPage() {
  const { session } = useAuth()
  const authenticatedRequest = useAuthenticatedRequest()
  const queryClient = useQueryClient()
  const [error, setError] = useState<string | null>(null)
  const [formOpen, setFormOpen] = useState(false)
  const [eventToEdit, setEventToEdit] = useState<EventItem | null>(null)

  const isManager = Boolean(
    session?.user.roles.some(
      (role) =>
        role === applicationRoles.organizer || role === applicationRoles.admin,
    ),
  )
  const eventsQueryKey = useMemo(
    () => ['events', isManager ? 'manage' : 'published'],
    [isManager],
  )

  const { data: events = [], isLoading } = useQuery({
    queryKey: eventsQueryKey,
    queryFn: () =>
      isManager
        ? listManageableEvents(authenticatedRequest)
        : listPublishedEvents(authenticatedRequest),
    enabled: Boolean(session?.accessToken),
  })

  const refreshEvents = async () => {
    await queryClient.invalidateQueries({ queryKey: eventsQueryKey })
  }

  const createMutation = useMutation({
    mutationFn: (values: EventFormValues) =>
      createEvent(authenticatedRequest, values),
    onSuccess: async () => {
      setError(null)
      setFormOpen(false)
      await refreshEvents()
    },
    onError: (mutationError) => setError(getErrorMessage(mutationError)),
  })

  const updateMutation = useMutation({
    mutationFn: (values: EventFormValues) => {
      if (!eventToEdit) {
        throw new Error('Događaj nije izabran.')
      }

      return updateEvent(authenticatedRequest, eventToEdit.id, values)
    },
    onSuccess: async () => {
      setError(null)
      setEventToEdit(null)
      setFormOpen(false)
      await refreshEvents()
    },
    onError: (mutationError) => setError(getErrorMessage(mutationError)),
  })

  const actionMutation = useMutation({
    mutationFn: async ({
      eventItem,
      action,
    }: {
      eventItem: EventItem
      action: 'publish' | 'cancel' | 'complete'
    }) => {
      if (action === 'publish') {
        await publishEvent(authenticatedRequest, eventItem.id)
      } else if (action === 'cancel') {
        await cancelEvent(authenticatedRequest, eventItem.id)
      } else {
        await completeEvent(authenticatedRequest, eventItem.id)
      }
    },
    onSuccess: async () => {
      setError(null)
      await refreshEvents()
    },
    onError: (mutationError) => setError(getErrorMessage(mutationError)),
  })

  const handleFormSubmit = (values: EventFormValues) => {
    if (eventToEdit) {
      updateMutation.mutate(values)
      return
    }

    createMutation.mutate(values)
  }

  const isFormSubmitting = createMutation.isPending || updateMutation.isPending

  return (
    <Stack spacing={3}>
      <Stack
        direction={{ xs: 'column', md: 'row' }}
        spacing={2}
        sx={{ justifyContent: 'space-between' }}
      >
        <Stack spacing={0.75}>
          <Typography component="h1" variant="h4">
            Događaji
          </Typography>
          <Typography color="text.secondary">
            {isManager
              ? 'Pregled i upravljanje događajima kroz lifecycle.'
              : 'Pregled objavljenih događaja dostupnih učesnicima.'}
          </Typography>
        </Stack>
        {isManager && (
          <Button
            variant="contained"
            startIcon={<AddRoundedIcon />}
            onClick={() => {
              setEventToEdit(null)
              setFormOpen(true)
            }}
          >
            Novi događaj
          </Button>
        )}
      </Stack>

      {error && <Alert severity="error">{error}</Alert>}

      <TableContainer component={Paper} variant="outlined">
        <Table>
          <TableHead>
            <TableRow>
              <TableCell>Događaj</TableCell>
              <TableCell>Termin</TableCell>
              <TableCell>Plan</TableCell>
              <TableCell>Status</TableCell>
              {isManager && <TableCell align="right">Akcije</TableCell>}
            </TableRow>
          </TableHead>
          <TableBody>
            {isLoading && (
              <TableRow>
                <TableCell colSpan={isManager ? 5 : 4}>
                  Učitavanje događaja...
                </TableCell>
              </TableRow>
            )}
            {!isLoading && events.length === 0 && (
              <TableRow>
                <TableCell colSpan={isManager ? 5 : 4}>
                  {isManager
                    ? 'Nema događaja za upravljanje.'
                    : 'Trenutno nema objavljenih događaja.'}
                </TableCell>
              </TableRow>
            )}
            {events.map((eventItem) => (
              <TableRow key={eventItem.id} hover>
                <TableCell>
                  <Stack spacing={0.35}>
                    <Typography sx={{ fontWeight: 650 }}>
                      {eventItem.title}
                    </Typography>
                    <Typography variant="body2" color="text.secondary">
                      {eventItem.area}
                    </Typography>
                  </Stack>
                </TableCell>
                <TableCell>
                  <Stack spacing={0.25}>
                    <Typography variant="body2">
                      {formatDateTime(eventItem.startsAtUtc)}
                    </Typography>
                    <Typography variant="body2" color="text.secondary">
                      {formatDateTime(eventItem.endsAtUtc)}
                    </Typography>
                  </Stack>
                </TableCell>
                <TableCell>
                  <Stack spacing={0.25}>
                    <Typography variant="body2">
                      {eventItem.capacity} mesta · {formatMoney(eventItem.budget)}
                    </Typography>
                    <Typography variant="body2" color="text.secondary">
                      {eventItem.requiredSpeakerCount} predavača
                      {eventItem.requiresEquipment ? ' · oprema' : ''}
                    </Typography>
                  </Stack>
                </TableCell>
                <TableCell>
                  <StatusChip status={eventItem.status} />
                </TableCell>
                {isManager && (
                  <TableCell align="right">
                    <Stack
                      direction="row"
                      spacing={1}
                      sx={{ justifyContent: 'flex-end', flexWrap: 'wrap' }}
                    >
                      {eventItem.status === 'Draft' && (
                        <>
                          <Button
                            size="small"
                            startIcon={<EditRoundedIcon />}
                            onClick={() => {
                              setEventToEdit(eventItem)
                              setFormOpen(true)
                            }}
                          >
                            Izmeni
                          </Button>
                          <Button
                            size="small"
                            variant="contained"
                            startIcon={<PublishRoundedIcon />}
                            loading={actionMutation.isPending}
                            onClick={() =>
                              actionMutation.mutate({
                                eventItem,
                                action: 'publish',
                              })
                            }
                          >
                            Objavi
                          </Button>
                        </>
                      )}
                      {eventItem.status === 'Published' && hasEnded(eventItem) && (
                        <Button
                          size="small"
                          startIcon={<CheckCircleRoundedIcon />}
                          loading={actionMutation.isPending}
                          onClick={() =>
                            actionMutation.mutate({
                              eventItem,
                              action: 'complete',
                            })
                          }
                        >
                          Završi
                        </Button>
                      )}
                      {eventItem.status !== 'Cancelled' &&
                        eventItem.status !== 'Completed' && (
                          <Button
                            size="small"
                            color="error"
                            startIcon={<CancelRoundedIcon />}
                            loading={actionMutation.isPending}
                            onClick={() =>
                              actionMutation.mutate({
                                eventItem,
                                action: 'cancel',
                              })
                            }
                          >
                            Otkaži
                          </Button>
                        )}
                    </Stack>
                  </TableCell>
                )}
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </TableContainer>

      <EventFormDialog
        open={formOpen}
        eventItem={eventToEdit}
        isSubmitting={isFormSubmitting}
        onClose={() => {
          if (!isFormSubmitting) {
            setFormOpen(false)
            setEventToEdit(null)
          }
        }}
        onSubmit={handleFormSubmit}
      />
    </Stack>
  )
}
