import { staffApi } from '@/lib/api/server-client';
import type { DispenseResult } from '../types/prescription.types';

/**
 * `POST /staff/prescriptions/{id}/dispense`: "จ่ายยาจริง, ตัดสต๊อกและพิมพ์ฉลาก". Needs
 * `rx:dispense`. No body. Once only: a second press answers `409 state_conflict`, which is what stops
 * one bag being handed out twice from two counters.
 */
export function dispensePrescription(prescriptionId: string): Promise<DispenseResult> {
  return staffApi().post<DispenseResult>(`/staff/prescriptions/${encodeURIComponent(prescriptionId)}/dispense`);
}
