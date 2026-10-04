import { Card, CardHeader } from '@/components/ui/card';
import { clinicSettings } from '../../content/clinic-settings';
import type { Branch } from '../../types/clinic.types';
import { AddBranchForm } from './add-branch-form';
import { BranchItem } from './branch-item';

/** The open branches, and the form that opens another. */
export function BranchesCard({ branches }: { branches: Branch[] }) {
  const text = clinicSettings.branches;

  return (
    <div className="space-y-3">
      <Card>
        <CardHeader title={text.heading} />
        {branches.length === 0 ? (
          <p className="px-5 py-6 text-sm text-ink-500">{text.empty}</p>
        ) : (
          <ul className="divide-y divide-ink-100">
            {branches.map((branch) => (
              <BranchItem key={branch.id} branch={branch} />
            ))}
          </ul>
        )}
        <p className="border-t border-ink-100 px-5 py-3 text-xs text-ink-500">{text.editNote}</p>
      </Card>

      <AddBranchForm />
    </div>
  );
}
