'use server';

import { revalidatePath } from 'next/cache';
import { z } from 'zod';
import { runAction } from '@/lib/actions/action-result';
import { generateDocument } from '../api/generateDocument';
import { startPetDocument } from '../api/petDocuments';
import { DOCUMENT_KINDS, DOCUMENT_MAX_BYTES, DOCUMENT_TYPES, PRINTABLE_DOCUMENTS } from '../lib/document-rules';
import { petPaths } from '../lib/pet-paths';

const id = z.string().min(1).max(64);

const newDocument = z.object({
  petId: id,
  fileName: z.string().trim().min(1).max(200),
  contentType: z.enum(DOCUMENT_TYPES),
  sizeBytes: z.number().int().positive().max(DOCUMENT_MAX_BYTES),
  kind: z.enum(DOCUMENT_KINDS),
  title: z.string().trim().max(200),
  visibleToOwner: z.boolean(),
});

/**
 * Step one of an upload: records the document and returns the signed URL. The browser sends the
 * file straight there; the bytes never pass through this server.
 */
export async function startPetDocumentAction(values: z.infer<typeof newDocument>) {
  const input = newDocument.parse(values);
  const result = await runAction(() =>
    startPetDocument(input.petId, {
      file_name: input.fileName,
      content_type: input.contentType,
      size_bytes: input.sizeBytes,
      kind: input.kind,
      title: input.title || undefined,
      visible_to_owner: input.visibleToOwner,
    }),
  );

  revalidatePath(petPaths.detail(input.petId));
  return result;
}

const printRequest = z.object({
  petId: id,
  type: z.enum(PRINTABLE_DOCUMENTS),
  referTo: z.string().trim().max(200),
  reason: z.string().trim().max(1000),
  remarks: z.string().trim().max(1000),
});

/** Prints a document from the record as a PDF, handed back for the browser to save. */
export async function generateDocumentAction(values: z.infer<typeof printRequest>) {
  const input = printRequest.parse(values);
  const referral = input.type === 'referral_letter';

  return runAction(() =>
    generateDocument({
      type: input.type,
      pet_id: input.petId,
      refer_to: referral ? input.referTo || undefined : undefined,
      reason: referral ? input.reason || undefined : undefined,
      remarks: input.type === 'health_certificate' ? input.remarks || undefined : undefined,
    }),
  );
}
