import { Badge } from '@/components/ui/badge';
import { clinicSettings } from '../../content/clinic-settings';
import type { Branch } from '../../types/clinic.types';
import { BranchMapLink } from './branch-map-link';
import { EditBranchButton } from './edit-branch-button';
import { ExamRoomBadges } from './exam-room-badges';

/** One branch as it stands: name, code, address, rooms and a map link, with its "แก้ไข" button. */
export function BranchItem({ branch }: { branch: Branch }) {
  const text = clinicSettings.branches;

  return (
    <li className="flex flex-wrap items-start gap-x-4 gap-y-3 px-5 py-4">
      <div className="min-w-0 flex-1 space-y-2">
        <p className="flex flex-wrap items-center gap-2">
          <span className="font-medium text-ink-900">{branch.name}</span>
          <Badge>{branch.code}</Badge>
        </p>
        <p className="text-sm text-ink-600">{branch.address ?? text.noAddress}</p>
        <ExamRoomBadges rooms={branch.exam_rooms} />
        {branch.latitude !== undefined && branch.longitude !== undefined && (
          <BranchMapLink latitude={branch.latitude} longitude={branch.longitude} />
        )}
      </div>
      <EditBranchButton branch={branch} />
    </li>
  );
}
