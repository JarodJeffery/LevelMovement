import { useState } from 'react';
import { useMutation } from '@tanstack/react-query';
import { vehiclesApi } from '../api/vehiclesApi';
export function useBookingController() {
  const [registration, setRegistration] = useState('');
  const check = useMutation({ mutationFn: () => vehiclesApi.booking(registration.trim().toUpperCase()) });
  return { registration, setRegistration, check };
}
