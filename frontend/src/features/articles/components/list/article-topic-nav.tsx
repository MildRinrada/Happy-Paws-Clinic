import { ChipNav } from '@/components/site/chip-nav';
import { DotChip } from '@/components/site/dot-chip';
import { articleTopics } from '../../content/articles-page';
import { articlesHref } from '../../lib/articles-href';
import type { ArticleTagCount } from '../../types/article-list.types';

interface Props {
  tags: ArticleTagCount[];
  /** The topic on screen now. None means "ทั้งหมด". */
  current?: string;
  /** The search on screen now, kept when changing topic. */
  q?: string;
}

/**
 * "ทั้งหมด" then one chip per topic, the one on screen filled dark green. Not drawn at all while
 * there are no topics, since "ทั้งหมด" alone would choose nothing.
 */
export function ArticleTopicNav({ tags, current, q }: Props) {
  if (tags.length === 0) {
    return null;
  }

  return (
    <ChipNav label={articleTopics.label} layout="inline">
      <DotChip href={articlesHref({ q })} label={articleTopics.all} current={!current} />
      {tags.map(({ tag }) => (
        <DotChip key={tag} href={articlesHref({ q, tag })} label={tag} current={tag === current} />
      ))}
    </ChipNav>
  );
}
