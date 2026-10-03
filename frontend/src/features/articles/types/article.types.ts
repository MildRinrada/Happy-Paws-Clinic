/** An article as `GET /articles` lists it: everything but the body. */
export interface ArticleSummary {
  id: string;
  slug: string;
  title: string;
  summary: string;
  author_name: string;
  reading_minutes: number;
  /** Path under `/public`. Absent until a picture is supplied. */
  cover_image_path?: string;
  published_at: string;
}

/** One whole article, from `GET /articles/{slug}`. */
export interface Article extends ArticleSummary {
  /** Plain text; a blank line separates paragraphs. Rendered as text, never as HTML. */
  body: string;
}
