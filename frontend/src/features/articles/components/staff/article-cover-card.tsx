'use client';

import { useId, useRef, useState, type ChangeEvent } from 'react';
import { buttonClass } from '@/components/ui/button';
import { Card, CardBody, CardHeader } from '@/components/ui/card';
import { ConfirmButton } from '@/features/integrations/components/confirm-button';
import { formMessages } from '@/features/settings/content/form-messages';
import { uploadToSignedUrl } from '@/lib/api/signed-upload';
import { reloadArticleAction, requestCoverAction, updateArticleAction } from '../../actions/manage-article-actions';
import { articleEditorText } from '../../content/article-editor';
import { COVER_MAX_BYTES, coverTypeOf } from '../../lib/cover-rules';
import type { StaffArticle } from '../../types/staff-article.types';
import { CoverPreview } from './cover-preview';

interface ArticleCoverCardProps {
  article: StaffArticle;
  onChange: (article: StaffArticle) => void;
}

/**
 * The cover picture. Picking a file asks the API for a signed URL, sends the file straight there
 * from the browser, then reads the article again: the file's arrival is what swaps the cover in,
 * and it moves the article's version on.
 */
export function ArticleCoverCard({ article, onChange }: ArticleCoverCardProps) {
  const text = articleEditorText.cover;
  const id = useId();
  const input = useRef<HTMLInputElement>(null);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string>();
  const [done, setDone] = useState(false);

  async function upload(file: File) {
    const contentType = coverTypeOf(file);
    if (!contentType) return setError(text.wrongType);
    if (file.size > COVER_MAX_BYTES) return setError(text.tooBig);

    setBusy(true);
    try {
      const started = await requestCoverAction({ id: article.id, version: article.version, contentType, sizeBytes: file.size });
      if (!started.ok) {
        return setError(started.error.code === 'version_conflict' ? formMessages.conflict : started.error.message);
      }

      if (!(await uploadToSignedUrl(started.data.upload, file))) {
        onChange(started.data.article);
        return setError(text.failed);
      }

      const fresh = await reloadArticleAction({ id: article.id });
      onChange(fresh.ok ? fresh.data : started.data.article);
      setDone(true);
    } catch {
      setError(formMessages.unreachable);
    } finally {
      setBusy(false);
      if (input.current) input.current.value = '';
    }
  }

  function picked(event: ChangeEvent<HTMLInputElement>) {
    setError(undefined);
    setDone(false);
    const file = event.target.files?.[0];
    if (file) void upload(file);
  }

  const hasCover = Boolean(article.cover_image_url || article.cover_image_path);

  return (
    <Card>
      <CardHeader title={text.heading} />
      <CardBody className="space-y-3">
        <CoverPreview article={article} />
        <p id={`${id}-hint`} className="text-xs text-ink-600">
          {text.hint}
        </p>

        <div className="flex flex-wrap items-center gap-3">
          <label className={buttonClass('secondary', 'sm', busy ? 'pointer-events-none opacity-60' : 'cursor-pointer')}>
            {busy ? text.uploading : text.choose}
            <input
              ref={input}
              type="file"
              accept="image/jpeg,image/png,image/webp"
              className="sr-only"
              disabled={busy}
              aria-describedby={`${id}-hint`}
              onChange={picked}
            />
          </label>

          {hasCover && (
            <ConfirmButton
              label={text.remove}
              confirmText={text.removeConfirm}
              confirmLabel={text.removeConfirmButton}
              pendingLabel={text.removing}
              cancelLabel={text.keep}
              variant="secondary"
              action={() => updateArticleAction({ id: article.id, version: article.version, changes: { remove_cover: true } })}
              onDone={onChange}
            />
          )}
        </div>

        {done && (
          <p role="status" className="text-sm text-emerald-700">
            {text.uploaded}
          </p>
        )}
        {error && (
          <p role="alert" className="text-sm text-red-700">
            {error}
          </p>
        )}
      </CardBody>
    </Card>
  );
}
