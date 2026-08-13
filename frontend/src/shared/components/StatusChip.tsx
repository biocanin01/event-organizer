import { Chip, type ChipProps } from '@mui/material'

const statusColors: Record<string, ChipProps['color']> = {
  Active: 'success',
  Approved: 'success',
  Completed: 'success',
  Draft: 'default',
  Published: 'success',
  Pending: 'warning',
  PendingVerification: 'warning',
  Rejected: 'error',
  Cancelled: 'error',
  Suspended: 'error',
  Withdrawn: 'default',
  Deleted: 'default',
}

const statusLabels: Record<string, string> = {
  Active: 'Aktivan',
  Approved: 'Odobren',
  Cancelled: 'Otkazan',
  Completed: 'Završen',
  Draft: 'Draft',
  Published: 'Objavljen',
  Pending: 'Na čekanju',
  PendingVerification: 'Čeka verifikaciju',
  Rejected: 'Odbijen',
  Suspended: 'Suspendovan',
  Withdrawn: 'Povučen',
  Deleted: 'Obrisan',
}

interface StatusChipProps {
  status: string
  size?: ChipProps['size']
}

export function StatusChip({ status, size = 'small' }: StatusChipProps) {
  return (
    <Chip
      label={statusLabels[status] ?? status}
      color={statusColors[status] ?? 'default'}
      size={size}
      variant="outlined"
    />
  )
}
