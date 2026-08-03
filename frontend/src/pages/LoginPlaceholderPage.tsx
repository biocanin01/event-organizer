import ArrowBackRoundedIcon from '@mui/icons-material/ArrowBackRounded'
import { Box, Button, Paper, Stack, Typography } from '@mui/material'
import { Link } from 'react-router'
import { BrandMark } from '../shared/components/BrandMark'

export function LoginPlaceholderPage() {
  return (
    <Box
      sx={{ minHeight: '100vh', display: 'grid', placeItems: 'center', p: 2 }}
    >
      <Paper variant="outlined" sx={{ width: '100%', maxWidth: 440, p: 4 }}>
        <Stack spacing={3}>
          <BrandMark />
          <Stack spacing={1}>
            <Typography component="h1" variant="h4">
              Prijava na nalog
            </Typography>
            <Typography color="text.secondary">
              Forma za prijavu biće povezana sa postojećim backend auth tokom u
              sledećem issue-u.
            </Typography>
          </Stack>
          <Button
            component={Link}
            to="/"
            startIcon={<ArrowBackRoundedIcon />}
            sx={{ alignSelf: 'flex-start' }}
          >
            Nazad na početnu
          </Button>
        </Stack>
      </Paper>
    </Box>
  )
}
