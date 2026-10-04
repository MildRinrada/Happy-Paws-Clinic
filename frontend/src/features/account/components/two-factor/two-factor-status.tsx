import { Badge } from '@/components/ui/badge';
import { twoFactorSettings } from '../../content/two-factor-settings';
import type { Me } from '../../types/account.types';

/** Few enough left that losing the phone could leave the person with no way in. */
const LOW_RECOVERY_CODES = 2;

/** Whether a second factor is on, which kind, and how many recovery codes are still unused. */
export function TwoFactorStatus({ twoFactor }: { twoFactor: Me['two_factor'] }) {
  const text = twoFactorSettings.status;
  const left = twoFactor.recovery_codes_left ?? 0;

  return (
    <div className="space-y-1.5">
      <div className="flex flex-wrap items-center gap-2 text-sm text-ink-700">
        <Badge tone={twoFactor.enabled ? 'green' : 'amber'}>{twoFactor.enabled ? text.on : text.off}</Badge>
        {twoFactor.enabled && <span>{text.methods[twoFactor.method] ?? twoFactor.method}</span>}
        {twoFactor.enabled && <span className="text-ink-500">{text.recoveryLeft(left)}</span>}
      </div>
      {twoFactor.enabled && left <= LOW_RECOVERY_CODES && <p className="text-sm text-amber-800">{text.recoveryLow}</p>}
    </div>
  );
}
