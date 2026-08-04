import { zodResolver } from '@hookform/resolvers/zod'
import HowToRegRoundedIcon from '@mui/icons-material/HowToRegRounded'
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
import { Link, useNavigate } from 'react-router'
import { ApiError } from '../api/ApiError'
import {
  registerSchema,
  type RegisterFormValues,
} from '../features/auth/authSchemas'
import { useAuth } from '../features/auth/useAuth'
import { BrandMark } from '../shared/components/BrandMark'

export function RegisterPage() {
  const { register: registerUser } = useAuth()
  const navigate = useNavigate()
  const [error, setError] = useState<string | null>(null)

  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<RegisterFormValues>({
    resolver: zodResolver(registerSchema),
    defaultValues: {
      fullName: '',
      email: '',
      password: '',
    },
  })

  const onSubmit = handleSubmit(async (values) => {
    setError(null)

    try {
      await registerUser(values)
      navigate('/dashboard', { replace: true })
    } catch (requestError) {
      setError(
        requestError instanceof ApiError
          ? requestError.message
          : 'Registracija trenutno nije uspela.',
      )
    }
  })

  return (
    <Box sx={{ minHeight: '100vh', display: 'grid', placeItems: 'center', p: 2 }}>
      <Paper variant="outlined" sx={{ width: '100%', maxWidth: 480, p: 4 }}>
        <Stack spacing={3}>
          <Stack spacing={1}>
            <BrandMark />
            <Typography component="h1" variant="h4">
              Kreiranje naloga
            </Typography>
            <Typography color="text.secondary">
              Novi korisnici pocinju kao Participant.
            </Typography>
          </Stack>

          {error && <Alert severity="error">{error}</Alert>}

          <Stack component="form" spacing={2.25} onSubmit={onSubmit}>
            <TextField
              label="Ime i prezime"
              autoComplete="name"
              {...register('fullName')}
              error={Boolean(errors.fullName)}
              helperText={errors.fullName?.message}
            />
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
              autoComplete="new-password"
              {...register('password')}
              error={Boolean(errors.password)}
              helperText={errors.password?.message}
            />
            <Button
              type="submit"
              variant="contained"
              size="large"
              loading={isSubmitting}
              startIcon={<HowToRegRoundedIcon />}
            >
              Registruj se
            </Button>
          </Stack>

          <Typography color="text.secondary">
            Vec imate nalog?{' '}
            <MuiLink component={Link} to="/login">
              Prijavite se
            </MuiLink>
          </Typography>
        </Stack>
      </Paper>
    </Box>
  )
}
