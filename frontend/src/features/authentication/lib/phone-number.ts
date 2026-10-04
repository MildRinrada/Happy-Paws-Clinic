/**
 * Phone numbers as the backend reads them: the digits and nothing else, so "081-234-5678" and
 * "0812345678" are one number. Nine to fifteen digits, the backend's own bounds.
 *
 * Plain functions with no server imports, so the form can check before a round trip and the
 * server action can check again.
 */

export const PHONE_MIN_DIGITS = 9;
export const PHONE_MAX_DIGITS = 15;

export function phoneDigits(value: string): string {
  return value.replace(/\D/g, '');
}

export function isValidPhone(value: string): boolean {
  const digits = phoneDigits(value);
  return digits.length >= PHONE_MIN_DIGITS && digits.length <= PHONE_MAX_DIGITS;
}
