'use client';

import { useState } from 'react';
import { Button } from '@/components/ui/button';
import { aiCommon } from '../../content/ai-common';

/** Copies text to the clipboard and says so. Where the clipboard is refused, says how to copy by hand. */
export function CopyButton({ text }: { text: string }) {
  const [state, setState] = useState<'idle' | 'copied' | 'failed'>('idle');

  async function copy() {
    try {
      await navigator.clipboard.writeText(text);
      setState('copied');
    } catch {
      setState('failed');
    }
  }

  return (
    <div className="flex flex-wrap items-center gap-2">
      <Button variant="secondary" size="sm" onClick={copy} disabled={text.trim() === ''}>
        {aiCommon.copy}
      </Button>
      {state !== 'idle' && (
        <span role="status" className={`text-sm ${state === 'copied' ? 'text-emerald-700' : 'text-red-700'}`}>
          {state === 'copied' ? aiCommon.copied : aiCommon.copyFailed}
        </span>
      )}
    </div>
  );
}
