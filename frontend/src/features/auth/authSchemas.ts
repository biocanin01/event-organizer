import { z } from 'zod'

export const loginSchema = z.object({
  email: z.email('Unesite ispravnu email adresu.'),
  password: z.string().min(1, 'Lozinka je obavezna.'),
})

export const registerSchema = z.object({
  fullName: z
    .string()
    .trim()
    .min(2, 'Ime i prezime moraju imati najmanje 2 karaktera.')
    .max(150, 'Ime i prezime mogu imati najvise 150 karaktera.'),
  email: z.email('Unesite ispravnu email adresu.'),
  password: z
    .string()
    .min(8, 'Lozinka mora imati najmanje 8 karaktera.')
    .max(100, 'Lozinka moze imati najvise 100 karaktera.'),
})

export type LoginFormValues = z.infer<typeof loginSchema>
export type RegisterFormValues = z.infer<typeof registerSchema>
