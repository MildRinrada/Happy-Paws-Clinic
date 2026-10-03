import { staffApi } from '@/lib/api/server-client';
import type { MyPet } from '../types/portal.types';

/** `GET /portal/pets` — "สัตว์ทั้งหมดของบัญชีนี้", owned and co-cared for, the deceased included. */
export async function listMyPets(): Promise<MyPet[]> {
  const { data } = await staffApi().get<{ data: MyPet[] }>('/portal/pets');
  return data;
}
