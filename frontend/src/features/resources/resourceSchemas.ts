import { z } from 'zod'
import type { ResourceType } from './types'

const resourceTypeSchema = z.enum(['Venue', 'Speaker', 'EquipmentPackage'])

export const resourceFormSchema = z
  .object({
    name: z
      .string()
      .trim()
      .min(1, 'Naziv je obavezan.')
      .max(200, 'Naziv može imati najviše 200 karaktera.'),
    description: z
      .string()
      .max(2000, 'Opis može imati najviše 2000 karaktera.'),
    type: resourceTypeSchema,
    cost: z.coerce
      .number()
      .min(0, 'Cena ne može biti negativna.'),
    qualityScore: z.coerce
      .number()
      .int('Ocena kvaliteta mora biti ceo broj.')
      .min(1, 'Ocena kvaliteta mora biti najmanje 1.')
      .max(5, 'Ocena kvaliteta može biti najviše 5.'),
    capacity: z.coerce.number().int().positive().nullable(),
    expertiseArea: z.string().nullable(),
    providerName: z.string().nullable(),
    supportedCapacity: z.coerce.number().int().positive().nullable(),
    serviceArea: z.string().nullable(),
    includesTechnicalSupport: z.boolean().nullable(),
    contentsSummary: z.string().nullable(),
  })
  .superRefine((value, context) => {
    if (value.type === 'Venue' && value.capacity === null) {
      context.addIssue({
        code: 'custom',
        path: ['capacity'],
        message: 'Kapacitet sale je obavezan.',
      })
    }

    if (value.type === 'Speaker' && !value.expertiseArea?.trim()) {
      context.addIssue({
        code: 'custom',
        path: ['expertiseArea'],
        message: 'Oblast ekspertize je obavezna.',
      })
    }

    if (value.type === 'EquipmentPackage') {
      if (!value.providerName?.trim()) {
        context.addIssue({
          code: 'custom',
          path: ['providerName'],
          message: 'Dobavljač je obavezan.',
        })
      }

      if (value.supportedCapacity === null) {
        context.addIssue({
          code: 'custom',
          path: ['supportedCapacity'],
          message: 'Podržani kapacitet je obavezan.',
        })
      }

      if (!value.serviceArea?.trim()) {
        context.addIssue({
          code: 'custom',
          path: ['serviceArea'],
          message: 'Service area je obavezna.',
        })
      }

      if (value.includesTechnicalSupport === null) {
        context.addIssue({
          code: 'custom',
          path: ['includesTechnicalSupport'],
          message: 'Tehnička podrška mora biti označena.',
        })
      }

      if (!value.contentsSummary?.trim()) {
        context.addIssue({
          code: 'custom',
          path: ['contentsSummary'],
          message: 'Sadržaj paketa je obavezan.',
        })
      }
    }
  })

export type ResourceFormSchemaValues = z.infer<typeof resourceFormSchema>
export type ResourceFormInputValues = z.input<typeof resourceFormSchema>

export function nullableNumber(value: number | null | undefined) {
  return value ?? null
}

export function nullableText(value: string | null | undefined) {
  return value ?? null
}

export function defaultResourceType(type: ResourceType | undefined): ResourceType {
  return type ?? 'Venue'
}
