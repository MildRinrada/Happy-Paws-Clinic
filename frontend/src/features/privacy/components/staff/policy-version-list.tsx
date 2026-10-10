import { Card, CardHeader } from '@/components/ui/card';
import { versionListText } from '../../content/staff-privacy-policy';
import type { StaffPolicyVersion } from '../../types/staff-privacy-policy.types';
import { PolicyVersionEntry } from './policy-version-entry';

/** Every version, newest made first, drafts included. */
export function PolicyVersionList({ versions }: { versions: StaffPolicyVersion[] }) {
  return (
    <Card>
      <CardHeader title={versionListText.heading} />
      {versions.length === 0 ? (
        <p className="px-5 py-4 text-sm text-ink-600">{versionListText.empty}</p>
      ) : (
        <ul className="divide-y divide-ink-100">
          {versions.map((version) => (
            <PolicyVersionEntry key={version.id} version={version} />
          ))}
        </ul>
      )}
    </Card>
  );
}
