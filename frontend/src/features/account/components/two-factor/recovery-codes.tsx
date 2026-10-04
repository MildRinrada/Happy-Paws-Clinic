import { Button } from '@/components/ui/button';
import { twoFactorSettings } from '../../content/two-factor-settings';
import { CopyButton } from './copy-button';

interface RecoveryCodesProps {
  codes: string[];
  onDone: () => void;
}

/**
 * The recovery codes, shown this once. The backend keeps only their hashes, so nothing on this
 * side can show them again: once "เก็บรหัสแล้ว" is pressed they are gone from the page too.
 */
export function RecoveryCodes({ codes, onDone }: RecoveryCodesProps) {
  const text = twoFactorSettings.recovery;

  return (
    <section className="space-y-4">
      <div role="alert" className="rounded-lg border border-amber-300 bg-amber-50 px-3.5 py-2.5 text-sm text-amber-900">
        <p className="font-semibold">{text.title}</p>
        <p>{text.warning}</p>
      </div>

      <ol aria-label={text.listLabel} className="grid grid-cols-2 gap-2">
        {codes.map((code) => (
          <li key={code} className="rounded-lg bg-ink-50 px-3 py-2 text-center font-mono text-sm text-ink-900">
            {code}
          </li>
        ))}
      </ol>

      <div className="flex flex-wrap items-center justify-end gap-3">
        <CopyButton text={codes.join('\n')} label={text.copy} />
        <Button onClick={onDone}>{text.done}</Button>
      </div>
    </section>
  );
}
