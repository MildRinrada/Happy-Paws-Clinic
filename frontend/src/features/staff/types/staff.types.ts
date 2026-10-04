export type StaffStatus = 'active' | 'deactivated' | 'invited';

/** A member's public profile as staff edit it. Extension, docs/contract-extensions.md § 10. */
export interface StaffPublicProfile {
  job_title?: string;
  specialty?: string;
  bio?: string;
  photo_path?: string;
  career_start_year?: number;
  show_on_website: boolean;
}

/**
 * One person on the staff list — `GET /staff/members`.
 *
 * An `invited` row is an invitation nobody has accepted yet: its `id` is the invitation's
 * (`sin_…`), it has no `user_id`, and `invitation_expires_at` says how long the link has left.
 */
export interface StaffMember {
  id: string;
  version: number;
  user_id?: string;
  display_name: string;
  email?: string;
  staff_type: string;
  license_no?: string;
  /** Role names, as `GET /me` prints them. */
  roles: string[];
  /** Branch ids. Empty means not bound to any branch, which reads as "every branch". */
  branches: string[];
  status: StaffStatus;
  two_factor_enabled: boolean;
  created_at: string;
  invitation_expires_at?: string;
  /** Department code. Extension, § 10. */
  department: string;
  public_profile?: StaffPublicProfile;
}

/** One entry of `GET /staff/staff-types`: a position in use here, or one of the four suggestions. */
export interface StaffTypeOption {
  staff_type: string;
  /** Active members holding it. */
  members: number;
}

/** One entry of `GET /staff/departments`. Extension, § 10. */
export interface DepartmentOption {
  department: string;
  name: string;
  members: number;
}

/** The filters `GET /staff/members` takes, as the page carries them in its query string. */
export interface StaffListFilters {
  status?: StaffStatus;
  branch?: string;
  department?: string;
  role?: string;
  cursor?: string;
}

/** `POST /staff/members/invitations`. `roles` are role names and `branches` branch ids. */
export interface InviteStaffInput {
  email: string;
  display_name: string;
  staff_type: string;
  roles: string[];
  branches?: string[];
  license_no?: string;
}

/**
 * `PATCH /staff/members/{id}`. An absent field is left alone; `roles` and `branches` each replace
 * their whole set; an empty `license_no` clears it. Display name and email are not editable here.
 */
export interface UpdateStaffInput {
  staff_type?: string;
  roles?: string[];
  branches?: string[];
  license_no?: string;
  department?: string;
}
