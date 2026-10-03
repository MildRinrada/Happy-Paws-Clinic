/** One person on the staff list — `GET /staff/members`. */
export interface StaffMember {
  id: string;
  version: number;
  user_id?: string;
  display_name: string;
  email?: string;
  staff_type: string;
  license_no?: string;
  roles: string[];
  branches: string[];
  status: 'active' | 'deactivated' | 'invited';
  two_factor_enabled: boolean;
  created_at: string;
}
