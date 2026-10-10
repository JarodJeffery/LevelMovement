import type { VehicleDetailsDto, VehicleSummaryDto } from '../dtos/vehicleDtos';
export type Vehicle = VehicleSummaryDto;
export type EditableVehicle = VehicleDetailsDto;
export const vehicleKeys = {
  list: ['vehicles'] as const,
  details: (registration: string) => ['vehicle', registration] as const,
};
