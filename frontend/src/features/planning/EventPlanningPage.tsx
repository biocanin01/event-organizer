import ArrowBackRoundedIcon from '@mui/icons-material/ArrowBackRounded'
import AutoAwesomeRoundedIcon from '@mui/icons-material/AutoAwesomeRounded'
import PublishRoundedIcon from '@mui/icons-material/PublishRounded'
import SaveRoundedIcon from '@mui/icons-material/SaveRounded'
import SendRoundedIcon from '@mui/icons-material/SendRounded'
import UndoRoundedIcon from '@mui/icons-material/UndoRounded'
import {
  Alert,
  Box,
  Button,
  Checkbox,
  Divider,
  FormControl,
  InputLabel,
  ListItemText,
  MenuItem,
  Paper,
  Select,
  Stack,
  Typography,
} from '@mui/material'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useEffect, useMemo, useState } from 'react'
import { Link, useParams } from 'react-router'
import { ApiError } from '../../api/ApiError'
import { StatusChip } from '../../shared/components/StatusChip'
import { formatDateTime } from '../../shared/format/dateTime'
import { formatMoney } from '../../shared/format/money'
import {
  getEventBooking,
  getEventRecommendation,
  reviseEventBooking,
  submitEventBooking,
  updateEventBookingDraft,
  withdrawEventBooking,
} from '../bookings/bookingsApi'
import type {
  BookingConflictDetail,
  EventRecommendation,
  EventResourceBooking,
} from '../bookings/types'
import { useAuthenticatedRequest } from '../auth/useAuthenticatedRequest'
import { getManageableEventById, publishEvent } from '../events/eventsApi'
import type { EventItem } from '../events/types'
import { listResources } from '../resources/resourcesApi'
import { resourceTypeLabels } from '../resources/resourceLabels'
import type { ResourceItem } from '../resources/types'

interface DraftSelection {
  venueId: string | null
  speakerIds: string[]
  equipmentPackageId: string | null
}

function getErrorMessage(error: unknown) {
  return error instanceof ApiError
    ? error.message
    : 'Akcija trenutno nije uspela.'
}

function isBookingConflict(value: unknown): value is BookingConflictDetail {
  return (
    typeof value === 'object' &&
    value !== null &&
    'resourceName' in value &&
    'startsAtUtc' in value &&
    'endsAtUtc' in value
  )
}

function selectionFromBooking(booking: EventResourceBooking): DraftSelection {
  return {
    venueId: booking.venue?.id ?? null,
    speakerIds: booking.speakers.map((speaker) => speaker.id),
    equipmentPackageId: booking.equipmentPackage?.id ?? null,
  }
}

function findResource(resources: ResourceItem[], resourceId: string | null) {
  return resourceId
    ? resources.find((resource) => resource.id === resourceId) ?? null
    : null
}

function getSelectedResources(
  resources: ResourceItem[],
  selection: DraftSelection,
) {
  return [
    findResource(resources, selection.venueId),
    ...selection.speakerIds
      .map((speakerId) => findResource(resources, speakerId))
      .filter((resource): resource is ResourceItem => resource !== null),
    findResource(resources, selection.equipmentPackageId),
  ].filter((resource): resource is ResourceItem => resource !== null)
}

function getResourceOptions(
  resources: ResourceItem[],
  type: ResourceItem['type'],
  selectedIds: string[],
) {
  return resources.filter(
    (resource) =>
      resource.type === type &&
      (resource.status === 'Available' || selectedIds.includes(resource.id)),
  )
}

function formatResourceOption(resource: ResourceItem) {
  const details =
    resource.type === 'Venue'
      ? resource.capacity === null
        ? null
        : `${resource.capacity} mesta`
      : resource.type === 'Speaker'
        ? resource.expertiseArea
        : resource.supportedCapacity === null
          ? resource.providerName
          : `${resource.providerName} · ${resource.supportedCapacity} mesta`

  return details
    ? `${resource.name} (${details}, ${formatMoney(resource.cost)})`
    : `${resource.name} (${formatMoney(resource.cost)})`
}

function getSelectionCost(resources: ResourceItem[], selection: DraftSelection) {
  return getSelectedResources(resources, selection).reduce(
    (total, resource) => total + resource.cost,
    0,
  )
}

function getRecommendationSelection(
  recommendation: EventRecommendation,
): DraftSelection {
  return {
    venueId: recommendation.venue?.id ?? null,
    speakerIds: recommendation.speakers.map((speaker) => speaker.id),
    equipmentPackageId: recommendation.equipmentPackage?.id ?? null,
  }
}

