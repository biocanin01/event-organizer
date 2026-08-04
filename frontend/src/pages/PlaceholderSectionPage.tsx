import { Paper, Stack, Typography } from '@mui/material'

interface PlaceholderSectionPageProps {
  title: string
  description: string
}

export function PlaceholderSectionPage({
  title,
  description,
}: PlaceholderSectionPageProps) {
  return (
    <Stack spacing={3}>
      <Stack spacing={0.75}>
        <Typography component="h1" variant="h4">
          {title}
        </Typography>
        <Typography color="text.secondary">{description}</Typography>
      </Stack>
      <Paper variant="outlined" sx={{ p: 3 }}>
        <Typography color="text.secondary">
          Ova stranica je deo aplikacione navigacije i bice implementirana kroz
          odgovarajuci domain issue.
        </Typography>
      </Paper>
    </Stack>
  )
}
