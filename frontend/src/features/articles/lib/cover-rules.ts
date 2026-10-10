import type { CoverType } from '../types/staff-article.types';

/** The same limits the API keeps (docs/contract-extensions.md § 26): checked here first to save a trip. */
export const COVER_TYPES: readonly CoverType[] = ['image/jpeg', 'image/png', 'image/webp'];
export const COVER_MAX_BYTES = 5 * 1024 * 1024;

export function coverTypeOf(file: File): CoverType | undefined {
  const type = file.type.toLowerCase();
  return COVER_TYPES.find((allowed) => allowed === type);
}
