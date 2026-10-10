import type { StaffPhotoType } from '../types/staff.types';

/** The same limits the API keeps (docs/contract-extensions.md § 32): checked here first to save a trip. */
export const PHOTO_TYPES: readonly StaffPhotoType[] = ['image/jpeg', 'image/png', 'image/webp'];
export const PHOTO_MAX_BYTES = 5 * 1024 * 1024;

/** What an uploaded photo's `photo_path` starts with, in place of a site path's `/`. */
const UPLOADED_MARKER = 'storage:';

export function photoTypeOf(file: File): StaffPhotoType | undefined {
  const type = file.type.toLowerCase();
  return PHOTO_TYPES.find((allowed) => allowed === type);
}

/** True when the value names an uploaded photo rather than a file under the site's /public. */
export function isUploadedPhoto(photoPath: string): boolean {
  return photoPath.startsWith(UPLOADED_MARKER);
}
