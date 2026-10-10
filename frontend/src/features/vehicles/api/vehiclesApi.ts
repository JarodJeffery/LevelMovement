import { request } from '../../../shared/api/client';
import type { BookingEligibilityDto, UpsertVehicleDto, UpsertVehicleResponseDto, VehicleDetailsDto, VehicleSummaryDto } from '../dtos/vehicleDtos';
export const vehiclesApi = {
  list: () => request<VehicleSummaryDto[]>('/vehicles'),
  details: (registration: string) => request<VehicleDetailsDto>(`/vehicles/${encodeURIComponent(registration)}`),
  save: (vehicle: UpsertVehicleDto) => request<UpsertVehicleResponseDto>('/vehicles', { method: 'POST', body: JSON.stringify(vehicle) }),
  delete: (registration: string, concurrencyStamp: string) => request<void>(`/vehicles/${encodeURIComponent(registration)}`, { method: 'DELETE', body: JSON.stringify({ concurrencyStamp }) }),
  booking: (registrationNumber: string) => request<BookingEligibilityDto>('/bookings/validate', { method: 'POST', body: JSON.stringify({ registrationNumber }) }),
};
