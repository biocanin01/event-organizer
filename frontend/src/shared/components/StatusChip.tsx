import { Chip, type ChipProps } from '@mui/material'

const statusColors: Record<string, ChipProps['color']> = {
  Active: 'success',
  Approved: 'success',
  Pending: 'warning',
  PendingVerification: 'warning',
  Rejected: 'error',
  Suspended: 'error',
  Withdrawn: 'default',
  Deleted: 'default',
}

const statusLabels: Record<string, string> = {
  Active: 'Aktivan',
  Approved: 'Odobren',
  Pending: 'Na cekanju',
  PendingVerification: 'Ceka verifikaciju',
  Rejected: 'Odbijen',
  Suspended: 'Suspendovan',
  Withdrawn: 'Povucen',
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
