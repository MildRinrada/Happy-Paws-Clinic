import Link from 'next/link';
import { EmptyState } from '@/components/ui/empty-state';
import { articlesList } from '../../content/articles-page';
import { articlesHref } from '../../lib/articles-href';

interface Props {
  /** What happened, such as nothing matching the search. */
  title: string;
  /** A suggestion under the title. */
  hint?: string;
  /** Adds the link back to every article, after a search or a topic that found nothing. */
  showAll?: boolean;
}

/** Why there are no articles to show, and, after a search or topic, the way back to all of them. */
export function ArticleListMessage({ title, hint, showAll = false }: Props) {
  return (
    <EmptyState title={title}>
      {(hint || showAll) && (
        <>
          {hint && `${hint} `}
          {showAll && (
            <Link href={articlesHref()} className="font-semibold text-site-teal underline">
              {articlesList.showAll}
            </Link>
          )}
        </>
      )}
    </EmptyState>
  );
}
