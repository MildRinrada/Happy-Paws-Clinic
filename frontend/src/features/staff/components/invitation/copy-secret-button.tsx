'use client';

import { useState } from 'react';
import { Button } from '@/components/ui/button';
import { invitationPage } from '../../content/invitation-page';

/** Copies the secret, for pasting into an authenticator on the same computer. */
export function CopySecretButton({ secret }: { secret: string }) {
  const [copied, setCopied] = useState(false);

  async function copy() {
    await navigator.clipboard.writeText(secret).catch(() => undefined);
    setCopied(true);
  }

  return (
    <Button variant="secondary" size="sm" onClick={copy}>
      {copied ? invitationPage.setup.copied : invitationPage.setup.copy}
    </Button>
  );
}
