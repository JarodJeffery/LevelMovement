import { useQuery } from '@tanstack/react-query';
import { vehiclesApi } from '../api/vehiclesApi';
import { vehicleKeys } from '../models/vehicle';
export function useVehicleListController() {
  return useQuery({ queryKey: vehicleKeys.list, queryFn: vehiclesApi.list });
}
