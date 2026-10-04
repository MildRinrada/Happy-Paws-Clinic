import { oneTimeCodeDigits } from '@/lib/auth/one-time-code';
import { registerPage } from '../content/register-page';

/**
 * Field-by-field problems with a sign-up form. Keys are the API's snake_case field names, so a
 * problem found here and a refusal from the backend land on the same field. `confirm` is the one
 * field the backend never sees.
 */
export type RegisterErrors = Partial<Record<'phone' | 'code' | 'display_name' | 'email' | 'password' | 'confirm', string>> & {
  general?: string;
};

export interface RegisterForm {
  code: string;
  displayName: string;
  email: string;
  password: string;
  confirm: string;
}

/** Checks what can be checked without the backend, so a typo costs no round trip or code attempt. */
export function checkRegistration(form: RegisterForm): RegisterErrors {
  const text = registerPage.errors;
  const errors: RegisterErrors = {};

  if (!/^\d{6}$/.test(oneTimeCodeDigits(form.code))) errors.code = text.badCode;

  const name = form.displayName.trim();
  if (!name) errors.display_name = text.missingName;
  else if (name.length > 200) errors.display_name = text.nameTooLong;

  const email = form.email.trim();
  if (email && !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email)) errors.email = text.badEmail;

  if (form.password.length < 8) errors.password = text.passwordTooShort;
  else if (form.password.length > 256) errors.password = text.passwordTooLong;
  else if (form.password !== form.confirm) errors.confirm = text.mismatch;

  return errors;
}
