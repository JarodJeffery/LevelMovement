import type { VehicleDetailsDto, VehicleSummaryDto } from '../dtos/vehicleDtos';
import { normalizeRegistration } from './registrationNumber';
export type Vehicle = VehicleSummaryDto;
export type EditableVehicle = VehicleDetailsDto;
export const vehicleKeys = {
  lists: ['vehicles'] as const,
  list: (pageNumber: number, pageSize: number) => ['vehicles', pageNumber, pageSize] as const,
  details: (registration: string) => ['vehicle', normalizeRegistration(registration)] as const,
};

export function vehicleListReturnTo(state: unknown): string {
  const returnTo = (state as { returnTo?: unknown } | null)?.returnTo;
  return typeof returnTo === 'string' && /^\/vehicles(?:\?[^#]*)?$/.test(returnTo) ? returnTo : '/vehicles';
}
