import { publicApi } from '@/lib/api/server-client';
import type { Branch } from '../types/clinic.types';

/** `GET /clinic/branches` — public; open branches only. */
export async function listBranches(): Promise<Branch[]> {
  const { data } = await publicApi().get<{ data: Branch[] }>('/clinic/branches');

  return data;
}
