'use client';

import { useState, useTransition } from 'react';
import { Button } from '@/components/ui/button';
import { draftThreadReplyAction } from '../../actions/staff-ai-actions';
import { aiCommon } from '../../content/ai-common';
import { ownerMessageText } from '../../content/owner-message';
import { AiDraftBadge } from '../common/ai-draft-badge';

interface ReplyDraftButtonProps {
  threadId: string;
  /** Puts the drafted words in the reply box. Staff still read them and press send. */
  onDraft: (text: string) => void;
}

/** "ร่างคำตอบ": a reply drafted from the thread's latest messages, into the reply box, never sent. */
export function ReplyDraftButton({ threadId, onDraft }: ReplyDraftButtonProps) {
  const text = ownerMessageText.reply;
  const [status, setStatus] = useState<{ ok: boolean; message: string }>();
  const [pending, startTransition] = useTransition();

  function run() {
    setStatus(undefined);
    startTransition(async () => {
      try {
        const result = await draftThreadReplyAction(threadId);
        if (result.ok) {
          onDraft(result.data.text);
          setStatus({ ok: true, message: text.filled });
        } else {
          setStatus({ ok: false, message: result.error.message });
        }
      } catch {
        setStatus({ ok: false, message: aiCommon.unreachable });
      }
    });
  }

  return (
    <div className="flex flex-wrap items-center gap-2">
      <Button variant="secondary" size="sm" onClick={run} disabled={pending}>
        {pending ? text.working : text.run}
      </Button>
      {status?.ok && <AiDraftBadge />}
      {status && (
        <span role={status.ok ? 'status' : 'alert'} className={`text-sm ${status.ok ? 'text-amber-900' : 'text-red-700'}`}>
          {status.message}
        </span>
      )}
    </div>
  );
}
