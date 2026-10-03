import { staffApi } from '@/lib/api/server-client';
import type { Customer, NewCustomer } from '../types/customer.types';

/**
 * `POST /staff/customers` — "สร้างจากเคาน์เตอร์ · ตรวจซ้ำด้วยเบอร์โทร". Needs `customer:write`.
 * A number already on file answers `409 duplicate_customer` with the matches in `details.existing`.
 */
export function createCustomer(customer: NewCustomer): Promise<Customer> {
  return staffApi().post<Customer>('/staff/customers', { body: customer });
}
