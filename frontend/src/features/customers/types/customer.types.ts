import type { PetSummary } from '@/features/pets/types/pet.types';

export interface Customer {
  id: string;
  version: number;
  display_name: string;
  phone: string;
  email?: string;
  has_portal_account: boolean;
  is_active: boolean;
  merged_into_customer_id?: string;
  created_at: string;
}

/** The latest portal invitation, for the counter's "have we asked them yet". */
export interface PortalInvitation {
  id: string;
  sent_to: string;
  invited_by_staff_id: string;
  sent_at: string;
  expires_at: string;
  accepted_at?: string;
  is_live: boolean;
}

/** `GET /staff/customers/{id}` — "ข้อมูลติดต่อ สัตว์ทั้งหมด ยอดค้างชำระ". */
export interface CustomerDetail {
  customer: Customer;
  pets: PetSummary[];
  outstanding_satang: number;
  invitation?: PortalInvitation;
}

/** `POST /staff/customers`. `allow_duplicate_phone` only after a `409 duplicate_customer`. */
export interface NewCustomer {
  display_name: string;
  phone: string;
  email?: string;
  allow_duplicate_phone?: boolean;
}
