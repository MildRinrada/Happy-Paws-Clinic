'use client';

import { useRouter } from 'next/navigation';
import { useEffect, useId, useRef, useState, useTransition, type FormEvent } from 'react';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/field';
import { DISPLAY_PATH } from '@/lib/display/display-paths';
import { claimDisplayAction } from '../../actions/display-setup-actions';
import { queueDisplay } from '../../content/queue-display';

const text = queueDisplay.setup;

/**
 * Trades a setup code for the screen's token, which the server action puts in the screen's
 * httpOnly cookie, then opens the screen.
 *
 * A code that came in the link (`/display/setup#code=ABCD-EFGH`) is taken out of the address bar
 * before anything else happens (`history.replaceState`), so the code does not stay on screen or in
 * this history entry, and is used at once. The fragment never reaches a server. The code is single
 * use and dies in 30 minutes, so a copy left in the browser's history opens nothing.
 */
export function DisplaySetupForm() {
  const id = useId();
  const router = useRouter();
  const [code, setCode] = useState('');
  const [error, setError] = useState<string>();
  const [done, setDone] = useState<string>();
  const [fromLink, setFromLink] = useState(false);
  const [pending, startTransition] = useTransition();
  const started = useRef(false);

  const claim = (value: string) => {
    setError(undefined);
    startTransition(async () => {
      try {
        const result = await claimDisplayAction({ code: value });
        if (result.ok) {
          setDone(result.data.name);
          router.replace(DISPLAY_PATH);
        } else {
          setError(result.error.message);
          setFromLink(false);
        }
      } catch {
        setError(text.unreachable);
        setFromLink(false);
      }
    });
  };

  useEffect(() => {
    if (started.current) {
      return;
    }
    started.current = true;

    const linked = new URLSearchParams(window.location.hash.slice(1)).get('code');
    if (!linked) {
      return;
    }

    window.history.replaceState(null, '', window.location.pathname);
    setFromLink(true);
    claim(linked);
    // Runs once, on arrival: the link's code is read and used a single time.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    claim(code);
  }

  return (
    <div className="flex min-h-dvh items-center justify-center p-6">
      <form onSubmit={submit} className="w-full max-w-md space-y-5 rounded-2xl bg-ink-800 p-6 sm:p-8">
        <h1 className="font-display text-2xl font-semibold">{text.title}</h1>
        <p className="text-sm text-ink-300">{text.body}</p>

        <div className="space-y-1.5">
          <label htmlFor={`${id}-code`} className="block text-sm font-medium text-ink-200">
            {text.code}
          </label>
          <Input
            id={`${id}-code`}
            value={code}
            onChange={(event) => setCode(event.target.value)}
            autoComplete="off"
            autoCapitalize="characters"
            spellCheck={false}
            maxLength={20}
            aria-describedby={`${id}-hint`}
            className="font-display text-xl tracking-widest text-ink-900"
          />
          <p id={`${id}-hint`} className="text-xs text-ink-400">
            {text.codeHint}
          </p>
        </div>

        {error && (
          <p role="alert" className="text-sm text-red-300">
            {error}
          </p>
        )}
        {done && (
          <p role="status" className="text-sm text-emerald-300">
            {text.done(done)}
          </p>
        )}
        {fromLink && pending && (
          <p role="status" className="text-sm text-ink-300">
            {text.fromLink}
          </p>
        )}

        <Button type="submit" disabled={pending || code.trim() === ''} className="w-full">
          {pending ? text.pending : text.submit}
        </Button>
      </form>
    </div>
  );
}
