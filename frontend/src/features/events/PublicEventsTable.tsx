import ArrowForwardRoundedIcon from '@mui/icons-material/ArrowForwardRounded'
import {
  Alert,
  Button,
  Paper,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Typography,
} from '@mui/material'
import { Link } from 'react-router'
import { formatDateTime } from '../../shared/format/dateTime'
import { getAvailableSpots } from './eventAvailability'
import type { EventItem } from './types'

export function PublicEventsTable({
  events,
  errorMessage,
  isLoading,
}: {
  events: EventItem[]
  errorMessage?: string | null
  isLoading: boolean
}) {
  return (
    <Stack spacing={2}>
      {errorMessage && <Alert severity="error">{errorMessage}</Alert>}
      <TableContainer component={Paper} variant="outlined">
        <Table>
          <TableHead>
            <TableRow>
              <TableCell>Događaj</TableCell>
              <TableCell>Termin</TableCell>
              <TableCell>Kapacitet</TableCell>
              <TableCell align="right">Akcija</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {isLoading && (
              <TableRow>
                <TableCell colSpan={4}>Učitavanje događaja...</TableCell>
              </TableRow>
            )}
            {!isLoading && events.length === 0 && (
              <TableRow>
                <TableCell colSpan={4}>
                  Trenutno nema predstojećih objavljenih događaja.
                </TableCell>
              </TableRow>
            )}
            {events.map((eventItem) => {
              const availableSpots = getAvailableSpots(eventItem)

              return (
                <TableRow key={eventItem.id} hover>
                  <TableCell>
                    <Stack spacing={0.35}>
                      <Typography sx={{ fontWeight: 650 }}>
                        {eventItem.title}
                      </Typography>
                      <Typography variant="body2" color="text.secondary">
                        {eventItem.area}
                      </Typography>
                    </Stack>
                  </TableCell>
                  <TableCell>
                    <Stack spacing={0.25}>
                      <Typography variant="body2">
                        {formatDateTime(eventItem.startsAtUtc)}
                      </Typography>
                      <Typography variant="body2" color="text.secondary">
                        do {formatDateTime(eventItem.endsAtUtc)}
                      </Typography>
                    </Stack>
                  </TableCell>
                  <TableCell>
                    <Typography variant="body2">
                      {availableSpots} od {eventItem.capacity} slobodno
                    </Typography>
                  </TableCell>
                  <TableCell align="right">
                    <Button
                      component={Link}
                      to={`/discover/${eventItem.id}`}
                      size="small"
                      endIcon={<ArrowForwardRoundedIcon />}
                    >
                      Detalji
                    </Button>
                  </TableCell>
                </TableRow>
              )
            })}
          </TableBody>
        </Table>
      </TableContainer>
    </Stack>
  )
}
