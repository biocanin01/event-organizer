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
            Dobrodošli, {session?.user.fullName}. Izaberite opciju iz navigacije da biste nastavili.
        </Typography>
      </Stack>
      <Paper variant="outlined" sx={{ p: 3 }}>
        <Typography color="text.secondary">
            U navigaciji možete pronaći događaje, prijave, recenzije i ostale dostupne opcije.
        </Typography>
      </Paper>
      <ParticipantOrganizerRequestPanel />
    </Stack>
  )
}
