import type { Metadata } from 'next';
import { Alert } from '@/components/ui/alert';
import { BackLink } from '@/components/ui/back-link';
import { PageHeader } from '@/components/ui/page-header';
import { loadStaffContext } from '@/features/account/lib/staff-context';
import { getStaffArticle } from '@/features/articles/api/manageArticles';
import { ArticleEditor } from '@/features/articles/components/staff/article-editor';
import { articleEditorText } from '@/features/articles/content/article-editor';
import { staffArticlesText } from '@/features/articles/content/staff-articles';
import { articlePaths } from '@/features/articles/lib/article-paths';
import { optional } from '@/lib/api/api-error';
import { Scope } from '@/lib/auth/scopes';
import { authed } from '@/lib/auth/session';

export const metadata: Metadata = { title: articleEditorText.editMetaTitle };

/** One article's editor: its text, its cover, publishing, and deleting (docs/contract-extensions.md § 26). */
export default async function EditArticlePage({ params }: { params: Promise<{ id: string }> }) {
  const [context, { id }] = await Promise.all([loadStaffContext(), params]);

  if (!context?.can(Scope.ContentManage)) {
    return <Alert tone="info">{staffArticlesText.noAccess}</Alert>;
  }

  const article = await authed(() => optional(getStaffArticle(id)));

  return (
    <div className="space-y-6">
      <BackLink href={articlePaths.list}>{articleEditorText.back}</BackLink>
      {article ? (
        <>
          <PageHeader title={articleEditorText.editTitle} subtitle={`/articles/${article.slug}`} />
          <ArticleEditor key={article.id} article={article} />
        </>
      ) : (
        <Alert tone="info">{articleEditorText.missing}</Alert>
      )}
    </div>
  );
}
