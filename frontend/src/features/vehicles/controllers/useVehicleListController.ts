import { useEffect } from 'react';
import { useQuery } from '@tanstack/react-query';
import { useLocation, useSearchParams } from 'react-router-dom';
import { vehiclesApi } from '../api/vehiclesApi';
import { vehicleKeys } from '../models/vehicle';
export function useVehicleListController() {
  const [params, setParams] = useSearchParams();
  const location = useLocation();
  const rawPage = params.get('pageNumber') ?? '';
  const rawSize = params.get('pageSize') ?? '';
  const pageNumber = /^\d+$/.test(rawPage) && Number(rawPage) > 0 && Number(rawPage) <= 2147483647 ? Number(rawPage) : 1;
  const pageSize = /^\d+$/.test(rawSize) && Number(rawSize) >= 1 && Number(rawSize) <= 100 ? Number(rawSize) : 10;
  const query = useQuery({ queryKey: vehicleKeys.list(pageNumber, pageSize),
    queryFn: ({ signal }) => vehiclesApi.list(pageNumber, pageSize, signal) });
  const setPage = (page: number, size = pageSize, replace = false) => {
    const next = new URLSearchParams(params);
    next.set('pageNumber', String(page));
    next.set('pageSize', String(size));
    setParams(next, { replace, state: location.state });
  };
  const totalPages = query.data?.totalPages ?? 0;
  const lastPage = query.data ? Math.max(1, totalPages) : undefined;
  useEffect(() => {
    if (lastPage !== undefined && pageNumber > lastPage) {
      const next = new URLSearchParams(params);
      next.set('pageNumber', String(lastPage));
      next.set('pageSize', String(pageSize));
      setParams(next, { replace: true, state: location.state });
    }
  }, [lastPage, pageNumber, pageSize, params, setParams, location.state]);
  return { ...query, pageNumber, pageSize, totalPages, setPage, setPageSize: (size: number) => setPage(1, size) };
}
