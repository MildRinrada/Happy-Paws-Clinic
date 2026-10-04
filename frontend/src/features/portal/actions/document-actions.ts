'use server';

import { revalidatePath } from 'next/cache';
import { z } from 'zod';
import { runAction } from '@/lib/actions/action-result';
import { createPetDocument } from '../api/createPetDocument';
import { DOCUMENT_KINDS, DOCUMENT_MAX_BYTES, DOCUMENT_TYPES } from '../lib/document-rules';

/**
 * An owner sending the clinic a file for their pet, in two moves: this asks the backend for an
 * upload URL, then the browser sends the bytes straight there (`PUT /storage/uploads/{token}`).
 * The bytes never pass through this server.
 */

const id = z.string().min(1).max(64);

const documentInput = z.object({
  petId: id,
  fileName: z.string().trim().min(1).max(200),
  contentType: z.enum(DOCUMENT_TYPES),
  sizeBytes: z.number().int().positive().max(DOCUMENT_MAX_BYTES),
  kind: z.enum(DOCUMENT_KINDS),
  title: z.string().trim().max(200),
});

export async function startDocumentUploadAction(values: z.infer<typeof documentInput>) {
  const input = documentInput.parse(values);
  return runAction(() => createPetDocument(input));
}

/** Called once the bytes are in, so the pet's records show the new file on the next visit. */
export async function documentSentAction() {
  revalidatePath('/portal', 'layout');
}
