import { Box, Container, Stack, Typography } from '@mui/material'
import { useQuery } from '@tanstack/react-query'
import { apiRequest } from '../../api/apiClient'
import { ApiError } from '../../api/ApiError'
import { listPublishedEvents } from './eventsApi'
import { PublicEventsHeader } from './PublicEventsHeader'
import { PublicEventsTable } from './PublicEventsTable'

function getErrorMessage(error: unknown) {
  return error instanceof ApiError
    ? error.message
    : 'Događaji trenutno nisu dostupni.'
}

export function EventDiscoveryPage() {
  const { data: events = [], error, isLoading } = useQuery({
    queryKey: ['events', 'published'],
    queryFn: () => listPublishedEvents(apiRequest),
  })

  return (
    <Box sx={{ minHeight: '100vh' }}>
      <PublicEventsHeader />
      <Container component="main" maxWidth="lg" sx={{ py: { xs: 4, md: 6 } }}>
        <Stack spacing={3}>
          <Stack spacing={0.75}>
            <Typography component="h1" variant="h4">
              Predstojeći događaji
            </Typography>
            <Typography color="text.secondary">
              Pregledajte objavljene događaje i pronađite onaj koji vam odgovara.
            </Typography>
          </Stack>
          <PublicEventsTable
            events={events}
            errorMessage={error ? getErrorMessage(error) : null}
            isLoading={isLoading}
          />
        </Stack>
      </Container>
    </Box>
  )
}
