'use client';

import { useEffect, useId, useRef, useState } from 'react';

interface BriefTextProps {
  text: string;
  more: string;
  less: string;
}

/**
 * The brief's sentences, cut to three lines until asked for the rest. The button appears only when
 * the text is in fact longer than three lines, which only the browser can measure; before that the
 * text is simply clamped, so the server and the first client render agree.
 */
export function BriefText({ text, more, less }: BriefTextProps) {
  const id = useId();
  const paragraph = useRef<HTMLParagraphElement>(null);
  const [open, setOpen] = useState(false);
  const [long, setLong] = useState(false);

  useEffect(() => {
    const element = paragraph.current;
    if (!element || open) return;

    const measure = () => setLong(element.scrollHeight > element.clientHeight + 1);
    const observer = new ResizeObserver(measure);
    observer.observe(element);

    return () => observer.disconnect();
  }, [open, text]);

  return (
    <div className="space-y-1">
      <p
        id={id}
        ref={paragraph}
        className={`whitespace-pre-line text-sm leading-relaxed text-ink-900 ${open ? '' : 'line-clamp-3'}`}
      >
        {text}
      </p>
      {(long || open) && (
        <button
          type="button"
          aria-expanded={open}
          aria-controls={id}
          onClick={() => setOpen((value) => !value)}
          className="text-sm font-medium text-brand-700 hover:underline"
        >
          {open ? less : more}
        </button>
      )}
    </div>
  );
}
