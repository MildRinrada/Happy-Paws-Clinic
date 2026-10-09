import { staffApi } from '@/lib/api/server-client';
import type { LoyaltySettings, LoyaltySettingsInput } from '../types/crm.types';

/** `GET /clinic/settings/loyalty`. Needs `settings:read`. The defaults at version 0 until saved. § 25. */
export function getLoyaltySettings(): Promise<LoyaltySettings> {
  return staffApi().get<LoyaltySettings>('/clinic/settings/loyalty');
}

/**
 * `PUT /clinic/settings/loyalty`. Needs `settings:write`. Every value is sent; the version read goes
 * back as `If-Match`. Applies from now on, never to points already given. § 25.
 */
export function replaceLoyaltySettings(input: LoyaltySettingsInput, version: number): Promise<LoyaltySettings> {
  return staffApi().put<LoyaltySettings>('/clinic/settings/loyalty', { body: input, ifMatch: version });
}
