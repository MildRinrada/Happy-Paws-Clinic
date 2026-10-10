'use client';

import { useState } from 'react';
import { Card, CardBody } from '@/components/ui/card';
import type { StaffArticle } from '../../types/staff-article.types';
import { ArticleCoverCard } from './article-cover-card';
import { ArticlePublishCard } from './article-publish-card';
import { ArticleTextForm } from './article-text-form';
import { DeleteArticleCard } from './delete-article-card';

/**
 * One article's editor. Every part changes the article's version (saving text, publishing, a new
 * cover arriving), so the article as last answered by the API lives here and each part reads its
 * version from it. The text being typed stays in the text form and is not lost when a cover lands.
 */
export function ArticleEditor({ article }: { article: StaffArticle }) {
  const [current, setCurrent] = useState(article);

  return (
    <div className="grid gap-5 lg:grid-cols-[1fr_20rem]">
      <Card>
        <CardBody>
          <ArticleTextForm article={current} onSaved={setCurrent} />
        </CardBody>
      </Card>

      <div className="space-y-5">
        <ArticlePublishCard article={current} onChange={setCurrent} />
        <ArticleCoverCard article={current} onChange={setCurrent} />
        <DeleteArticleCard id={current.id} version={current.version} />
      </div>
    </div>
  );
}
