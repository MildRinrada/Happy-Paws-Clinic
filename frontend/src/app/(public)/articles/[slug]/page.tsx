import type { Metadata } from 'next';
import { notFound } from 'next/navigation';
import { SiteContainer } from '@/components/layout/site-container';
import { getArticle } from '@/features/articles/api/getArticle';
import { ArticleBody } from '@/features/articles/components/article-body';
import { ArticleCover } from '@/features/articles/components/article-cover';
import { ArticleHeader } from '@/features/articles/components/article-header';
import { ArticleJsonLd } from '@/features/seo/components/article-json-ld';
import { articleMetadata } from '@/features/seo/lib/article-metadata';
import { optional } from '@/lib/api/api-error';

interface Props {
  params: Promise<{ slug: string }>;
}

export async function generateMetadata({ params }: Props): Promise<Metadata> {
  const article = await optional(getArticle((await params).slug));

  return article ? articleMetadata(article) : {};
}

/**
 * One article, readable by anyone. A missing, draft, scheduled or deleted article is the 404 page.
 * Rendered on every request, so a fresh cover link comes with each visit; the staff screens also
 * revalidate this path when they publish, edit or take an article down.
 */
export default async function ArticlePage({ params }: Props) {
  const article = await optional(getArticle((await params).slug));

  if (!article) {
    notFound();
  }

  return (
    <main className="py-12">
      <SiteContainer>
        <article className="mx-auto max-w-[720px]">
          <ArticleHeader article={article} />

          <ArticleCover
            article={article}
            label="รูปประกอบบทความ"
            sizes="720px"
            className="mt-8 aspect-[16/9] w-full overflow-hidden rounded-2xl"
          />

          <div className="mt-8">
            <ArticleBody body={article.body} />
          </div>
        </article>
      </SiteContainer>

      <ArticleJsonLd article={article} />
    </main>
  );
}
