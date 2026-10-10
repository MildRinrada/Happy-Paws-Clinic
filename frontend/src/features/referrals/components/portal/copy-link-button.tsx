'use client';

import { useState } from 'react';
import { portalReferral } from '../../content/portal-referral';

/** Copies the invite link and says so. Where the clipboard is refused, says how to copy by hand. */
export function CopyLinkButton({ link }: { link: string }) {
  const text = portalReferral;
  const [state, setState] = useState<'idle' | 'copied' | 'failed'>('idle');

  async function copy() {
    try {
      await navigator.clipboard.writeText(link);
      setState('copied');
    } catch {
      setState('failed');
    }
  }

  return (
    <div className="flex flex-wrap items-center gap-2">
      <button
        type="button"
        onClick={copy}
        className="h-11 rounded-xl bg-site-orange px-5 text-sm font-bold text-white transition hover:brightness-95"
      >
        {text.copy}
      </button>
      {state !== 'idle' && (
        <span role="status" className={`text-sm ${state === 'copied' ? 'text-emerald-700' : 'text-red-700'}`}>
          {state === 'copied' ? text.copied : text.copyFailed}
        </span>
      )}
    </div>
  );
}
