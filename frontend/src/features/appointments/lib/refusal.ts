import type { ActionError } from '@/lib/actions/action-result';
import { fieldErrors } from '@/lib/actions/field-errors';
import { appointmentDetail } from '../content/appointment-calendar';

/** The one line to show for a refused calendar action. */
export function refusalText(error: ActionError): string {
  if (error.code === 'version_conflict') {
    return appointmentDetail.conflict;
  }

  return (error.field && fieldErrors(error)[error.field]) || error.message;
}
