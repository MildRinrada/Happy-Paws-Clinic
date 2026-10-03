import { invitationPage } from '../../content/invitation-page';

/**
 * The one answer for every way a link can be wrong. The backend does not say which (unknown,
 * expired, used, mistyped), and this page cannot either.
 */
export function InvitationLinkInvalid() {
  const text = invitationPage.invalid;

  return (
    <div className="space-y-2 text-center">
      <h2 className="text-base font-semibold text-ink-900">{text.title}</h2>
      <p className="text-sm text-ink-600">{text.body}</p>
    </div>
  );
}
