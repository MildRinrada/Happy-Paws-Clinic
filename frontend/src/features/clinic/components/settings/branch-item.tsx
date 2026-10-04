import { Badge } from '@/components/ui/badge';
import { clinicSettings } from '../../content/clinic-settings';
import type { Branch } from '../../types/clinic.types';

/** One branch as it stands: name, code, address, rooms and coordinates. Read-only. */
export function BranchItem({ branch }: { branch: Branch }) {
  const text = clinicSettings.branches;

  return (
    <li className="space-y-1.5 px-5 py-4">
      <p className="flex flex-wrap items-center gap-2">
        <span className="font-medium text-ink-900">{branch.name}</span>
        <Badge>{branch.code}</Badge>
      </p>
      {branch.address && <p className="text-sm text-ink-600">{branch.address}</p>}
      <p className="text-sm text-ink-600">
        {text.rooms}: {branch.exam_rooms.length > 0 ? branch.exam_rooms.join(', ') : text.noRooms}
      </p>
      {branch.latitude !== undefined && branch.longitude !== undefined && (
        <p className="text-xs text-ink-500">
          {text.coordinates}: {branch.latitude}, {branch.longitude}
        </p>
      )}
    </li>
  );
}
