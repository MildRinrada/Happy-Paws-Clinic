import { staffApi } from '@/lib/api/server-client';
import type { DrugOptions } from '../types/drug-option.types';

/**
 * `GET /staff/prescriptions/drug-options?q=&branch=&limit=`, a contract extension
 * (docs/contract-extensions.md § 20). Needs `rx:write`, not `inventory:read`: medicines only, with
 * the selling price and the stock status at one branch, usually the case's.
 */
export function listDrugOptions(query: { q?: string; branch?: string; limit?: number }): Promise<DrugOptions> {
  return staffApi().get<DrugOptions>('/staff/prescriptions/drug-options', {
    query: { q: query.q, branch: query.branch, limit: query.limit },
  });
}
