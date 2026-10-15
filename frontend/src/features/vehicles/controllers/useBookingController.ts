import { useRef, useState } from 'react';
import { useMutation } from '@tanstack/react-query';
import { vehiclesApi } from '../api/vehiclesApi';
import { ApiError } from '../../../shared/api/client';
import { normalizeRegistration, formatRegistration } from '../models/registrationNumber';
export function useBookingController() {
  const [registration, setRegistration] = useState('');
  const [fieldError, setFieldError] = useState('');
  const inputRef = useRef<HTMLInputElement>(null);
  const check = useMutation({
    mutationFn: () => vehiclesApi.booking(registration),
    onError: error => {
      if (error instanceof ApiError && error.problem.errors?.registrationNumber) {
        setFieldError(error.problem.errors.registrationNumber.join(' '));
        inputRef.current?.focus();
      }
    },
  });
  const changeRegistration = (value: string) => { setRegistration(value); setFieldError(''); check.reset(); };
  const submit = () => {
    const value = normalizeRegistration(registration);
    if (!value || value.length > 20) {
      setFieldError(value ? 'Use at most 20 characters.' : 'Enter the registration number.');
      inputRef.current?.focus();
      return;
    }
    setFieldError('');
    setRegistration(formatRegistration(value));
    check.mutate();
  };
  return { registration, setRegistration: changeRegistration, check, fieldError, inputRef, submit };
}
