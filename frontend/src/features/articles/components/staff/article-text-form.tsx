'use client';

import { useRouter } from 'next/navigation';
import { useId, useState, type FormEvent } from 'react';
import { Button } from '@/components/ui/button';
import { Field, Input, Textarea } from '@/components/ui/field';
import { FormStatus } from '@/components/ui/form-status';
import { formMessages } from '@/features/settings/content/form-messages';
import { useLeaveWarning } from '@/features/settings/hooks/use-leave-warning';
import { sameValue } from '@/features/settings/lib/same-value';
import { useFormSubmit } from '@/hooks/use-form-submit';
import { createArticleAction, updateArticleAction } from '../../actions/manage-article-actions';
import { articleEditorText } from '../../content/article-editor';
import { draftFromArticle, emptyDraft, toArticleInput, type ArticleDraft } from '../../lib/article-draft';
import { articlePaths } from '../../lib/article-paths';
import type { StaffArticle } from '../../types/staff-article.types';
import { ArticleBodyField } from './article-body-field';
import { ArticlePreview } from './article-preview';

interface ArticleTextFormProps {
  /** The article being edited, or absent for a new draft. */
  article?: StaffArticle;
  /** Called with the article as saved, so the editor around the form has the new version. */
  onSaved?: (article: StaffArticle) => void;
}

/**
 * What an article says: title, address, summary, byline, reading time and body. A new article is
 * saved as a draft and opens in the editor; an existing one sends the version it was read at, so
 * a colleague's save in between is refused rather than overwritten.
 */
export function ArticleTextForm({ article, onSaved }: ArticleTextFormProps) {
  const text = articleEditorText;
  const id = useId();
  const router = useRouter();
  const [draft, setDraft] = useState<ArticleDraft>(() => (article ? draftFromArticle(article) : emptyDraft));
  const [savedDraft, setSavedDraft] = useState(draft);
  // Keeps its own save beside publishing and the cover, so it is not in the save bar; leaving still asks.
  useLeaveWarning('article-text-form', !sameValue(draft, savedDraft));
  const [minutesBad, setMinutesBad] = useState(false);
  const { errors, message, saved, pending, submit, touch } = useFormSubmit(formMessages);

  function set(field: keyof ArticleDraft, value: string) {
    touch();
    setDraft((current) => ({ ...current, [field]: value }));
  }

  function save(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const input = toArticleInput(draft);
    setMinutesBad(input === null);
    if (!input) return;

    if (!article) {
      submit(
        () => createArticleAction(input),
        (created) => router.push(articlePaths.edit(created.id)),
      );
      return;
    }

    const { slug, ...rest } = input;
    submit(
      () => updateArticleAction({ id: article.id, version: article.version, changes: article.slug_locked ? rest : { ...rest, slug } }),
      (stored) => {
        const next = draftFromArticle(stored);
        setDraft(next);
        setSavedDraft(next);
        onSaved?.(stored);
      },
    );
  }

  const field = (name: string) => `${id}-${name}`;
  const slugHint = !article ? text.fields.slugHintNew : article.slug_locked ? text.fields.slugLocked : text.fields.slugHintDraft;

  return (
    <form onSubmit={save} noValidate className="space-y-4">
      <Field label={text.fields.title} htmlFor={field('title')} error={errors.title}>
        <Input
          id={field('title')}
          value={draft.title}
          maxLength={200}
          aria-invalid={Boolean(errors.title)}
          onChange={(event) => set('title', event.target.value)}
        />
      </Field>

      <Field label={text.fields.slug} htmlFor={field('slug')} hint={slugHint} error={errors.slug}>
        <Input
          id={field('slug')}
          value={draft.slug}
          maxLength={80}
          spellCheck={false}
          autoCapitalize="none"
          disabled={article?.slug_locked}
          aria-invalid={Boolean(errors.slug)}
          className="font-mono"
          onChange={(event) => set('slug', event.target.value)}
        />
      </Field>

      <Field label={text.fields.summary} htmlFor={field('summary')} hint={text.fields.summaryHint} error={errors.summary}>
        <Textarea
          id={field('summary')}
          rows={2}
          value={draft.summary}
          maxLength={500}
          aria-invalid={Boolean(errors.summary)}
          onChange={(event) => set('summary', event.target.value)}
        />
      </Field>

      <div className="grid gap-4 sm:grid-cols-[1fr_12rem]">
        <Field label={text.fields.author} htmlFor={field('author')} hint={text.fields.authorHint} error={errors.author_name}>
          <Input
            id={field('author')}
            value={draft.author_name}
            maxLength={120}
            aria-invalid={Boolean(errors.author_name)}
            onChange={(event) => set('author_name', event.target.value)}
          />
        </Field>
        <Field
          label={text.fields.minutes}
          htmlFor={field('minutes')}
          hint={article ? undefined : text.fields.minutesHint}
          error={minutesBad ? text.minutesBad : errors.reading_minutes}
        >
          <Input
            id={field('minutes')}
            inputMode="numeric"
            value={draft.minutes}
            className="tabular-nums"
            aria-invalid={minutesBad || Boolean(errors.reading_minutes)}
            onChange={(event) => set('minutes', event.target.value)}
          />
        </Field>
      </div>

      <ArticleBodyField
        id={field('body')}
        value={draft.body}
        error={errors.body}
        onChange={(value) => set('body', value)}
        preview={
          <ArticlePreview
            title={draft.title}
            summary={draft.summary}
            author={draft.author_name}
            minutes={Number(draft.minutes) || 1}
            body={draft.body}
          />
        }
      />

      <div className="flex flex-wrap items-center justify-end gap-3 border-t border-ink-100 pt-4">
        <FormStatus message={message} saved={saved} savedText={article?.status === 'published' ? text.savedLive : text.saved} />
        <Button type="submit" disabled={pending}>
          {article ? (pending ? text.saving : text.save) : pending ? text.creating : text.create}
        </Button>
      </div>
    </form>
  );
}