function ResourceSummary({
  title,
  resource,
}: {
  title: string
  resource: ResourceItem | null
}) {
  return (
    <Stack spacing={0.5}>
      <Typography variant="body2" color="text.secondary">
        {title}
      </Typography>
      <Typography sx={{ fontWeight: 650 }}>
        {resource ? resource.name : 'Nije izabrano'}
      </Typography>
      {resource && (
        <Typography variant="body2" color="text.secondary">
          {resourceTypeLabels[resource.type]} · {formatMoney(resource.cost)}
        </Typography>
      )}
    </Stack>
  )
}

function EventDetails({ eventItem }: { eventItem: EventItem }) {
  return (
    <Paper variant="outlined" sx={{ p: 3 }}>
      <Stack spacing={2}>
        <Stack
          direction={{ xs: 'column', md: 'row' }}
          spacing={2}
          sx={{ justifyContent: 'space-between' }}
        >
          <Stack spacing={0.5}>
            <Typography component="h1" variant="h4">
              {eventItem.title}
            </Typography>
            <Typography color="text.secondary">
              {eventItem.description}
            </Typography>
          </Stack>
          <StatusChip status={eventItem.status} />
        </Stack>
        <Divider />
        <Box
          sx={{
            display: 'grid',
            gap: 2,
            gridTemplateColumns: { xs: '1fr', md: 'repeat(4, 1fr)' },
          }}
        >
          <Stack spacing={0.5}>
            <Typography variant="body2" color="text.secondary">
              Termin
            </Typography>
            <Typography>{formatDateTime(eventItem.startsAtUtc)}</Typography>
            <Typography variant="body2" color="text.secondary">
              {formatDateTime(eventItem.endsAtUtc)}
            </Typography>
          </Stack>
          <Stack spacing={0.5}>
            <Typography variant="body2" color="text.secondary">
              Kapacitet
            </Typography>
            <Typography>{eventItem.capacity} mesta</Typography>
          </Stack>
          <Stack spacing={0.5}>
            <Typography variant="body2" color="text.secondary">
              Budžet
            </Typography>
            <Typography>{formatMoney(eventItem.budget)}</Typography>
          </Stack>
          <Stack spacing={0.5}>
            <Typography variant="body2" color="text.secondary">
              Uslovi
            </Typography>
            <Typography>
              {eventItem.area} · {eventItem.requiredSpeakerCount} predavača
              {eventItem.requiresEquipment ? ' · oprema' : ''}
            </Typography>
          </Stack>
        </Box>
      </Stack>
    </Paper>
  )
}

