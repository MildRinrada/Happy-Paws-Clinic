import type { ActionError } from './action-result';

/**
 * A refusal spread over the fields it is about, one message each.
 *
 * A `validation_failed` carries a general sentence at the top ("ข้อมูลที่ส่งมาไม่ถูกต้อง") and the
 * useful ones per field in `details.errors`. Any other refusal that names a field (`409
 * duplicate_customer` names `phone`) puts its own message there. Field names are the API's
 * snake_case ones, so a form looks up `errors.display_name`, not its own state names.
 */
export function fieldErrors(error: ActionError): Record<string, string> {
  const result: Record<string, string> = {};
  const perField = error.details?.errors;

  if (perField && typeof perField === 'object') {
    for (const [field, messages] of Object.entries(perField as Record<string, unknown>)) {
      if (Array.isArray(messages) && typeof messages[0] === 'string') {
        result[field] = messages[0];
      }
    }
  }

  if (error.field && !result[error.field]) {
    result[error.field] = error.message;
  }

  return result;
}
