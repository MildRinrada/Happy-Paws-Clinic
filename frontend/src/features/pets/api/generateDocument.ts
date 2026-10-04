import { staffApi } from '@/lib/api/server-client';
import type { DocumentRequest } from '../types/pet.types';

export interface GeneratedPdf {
  fileName: string;
  /** The PDF, base64. A server action hands back plain values, and this is one. */
  base64: string;
}

/**
 * `POST /staff/documents/generate` — needs `record:read`. Answers with the PDF itself, not JSON;
 * a refusal is still the usual error envelope. A certificate or a referral needs a caller with a
 * licence number; a vaccination book anyone with the scope may print. Every print is written to
 * the access log, and nothing is stored.
 */
export async function generateDocument(request: DocumentRequest): Promise<GeneratedPdf> {
  const file = await staffApi().postForFile('/staff/documents/generate', 'application/pdf', { body: request });

  return {
    fileName: file.fileName ?? `${request.type}.pdf`,
    base64: Buffer.from(file.bytes).toString('base64'),
  };
}
