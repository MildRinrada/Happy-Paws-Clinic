import { listLabRequests } from '../api/lab-requests';
import type { LabRequest } from '../types/lab.types';

/**
 * The lab requests made in one case.
 *
 * The consultation does not carry its lab requests: a request is its own record, because results
 * arrive after the case is signed. So this reads the patient's requests (`?pet_id=`, which the
 * backend writes to the access log as a look at the patient's results) and keeps the ones that
 * name this case. One page of 100 covers any one patient's recent work.
 */
export async function loadCaseLabRequests(petId: string, consultationId: string): Promise<LabRequest[]> {
  const page = await listLabRequests({ petId, limit: 100 });
  return page.data.filter((request) => request.consultation_id === consultationId);
}