export function EventPlanningPage() {
  const { eventId } = useParams()
  const authenticatedRequest = useAuthenticatedRequest()
  const queryClient = useQueryClient()
  const [selection, setSelection] = useState<DraftSelection>({
    venueId: null,
    speakerIds: [],
    equipmentPackageId: null,
  })
  const [error, setError] = useState<string | null>(null)
  const [conflicts, setConflicts] = useState<BookingConflictDetail[]>([])
  const [recommendation, setRecommendation] =
    useState<EventRecommendation | null>(null)

  const enabled = Boolean(eventId)
  const eventQueryKey = ['events', 'manage', eventId]
  const bookingQueryKey = ['events', eventId, 'booking']
  const resourcesQueryKey = ['resources']

  const { data: eventItem, isLoading: isEventLoading } = useQuery({
    queryKey: eventQueryKey,
    queryFn: () => getManageableEventById(authenticatedRequest, eventId ?? ''),
    enabled,
  })

  const { data: booking, isLoading: isBookingLoading } = useQuery({
    queryKey: bookingQueryKey,
    queryFn: () => getEventBooking(authenticatedRequest, eventId ?? ''),
    enabled,
  })

  const { data: resources = [], isLoading: isResourcesLoading } = useQuery({
    queryKey: resourcesQueryKey,
    queryFn: () => listResources(authenticatedRequest),
    enabled,
  })

  useEffect(() => {
    if (booking) {
      setSelection(selectionFromBooking(booking))
    }
  }, [booking])

  const selectedResources = useMemo(
    () => getSelectedResources(resources, selection),
    [resources, selection],
  )
  const selectedCost = useMemo(
    () => getSelectionCost(resources, selection),
    [resources, selection],
  )

  const isReadOnly =
    !booking ||
    !eventItem ||
    booking.status !== 'Draft' ||
    eventItem.status === 'Cancelled' ||
    eventItem.status === 'Completed'

  const venueOptions = getResourceOptions(
    resources,
    'Venue',
    selection.venueId ? [selection.venueId] : [],
  )
  const speakerOptions = getResourceOptions(
    resources,
    'Speaker',
    selection.speakerIds,
  )
  const equipmentOptions = getResourceOptions(
    resources,
    'EquipmentPackage',
    selection.equipmentPackageId ? [selection.equipmentPackageId] : [],
  )

  const resetErrors = () => {
    setError(null)
    setConflicts([])
  }

  const handleMutationError = (mutationError: unknown) => {
    setError(getErrorMessage(mutationError))
    setConflicts(
      mutationError instanceof ApiError
        ? mutationError.conflicts.filter(isBookingConflict)
        : [],
    )
  }

  const refreshBooking = async () => {
    await queryClient.invalidateQueries({ queryKey: bookingQueryKey })
  }

  const saveDraftMutation = useMutation({
    mutationFn: () => {
      if (!eventId || !booking || !eventItem) {
        throw new Error('Planiranje nije učitano.')
      }

      return updateEventBookingDraft(authenticatedRequest, eventId, {
        version: booking.version,
        venueId: selection.venueId,
        speakerIds: selection.speakerIds,
        equipmentPackageId: eventItem.requiresEquipment
          ? selection.equipmentPackageId
          : null,
      })
    },
    onSuccess: async (updatedBooking) => {
      resetErrors()
      setSelection(selectionFromBooking(updatedBooking))
      await refreshBooking()
    },
    onError: handleMutationError,
  })

  const submitMutation = useMutation({
    mutationFn: () => {
      if (!eventId || !booking) {
        throw new Error('Booking nije učitan.')
      }

      return submitEventBooking(authenticatedRequest, eventId, {
        version: booking.version,
      })
    },
    onSuccess: async (updatedBooking) => {
      resetErrors()
      setSelection(selectionFromBooking(updatedBooking))
      await refreshBooking()
    },
    onError: handleMutationError,
  })

  const withdrawMutation = useMutation({
    mutationFn: () => {
      if (!eventId || !booking) {
        throw new Error('Booking nije učitan.')
      }

      return withdrawEventBooking(authenticatedRequest, eventId, {
        version: booking.version,
      })
    },
    onSuccess: async (updatedBooking) => {
      resetErrors()
      setSelection(selectionFromBooking(updatedBooking))
      await refreshBooking()
    },
    onError: handleMutationError,
  })

  const reviseMutation = useMutation({
    mutationFn: () => {
      if (!eventId || !booking) {
        throw new Error('Booking nije učitan.')
      }

      return reviseEventBooking(authenticatedRequest, eventId, {
        version: booking.version,
      })
    },
    onSuccess: async (updatedBooking) => {
      resetErrors()
      setSelection(selectionFromBooking(updatedBooking))
      await refreshBooking()
    },
    onError: handleMutationError,
  })

  const recommendationMutation = useMutation({
    mutationFn: () => {
      if (!eventId) {
        throw new Error('Događaj nije izabran.')
      }

      return getEventRecommendation(authenticatedRequest, eventId)
    },
    onSuccess: (result) => {
      resetErrors()
      setRecommendation(result)
    },
    onError: handleMutationError,
  })

  const publishMutation = useMutation({
    mutationFn: () => {
      if (!eventId) {
        throw new Error('Događaj nije izabran.')
      }

      return publishEvent(authenticatedRequest, eventId)
    },
    onSuccess: async () => {
      resetErrors()
      await queryClient.invalidateQueries({ queryKey: eventQueryKey })
      await queryClient.invalidateQueries({ queryKey: ['events'] })
    },
    onError: handleMutationError,
  })

  if (!eventId) {
    return <Alert severity="error">Događaj nije izabran.</Alert>
  }

  if (isEventLoading || isBookingLoading || isResourcesLoading) {
    return <Typography color="text.secondary">Učitavanje planiranja...</Typography>
  }

  if (!eventItem || !booking) {
    return <Alert severity="error">Planiranje nije pronađeno.</Alert>
  }

  const canSaveDraft = booking.status === 'Draft' && !isReadOnly
  const canSubmit = booking.status === 'Draft'
  const canWithdraw = booking.status === 'Submitted'
  const canRevise = booking.status === 'Rejected' || booking.status === 'Expired'
  const canPublish =
    eventItem.status === 'Draft' && booking.status === 'Approved'

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

      <EventDetails eventItem={eventItem} />

      {error && (
        <Alert severity="error">
          <Stack spacing={1}>
            <Typography>{error}</Typography>
            {conflicts.map((conflict) => (
              <Typography key={`${conflict.resourceId}-${conflict.eventId}`} variant="body2">
                {conflict.resourceName}: {formatDateTime(conflict.startsAtUtc)} -{' '}
                {formatDateTime(conflict.endsAtUtc)}
              </Typography>
            ))}
          </Stack>
        </Alert>
      )}

      <Paper variant="outlined" sx={{ p: 3 }}>
        <Stack spacing={2.5}>
          <Stack
            direction={{ xs: 'column', md: 'row' }}
            spacing={2}
            sx={{ justifyContent: 'space-between' }}
          >
            <Stack spacing={0.5}>
              <Typography component="h2" variant="h6">
                Booking status
              </Typography>
              <Typography color="text.secondary">
                Verzija {booking.version}
                {booking.holdExpiresAtUtc
                  ? ` · hold do ${formatDateTime(booking.holdExpiresAtUtc)}`
                  : ''}
              </Typography>
            </Stack>
            <StatusChip status={booking.status} />
          </Stack>
          {booking.decisionReason && (
            <Alert severity="info">Razlog odluke: {booking.decisionReason}</Alert>
          )}

          <Box
            sx={{
              display: 'grid',
              gap: 2,
              gridTemplateColumns: { xs: '1fr', md: 'repeat(3, 1fr)' },
            }}
          >
            <FormControl fullWidth disabled={isReadOnly}>
              <InputLabel id="planning-venue-label">Sala</InputLabel>
              <Select
                labelId="planning-venue-label"
                label="Sala"
                value={selection.venueId ?? ''}
                onChange={(event) =>
                  setSelection((current) => ({
                    ...current,
                    venueId: event.target.value || null,
                  }))
                }
              >
                <MenuItem value="">Bez sale</MenuItem>
                {venueOptions.map((resource) => (
                  <MenuItem key={resource.id} value={resource.id}>
                    {formatResourceOption(resource)}
                  </MenuItem>
                ))}
              </Select>
            </FormControl>

            <FormControl fullWidth disabled={isReadOnly}>
              <InputLabel id="planning-speakers-label">Predavači</InputLabel>
              <Select
                multiple
                labelId="planning-speakers-label"
                label="Predavači"
                value={selection.speakerIds}
                renderValue={(selected) =>
                  selected
                    .map((speakerId) => findResource(resources, speakerId)?.name)
                    .filter(Boolean)
                    .join(', ')
                }
                onChange={(event) =>
                  setSelection((current) => ({
                    ...current,
                    speakerIds:
                      typeof event.target.value === 'string'
                        ? event.target.value.split(',')
                        : event.target.value,
                  }))
                }
              >
                {speakerOptions.map((resource) => (
                  <MenuItem key={resource.id} value={resource.id}>
                    <Checkbox
                      checked={selection.speakerIds.includes(resource.id)}
                    />
                    <ListItemText primary={formatResourceOption(resource)} />
                  </MenuItem>
                ))}
              </Select>
            </FormControl>

            {eventItem.requiresEquipment && (
              <FormControl fullWidth disabled={isReadOnly}>
                <InputLabel id="planning-equipment-label">
                  Paket opreme
                </InputLabel>
                <Select
                  labelId="planning-equipment-label"
                  label="Paket opreme"
                  value={selection.equipmentPackageId ?? ''}
                  onChange={(event) =>
                    setSelection((current) => ({
                      ...current,
                      equipmentPackageId: event.target.value || null,
                    }))
                  }
                >
                  <MenuItem value="">Bez paketa</MenuItem>
                  {equipmentOptions.map((resource) => (
                    <MenuItem key={resource.id} value={resource.id}>
                      {formatResourceOption(resource)}
                    </MenuItem>
                  ))}
                </Select>
              </FormControl>
            )}
          </Box>

          <Divider />

          <Box
            sx={{
              display: 'grid',
              gap: 2,
              gridTemplateColumns: { xs: '1fr', md: 'repeat(4, 1fr)' },
            }}
          >
            <ResourceSummary
              title="Sala"
              resource={findResource(resources, selection.venueId)}
            />
            <Stack spacing={0.5}>
              <Typography variant="body2" color="text.secondary">
                Predavači
              </Typography>
              <Typography sx={{ fontWeight: 650 }}>
                {selection.speakerIds.length > 0
                  ? `${selection.speakerIds.length} izabrano`
                  : 'Nije izabrano'}
              </Typography>
              <Typography variant="body2" color="text.secondary">
                {selection.speakerIds
                  .map((speakerId) => findResource(resources, speakerId)?.name)
                  .filter(Boolean)
                  .join(', ') || '-'}
              </Typography>
            </Stack>
            <ResourceSummary
              title="Paket opreme"
              resource={findResource(resources, selection.equipmentPackageId)}
            />
            <Stack spacing={0.5}>
              <Typography variant="body2" color="text.secondary">
                Ukupno
              </Typography>
              <Typography sx={{ fontWeight: 650 }}>
                {formatMoney(selectedCost)}
              </Typography>
              <Typography variant="body2" color="text.secondary">
                {selectedResources.length} resursa
              </Typography>
            </Stack>
          </Box>

          <Stack direction="row" spacing={1} sx={{ flexWrap: 'wrap' }}>
            <Button
              variant="contained"
              startIcon={<SaveRoundedIcon />}
              disabled={!canSaveDraft}
              loading={saveDraftMutation.isPending}
              onClick={() => saveDraftMutation.mutate()}
            >
              Sačuvaj draft
            </Button>
            <Button
              startIcon={<SendRoundedIcon />}
              disabled={!canSubmit}
              loading={submitMutation.isPending}
              onClick={() => submitMutation.mutate()}
            >
              Podnesi zahtev
            </Button>
            <Button
              startIcon={<UndoRoundedIcon />}
              disabled={!canWithdraw}
              loading={withdrawMutation.isPending}
              onClick={() => withdrawMutation.mutate()}
            >
              Povuci zahtev
            </Button>
            <Button
              startIcon={<UndoRoundedIcon />}
              disabled={!canRevise}
              loading={reviseMutation.isPending}
              onClick={() => reviseMutation.mutate()}
            >
              Revidiraj
            </Button>
            <Button
              variant="contained"
              color="success"
              startIcon={<PublishRoundedIcon />}
              disabled={!canPublish}
              loading={publishMutation.isPending}
              onClick={() => publishMutation.mutate()}
            >
              Objavi događaj
            </Button>
          </Stack>
        </Stack>
      </Paper>

      <Paper variant="outlined" sx={{ p: 3 }}>
        <Stack spacing={2}>
          <Stack
            direction={{ xs: 'column', md: 'row' }}
            spacing={2}
            sx={{ justifyContent: 'space-between' }}
          >
            <Stack spacing={0.5}>
              <Typography component="h2" variant="h6">
                Preporuka
              </Typography>
              <Typography color="text.secondary">
                Sistem predlaže kombinaciju resursa po uslovima događaja.
              </Typography>
            </Stack>
            <Button
              startIcon={<AutoAwesomeRoundedIcon />}
              loading={recommendationMutation.isPending}
              onClick={() => recommendationMutation.mutate()}
            >
              Prikaži preporuku
            </Button>
          </Stack>

          {recommendation && recommendation.isSuccessful && (
            <Alert severity="success">
              <Stack spacing={1}>
                <Typography>
                  Ukupno: {formatMoney(recommendation.totalCost)} · kvalitet{' '}
                  {recommendation.totalQualityScore}
                </Typography>
                <Typography variant="body2">
                  {[
                    recommendation.venue?.name,
                    ...recommendation.speakers.map((speaker) => speaker.name),
                    recommendation.equipmentPackage?.name,
                  ]
                    .filter(Boolean)
                    .join(' · ')}
                </Typography>
                <Button
                  variant="outlined"
                  sx={{ alignSelf: 'flex-start' }}
                  disabled={isReadOnly}
                  onClick={() =>
                    setSelection(getRecommendationSelection(recommendation))
                  }
                >
                  Primeni preporuku
                </Button>
              </Stack>
            </Alert>
          )}

          {recommendation && !recommendation.isSuccessful && (
            <Alert severity="warning">
              <Stack spacing={0.5}>
                <Typography>Nema izvodljive preporuke.</Typography>
                {recommendation.failureReasons.map((reason) => (
                  <Typography key={reason} variant="body2">
                    {reason}
                  </Typography>
                ))}
              </Stack>
            </Alert>
          )}
        </Stack>
      </Paper>
    </Stack>
  )
}
