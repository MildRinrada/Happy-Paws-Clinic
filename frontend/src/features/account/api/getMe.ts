import { staffApi } from '@/lib/api/server-client';
import type { Me } from '../types/account.types';

/** `GET /me` — "โปรไฟล์ + roles + permissions + สถานะบัญชี". */
export function getMe(): Promise<Me> {
  return staffApi().get<Me>('/me');
}
