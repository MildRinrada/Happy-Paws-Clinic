'use client';

import { useId } from 'react';
import { Card, CardBody, CardHeader } from '@/components/ui/card';
import { Field, Input, Textarea } from '@/components/ui/field';
import { editorText } from '../../content/staff-privacy-policy';
import type { SectionDraft } from '../../lib/section-draft';
import { SectionOrderButtons } from './section-order-buttons';

interface SectionEditorCardProps {
  section: SectionDraft;
  index: number;
  count: number;
  /** The API's messages for this section, by field: `heading`, `anchor`, `body`. */
  errors: Partial<Record<'heading' | 'anchor' | 'body', string>>;
  onChange: (changes: Partial<SectionDraft>) => void;
  onMove: (by: -1 | 1) => void;
  onRemove: () => void;
}

/** One section of a draft: its heading, its anchor, and its body as plain text. */
export function SectionEditorCard({ section, index, count, errors, onChange, onMove, onRemove }: SectionEditorCardProps) {
  const id = useId();
  const text = editorText;
  const field = (name: string) => `${id}-${name}`;
  const title = section.heading.trim() || text.sectionHeading(index);

  return (
    <Card>
      <CardHeader
        title={<span className="break-words">{text.sectionHeading(index)}</span>}
        aside={<SectionOrderButtons index={index} count={count} heading={title} onMove={onMove} onRemove={onRemove} />}
      />
      <CardBody className="space-y-4">
        <div className="grid gap-4 sm:grid-cols-[minmax(0,1fr)_14rem]">
          <Field label={text.fields.heading} htmlFor={field('heading')} error={errors.heading}>
            <Input
              id={field('heading')}
              value={section.heading}
              maxLength={150}
              aria-invalid={Boolean(errors.heading)}
              aria-describedby={errors.heading ? `${field('heading')}-error` : undefined}
              onChange={(event) => onChange({ heading: event.target.value })}
            />
          </Field>
          <Field label={text.fields.anchor} htmlFor={field('anchor')} hint={text.fields.anchorHint} error={errors.anchor}>
            <Input
              id={field('anchor')}
              value={section.anchor}
              maxLength={40}
              spellCheck={false}
              autoCapitalize="none"
              className="font-mono"
              aria-invalid={Boolean(errors.anchor)}
              aria-describedby={errors.anchor ? `${field('anchor')}-error` : undefined}
              onChange={(event) => onChange({ anchor: event.target.value })}
            />
          </Field>
        </div>

        <Field label={text.fields.body} htmlFor={field('body')} hint={text.fields.count(section.body.length)} error={errors.body}>
          <Textarea
            id={field('body')}
            rows={12}
            value={section.body}
            maxLength={10_000}
            className="font-[inherit] text-[15px]"
            aria-invalid={Boolean(errors.body)}
            aria-describedby={errors.body ? `${field('body')}-error` : undefined}
            onChange={(event) => onChange({ body: event.target.value })}
          />
        </Field>
      </CardBody>
    </Card>
  );
}
