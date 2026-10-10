/** Where an article stands with the public, worked out by the API each time it is read. */
export type ArticleStatus = 'draft' | 'scheduled' | 'published';

/** A row of `GET /staff/articles`: drafts included, no body (docs/contract-extensions.md § 26). */
export interface StaffArticleSummary {
  id: string;
  slug: string;
  title: string;
  summary: string;
  author_name: string;
  reading_minutes: number;
  status: ArticleStatus;
  published_at?: string;
  first_published_at?: string;
  /** True once it has ever been published: the slug can no longer change. */
  slug_locked: boolean;
  has_cover: boolean;
  created_at: string;
  updated_at?: string;
  version: number;
}

/** One article as the editor opens it, from `GET /staff/articles/{id}`. */
export interface StaffArticle extends Omit<StaffArticleSummary, 'has_cover'> {
  /** Plain text; a blank line separates paragraphs. */
  body: string;
  /** A seeded picture under `/public`. */
  cover_image_path?: string;
  /** A signed link to the uploaded cover, good for half an hour. */
  cover_image_url?: string;
  /** True while an upload URL is out and its file has not arrived. */
  cover_pending: boolean;
}

/** What the editor sends. `slug` may be left empty on a new article with an English title. */
export interface ArticleInput {
  title: string;
  slug?: string;
  summary: string;
  body: string;
  author_name: string;
  reading_minutes?: number;
}

/** The answer of `POST /staff/articles/{id}/cover`: the article, and where to send the file. */
export interface ArticleCoverUpload {
  article: StaffArticle;
  upload: { url: string; method: string; headers: Record<string, string>; expires_at: string };
}

/** The media types the API takes for a cover. */
export type CoverType = 'image/jpeg' | 'image/png' | 'image/webp';

export interface StaffArticlePage {
  data: StaffArticleSummary[];
  page: { next_cursor?: string | null };
}
