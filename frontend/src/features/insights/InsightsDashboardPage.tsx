import InsightsRoundedIcon from '@mui/icons-material/InsightsRounded'
import {
  Alert,
  Box,
  Button,
  LinearProgress,
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
import { useQuery } from '@tanstack/react-query'
import { useMemo, useState } from 'react'
import { ApiError } from '../../api/ApiError'
import { StatusChip } from '../../shared/components/StatusChip'
import { formatDateTime } from '../../shared/format/dateTime'
import { useAuthenticatedRequest } from '../auth/useAuthenticatedRequest'
import { getEventInsightById, listEventInsights } from './insightsApi'
import type { EventInsightSummary } from './types'

function getErrorMessage(error: unknown) {
  return error instanceof ApiError
    ? error.message
    : 'Izveštaji trenutno nisu dostupni.'
}

function formatRating(value: number | null) {
  return value === null ? '-' : value.toFixed(1)
}

function getTotalRegistrationCount(insight: EventInsightSummary) {
  return (
    insight.pendingRegistrationCount +
    insight.confirmedRegistrationCount +
    insight.rejectedRegistrationCount +
    insight.cancelledRegistrationCount
  )
}

export function InsightsDashboardPage() {
  const authenticatedRequest = useAuthenticatedRequest()
  const [selectedEventId, setSelectedEventId] = useState<string | null>(null)
  const insightsQuery = useQuery({
    queryKey: ['insights', 'events'],
    queryFn: () => listEventInsights(authenticatedRequest),
  })
  const selectedInsight = useMemo(() => {
    if (!insightsQuery.data || insightsQuery.data.length === 0) {
      return null
    }

    return (
      insightsQuery.data.find((insight) => insight.eventId === selectedEventId) ??
      insightsQuery.data[0]
    )
  }, [insightsQuery.data, selectedEventId])
  const detailsQuery = useQuery({
    queryKey: ['insights', 'events', selectedInsight?.eventId],
    queryFn: () =>
      getEventInsightById(authenticatedRequest, selectedInsight!.eventId),
    enabled: Boolean(selectedInsight),
  })

  return (
    <Stack spacing={3}>
      <Stack spacing={0.75}>
        <Typography component="h1" variant="h4">
          Izveštaji
        </Typography>
        <Typography color="text.secondary">
          Pregled prijava, popunjenosti kapaciteta i recenzija po događaju.
        </Typography>
      </Stack>

      {(insightsQuery.error || detailsQuery.error) && (
        <Alert severity="error">
          {getErrorMessage(insightsQuery.error ?? detailsQuery.error)}
        </Alert>
      )}

      <TableContainer component={Paper} variant="outlined">
        <Table>
          <TableHead>
            <TableRow>
              <TableCell>Događaj</TableCell>
              <TableCell>Status</TableCell>
              <TableCell>Prijave</TableCell>
              <TableCell>Popunjenost</TableCell>
              <TableCell>Ocena</TableCell>
              <TableCell align="right">Detalj</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {insightsQuery.isLoading && (
              <TableRow>
                <TableCell colSpan={6}>Učitavanje izveštaja...</TableCell>
              </TableRow>
            )}
            {!insightsQuery.isLoading &&
              (insightsQuery.data?.length ?? 0) === 0 && (
                <TableRow>
                  <TableCell colSpan={6}>Nema događaja za izveštaje.</TableCell>
                </TableRow>
              )}
            {insightsQuery.data?.map((insight) => (
              <TableRow
                key={insight.eventId}
                hover
                selected={insight.eventId === selectedInsight?.eventId}
              >
                <TableCell>
                  <Stack spacing={0.25}>
                    <Typography sx={{ fontWeight: 650 }}>
                      {insight.eventTitle}
                    </Typography>
                    <Typography variant="body2" color="text.secondary">
                      {formatDateTime(insight.startsAtUtc)}
                    </Typography>
                  </Stack>
                </TableCell>
                <TableCell>
                  <StatusChip status={insight.status} />
                </TableCell>
                <TableCell>
                  <Stack spacing={0.25}>
                    <Typography>{getTotalRegistrationCount(insight)} ukupno</Typography>
                    <Typography variant="body2" color="text.secondary">
                      {insight.confirmedRegistrationCount} potvrđeno
                    </Typography>
                  </Stack>
                </TableCell>
                <TableCell sx={{ minWidth: 160 }}>
                  <Stack spacing={0.75}>
                    <Typography>{insight.capacityFillPercentage}%</Typography>
                    <LinearProgress
                      variant="determinate"
                      value={Math.min(insight.capacityFillPercentage, 100)}
                    />
                  </Stack>
                </TableCell>
                <TableCell>
                  <Stack spacing={0.25}>
                    <Typography>{formatRating(insight.averageRating)}</Typography>
                    <Typography variant="body2" color="text.secondary">
                      {insight.reviewCount} recenzija
                    </Typography>
                  </Stack>
                </TableCell>
                <TableCell align="right">
                  <Button
                    size="small"
                    startIcon={<InsightsRoundedIcon />}
                    onClick={() => setSelectedEventId(insight.eventId)}
                  >
                    Prikaži
                  </Button>
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </TableContainer>

      {selectedInsight && (
        <Paper variant="outlined" sx={{ p: { xs: 2, md: 3 } }}>
          {detailsQuery.isLoading || !detailsQuery.data ? (
            <Typography color="text.secondary">Učitavanje detalja...</Typography>
          ) : (
            <Stack spacing={3}>
              <Stack spacing={0.75}>
                <Typography component="h2" variant="h5">
                  {detailsQuery.data.eventTitle}
                </Typography>
                <Typography color="text.secondary">
                  {formatDateTime(detailsQuery.data.startsAtUtc)} -{' '}
                  {formatDateTime(detailsQuery.data.endsAtUtc)}
                </Typography>
              </Stack>

              <Box
                sx={{
                  display: 'grid',
                  gap: 2,
                  gridTemplateColumns: {
                    xs: '1fr',
                    sm: 'repeat(2, 1fr)',
                    lg: 'repeat(4, 1fr)',
                  },
                }}
              >
                <Metric label="Na čekanju" value={detailsQuery.data.pendingRegistrationCount} />
                <Metric label="Potvrđeno" value={detailsQuery.data.confirmedRegistrationCount} />
                <Metric label="Odbijeno" value={detailsQuery.data.rejectedRegistrationCount} />
                <Metric label="Otkazano" value={detailsQuery.data.cancelledRegistrationCount} />
              </Box>

              <Box
                sx={{
                  display: 'grid',
                  gap: 3,
                  gridTemplateColumns: { xs: '1fr', md: '1fr 1fr' },
                }}
              >
                <Stack spacing={1.5}>
                  <Typography variant="h6">Raspodela ocena</Typography>
                  {detailsQuery.data.ratingDistribution.map((item) => (
                    <Stack
                      key={item.rating}
                      direction="row"
                      spacing={1.5}
                      sx={{ alignItems: 'center' }}
                    >
                      <Rating value={item.rating} readOnly size="small" />
                      <Box sx={{ flexGrow: 1 }}>
                        <LinearProgress
                          variant="determinate"
                          value={
                            detailsQuery.data.reviewCount === 0
                              ? 0
                              : (item.count * 100) / detailsQuery.data.reviewCount
                          }
                        />
                      </Box>
                      <Typography sx={{ width: 32, textAlign: 'right' }}>
                        {item.count}
                      </Typography>
                    </Stack>
                  ))}
                </Stack>

                <Stack spacing={1.5}>
                  <Typography variant="h6">Poslednje recenzije</Typography>
                  {detailsQuery.data.recentReviews.length === 0 ? (
                    <Typography color="text.secondary">
                      Još nema recenzija za ovaj događaj.
                    </Typography>
                  ) : (
                    detailsQuery.data.recentReviews.map((review) => (
                      <Stack key={review.id} spacing={0.5}>
                        <Stack
                          direction={{ xs: 'column', sm: 'row' }}
                          spacing={1}
                          sx={{ justifyContent: 'space-between' }}
                        >
                          <Typography sx={{ fontWeight: 650 }}>
                            {review.participantName}
                          </Typography>
                          <Typography variant="body2" color="text.secondary">
                            {formatDateTime(review.updatedAtUtc ?? review.createdAtUtc)}
                          </Typography>
                        </Stack>
                        <Rating value={review.rating} readOnly size="small" />
                        <Typography>{review.comment}</Typography>
                      </Stack>
                    ))
                  )}
                </Stack>
              </Box>
            </Stack>
          )}
        </Paper>
      )}
    </Stack>
  )
}

function Metric({ label, value }: { label: string; value: number }) {
  return (
    <Paper variant="outlined" sx={{ p: 2 }}>
      <Stack spacing={0.35}>
        <Typography variant="body2" color="text.secondary">
          {label}
        </Typography>
        <Typography variant="h5">{value}</Typography>
      </Stack>
    </Paper>
  )
}
