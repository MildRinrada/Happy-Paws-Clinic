import { staffApi } from '@/lib/api/server-client';
import type {
  ArticleCoverUpload,
  ArticleInput,
  ArticleStatus,
  CoverType,
  StaffArticle,
  StaffArticlePage,
} from '../types/staff-article.types';

/**
 * The staff side of articles (docs/contract-extensions.md § 26). Every route needs `content:manage`.
 * Not in the API contract; the routes and the scope were approved together.
 */

/** `GET /staff/articles?status=&q=&cursor=` — drafts included, newest written first. */
export function listStaffArticles(query: { status?: ArticleStatus; q?: string; cursor?: string }): Promise<StaffArticlePage> {
  return staffApi().get<StaffArticlePage>('/staff/articles', { query: { ...query, limit: 25 } });
}

/** `GET /staff/articles/{id}` — one article for the editor, draft or not. */
export function getStaffArticle(id: string): Promise<StaffArticle> {
  return staffApi().get<StaffArticle>(`/staff/articles/${encodeURIComponent(id)}`);
}

/** `POST /staff/articles` — a draft. Nothing is public until it is published. */
export function createArticle(input: ArticleInput): Promise<StaffArticle> {
  return staffApi().post<StaffArticle>('/staff/articles', { body: input });
}

/** `PATCH /staff/articles/{id}` — `If-Match` is required, so the version read always goes back. */
export function updateArticle(
  id: string,
  changes: Partial<ArticleInput> & { remove_cover?: boolean },
  version: number,
): Promise<StaffArticle> {
  return staffApi().patch<StaffArticle>(`/staff/articles/${encodeURIComponent(id)}`, { body: changes, ifMatch: version });
}

/** `POST /staff/articles/{id}/publish` — on the public site now. */
export function publishArticle(id: string, version: number): Promise<StaffArticle> {
  return staffApi().post<StaffArticle>(`/staff/articles/${encodeURIComponent(id)}/publish`, { ifMatch: version });
}

/** `POST /staff/articles/{id}/unpublish` — off the public site, a draft again. */
export function unpublishArticle(id: string, version: number): Promise<StaffArticle> {
  return staffApi().post<StaffArticle>(`/staff/articles/${encodeURIComponent(id)}/unpublish`, { ifMatch: version });
}

/** `DELETE /staff/articles/{id}` — for good. The slug stays taken. */
export function deleteArticle(id: string, version: number): Promise<void> {
  return staffApi().delete<void>(`/staff/articles/${encodeURIComponent(id)}`, { ifMatch: version });
}

/** `POST /staff/articles/{id}/cover` — the signed URL to send a new cover to. */
export function requestArticleCover(
  id: string,
  file: { content_type: CoverType; size_bytes: number },
  version: number,
): Promise<ArticleCoverUpload> {
  return staffApi().post<ArticleCoverUpload>(`/staff/articles/${encodeURIComponent(id)}/cover`, { body: file, ifMatch: version });
}
