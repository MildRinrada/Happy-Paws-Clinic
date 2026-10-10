/** An article as `GET /articles` lists it: everything but the body. */
export interface ArticleSummary {
  id: string;
  slug: string;
  title: string;
  summary: string;
  author_name: string;
  reading_minutes: number;
  /** Path under `/public`, from the seed. Absent otherwise. */
  cover_image_path?: string;
  /**
   * A signed link to the cover staff uploaded, good for an hour (docs/contract-extensions.md § 26).
   * Present only while the article is published. Wins over `cover_image_path`.
   */
  cover_image_url?: string;
  published_at: string;
}

/** One whole article, from `GET /articles/{slug}`. */
export interface Article extends ArticleSummary {
  /** Plain text; a blank line separates paragraphs. Rendered as text, never as HTML. */
  body: string;
}
