'use server';

import { revalidatePath } from 'next/cache';
import { z } from 'zod';
import { runAction } from '@/lib/actions/action-result';
import { createPetDocument } from '../api/createPetDocument';
import { updateMyPet } from '../api/updateMyPet';
import { PHOTO_MAX_BYTES, PHOTO_TYPES } from '../lib/photo-rules';

/**
 * Editing a pet's owner details. The photo goes in three moves: this asks the backend for an
 * upload URL, the browser sends the file straight there, and the save below names the new
 * document. The bytes never pass through this server.
 */

const id = z.string().min(1).max(64);

const photoInput = z.object({
  petId: id,
  fileName: z.string().trim().min(1).max(200),
  contentType: z.enum(PHOTO_TYPES),
  sizeBytes: z.number().int().positive().max(PHOTO_MAX_BYTES),
});

export async function startPhotoUploadAction(values: z.infer<typeof photoInput>) {
  const input = photoInput.parse(values);
  return runAction(() => createPetDocument({ ...input, kind: 'photo' }));
}

const petInput = z.object({
  petId: id,
  version: z.number().int().nonnegative(),
  nickname: z.string().trim().max(60),
  diet: z.string().trim().max(500),
  behaviour: z.string().trim().max(1000),
  photoDocumentId: id.optional(),
});

export async function updatePetAction(values: z.infer<typeof petInput>) {
  const { petId, version, ...changes } = petInput.parse(values);
  const result = await runAction(() => updateMyPet(petId, version, changes));

  revalidatePath('/portal', 'layout');

  return result.ok ? { ok: true as const } : result;
}
