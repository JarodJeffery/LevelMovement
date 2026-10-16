import { describe, expect, it } from 'vitest';
import { formatRegistration, normalizeRegistration } from './registrationNumber';
import { vehicleSchema } from '../validation/vehicleSchema';
import { vehicleKeys } from './vehicle';

describe('registration identity and presentation', () => {
  it.each(['KW00KBGP', 'KW 00 KB GP', ' kw\t00\nkb\u00a0gp ', 'KW\u008500\u202fKB\u3000GP'])('normalizes and formats %s', input => {
    expect(normalizeRegistration(input)).toBe('KW00KBGP');
    expect(formatRegistration(input)).toBe('KW 00 KB GP');
    expect(vehicleKeys.details(input)).toEqual(['vehicle', 'KW00KBGP']);
  });
  it('preserves punctuation and does not guess groupings for other plate formats', () => {
    expect(formatRegistration(' ca 123-456 ')).toBe('CA123-456');
    expect(formatRegistration('CUSTOMGP')).toBe('CUSTOMGP');
  });
  it('checks the length after normalization and rejects whitespace-only input', () => {
    const vehicle = { make: 'Toyota', model: 'Corolla', year: 2024, dailyRate: 100 };
    expect(vehicleSchema.parse({ ...vehicle, registrationNumber: Array(20).fill('a').join(' ') }).registrationNumber).toBe('A'.repeat(20));
    expect(vehicleSchema.safeParse({ ...vehicle, registrationNumber: 'A'.repeat(21) }).success).toBe(false);
    expect(vehicleSchema.safeParse({ ...vehicle, registrationNumber: ' \t\u00a0' }).success).toBe(false);
  });
});
