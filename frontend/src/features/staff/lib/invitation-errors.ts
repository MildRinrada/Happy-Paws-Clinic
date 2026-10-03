import type { ActionError } from '@/lib/actions/action-result';
import type { PasswordFormErrors } from '../components/invitation/set-password-form';
import { invitationPage } from '../content/invitation-page';

/**
 * Where a refusal from the accept endpoint belongs on the page.
 *
 * `invalid` replaces the whole page: every way the link can be wrong is the same `404`. Anything
 * else stays on the form, on the field the backend named when it named one.
 */
export function placeAcceptError(error: ActionError): 'invalid' | PasswordFormErrors {
  if (error.status === 404) {
    return 'invalid';
  }

  if (error.status === 409) {
    // `superseded`: the email gained an account by another route since the invitation was sent.
    return { general: invitationPage.errors.alreadyHasAccount };
  }

  if (error.field === 'code' || error.field === 'password') {
    return { [error.field]: error.message };
  }

  return { general: error.message };
}
