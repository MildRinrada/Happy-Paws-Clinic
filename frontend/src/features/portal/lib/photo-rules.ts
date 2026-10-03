/**
 * What the backend accepts as a pet photo (`PetDocument`): these image types, up to 10 MB.
 * Checked here first so the owner hears about a wrong file before anything is sent.
 */

export const PHOTO_TYPES = ['image/jpeg', 'image/png', 'image/webp', 'image/heic'] as const;

export const PHOTO_MAX_BYTES = 10 * 1024 * 1024;

export type PhotoType = (typeof PHOTO_TYPES)[number];

export function isPhotoType(type: string): type is PhotoType {
  return (PHOTO_TYPES as readonly string[]).includes(type.toLowerCase());
}
