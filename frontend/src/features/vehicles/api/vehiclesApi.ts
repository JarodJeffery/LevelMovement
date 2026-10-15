import { z } from 'zod';
import { ApiError } from '../../../shared/api/client';
import { request } from '../../../shared/api/client';
import { normalizeRegistration } from '../models/registrationNumber';
import type { BookingEligibilityDto, UpsertVehicleDto, UpsertVehicleResponseDto, VehicleDetailsDto, VehicleSummaryDto } from '../dtos/vehicleDtos';
const summarySchema = z.object({ registrationNumber: z.string().min(1), make: z.string(), model: z.string(), year: z.number().int(), dailyRate: z.number().finite(), createdDate: z.string().refine(value => !Number.isNaN(Date.parse(value))) });
const detailsSchema = summarySchema.extend({ concurrencyStamp: z.string().min(1) });
const saveSchema = z.object({ registrationNumber: z.string().min(1), make: z.string(), model: z.string(), message: z.string(), concurrencyStamp: z.string().min(1) });
const bookingSchema = z.object({ registrationNumber: z.string().min(1), eligible: z.boolean(), message: z.string() });
async function validated<T>(schema: z.ZodType<T>, response: Promise<unknown>): Promise<T> {
  const result = schema.safeParse(await response);
  if (!result.success) throw new ApiError(502, { detail: 'The API returned incomplete vehicle details. Please try again.' });
  return result.data;
}
export const vehiclesApi = {
  list: () => validated<VehicleSummaryDto[]>(z.array(summarySchema), request('/vehicles')),
  details: (registration: string) => validated<VehicleDetailsDto>(detailsSchema, request(`/vehicles/${encodeURIComponent(normalizeRegistration(registration))}`)),
  save: (vehicle: UpsertVehicleDto) => validated<UpsertVehicleResponseDto>(saveSchema, request('/vehicles', { method: 'POST', body: JSON.stringify({ ...vehicle, registrationNumber: normalizeRegistration(vehicle.registrationNumber) }) })),
  delete: (registration: string, concurrencyStamp: string) => request<void>(`/vehicles/${encodeURIComponent(normalizeRegistration(registration))}`, { method: 'DELETE', body: JSON.stringify({ concurrencyStamp }) }),
  booking: (registrationNumber: string) => validated<BookingEligibilityDto>(bookingSchema, request('/bookings/validate', { method: 'POST', body: JSON.stringify({ registrationNumber: normalizeRegistration(registrationNumber) }) })),
};
