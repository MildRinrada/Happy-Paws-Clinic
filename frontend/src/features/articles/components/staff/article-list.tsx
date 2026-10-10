import type { ReactNode } from 'react';
import { Card, CardBody, CardHeader } from '@/components/ui/card';
import { PageLinks } from '@/features/crm/components/shared/page-links';
import { staffArticlesText } from '../../content/staff-articles';
import type { StaffArticleSummary } from '../../types/staff-article.types';
import { ArticleListEntry } from './article-list-entry';

interface ArticleListProps {
  articles: StaffArticleSummary[];
  filters: ReactNode;
  nextHref?: string;
  firstHref?: string;
}

/** Every article that is not deleted, drafts included, newest written first. */
export function ArticleList({ articles, filters, nextHref, firstHref }: ArticleListProps) {
  const text = staffArticlesText;

  return (
    <Card>
      <CardHeader title={text.heading} />
      <CardBody className="space-y-4">
        <p className="text-sm text-ink-600">{text.hint}</p>
        {filters}
        {articles.length === 0 ? (
          <p className="text-sm text-ink-500">{text.empty}</p>
        ) : (
          <ul className="divide-y divide-ink-100">
            {articles.map((article) => (
              <ArticleListEntry key={article.id} article={article} />
            ))}
          </ul>
        )}
        <PageLinks nextHref={nextHref} firstHref={firstHref} nextLabel={text.next} firstLabel={text.first} />
      </CardBody>
    </Card>
  );
}
