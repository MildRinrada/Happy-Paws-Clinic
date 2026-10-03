/**
 * `POST /staff/members/invitations/{id}/accept`, which answers in one of two shapes.
 *
 * The first call (token only) answers `two_factor_setup_required` with the secret to load into an
 * authenticator app. The second (token, password, code) answers `accepted`. Neither is a session.
 */
export type AcceptInvitationResponse =
  | {
      status: 'two_factor_setup_required';
      email: string;
      two_factor: { secret: string; otp_auth_uri: string };
    }
  | {
      status: 'accepted';
      email: string;
      user_id: string;
      staff_id: string;
    };

/** What the setup step shows: the secret both as a QR code and as text to type in by hand. */
export interface AuthenticatorSetup {
  email: string;
  secret: string;
  qrDataUrl: string;
}
