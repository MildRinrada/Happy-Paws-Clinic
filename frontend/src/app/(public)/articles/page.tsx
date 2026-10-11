import { SiteContainer } from '@/components/layout/site-container';
import { ArticleListing } from '@/features/articles/components/list/article-listing';
import { ArticlesCta } from '@/features/articles/components/list/articles-cta';
import { ArticlesHeading } from '@/features/articles/components/list/articles-heading';
import { articlesMeta } from '@/features/articles/content/articles-page';
import { loadArticleList } from '@/features/articles/lib/load-article-list';
import { readArticleListQuery } from '@/features/articles/lib/read-article-list-query';
import { BreadcrumbJsonLd } from '@/features/seo/components/breadcrumb-json-ld';
import { sitePages } from '@/features/seo/content/seo';
import { pageMetadata } from '@/features/seo/lib/page-metadata';

export const metadata = pageMetadata({ ...articlesMeta, path: sitePages.articles.path });

interface Props {
  searchParams: Promise<Record<string, string | string[] | undefined>>;
}

/**
 * Published articles, newest first: a heading with a search box, the newest article large, the
 * rest in a grid, and a grey band to book or get in touch. `?q=` searches, and `?tag=` and
 * `?cursor=` are ready for topics and paging. Everything is read through `loadArticleList`, on
 * each request, so a new article shows at once.
 */
export default async function ArticlesPage({ searchParams }: Props) {
  const query = readArticleListQuery(await searchParams);
  const list = await loadArticleList(query);

  return (
    <main>
      <ArticlesHeading q={query.q} tag={query.tag} />
      <SiteContainer className="pb-16">
        <ArticleListing list={list} query={query} />
      </SiteContainer>
      <ArticlesCta />

      <BreadcrumbJsonLd trail={[sitePages.articles]} />
    </main>
  );
}
