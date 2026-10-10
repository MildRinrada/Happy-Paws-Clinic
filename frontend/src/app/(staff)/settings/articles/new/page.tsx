import type { Metadata } from 'next';
import { Alert } from '@/components/ui/alert';
import { BackLink } from '@/components/ui/back-link';
import { Card, CardBody } from '@/components/ui/card';
import { PageHeader } from '@/components/ui/page-header';
import { loadStaffContext } from '@/features/account/lib/staff-context';
import { ArticleTextForm } from '@/features/articles/components/staff/article-text-form';
import { articleEditorText } from '@/features/articles/content/article-editor';
import { staffArticlesText } from '@/features/articles/content/staff-articles';
import { articlePaths } from '@/features/articles/lib/article-paths';
import { Scope } from '@/lib/auth/scopes';

export const metadata: Metadata = { title: articleEditorText.newMetaTitle };

/** A new article, saved as a draft; the editor it opens in adds the cover and publishes. */
export default async function NewArticlePage() {
  const context = await loadStaffContext();

  if (!context?.can(Scope.ContentManage)) {
    return <Alert tone="info">{staffArticlesText.noAccess}</Alert>;
  }

  return (
    <div className="max-w-3xl space-y-6">
      <BackLink href={articlePaths.list}>{articleEditorText.back}</BackLink>
      <PageHeader title={articleEditorText.newTitle} subtitle={articleEditorText.newSubtitle} />
      <Card>
        <CardBody>
          <ArticleTextForm />
        </CardBody>
      </Card>
    </div>
  );
}
