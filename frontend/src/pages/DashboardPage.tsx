import { Paper, Stack, Typography } from '@mui/material'
import { ParticipantOrganizerRequestPanel } from '../features/organizerRequests/ParticipantOrganizerRequestPanel'
import { useAuth } from '../features/auth/useAuth'

export function DashboardPage() {
  const { session } = useAuth()

  return (
    <Stack spacing={3}>
      <Stack spacing={0.75}>
        <Typography component="h1" variant="h4">
          Dashboard
        </Typography>
        <Typography color="text.secondary">
          Dobrodošli, {session?.user.fullName}. Ovde će biti pregled aktivnosti
          prema vasoj roli.
        </Typography>
      </Stack>
      <Paper variant="outlined" sx={{ p: 3 }}>
        <Typography color="text.secondary">
          Sledeći issue-i popunjavaju ove sekcije stvarnim podacima iz backend
          modula.
        </Typography>
      </Paper>
      <ParticipantOrganizerRequestPanel />
    </Stack>
  )
}
