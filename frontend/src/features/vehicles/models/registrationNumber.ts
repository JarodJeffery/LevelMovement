export function normalizeRegistration(value: string): string {
  return value.replace(/\p{White_Space}/gu, '').toUpperCase();
}

export function formatRegistration(value: string): string {
  const normalized = normalizeRegistration(value);
  return normalized.replace(/^([A-Z]{2})(\d{2})([A-Z]{2})(GP)$/, '$1 $2 $3 $4');
}

export function formatRegistrationMessage(message: string, registration: string): string {
  return message.replace(registration, formatRegistration(registration));
}
