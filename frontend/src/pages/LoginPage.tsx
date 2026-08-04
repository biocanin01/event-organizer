import { zodResolver } from '@hookform/resolvers/zod'
import ArrowBackRoundedIcon from '@mui/icons-material/ArrowBackRounded'
import LoginRoundedIcon from '@mui/icons-material/LoginRounded'
import {
  Alert,
  Box,
  Button,
  Link as MuiLink,
  Paper,
  Stack,
  TextField,
  Typography,
} from '@mui/material'
import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { Link, useLocation, useNavigate } from 'react-router'
import { ApiError } from '../api/ApiError'
import {
  loginSchema,
  type LoginFormValues,
} from '../features/auth/authSchemas'
import { useAuth } from '../features/auth/useAuth'
import { BrandMark } from '../shared/components/BrandMark'

export function LoginPage() {
  const { login } = useAuth()
  const navigate = useNavigate()
  const location = useLocation()
  const [error, setError] = useState<string | null>(null)
  const from = (location.state as { from?: Location } | null)?.from?.pathname
  const redirectTo = from && from !== '/login' ? from : '/dashboard'

  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<LoginFormValues>({
    resolver: zodResolver(loginSchema),
    defaultValues: {
      email: '',
      password: '',
    },
  })

  const onSubmit = handleSubmit(async (values) => {
    setError(null)

    try {
      await login(values)
      navigate(redirectTo, { replace: true })
    } catch (requestError) {
      setError(
        requestError instanceof ApiError
          ? requestError.message
          : 'Prijava trenutno nije uspela.',
      )
    }
  })

  return (
    <Box sx={{ minHeight: '100vh', display: 'grid', placeItems: 'center', p: 2 }}>
      <Paper variant="outlined" sx={{ width: '100%', maxWidth: 440, p: 4 }}>
        <Stack spacing={3}>
          <Stack spacing={1}>
            <BrandMark />
            <Typography component="h1" variant="h4">
              Prijava na nalog
            </Typography>
            <Typography color="text.secondary">
              Unesite pristupne podatke za EventOrganizer.
            </Typography>
          </Stack>

          {error && <Alert severity="error">{error}</Alert>}

          <Stack component="form" spacing={2.25} onSubmit={onSubmit}>
            <TextField
              label="Email"
              autoComplete="email"
              {...register('email')}
              error={Boolean(errors.email)}
              helperText={errors.email?.message}
            />
            <TextField
              label="Lozinka"
              type="password"
              autoComplete="current-password"
              {...register('password')}
              error={Boolean(errors.password)}
              helperText={errors.password?.message}
            />
            <Button
              type="submit"
              variant="contained"
              size="large"
              loading={isSubmitting}
              startIcon={<LoginRoundedIcon />}
            >
              Prijavi se
            </Button>
          </Stack>

          <Stack spacing={1.5}>
            <Typography color="text.secondary">
              Nemate nalog?{' '}
              <MuiLink component={Link} to="/register">
                Registrujte se
              </MuiLink>
            </Typography>
            <Button
              component={Link}
              to="/"
              variant="text"
              startIcon={<ArrowBackRoundedIcon />}
              sx={{ alignSelf: 'flex-start' }}
            >
              Nazad na pocetnu
            </Button>
          </Stack>
        </Stack>
      </Paper>
    </Box>
  )
}
