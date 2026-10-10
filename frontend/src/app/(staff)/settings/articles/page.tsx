import type { Metadata } from 'next';
import Link from 'next/link';
import { Alert } from '@/components/ui/alert';
import { buttonClass } from '@/components/ui/button';
import { PageHeader } from '@/components/ui/page-header';
import { loadStaffContext } from '@/features/account/lib/staff-context';
import { listStaffArticles } from '@/features/articles/api/manageArticles';
import { ArticleFilters } from '@/features/articles/components/staff/article-filters';
import { ArticleList } from '@/features/articles/components/staff/article-list';
import { staffArticlesText } from '@/features/articles/content/staff-articles';
import { articlePaths } from '@/features/articles/lib/article-paths';
import { readArticleQuery } from '@/features/articles/lib/read-article-query';
import { withCursor } from '@/features/crm/lib/read-list-query';
import { Scope } from '@/lib/auth/scopes';
import { authed } from '@/lib/auth/session';

export const metadata: Metadata = { title: staffArticlesText.metaTitle };

/**
 * The health articles on the public site, drafts included (docs/contract-extensions.md § 26), on
 * `content:manage`. Opening one leads to its editor.
 */
export default async function ArticlesSettingsPage({
  searchParams,
}: {
  searchParams: Promise<Record<string, string | string[] | undefined>>;
}) {
  const [context, params] = await Promise.all([loadStaffContext(), searchParams]);

  if (!context?.can(Scope.ContentManage)) {
    return <Alert tone="info">{staffArticlesText.noAccess}</Alert>;
  }

  const query = readArticleQuery(params);
  const page = await authed(() => listStaffArticles(query));
  const filters = { q: query.q, status: query.status };

  return (
    <div className="space-y-6">
      <PageHeader
        title={staffArticlesText.title}
        subtitle={staffArticlesText.subtitle}
        actions={
          <Link href={articlePaths.create} className={buttonClass('primary', 'md')}>
            {staffArticlesText.create}
          </Link>
        }
      />
      <ArticleList
        articles={page.data}
        filters={<ArticleFilters q={query.q} status={query.status} />}
        nextHref={page.page.next_cursor ? withCursor(articlePaths.list, filters, page.page.next_cursor) : undefined}
        firstHref={query.cursor ? withCursor(articlePaths.list, filters) : undefined}
      />
    </div>
  );
}
