'use client';

import { useState, type ReactNode } from 'react';
import { Button } from '@/components/ui/button';
import { Field, Textarea } from '@/components/ui/field';
import { articleEditorText } from '../../content/article-editor';

interface ArticleBodyFieldProps {
  id: string;
  value: string;
  error?: string;
  onChange: (value: string) => void;
  /** What "ดูตัวอย่าง" shows in place of the text box. */
  preview: ReactNode;
}

/** The body: a plain text box, or the preview of the page it will make. */
export function ArticleBodyField({ id, value, error, onChange, preview }: ArticleBodyFieldProps) {
  const text = articleEditorText;
  const [previewing, setPreviewing] = useState(false);

  return (
    <div className="space-y-2">
      <div className="flex flex-wrap items-center gap-2" role="group" aria-label={text.fields.body}>
        <Button size="sm" variant={previewing ? 'ghost' : 'secondary'} aria-pressed={!previewing} onClick={() => setPreviewing(false)}>
          {text.write}
        </Button>
        <Button size="sm" variant={previewing ? 'secondary' : 'ghost'} aria-pressed={previewing} onClick={() => setPreviewing(true)}>
          {text.preview}
        </Button>
        <span className="ml-auto text-xs tabular-nums text-ink-500">{text.fields.count(value.length)}</span>
      </div>

      {previewing ? (
        preview
      ) : (
        <Field label={text.fields.body} htmlFor={id} hint={text.fields.bodyHint} error={error}>
          <Textarea
            id={id}
            rows={18}
            value={value}
            maxLength={20_000}
            aria-invalid={Boolean(error)}
            aria-describedby={error ? `${id}-error` : undefined}
            className="font-[inherit] text-[15px]"
            onChange={(event) => onChange(event.target.value)}
          />
        </Field>
      )}
    </div>
  );
}
