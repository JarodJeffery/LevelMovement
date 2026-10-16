import type { VehicleDetailsDto, VehicleSummaryDto } from '../dtos/vehicleDtos';
import { normalizeRegistration } from './registrationNumber';
export type Vehicle = VehicleSummaryDto;
export type EditableVehicle = VehicleDetailsDto;
export const vehicleKeys = {
  list: ['vehicles'] as const,
  details: (registration: string) => ['vehicle', normalizeRegistration(registration)] as const,
};
