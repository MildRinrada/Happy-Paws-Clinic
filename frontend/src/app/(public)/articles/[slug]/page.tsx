import type { Metadata } from 'next';
import { notFound } from 'next/navigation';
import { SiteContainer } from '@/components/layout/site-container';
import { ImageSlot } from '@/components/shared/image-slot';
import { getArticle } from '@/features/articles/api/getArticle';
import { ArticleBody } from '@/features/articles/components/article-body';
import { ArticleHeader } from '@/features/articles/components/article-header';
import { optional } from '@/lib/api/api-error';

interface Props {
  params: Promise<{ slug: string }>;
}

export async function generateMetadata({ params }: Props): Promise<Metadata> {
  const article = await optional(getArticle((await params).slug));

  return article ? { title: `${article.title} | Happy Paws Clinic`, description: article.summary } : {};
}

/** One article, readable by anyone. A missing or not yet published article is the 404 page. */
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

          <ImageSlot
            src={article.cover_image_path}
            alt=""
            label="รูปประกอบบทความ"
            sizes="720px"
            className="mt-8 aspect-[16/9] w-full overflow-hidden rounded-2xl"
          />

          <div className="mt-8">
            <ArticleBody body={article.body} />
          </div>
        </article>
      </SiteContainer>
    </main>
  );
}
