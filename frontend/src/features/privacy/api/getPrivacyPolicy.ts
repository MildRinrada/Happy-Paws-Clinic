import { publicApi } from '@/lib/api/server-client';
import type { PublicPrivacyPolicy } from '../types/privacy-policy.types';

/**
 * The published privacy notice (docs/contract-extensions.md § 35). Public, and kept for a minute:
 * the API marks both reads cacheable for that long, and publishing in the console revalidates the
 * page at once.
 */
const KEEP_SECONDS = 60;

/** `GET /privacy-policy`: the current notice, or `published: false` when there is none yet. */
export function getCurrentPrivacyPolicy(): Promise<PublicPrivacyPolicy> {
  return publicApi().get<PublicPrivacyPolicy>('/privacy-policy', { revalidate: KEEP_SECONDS });
}

/** `GET /privacy-policy/versions/{label}`: a published version, current or older. A draft is a 404. */
export function getPrivacyPolicyVersion(label: string): Promise<PublicPrivacyPolicy> {
  return publicApi().get<PublicPrivacyPolicy>(`/privacy-policy/versions/${encodeURIComponent(label)}`, {
    revalidate: KEEP_SECONDS,
  });
}
