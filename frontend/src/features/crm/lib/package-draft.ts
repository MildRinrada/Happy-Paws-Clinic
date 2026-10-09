import { parseBahtToSatang } from '@/lib/format/money';
import type { SellPackageInput } from '../types/crm.types';
import { parseCount } from './parse-count';

/** The package terms as typed. Strings until saved, so a half-typed price is not lost. */
export interface PackageDraft {
  name: string;
  serviceIds: string[];
  totalUses: string;
  price: string;
  expiresOn: string;
  note: string;
}

export type PackageProblem = 'name' | 'services' | 'totalUses' | 'price';

export const MAX_PACKAGE_SERVICES = 20;

export const emptyPackageDraft = (): PackageDraft => ({
  name: '',
  serviceIds: [],
  totalUses: '',
  price: '',
  expiresOn: '',
  note: '',
});

/** Which terms break the API's own rules. The backend checks them again. */
export function packageProblems(draft: PackageDraft): PackageProblem[] {
  const problems: PackageProblem[] = [];
  const uses = parseCount(draft.totalUses);

  if (!draft.name.trim()) problems.push('name');
  if (draft.serviceIds.length === 0 || draft.serviceIds.length > MAX_PACKAGE_SERVICES) problems.push('services');
  if (uses === null || uses < 1 || uses > 1000) problems.push('totalUses');
  if (parseBahtToSatang(draft.price) === null) problems.push('price');

  return problems;
}

/** The request body, once `packageProblems` is empty. */
export function toSaleInput(
  draft: PackageDraft,
  target: { customerId: string; petId: string; branchId: string },
): SellPackageInput {
  return {
    customer_id: target.customerId,
    pet_id: target.petId || null,
    name: draft.name.trim(),
    service_ids: draft.serviceIds,
    total_uses: parseCount(draft.totalUses) ?? 0,
    price_satang: parseBahtToSatang(draft.price) ?? 0,
    expires_on: draft.expiresOn || null,
    branch_id: target.branchId,
    note: draft.note.trim() || null,
  };
}
