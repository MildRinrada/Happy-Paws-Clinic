'use client';

import { useId, useState } from 'react';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/field';

interface CopyValueProps {
  label: string;
  value: string;
  copyLabel: string;
  copiedLabel: string;
}

/**
 * A value to take elsewhere: shown read-only in full, selectable by hand, with a copy button. Says
 * "copied" even if the browser refused, since the value is on screen to select anyway.
 */
export function CopyValue({ label, value, copyLabel, copiedLabel }: CopyValueProps) {
  const id = useId();
  const [copied, setCopied] = useState(false);

  async function copy() {
    await navigator.clipboard?.writeText(value).catch(() => undefined);
    setCopied(true);
  }

  return (
    <div className="space-y-1.5">
      <label htmlFor={id} className="block text-sm font-medium text-ink-700">
        {label}
      </label>
      <div className="flex gap-2">
        <Input id={id} readOnly value={value} className="font-mono text-xs" onFocus={(event) => event.target.select()} />
        <Button variant="secondary" className="shrink-0" onClick={copy}>
          {copied ? copiedLabel : copyLabel}
        </Button>
      </div>
    </div>
  );
}
