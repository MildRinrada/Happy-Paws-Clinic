'use client';

import { useState } from 'react';
import { Button } from '@/components/ui/button';
import { twoFactorSettings } from '../../content/two-factor-settings';

interface CopyButtonProps {
  text: string;
  label?: string;
}

/**
 * Copies a value for pasting into an authenticator or a password manager on the same computer.
 * Says "copied" even if the browser refused, since the value is on screen to type by hand anyway.
 */
export function CopyButton({ text, label = twoFactorSettings.copy }: CopyButtonProps) {
  const [copied, setCopied] = useState(false);

  async function copy() {
    await navigator.clipboard?.writeText(text).catch(() => undefined);
    setCopied(true);
  }

  return (
    <Button variant="secondary" size="sm" className="shrink-0" onClick={copy}>
      {copied ? twoFactorSettings.copied : label}
    </Button>
  );
}
