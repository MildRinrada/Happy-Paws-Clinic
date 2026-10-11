import { Card, CardHeader } from '@/components/ui/card';
import { clinicSettings } from '../../content/clinic-settings';
import type { Branch } from '../../types/clinic.types';
import { AddBranchButton } from './add-branch-button';
import { BranchItem } from './branch-item';

/** The open branches, each editable in a dialog, and "+ เปิดสาขาใหม่" in the header. */
export function BranchesCard({ branches }: { branches: Branch[] }) {
  const text = clinicSettings.branches;

  return (
    <Card>
      <CardHeader title={text.heading} aside={<AddBranchButton />} />
      {branches.length === 0 ? (
        <p className="px-5 py-6 text-sm text-ink-500">{text.empty}</p>
      ) : (
        <ul className="divide-y divide-ink-100">
          {branches.map((branch) => (
            <BranchItem key={`${branch.id}:${branch.version}`} branch={branch} />
          ))}
        </ul>
      )}
    </Card>
  );
}
