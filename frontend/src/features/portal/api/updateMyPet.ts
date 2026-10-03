import { staffApi } from '@/lib/api/server-client';
import type { MyPet } from '../types/portal.types';

export interface OwnerPetChanges {
  nickname: string;
  diet: string;
  behaviour: string;
  /** Left out to keep the current photo. */
  photoDocumentId?: string;
}

/**
 * `PATCH /portal/pets/{id}` — "แก้ได้เฉพาะรูป ชื่อเล่น อาหาร พฤติกรรม". The three texts are always
 * sent, so an emptied box clears that field (the backend reads an empty string as "clear" and a
 * missing one as "leave alone"). `version` goes in `If-Match`: an edit made elsewhere since the
 * page loaded answers 409 rather than being silently overwritten.
 */
export function updateMyPet(petId: string, version: number, changes: OwnerPetChanges): Promise<MyPet> {
  return staffApi().patch<MyPet>(`/portal/pets/${encodeURIComponent(petId)}`, {
    ifMatch: version,
    body: {
      nickname: changes.nickname,
      diet: changes.diet,
      behaviour: changes.behaviour,
      photo_document_id: changes.photoDocumentId,
    },
  });
}
