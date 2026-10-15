import { useEffect, useRef, useState } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { useNavigate } from 'react-router-dom';
import { vehiclesApi } from '../api/vehiclesApi';
import { vehicleKeys } from '../models/vehicle';
import { formatRegistration, formatRegistrationMessage } from '../models/registrationNumber';
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
    if (details.data && !initialized.current) { form.reset({ ...details.data, registrationNumber: formatRegistration(details.data.registrationNumber) }); formStamp.current = details.data.concurrencyStamp; initialized.current = true; }
  }, [details.data, form]);
  const save = useMutation({
    mutationFn: (values: VehicleForm) => {
      if (registration && !formStamp.current) throw new ApiError(400, { detail: 'Load the latest vehicle details before saving.' });
      return vehiclesApi.save({ ...values, concurrencyStamp: registration ? formStamp.current : undefined });
    },
    onSuccess: async (response, values) => {
      setMessage(formatRegistrationMessage(response.message, response.registrationNumber));
      formStamp.current = response.concurrencyStamp;
      if (registration && details.data) client.setQueryData(vehicleKeys.details(registration), { ...details.data, ...values, concurrencyStamp: response.concurrencyStamp });
      await client.invalidateQueries({ queryKey: vehicleKeys.list });
      if (registration) {
        const refreshed = await details.refetch();
        if (refreshed.data && !refreshed.isError) { form.reset({ ...refreshed.data, registrationNumber: formatRegistration(refreshed.data.registrationNumber) }); formStamp.current = refreshed.data.concurrencyStamp; }
      }
      if (!registration) navigate(`/vehicles/${encodeURIComponent(response.registrationNumber)}`, { state: { message: formatRegistrationMessage(response.message, response.registrationNumber) } });
    },
    onError: error => {
      if (error instanceof ApiError && error.problem.errors) {
        let first = true;
        for (const [key, messages] of Object.entries(error.problem.errors)) {
          const field = key.replace(/^\$\./, '').replace(/^./, character => character.toLowerCase());
          if (Object.hasOwn(form.getValues(), field)) {
            form.setError(field as keyof VehicleForm, { type: 'server', message: messages.join(' ') }, { shouldFocus: first });
            first = false;
          }
        }
      } else if (!registration && error instanceof ApiError && error.status === 409 && error.problem.title === 'Registration number already exists.') {
        form.setError('registrationNumber', { type: 'server', message: error.message }, { shouldFocus: true });
      }
    },
  });
  const remove = useMutation({
    mutationFn: () => {
      if (!registration || !formStamp.current) throw new ApiError(400, { detail: 'Load the latest vehicle details before deleting.' });
      return vehiclesApi.delete(registration, formStamp.current);
    },
    onSuccess: async () => {
      client.removeQueries({ queryKey: vehicleKeys.details(registration!) });
      await client.invalidateQueries({ queryKey: vehicleKeys.list });
      navigate('/vehicles', { state: { message: `Deleted vehicle ${formatRegistration(registration!)}. You can check its booking eligibility below.` } });
    },
  });
  const error = save.error ?? remove.error;
  const reload = async () => {
    const result = await details.refetch();
    if (result.data && !result.isError) { form.reset({ ...result.data, registrationNumber: formatRegistration(result.data.registrationNumber) }); formStamp.current = result.data.concurrencyStamp; save.reset(); remove.reset(); setMessage('Latest vehicle details loaded.'); setConfirmDelete(false); }
  };
  return { form, details, save, remove, error, reload, message, confirmDelete, setConfirmDelete, submit: form.handleSubmit(values => { setMessage(''); save.mutate(values); }), conflict: !!registration && error instanceof ApiError && error.status === 409 };
}
