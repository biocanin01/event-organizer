import SearchRoundedIcon from '@mui/icons-material/SearchRounded'
import {
  Button,
  FormControl,
  InputLabel,
  MenuItem,
  Paper,
  Select,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  TextField,
  Typography,
} from '@mui/material'
import { useQuery } from '@tanstack/react-query'
import { useMemo, useState } from 'react'
import { StatusChip } from '../../shared/components/StatusChip'
import { formatDateTime } from '../../shared/format/dateTime'
import { applicationRoles, type ApplicationRole } from '../auth/types'
import { useAuthenticatedRequest } from '../auth/useAuthenticatedRequest'
import { useAuth } from '../auth/useAuth'
import { UserDetailsDialog } from './UserDetailsDialog'
import { listUsers } from './usersApi'
import type { UserStatus, UserSummary } from './types'

const userStatuses: UserStatus[] = [
  'Active',
  'Suspended',
  'PendingVerification',
  'Deleted',
]

const roles: ApplicationRole[] = [
  applicationRoles.participant,
  applicationRoles.organizer,
  applicationRoles.admin,
]

export function AdminUsersPage() {
  const { session } = useAuth()
  const authenticatedRequest = useAuthenticatedRequest()
  const [searchInput, setSearchInput] = useState('')
  const [search, setSearch] = useState('')
  const [status, setStatus] = useState<UserStatus | ''>('')
  const [role, setRole] = useState<ApplicationRole | ''>('')
  const [selectedUser, setSelectedUser] = useState<UserSummary | null>(null)

  const filters = useMemo(
    () => ({
      search,
      status,
      role,
    }),
    [search, status, role],
  )

  const { data: users = [], isLoading } = useQuery({
    queryKey: ['admin-users', filters],
    queryFn: () => listUsers(authenticatedRequest, filters),
    enabled: Boolean(session?.accessToken),
  })

  return (
    <Stack spacing={3}>
      <Stack spacing={0.75}>
        <Typography component="h1" variant="h4">
          Korisnici
        </Typography>
        <Typography color="text.secondary">
          Pregled korisnika, rola, statusa i osnovne aktivnosti u sistemu.
        </Typography>
      </Stack>

      <Paper variant="outlined" sx={{ p: 2 }}>
        <Stack
          direction={{ xs: 'column', md: 'row' }}
          spacing={2}
          component="form"
          onSubmit={(event) => {
            event.preventDefault()
            setSearch(searchInput)
          }}
        >
          <TextField
            label="Pretraga"
            value={searchInput}
            onChange={(event) => setSearchInput(event.target.value)}
            sx={{ flexGrow: 1 }}
          />
          <FormControl sx={{ minWidth: 180 }}>
            <InputLabel id="user-status-filter-label">Status</InputLabel>
            <Select
              labelId="user-status-filter-label"
              label="Status"
              value={status}
              onChange={(event) => setStatus(event.target.value as UserStatus | '')}
            >
              <MenuItem value="">Svi statusi</MenuItem>
              {userStatuses.map((statusOption) => (
                <MenuItem key={statusOption} value={statusOption}>
                  {statusOption}
                </MenuItem>
              ))}
            </Select>
          </FormControl>
          <FormControl sx={{ minWidth: 170 }}>
            <InputLabel id="user-role-filter-label">Rola</InputLabel>
            <Select
              labelId="user-role-filter-label"
              label="Rola"
              value={role}
              onChange={(event) =>
                setRole(event.target.value as ApplicationRole | '')
              }
            >
              <MenuItem value="">Sve role</MenuItem>
              {roles.map((roleOption) => (
                <MenuItem key={roleOption} value={roleOption}>
                  {roleOption}
                </MenuItem>
              ))}
            </Select>
          </FormControl>
          <Button
            type="submit"
            variant="contained"
            startIcon={<SearchRoundedIcon />}
          >
            Pretraži
          </Button>
        </Stack>
      </Paper>

      <TableContainer component={Paper} variant="outlined">
        <Table>
          <TableHead>
            <TableRow>
              <TableCell>Korisnik</TableCell>
              <TableCell>Role</TableCell>
              <TableCell>Status</TableCell>
              <TableCell>Kreiran</TableCell>
              <TableCell align="right">Akcije</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {isLoading && (
              <TableRow>
                <TableCell colSpan={5}>Učitavanje korisnika...</TableCell>
              </TableRow>
            )}
            {!isLoading && users.length === 0 && (
              <TableRow>
                <TableCell colSpan={5}>Nema korisnika za izabrane filtere.</TableCell>
              </TableRow>
            )}
            {users.map((user) => (
              <TableRow key={user.id} hover>
                <TableCell>
                  <Stack spacing={0.25}>
                    <Typography sx={{ fontWeight: 650 }}>{user.fullName}</Typography>
                    <Typography variant="body2" color="text.secondary">
                      {user.email}
                    </Typography>
                  </Stack>
                </TableCell>
                <TableCell>
                  <Stack direction="row" spacing={0.75} sx={{ flexWrap: 'wrap' }}>
                    {user.roles.map((roleName) => (
                      <StatusChip key={roleName} status={roleName} />
                    ))}
                  </Stack>
                </TableCell>
                <TableCell>
                  <StatusChip status={user.status} />
                </TableCell>
                <TableCell>{formatDateTime(user.createdAtUtc)}</TableCell>
                <TableCell align="right">
                  <Button size="small" onClick={() => setSelectedUser(user)}>
                    Detalji
                  </Button>
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </TableContainer>

      <UserDetailsDialog
        user={selectedUser}
        onClose={() => setSelectedUser(null)}
      />
    </Stack>
  )
}
