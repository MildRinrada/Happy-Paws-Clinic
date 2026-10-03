'use client';

import { useState, useTransition } from 'react';
import { markReadAction } from '../../actions/notification-actions';
import { portalNotifications } from '../../content/portal-notifications';

interface MarkReadButtonProps {
  /** Left out to mark every message read. */
  ids?: string[];
  label: string;
  variant?: 'link' | 'button';
}

/** Marks messages read. The page and the bell's count refresh from the server afterwards. */
export function MarkReadButton({ ids, label, variant = 'link' }: MarkReadButtonProps) {
  const [pending, startTransition] = useTransition();
  const [failed, setFailed] = useState(false);

  function mark() {
    setFailed(false);
    startTransition(async () => {
      try {
        const result = await markReadAction({ ids });
        if (!result.ok) setFailed(true);
      } catch {
        setFailed(true);
      }
    });
  }

  const style =
    variant === 'button'
      ? 'h-9 rounded-lg border border-ink-200 bg-white px-3 text-sm font-semibold text-ink-700 hover:bg-ink-50'
      : 'text-xs font-semibold text-site-teal hover:underline';

  return (
    <span className="inline-flex items-center gap-2">
      <button type="button" onClick={mark} disabled={pending} className={`${style} disabled:opacity-50`}>
        {pending ? portalNotifications.marking : label}
      </button>
      {failed && (
        <span role="alert" className="text-xs text-red-700">
          {portalNotifications.failed}
        </span>
      )}
    </span>
  );
}
