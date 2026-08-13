import { z } from 'zod'

export const eventFormSchema = z
  .object({
    title: z
      .string()
      .trim()
      .min(1, 'Naziv događaja je obavezan.')
      .max(200, 'Naziv može imati najviše 200 karaktera.'),
    description: z
      .string()
      .max(2000, 'Opis može imati najviše 2000 karaktera.'),
    startsAtUtc: z.string().min(1, 'Početak je obavezan.'),
    endsAtUtc: z.string().min(1, 'Kraj je obavezan.'),
    capacity: z.coerce
      .number()
      .int('Kapacitet mora biti ceo broj.')
      .positive('Kapacitet mora biti pozitivan.'),
    budget: z.coerce.number().positive('Budžet mora biti pozitivan.'),
    area: z
      .string()
      .trim()
      .min(1, 'Oblast je obavezna.')
      .max(100, 'Oblast može imati najviše 100 karaktera.'),
    requiredSpeakerCount: z.coerce
      .number()
      .int('Broj predavača mora biti ceo broj.')
      .positive('Broj predavača mora biti pozitivan.'),
    requiresEquipment: z.boolean(),
  })
  .refine(
    (value) => new Date(value.endsAtUtc) > new Date(value.startsAtUtc),
    {
      message: 'Kraj mora biti posle početka.',
      path: ['endsAtUtc'],
    },
  )

export type EventFormSchemaValues = z.infer<typeof eventFormSchema>
export type EventFormInputValues = z.input<typeof eventFormSchema>
