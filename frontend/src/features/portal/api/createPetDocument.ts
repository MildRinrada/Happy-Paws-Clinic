import { staffApi } from '@/lib/api/server-client';
import type { DocumentKind } from '../lib/document-rules';

export interface NewPetDocument {
  petId: string;
  fileName: string;
  contentType: string;
  sizeBytes: number;
  kind: DocumentKind;
  /** What the clinic sees as the document's name. Left out, the backend uses the file name. */
  title?: string;
}

/** What `POST /portal/documents` answers: the record, and where to send the bytes. */
export interface DocumentUpload {
  document: { id: string };
  upload: { url: string; method: 'PUT'; headers: Record<string, string>; expires_at: string };
}

/**
 * `POST /portal/documents` — records a file the owner is about to send and returns a signed URL,
 * good for 15 minutes, for the browser to `PUT` the bytes to. The document is usable (as a photo,
 * say) only once that upload has finished.
 */
export function createPetDocument(input: NewPetDocument): Promise<DocumentUpload> {
  return staffApi().post<DocumentUpload>('/portal/documents', {
    body: {
      pet_id: input.petId,
      file_name: input.fileName,
      content_type: input.contentType,
      size_bytes: input.sizeBytes,
      kind: input.kind,
      title: input.title || undefined,
    },
  });
}
