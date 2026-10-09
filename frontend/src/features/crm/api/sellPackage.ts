import { staffApi } from '@/lib/api/server-client';
import type { PackageSale, SellPackageInput } from '../types/crm.types';

/**
 * `POST /staff/packages`. Needs `crm:manage` and an `Idempotency-Key`. Raises the sale bill in the
 * same step; the package becomes usable once that bill is paid.
 */
export function sellPackage(input: SellPackageInput, idempotencyKey: string): Promise<PackageSale> {
  return staffApi().post<PackageSale>('/staff/packages', { idempotencyKey, body: input });
}
