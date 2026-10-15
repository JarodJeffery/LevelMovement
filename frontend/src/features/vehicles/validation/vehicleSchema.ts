import { z } from 'zod';
import { normalizeRegistration } from '../models/registrationNumber';
export const vehicleSchema = z.object({
  registrationNumber: z.string().transform(normalizeRegistration).pipe(z.string().min(1, 'Enter the registration number.').max(20, 'Use at most 20 characters.')),
  make: z.string().trim().min(1, 'Enter the make.').max(100),
  model: z.string().trim().min(1, 'Enter the model.').max(100),
  year: z.number({ invalid_type_error: 'Enter a four-digit year.' }).int().min(1000).max(9999),
  dailyRate: z.number({ invalid_type_error: 'Enter a daily rate.' }).finite().min(0.01, 'Daily rate must be at least R0.01.').max(9999999999999999),
});
export type VehicleForm = z.infer<typeof vehicleSchema>;
