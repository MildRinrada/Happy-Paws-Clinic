'use client';

import Link from 'next/link';
import { useState, useTransition } from 'react';
import { Button } from '@/components/ui/button';
import { Card, CardBody, CardHeader } from '@/components/ui/card';
import { ConfirmButton } from '@/features/integrations/components/confirm-button';
import { formMessages } from '@/features/settings/content/form-messages';
import { formatDateTime } from '@/lib/format/datetime';
import { publishArticleAction, unpublishArticleAction } from '../../actions/manage-article-actions';
import { articleEditorText } from '../../content/article-editor';
import { articleHref } from '../../lib/article-text';
import type { StaffArticle } from '../../types/staff-article.types';
import { ArticleStatusBadge } from './article-status-badge';

interface ArticlePublishCardProps {
  article: StaffArticle;
  onChange: (article: StaffArticle) => void;
}

/** Where the article stands with the public, and the one button that changes it. */
export function ArticlePublishCard({ article, onChange }: ArticlePublishCardProps) {
  const text = articleEditorText.status;
  const [error, setError] = useState<string>();
  const [pending, startTransition] = useTransition();
  const live = article.status !== 'draft';

  function publish() {
    setError(undefined);
    startTransition(async () => {
      try {
        const result = await publishArticleAction({ id: article.id, version: article.version });
        if (result.ok) onChange(result.data);
        else setError(result.error.code === 'version_conflict' ? formMessages.conflict : result.error.message);
      } catch {
        setError(formMessages.unreachable);
      }
    });
  }

  const where =
    article.status === 'published' && article.published_at
      ? text.published(formatDateTime(article.published_at))
      : article.status === 'scheduled' && article.published_at
        ? text.scheduled(formatDateTime(article.published_at))
        : text.draft;

  return (
    <Card>
      <CardHeader title={text.heading} aside={<ArticleStatusBadge status={article.status} />} />
      <CardBody className="space-y-3">
        <p className="text-sm text-ink-700">{where}</p>

        {live ? (
          <div className="flex flex-wrap items-center gap-3">
            {article.status === 'published' && (
              <Link href={articleHref(article.slug)} target="_blank" className="text-sm text-brand-700 hover:underline">
                {text.view}
              </Link>
            )}
            <ConfirmButton
              label={text.unpublish}
              confirmText={text.unpublishConfirm}
              confirmLabel={text.unpublishConfirmButton}
              pendingLabel={text.unpublishing}
              cancelLabel={text.keep}
              variant="secondary"
              action={() => unpublishArticleAction({ id: article.id, version: article.version })}
              onDone={onChange}
            />
          </div>
        ) : (
          <div className="flex flex-wrap items-center gap-3">
            <Button size="sm" disabled={pending} onClick={publish}>
              {pending ? text.publishing : text.publish}
            </Button>
            <span className="text-xs text-ink-600">{article.first_published_at ? text.republishNote : text.publishNote}</span>
          </div>
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
