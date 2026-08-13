import { zodResolver } from '@hookform/resolvers/zod'
import {
  Button,
  Checkbox,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  FormControlLabel,
  Stack,
  TextField,
} from '@mui/material'
import { Controller, useForm } from 'react-hook-form'
import { useEffect } from 'react'
import {
  eventFormSchema,
  type EventFormInputValues,
  type EventFormSchemaValues,
} from './eventSchemas'
import type { EventFormValues, EventItem } from './types'

interface EventFormDialogProps {
  open: boolean
  eventItem: EventItem | null
  isSubmitting: boolean
  onClose: () => void
  onSubmit: (values: EventFormValues) => void
}

const defaultValues: EventFormInputValues = {
  title: '',
  description: '',
  startsAtUtc: '',
  endsAtUtc: '',
  capacity: 80,
  budget: 1000,
  area: '',
  requiredSpeakerCount: 1,
  requiresEquipment: false,
}

function toDateTimeLocal(value: string) {
  const date = new Date(value)
  const offset = date.getTimezoneOffset()
  const localDate = new Date(date.getTime() - offset * 60_000)
  return localDate.toISOString().slice(0, 16)
}

function toPayload(values: EventFormSchemaValues): EventFormValues {
  return {
    ...values,
    title: values.title.trim(),
    description: values.description.trim(),
    startsAtUtc: new Date(values.startsAtUtc).toISOString(),
    endsAtUtc: new Date(values.endsAtUtc).toISOString(),
    area: values.area.trim(),
  }
}

function getFormValues(eventItem: EventItem | null): EventFormInputValues {
  if (!eventItem) {
    return defaultValues
  }

  return {
    title: eventItem.title,
    description: eventItem.description,
    startsAtUtc: toDateTimeLocal(eventItem.startsAtUtc),
    endsAtUtc: toDateTimeLocal(eventItem.endsAtUtc),
    capacity: eventItem.capacity,
    budget: eventItem.budget,
    area: eventItem.area,
    requiredSpeakerCount: eventItem.requiredSpeakerCount,
    requiresEquipment: eventItem.requiresEquipment,
  }
}

export function EventFormDialog({
  open,
  eventItem,
  isSubmitting,
  onClose,
  onSubmit,
}: EventFormDialogProps) {
  const {
    control,
    formState: { errors },
    handleSubmit,
    register,
    reset,
  } = useForm<EventFormInputValues, unknown, EventFormSchemaValues>({
    resolver: zodResolver(eventFormSchema),
    defaultValues,
  })

  useEffect(() => {
    if (open) {
      reset(getFormValues(eventItem))
    }
  }, [eventItem, open, reset])

  return (
    <Dialog open={open} onClose={onClose} fullWidth maxWidth="md">
      <DialogTitle>
        {eventItem ? 'Izmeni događaj' : 'Novi događaj'}
      </DialogTitle>
      <DialogContent>
        <Stack
          component="form"
          id="event-form"
          spacing={2}
          sx={{ pt: 1 }}
          onSubmit={handleSubmit((values) => onSubmit(toPayload(values)))}
        >
          <TextField
            label="Naziv"
            fullWidth
            {...register('title')}
            error={Boolean(errors.title)}
            helperText={errors.title?.message}
          />
          <TextField
            label="Opis"
            fullWidth
            multiline
            minRows={3}
            {...register('description')}
            error={Boolean(errors.description)}
            helperText={errors.description?.message}
          />
          <Stack direction={{ xs: 'column', md: 'row' }} spacing={2}>
            <TextField
              label="Početak"
              type="datetime-local"
              fullWidth
              slotProps={{ inputLabel: { shrink: true } }}
              {...register('startsAtUtc')}
              error={Boolean(errors.startsAtUtc)}
              helperText={errors.startsAtUtc?.message}
            />
            <TextField
              label="Kraj"
              type="datetime-local"
              fullWidth
              slotProps={{ inputLabel: { shrink: true } }}
              {...register('endsAtUtc')}
              error={Boolean(errors.endsAtUtc)}
              helperText={errors.endsAtUtc?.message}
            />
          </Stack>
          <Stack direction={{ xs: 'column', md: 'row' }} spacing={2}>
            <TextField
              label="Kapacitet"
              type="number"
              fullWidth
              {...register('capacity')}
              error={Boolean(errors.capacity)}
              helperText={errors.capacity?.message}
            />
            <TextField
              label="Budžet"
              type="number"
              fullWidth
              {...register('budget')}
              error={Boolean(errors.budget)}
              helperText={errors.budget?.message}
            />
          </Stack>
          <Stack direction={{ xs: 'column', md: 'row' }} spacing={2}>
            <TextField
              label="Oblast"
              fullWidth
              {...register('area')}
              error={Boolean(errors.area)}
              helperText={errors.area?.message}
            />
            <TextField
              label="Broj predavača"
              type="number"
              fullWidth
              {...register('requiredSpeakerCount')}
              error={Boolean(errors.requiredSpeakerCount)}
              helperText={errors.requiredSpeakerCount?.message}
            />
          </Stack>
          <Controller
            control={control}
            name="requiresEquipment"
            render={({ field }) => (
              <FormControlLabel
                control={
                  <Checkbox
                    checked={field.value}
                    onChange={(event) => field.onChange(event.target.checked)}
                  />
                }
                label="Potrebna oprema"
              />
            )}
          />
        </Stack>
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>Odustani</Button>
        <Button
          type="submit"
          form="event-form"
          variant="contained"
          loading={isSubmitting}
        >
          Sačuvaj
        </Button>
      </DialogActions>
    </Dialog>
  )
}
