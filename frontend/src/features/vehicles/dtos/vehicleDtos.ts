export interface VehicleSummaryDto {
  registrationNumber: string;
  make: string;
  model: string;
  year: number;
  dailyRate: number;
  createdDate: string;
}
export interface VehicleDetailsDto extends VehicleSummaryDto { concurrencyStamp: string }
export interface UpsertVehicleDto {
  registrationNumber: string;
  make: string;
  model: string;
  year: number;
  dailyRate: number;
  concurrencyStamp?: string;
}
export interface UpsertVehicleResponseDto {
  registrationNumber: string;
  make: string;
  model: string;
  message: string;
  concurrencyStamp: string;
}
export interface BookingEligibilityDto { registrationNumber: string; eligible: boolean; message: string }
export interface ApiProblem { title?: string; detail?: string; errors?: Record<string, string[]> }
