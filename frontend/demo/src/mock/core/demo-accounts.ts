/**
 * The accounts the demo bar signs in as, and the sign-in details the guide prints. Their user ids
 * are fixed so every module's seed can hang its rows on them: the staff module gives each staff
 * account its membership, the customers module gives the customer account its record and pets.
 */

export const DEMO_PASSWORD = 'demo1234';

/** Every second-factor code is this one in the demo; the guide and the demo bar show it. */
export const DEMO_TOTP = '123456';

export interface DemoAccount {
  key: 'owner' | 'vet' | 'reception' | 'customer';
  user_id: string;
  display_name: string;
  email?: string;
  phone?: string;
  /** Staff role names, for the staff module's seed. */
  roles?: string[];
  two_factor: boolean;
  label: string;
}

export const DEMO_ACCOUNTS: Record<DemoAccount['key'], DemoAccount> = {
  owner: {
    key: 'owner',
    user_id: '7a1d0001-0000-4000-8000-000000000001',
    display_name: 'สพ.ญ. ใจดี รักสัตว์',
    email: 'owner@happypaws.test',
    phone: '0890000001',
    roles: ['clinic_owner'],
    two_factor: true,
    label: 'เจ้าของคลินิก',
  },
  vet: {
    key: 'vet',
    user_id: '7a1d0001-0000-4000-8000-000000000002',
    display_name: 'สพ.ญ. มินท์ ศรีสุข',
    email: 'mint@staff.happypaws.test',
    phone: '0890000002',
    roles: ['veterinarian'],
    two_factor: true,
    label: 'สัตวแพทย์',
  },
  reception: {
    key: 'reception',
    user_id: '7a1d0001-0000-4000-8000-000000000003',
    display_name: 'ศิริพร มีสุข',
    email: 'siriporn@staff.happypaws.test',
    phone: '0890000003',
    roles: ['receptionist'],
    two_factor: false,
    label: 'พนักงานต้อนรับ',
  },
  customer: {
    key: 'customer',
    user_id: '7a1d0001-0000-4000-8000-000000000004',
    display_name: 'คุณนภา ใจงาม',
    email: 'napa@example.com',
    phone: '0812345678',
    two_factor: false,
    label: 'ลูกค้า (เจ้าของสัตว์)',
  },
};
