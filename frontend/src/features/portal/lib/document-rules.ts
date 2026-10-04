/**
 * What the backend accepts as an owner's file (`PetDocument`): these types, up to 10 MB, under one
 * of five kinds. Checked here first so the owner hears about a wrong file before anything is sent.
 */

export const DOCUMENT_TYPES = ['image/jpeg', 'image/png', 'image/webp', 'image/heic', 'application/pdf'] as const;

export const DOCUMENT_MAX_BYTES = 10 * 1024 * 1024;

export const DOCUMENT_KINDS = ['photo', 'lab_result', 'referral', 'imaging', 'other'] as const;

export type DocumentType = (typeof DOCUMENT_TYPES)[number];
export type DocumentKind = (typeof DOCUMENT_KINDS)[number];

/** The file input's `accept`: the types, plus the extensions a phone may not label with a type. */
export const DOCUMENT_ACCEPT = [...DOCUMENT_TYPES, '.heic', '.pdf'].join(',');

/**
 * The file's type as the backend will check it, or `undefined` when it is not one it takes. Some
 * browsers give a HEIC photo no type at all, so the extension decides for that one case.
 */
export function documentTypeOf(file: File): DocumentType | undefined {
  const type = file.type.toLowerCase();

  if ((DOCUMENT_TYPES as readonly string[]).includes(type)) return type as DocumentType;
  if (!type && file.name.toLowerCase().endsWith('.heic')) return 'image/heic';

  return undefined;
}

/** "350 KB", "2.4 MB". */
export function fileSizeLabel(bytes: number): string {
  return bytes < 1024 * 1024 ? `${Math.max(1, Math.round(bytes / 1024))} KB` : `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
}
