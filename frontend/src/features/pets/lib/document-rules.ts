/** What `POST /staff/pets/{id}/documents` accepts, checked in the browser before asking for a URL. */
export const DOCUMENT_TYPES = ['image/jpeg', 'image/png', 'image/webp', 'image/heic', 'application/pdf'] as const;

export type DocumentType = (typeof DOCUMENT_TYPES)[number];

export const DOCUMENT_MAX_BYTES = 10 * 1024 * 1024;

/** The backend's document kinds, in the order the form offers them. */
export const DOCUMENT_KINDS = ['lab_result', 'imaging', 'referral', 'photo', 'other'] as const;

/** The three documents `POST /staff/documents/generate` prints. */
export const PRINTABLE_DOCUMENTS = ['vaccination_book', 'health_certificate', 'referral_letter'] as const;

const byExtension: Record<string, DocumentType> = {
  jpg: 'image/jpeg',
  jpeg: 'image/jpeg',
  png: 'image/png',
  webp: 'image/webp',
  heic: 'image/heic',
  pdf: 'application/pdf',
};

/**
 * The file's type as the backend names it, or `undefined` when it is not one it takes. Falls back
 * to the extension because some systems give a HEIC photo no type at all.
 */
export function documentTypeOf(file: File): DocumentType | undefined {
  const declared = file.type.toLowerCase();
  if ((DOCUMENT_TYPES as readonly string[]).includes(declared)) return declared as DocumentType;
  if (declared && declared !== 'application/octet-stream') return undefined;

  const extension = file.name.split('.').pop()?.toLowerCase() ?? '';
  return byExtension[extension];
}
