'use server';

import { z } from 'zod';
import { runAction } from '@/lib/actions/action-result';
import { attachLabFile, removeLabFile, requestLabFileUpload } from '../api/lab-requests';
import { revalidateLab } from '../lib/revalidate-lab';

/**
 * Files from the lab (docs/contract-extensions.md § 54), in the two moves every upload here makes:
 * ask for a signed URL, send the bytes from the browser, then attach the value that names them.
 */

const id = z.string().min(1).max(64);
const fileType = z.enum(['application/pdf', 'image/jpeg', 'image/png', 'image/webp']);
const size = z.number().int().min(1).max(10 * 1024 * 1024);

const start = z.object({ labRequestId: id, contentType: fileType, sizeBytes: size });

/** A signed URL for one file. Nothing on the request changes yet. */
export async function startLabFileUploadAction(input: z.input<typeof start>) {
  const parsed = start.parse(input);
  return runAction(() => requestLabFileUpload(parsed.labRequestId, parsed.contentType, parsed.sizeBytes));
}

const attach = z.object({ labRequestId: id, file: z.string().min(1).max(300), fileName: z.string().trim().min(1).max(200), sizeBytes: size });

/** Puts the file that just arrived on the request. */
export async function attachLabFileAction(input: z.input<typeof attach>) {
  const parsed = attach.parse(input);
  const result = await runAction(() => attachLabFile(parsed.labRequestId, parsed.file, parsed.fileName, parsed.sizeBytes));

  revalidateLab();
  return result.ok ? { ok: true as const, data: undefined } : result;
}

const remove = z.object({ labRequestId: id, attachmentId: id, version: z.number().int().nonnegative() });

/** Takes a file off the request. The file itself stays in storage, and the removal is on record. */
export async function removeLabFileAction(input: z.input<typeof remove>) {
  const parsed = remove.parse(input);
  const result = await runAction(() => removeLabFile(parsed.labRequestId, parsed.attachmentId, parsed.version));

  revalidateLab();
  return result.ok ? { ok: true as const, data: undefined } : result;
}
