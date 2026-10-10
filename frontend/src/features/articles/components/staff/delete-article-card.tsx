'use client';

import { useRouter } from 'next/navigation';
import { Card, CardBody, CardHeader } from '@/components/ui/card';
import { ConfirmButton } from '@/features/integrations/components/confirm-button';
import { deleteArticleAction } from '../../actions/manage-article-actions';
import { articleEditorText } from '../../content/article-editor';
import { articlePaths } from '../../lib/article-paths';

/** Deleting for good, after a second press that says it cannot be undone. Goes back to the list. */
export function DeleteArticleCard({ id, version }: { id: string; version: number }) {
  const text = articleEditorText.remove;
  const router = useRouter();

  return (
    <Card>
      <CardHeader title={text.heading} />
      <CardBody className="space-y-3">
        <p className="text-sm text-ink-600">{text.note}</p>
        <ConfirmButton
          label={text.button}
          confirmText={text.confirm}
          confirmLabel={text.confirmButton}
          pendingLabel={text.pending}
          cancelLabel={text.keep}
          action={() => deleteArticleAction({ id, version })}
          onDone={() => router.push(articlePaths.list)}
        />
      </CardBody>
    </Card>
  );
}
