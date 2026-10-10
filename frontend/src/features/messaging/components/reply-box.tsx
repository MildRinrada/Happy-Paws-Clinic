'use client';

import { useId, useState, type FormEvent } from 'react';
import { Button } from '@/components/ui/button';
import { Field, Textarea } from '@/components/ui/field';
import { FormStatus } from '@/components/ui/form-status';
import { formMessages } from '@/features/settings/content/form-messages';
import { ReplyDraftButton } from '@/features/staff-ai/components/reply/reply-draft-button';
import { useFormSubmit } from '@/hooks/use-form-submit';
import { replyToThreadAction } from '../actions/reply-actions';
import { messagesPage } from '../content/messages-page';

const MAX_LENGTH = 1000;

interface ReplyBoxProps {
  threadId: string;
  /** Offer "ร่างคำตอบ", which puts an AI draft in the box (docs/contract-extensions.md § 34). */
  aiDraft?: boolean;
}

/**
 * The answer to a customer. The backend picks the channel; the result shows on the message itself.
 * An AI draft only fills the box: the reply goes out when staff press send, as any other.
 */
export function ReplyBox({ threadId, aiDraft = false }: ReplyBoxProps) {
  const id = useId();
  const [text, setText] = useState('');
  const [emptyError, setEmptyError] = useState<string>();
  const { errors, message, saved, pending, submit, touch } = useFormSubmit({
    conflict: formMessages.conflict,
    unreachable: messagesPage.reply.unreachable,
  });

  function send(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    if (text.trim() === '') {
      setEmptyError(messagesPage.reply.empty);
      return;
    }

    setEmptyError(undefined);
    submit(() => replyToThreadAction({ thread_id: threadId, text }), () => setText(''));
  }

  const error = emptyError ?? errors.text;

  return (
    <form onSubmit={send} className="space-y-3 border-t border-ink-100 px-5 py-4">
      <Field label={messagesPage.reply.label} htmlFor={id} error={error} hint={messagesPage.reply.hint}>
        <Textarea
          id={id}
          value={text}
          rows={3}
          maxLength={MAX_LENGTH}
          placeholder={messagesPage.reply.placeholder}
          aria-invalid={error ? true : undefined}
          aria-describedby={error ? `${id}-error` : undefined}
          onChange={(event) => {
            touch();
            setEmptyError(undefined);
            setText(event.target.value);
          }}
        />
      </Field>
      {aiDraft && (
        <ReplyDraftButton
          threadId={threadId}
          onDraft={(draft) => {
            touch();
            setEmptyError(undefined);
            setText(draft);
          }}
        />
      )}
      <div className="flex flex-wrap items-center gap-3">
        <Button type="submit" disabled={pending}>
          {pending ? messagesPage.reply.sending : messagesPage.reply.send}
        </Button>
        <FormStatus message={message} saved={saved} savedText={messagesPage.reply.sent} />
      </div>
    </form>
  );
}
