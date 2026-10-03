import type { ActionError } from '@/lib/actions/action-result';
import { fieldErrors } from '@/lib/actions/field-errors';
import { portalAccount } from '../content/portal-account';

/**
 * The line to show for a refused save.
 *
 * An edit clash gets a plain sentence. A `validation_failed` carries only a general message at the
 * top ("ข้อมูลที่ส่งมาไม่ถูกต้อง") and the useful one per field in `details.errors`, so the field's
 * own message is preferred when there is one. Anything else is the backend's own words.
 */
export function refusalMessage(error: ActionError): string {
  if (error.code === 'version_conflict') {
    return portalAccount.conflict;
  }

  return (error.field && fieldErrors(error)[error.field]) || error.message;
}
