'use client';

import { useState } from 'react';
import { Button } from '@/components/ui/button';
import { ModalDialog } from '@/components/ui/modal-dialog';
import { labPage } from '../../content/lab-page';
import { useRefreshHold } from '../../lib/refresh-hold';
import type { LabResult } from '../../types/lab.types';
import { AmendResultForm } from './amend-result-form';
import { VoidResultForm } from './void-result-form';

interface CorrectResultButtonProps {
  labRequestId: string;
  /** The request's version, sent as `If-Match`. */
  version: number;
  result: LabResult;
}

type Mode = 'amend' | 'void';

/**
 * "แก้ไข" on one value (`lab:write`): correct it, or strike it out. Both keep the value on file and
 * ask why (§ 52). The worklist's refresh waits while the dialog is open.
 */
export function CorrectResultButton({ labRequestId, version, result }: CorrectResultButtonProps) {
  const text = labPage.correct;
  const [open, setOpen] = useState(false);
  const [mode, setMode] = useState<Mode>('amend');
  useRefreshHold(open);

  const close = () => {
    setOpen(false);
    setMode('amend');
  };

  return (
    <>
      <Button variant="ghost" size="sm" aria-label={text.title(result.analyte)} onClick={() => setOpen(true)}>
        {labPage.results.actions}
      </Button>
      <ModalDialog open={open} title={text.title(result.analyte)} onClose={close}>
        <div role="group" aria-label={text.title(result.analyte)} className="flex gap-1 rounded-lg bg-ink-100 p-1 text-sm">
          {(['amend', 'void'] as const).map((option) => (
            <button
              key={option}
              type="button"
              aria-pressed={mode === option}
              onClick={() => setMode(option)}
              className={`flex-1 rounded-md px-3 py-1.5 ${mode === option ? 'bg-white font-medium text-ink-900 shadow-sm' : 'text-ink-600'}`}
            >
              {option === 'amend' ? text.amendTab : text.voidTab}
            </button>
          ))}
        </div>
        {mode === 'amend' ? (
          <AmendResultForm labRequestId={labRequestId} version={version} result={result} onDone={close} />
        ) : (
          <VoidResultForm labRequestId={labRequestId} version={version} result={result} onDone={close} />
        )}
      </ModalDialog>
    </>
  );
}
