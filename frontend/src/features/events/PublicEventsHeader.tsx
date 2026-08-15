import { AppBar, Button, Container, Stack, Toolbar } from '@mui/material'
import { Link } from 'react-router'
import { BrandMark } from '../../shared/components/BrandMark'
import { useAuth } from '../auth/useAuth'

export function PublicEventsHeader() {
  const { status } = useAuth()

  return (
    <AppBar
      position="static"
      color="transparent"
      elevation={0}
      sx={{ borderBottom: 1, borderColor: 'divider' }}
    >
      <Container maxWidth="lg">
        <Toolbar disableGutters sx={{ justifyContent: 'space-between' }}>
          <BrandMark />
          <Stack direction="row" spacing={0.5}>
            {status === 'authenticated' ? (
              <Button component={Link} to="/dashboard" variant="outlined">
                Nazad na aplikaciju
              </Button>
            ) : (
              <Button component={Link} to="/login" variant="outlined">
                Prijava
              </Button>
            )}
          </Stack>
        </Toolbar>
      </Container>
    </AppBar>
  )
}
