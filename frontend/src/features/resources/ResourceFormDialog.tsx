import { zodResolver } from '@hookform/resolvers/zod'
import {
  Button,
  Checkbox,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  FormControl,
  FormControlLabel,
  InputLabel,
  MenuItem,
  Select,
  Stack,
  TextField,
} from '@mui/material'
import { Controller, useForm, useWatch } from 'react-hook-form'
import { useEffect } from 'react'
import { resourceTypeLabels, resourceTypes } from './resourceLabels'
import {
  defaultResourceType,
  nullableNumber,
  nullableText,
  resourceFormSchema,
  type ResourceFormInputValues,
  type ResourceFormSchemaValues,
} from './resourceSchemas'
import type { ResourceFormValues, ResourceItem, ResourceType } from './types'

interface ResourceFormDialogProps {
  open: boolean
  resource: ResourceItem | null
  isSubmitting: boolean
  onClose: () => void
  onSubmit: (values: ResourceFormValues) => void
}

const defaultValues: ResourceFormInputValues = {
  name: '',
  description: '',
  type: 'Venue',
  cost: 0,
  qualityScore: 3,
  capacity: null,
  expertiseArea: null,
  providerName: null,
  supportedCapacity: null,
  serviceArea: null,
  includesTechnicalSupport: false,
  contentsSummary: null,
}

function getFormValues(resource: ResourceItem | null): ResourceFormInputValues {
  if (!resource) {
    return defaultValues
  }

  return {
    name: resource.name,
    description: resource.description,
    type: resource.type,
    cost: resource.cost,
    qualityScore: resource.qualityScore,
    capacity: nullableNumber(resource.capacity),
    expertiseArea: nullableText(resource.expertiseArea),
    providerName: nullableText(resource.providerName),
    supportedCapacity: nullableNumber(resource.supportedCapacity),
    serviceArea: nullableText(resource.serviceArea),
    includesTechnicalSupport: resource.includesTechnicalSupport ?? false,
    contentsSummary: nullableText(resource.contentsSummary),
  }
}

function trimOrNull(value: string | null) {
  const trimmed = value?.trim() ?? ''
  return trimmed.length > 0 ? trimmed : null
}

function toPayload(values: ResourceFormSchemaValues): ResourceFormValues {
  const type = defaultResourceType(values.type)

  return {
    name: values.name.trim(),
    description: values.description.trim(),
    type,
    cost: values.cost,
    qualityScore: values.qualityScore,
    capacity: type === 'Venue' ? values.capacity : null,
    expertiseArea: type === 'Speaker' ? trimOrNull(values.expertiseArea) : null,
    providerName:
      type === 'EquipmentPackage' ? trimOrNull(values.providerName) : null,
    supportedCapacity:
      type === 'EquipmentPackage' ? values.supportedCapacity : null,
    serviceArea:
      type === 'EquipmentPackage' ? trimOrNull(values.serviceArea) : null,
    includesTechnicalSupport:
      type === 'EquipmentPackage' ? values.includesTechnicalSupport : null,
    contentsSummary:
      type === 'EquipmentPackage' ? trimOrNull(values.contentsSummary) : null,
  }
}

export function ResourceFormDialog({
  open,
  resource,
  isSubmitting,
  onClose,
  onSubmit,
}: ResourceFormDialogProps) {
  const {
    control,
    formState: { errors },
    handleSubmit,
    register,
    reset,
  } = useForm<ResourceFormInputValues, unknown, ResourceFormSchemaValues>({
    resolver: zodResolver(resourceFormSchema),
    defaultValues,
  })

  const selectedType = useWatch({ control, name: 'type' }) as ResourceType

  useEffect(() => {
    if (open) {
      reset(getFormValues(resource))
    }
  }, [open, reset, resource])

  return (
    <Dialog open={open} onClose={onClose} fullWidth maxWidth="md">
      <DialogTitle>
        {resource ? 'Izmeni resurs' : 'Novi resurs'}
      </DialogTitle>
      <DialogContent>
        <Stack
          component="form"
          id="resource-form"
          spacing={2}
          sx={{ pt: 1 }}
          onSubmit={handleSubmit((values) => onSubmit(toPayload(values)))}
        >
          <Controller
            control={control}
            name="type"
            render={({ field }) => (
              <FormControl fullWidth disabled={resource !== null}>
                <InputLabel id="resource-type-label">Tip</InputLabel>
                <Select
                  {...field}
                  labelId="resource-type-label"
                  label="Tip"
                >
                  {resourceTypes.map((type) => (
                    <MenuItem key={type} value={type}>
                      {resourceTypeLabels[type]}
                    </MenuItem>
                  ))}
                </Select>
              </FormControl>
            )}
          />

          <TextField
            label="Naziv"
            fullWidth
            {...register('name')}
            error={Boolean(errors.name)}
            helperText={errors.name?.message}
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
              label="Cena"
              type="number"
              fullWidth
              {...register('cost')}
              error={Boolean(errors.cost)}
              helperText={errors.cost?.message}
            />
            <TextField
              label="Ocena kvaliteta"
              type="number"
              fullWidth
              slotProps={{ htmlInput: { min: 1, max: 5 } }}
              {...register('qualityScore')}
              error={Boolean(errors.qualityScore)}
              helperText={errors.qualityScore?.message}
            />
          </Stack>

          {selectedType === 'Venue' && (
            <TextField
              label="Kapacitet"
              type="number"
              fullWidth
              {...register('capacity')}
              error={Boolean(errors.capacity)}
              helperText={errors.capacity?.message}
            />
          )}

          {selectedType === 'Speaker' && (
            <TextField
              label="Oblast ekspertize"
              fullWidth
              {...register('expertiseArea')}
              error={Boolean(errors.expertiseArea)}
              helperText={errors.expertiseArea?.message}
            />
          )}

          {selectedType === 'EquipmentPackage' && (
            <>
              <Stack direction={{ xs: 'column', md: 'row' }} spacing={2}>
                <TextField
                  label="Dobavljač"
                  fullWidth
                  {...register('providerName')}
                  error={Boolean(errors.providerName)}
                  helperText={errors.providerName?.message}
                />
                <TextField
                  label="Podržani kapacitet"
                  type="number"
                  fullWidth
                  {...register('supportedCapacity')}
                  error={Boolean(errors.supportedCapacity)}
                  helperText={errors.supportedCapacity?.message}
                />
              </Stack>
              <TextField
                label="Service area"
                fullWidth
                {...register('serviceArea')}
                error={Boolean(errors.serviceArea)}
                helperText={errors.serviceArea?.message}
              />
              <Controller
                control={control}
                name="includesTechnicalSupport"
                render={({ field }) => (
                  <FormControlLabel
                    control={
                      <Checkbox
                        checked={Boolean(field.value)}
                        onChange={(event) =>
                          field.onChange(event.target.checked)
                        }
                      />
                    }
                    label="Uključuje tehničku podršku"
                  />
                )}
              />
              <TextField
                label="Sadržaj paketa"
                fullWidth
                multiline
                minRows={3}
                {...register('contentsSummary')}
                error={Boolean(errors.contentsSummary)}
                helperText={errors.contentsSummary?.message}
              />
            </>
          )}
        </Stack>
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>Odustani</Button>
        <Button
          type="submit"
          form="resource-form"
          variant="contained"
          loading={isSubmitting}
        >
          Sačuvaj
        </Button>
      </DialogActions>
    </Dialog>
  )
}
