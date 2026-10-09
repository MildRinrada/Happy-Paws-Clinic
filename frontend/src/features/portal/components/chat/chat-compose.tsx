'use client';

import { useId, useState, type FormEvent } from 'react';
import { Alert } from '@/components/ui/alert';
import { writeToClinicAction } from '../../actions/chat-actions';
import { portalChat } from '../../content/portal-chat';
import { refusalMessage } from '../../lib/form-error';
import { TextAreaField } from '../shared/text-area-field';

const MAX_LENGTH = 1000;

/** The box the owner types into. A sent message clears it; the page then shows it in place. */
export function ChatCompose() {
  const id = useId();
  const text = portalChat.compose;
  const [value, setValue] = useState('');
  const [pending, setPending] = useState(false);
  const [error, setError] = useState<string>();

  async function send(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    if (value.trim() === '') {
      setError(text.empty);
      return;
    }

    setError(undefined);
    setPending(true);

    try {
      const result = await writeToClinicAction({ text: value });
      if (result.ok) {
        setValue('');
      } else {
        setError(refusalMessage(result.error));
      }
    } catch {
      setError(text.unreachable);
    } finally {
      setPending(false);
    }
  }

  return (
    <form onSubmit={send} className="space-y-3">
      {error && <Alert>{error}</Alert>}

      <TextAreaField
        id={id}
        label={text.label}
        hint={text.hint}
        placeholder={text.placeholder}
        rows={3}
        maxLength={MAX_LENGTH}
        value={value}
        disabled={pending}
        onChange={(event) => {
          setError(undefined);
          setValue(event.target.value);
        }}
      />

      <button
        type="submit"
        disabled={pending}
        className="h-11 rounded-xl bg-site-orange px-8 text-sm font-bold text-white transition hover:brightness-95 disabled:cursor-not-allowed disabled:opacity-50"
      >
        {pending ? text.sending : text.send}
      </button>
    </form>
  );
}
