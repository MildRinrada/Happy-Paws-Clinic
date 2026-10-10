import { publicApi } from '@/lib/api/server-client';
import type { WaitEstimate } from '../types/wait-estimate.types';

/** `GET /queue/wait-estimate?branch=`: public, counts only, cached by the API for 30 seconds. */
export async function getWaitEstimate(branchId: string): Promise<WaitEstimate> {
  return publicApi().get<WaitEstimate>('/queue/wait-estimate', { query: { branch: branchId } });
}
