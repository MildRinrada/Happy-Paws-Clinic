import { twoFactorSettings } from '../../content/two-factor-settings';
import type { AuthenticatorSecret } from '../../types/account.types';
import { CopyButton } from './copy-button';

/** The QR code to scan, and the same secret as text for anyone who cannot scan. */
export function AuthenticatorQr({ setup }: { setup: AuthenticatorSecret }) {
  const text = twoFactorSettings.scan;

  return (
    <section className="space-y-3">
      <div>
        <h3 className="text-sm font-semibold text-ink-900">{text.title}</h3>
        <p className="text-sm text-ink-600">{text.body}</p>
      </div>

      {/* A data URL drawn on the server, so next/image has nothing to optimise. */}
      {/* eslint-disable-next-line @next/next/no-img-element */}
      <img
        src={setup.qrDataUrl}
        alt={text.qrAlt}
        width={224}
        height={224}
        className="mx-auto h-auto w-full max-w-56 rounded-lg border border-ink-200 bg-white p-2"
      />

      <div className="space-y-1.5">
        <p className="text-sm text-ink-600">{text.manualLabel}</p>
        <div className="flex items-center gap-2">
          <code className="min-w-0 flex-1 break-all rounded-lg bg-ink-50 px-3 py-2 font-mono text-sm text-ink-900">
            {setup.secret}
          </code>
          <CopyButton text={setup.secret} />
        </div>
      </div>
    </section>
  );
}
