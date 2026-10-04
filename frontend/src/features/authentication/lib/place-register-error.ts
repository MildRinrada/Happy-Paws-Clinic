import type { ActionError } from '@/lib/actions/action-result';
import { fieldErrors } from '@/lib/actions/field-errors';
import type { RegisterErrors } from './check-registration';

/** The fields the second step of sign-up shows. The phone is not one: it was typed on step one. */
const DETAIL_FIELDS = ['code', 'display_name', 'email', 'password'] as const;

/**
 * Puts a refusal from sign-up on the field it is about. Anything that names no field on the form
 * (a number that already has an account, too many requests, the phone itself) goes at the top, in
 * the backend's own words.
 */
export function placeRegisterError(error: ActionError): RegisterErrors {
  const perField = fieldErrors(error);
  const placed: RegisterErrors = {};

  for (const field of DETAIL_FIELDS) {
    if (perField[field]) placed[field] = perField[field];
  }

  if (Object.keys(placed).length === 0) {
    placed.general = perField.phone ?? error.message;
  }

  return placed;
}
