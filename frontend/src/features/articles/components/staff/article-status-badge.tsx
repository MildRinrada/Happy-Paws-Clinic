import { Badge } from '@/components/ui/badge';
import { articleStatusLabels } from '../../content/staff-articles';
import type { ArticleStatus } from '../../types/staff-article.types';

/** Draft, scheduled or published, in the clinic's words. */
export function ArticleStatusBadge({ status }: { status: ArticleStatus }) {
  const label = articleStatusLabels[status];

  return <Badge tone={label.tone}>{label.text}</Badge>;
}
