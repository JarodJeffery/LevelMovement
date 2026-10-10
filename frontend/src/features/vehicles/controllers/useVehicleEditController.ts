import { useEffect, useRef, useState } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { useNavigate } from 'react-router-dom';
import { vehiclesApi } from '../api/vehiclesApi';
import { vehicleKeys } from '../models/vehicle';
import { vehicleSchema } from '../validation/vehicleSchema';
import type { VehicleForm } from '../validation/vehicleSchema';
import { ApiError } from '../../../shared/api/client';
export function useVehicleEditController(registration?: string) {
  const client = useQueryClient();
  const navigate = useNavigate();
  const [message, setMessage] = useState('');
  const [confirmDelete, setConfirmDelete] = useState(false);
  const initialized = useRef(false);
  const formStamp = useRef<string | undefined>(undefined);
  const form = useForm<VehicleForm>({ resolver: zodResolver(vehicleSchema), defaultValues: { registrationNumber: '', make: '', model: '', year: new Date().getFullYear(), dailyRate: 0 } });
  const details = useQuery({ queryKey: vehicleKeys.details(registration ?? ''), queryFn: () => vehiclesApi.details(registration!), enabled: !!registration, refetchOnWindowFocus: false });
  useEffect(() => {
    if (details.data && !initialized.current) { form.reset(details.data); formStamp.current = details.data.concurrencyStamp; initialized.current = true; }
  }, [details.data, form]);
  const save = useMutation({
    mutationFn: (values: VehicleForm) => vehiclesApi.save({ ...values, concurrencyStamp: registration ? formStamp.current : undefined }),
    onSuccess: async (response, values) => {
      setMessage(response.message);
      formStamp.current = response.concurrencyStamp;
      if (registration && details.data) client.setQueryData(vehicleKeys.details(registration), { ...details.data, ...values, concurrencyStamp: response.concurrencyStamp });
      await client.invalidateQueries({ queryKey: vehicleKeys.list });
      if (registration) {
        const refreshed = await details.refetch();
        if (refreshed.data && !refreshed.isError) { form.reset(refreshed.data); formStamp.current = refreshed.data.concurrencyStamp; }
      }
      if (!registration) navigate(`/vehicles/${encodeURIComponent(response.registrationNumber)}`, { state: { message: response.message } });
    },
    onError: error => {
      if (error instanceof ApiError && error.problem.errors) {
        for (const [key, messages] of Object.entries(error.problem.errors)) {
          if (key in form.getValues()) form.setError(key as keyof VehicleForm, { message: messages.join(' ') });
        }
      }
    },
  });
  const remove = useMutation({
    mutationFn: () => vehiclesApi.delete(registration!, formStamp.current!),
    onSuccess: async () => {
      client.removeQueries({ queryKey: vehicleKeys.details(registration!) });
      await client.invalidateQueries({ queryKey: vehicleKeys.list });
      navigate('/vehicles', { state: { message: `Deleted vehicle ${registration}. You can check its booking eligibility below.` } });
    },
  });
  const error = save.error ?? remove.error;
  const reload = async () => {
    const result = await details.refetch();
    if (result.data && !result.isError) { form.reset(result.data); formStamp.current = result.data.concurrencyStamp; save.reset(); remove.reset(); setMessage('Latest vehicle details loaded.'); setConfirmDelete(false); }
  };
  return { form, details, save, remove, error, reload, message, confirmDelete, setConfirmDelete, submit: form.handleSubmit(values => { setMessage(''); save.mutate(values); }), conflict: !!registration && error instanceof ApiError && error.status === 409 };
}
