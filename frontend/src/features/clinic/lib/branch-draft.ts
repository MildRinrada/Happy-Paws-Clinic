import type { Branch } from '../types/clinic.types';
import type { BranchUpdateInput } from '../types/settings.types';
import { optionalNumber, parseCommaList } from './parse-list';

/** What the branch forms hold while typed: every box as text. */
export interface BranchDraft {
  name: string;
  address: string;
  latitude: string;
  longitude: string;
  rooms: string;
}

export const blankBranchDraft: BranchDraft = { name: '', address: '', latitude: '', longitude: '', rooms: '' };

/** A stored branch as the edit form starts it. */
export function branchDraft(branch: Branch): BranchDraft {
  return {
    name: branch.name,
    address: branch.address ?? '',
    latitude: branch.latitude?.toString() ?? '',
    longitude: branch.longitude?.toString() ?? '',
    rooms: branch.exam_rooms.join(', '),
  };
}

/**
 * The edit as `PATCH /clinic/branches/{id}` takes it: an empty address clears it, the pin goes only
 * as a pair (two empty boxes keep the stored one), and the rooms are the whole list.
 */
export function branchUpdate(draft: BranchDraft): BranchUpdateInput {
  const latitude = optionalNumber(draft.latitude);
  const longitude = optionalNumber(draft.longitude);
  const pin = latitude === undefined && longitude === undefined ? {} : { latitude, longitude };

  return { name: draft.name, address: draft.address.trim(), ...pin, exam_rooms: parseCommaList(draft.rooms) };
}
