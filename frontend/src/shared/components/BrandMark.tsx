import EventAvailableRoundedIcon from '@mui/icons-material/EventAvailableRounded'
import { Stack, Typography } from '@mui/material'

export function BrandMark() {
  return (
    <Stack direction="row" spacing={1.25} sx={{ alignItems: 'center' }}>
      <EventAvailableRoundedIcon color="primary" />
      <Typography sx={{ fontWeight: 750 }}>EventOrganizer</Typography>
    </Stack>
  )
}
