import { staffApi } from '@/lib/api/server-client';
import type { NewPetDocument, PetDocumentUpload } from '../types/pet.types';

/**
 * `POST /staff/pets/{id}/documents` — needs `record:write`. Records the file about to be sent and
 * answers with a signed URL for the browser to `PUT` the bytes to. JPEG, PNG, WEBP, HEIC or PDF,
 * at most 10 MB. The document can be opened only once that upload has arrived.
 */
export function startPetDocument(petId: string, document: NewPetDocument): Promise<PetDocumentUpload> {
  return staffApi().post<PetDocumentUpload>(`/staff/pets/${encodeURIComponent(petId)}/documents`, { body: document });
}
