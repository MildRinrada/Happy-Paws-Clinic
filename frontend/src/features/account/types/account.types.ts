/** `GET /me` — one account, and every membership it holds in this clinic. */
export interface Me {
  user_id: string;
  display_name: string;
  status: string;
  memberships: Membership[];
  two_factor: { enabled: boolean; method: string; recovery_codes_left?: number };
  email?: string | null;
  /** Digits only. */
  phone?: string | null;
  language?: 'th' | 'en';
  /** Which channels the account wants messages on. Consent to receive them is separate. */
  notifications?: { email: boolean; sms: boolean; line: boolean };
  /** For `If-Match` on `PATCH /me`. */
  version?: number;
}

/** The message channels on `GET /me` and `PATCH /me`. */
export type NotificationChannels = NonNullable<Me['notifications']>;

/**
 * `POST /me/2fa/enroll`. The first call (with the password) answers `confirmation_required` with
 * the secret; the second (with a code) answers `enabled` with the recovery codes, shown this once.
 */
export interface TwoFactorEnrolment {
  status: 'confirmation_required' | 'enabled';
  secret?: string;
  otp_auth_uri?: string;
  recovery_codes?: string[];
}

/** What the browser is given to scan: the secret as text and as a QR image. Never the raw URI. */
export interface AuthenticatorSecret {
  secret: string;
  qrDataUrl: string;
}

/** One signed-in device — `GET /me/sessions`. */
export interface AccountSession {
  id: string;
  user_agent?: string | null;
  ip_address?: string | null;
  created_at: string;
  last_used_at: string;
  expires_at: string;
  current: boolean;
}

/**
 * One membership. An account can hold a staff one and a customer one at once — the contract is
 * explicit that a vet may also be a pet owner — so this is a list on the account, not a role field.
 */
export interface Membership {
  type: 'staff' | 'customer';
  staff_id?: string;
  staff_type?: string;
  license_no?: string;
  customer_id?: string;
  roles: string[];
  branches: string[];
  /** Effective scopes, wildcards unexpanded: `*`, `queue:*`, `queue:read`. */
  scopes: string[];
}
