'use server';

import { revalidatePath } from 'next/cache';
import { z } from 'zod';
import { runAction, type ActionResult } from '@/lib/actions/action-result';
import {
  createArticle,
  deleteArticle,
  getStaffArticle,
  publishArticle,
  requestArticleCover,
  unpublishArticle,
  updateArticle,
} from '../api/manageArticles';
import { articlePaths } from '../lib/article-paths';
import type { ArticleCoverUpload, StaffArticle } from '../types/staff-article.types';

/**
 * Writing and publishing articles (docs/contract-extensions.md § 26). The backend decides the
 * rules: slugs, the version, what may be published. These check the shape, make the call, and mark
 * the pages that show articles stale, the public ones included, so a change is on the site at once.
 */

const id = z.string().trim().min(1).max(40);
const version = z.number().int().nonnegative();

const fields = z.object({
  title: z.string().max(400),
  slug: z.string().max(200).optional(),
  summary: z.string().max(1_000),
  body: z.string().max(40_000),
  author_name: z.string().max(240),
  reading_minutes: z.number().int().min(1).max(120).optional(),
});

/** The public pages that list or show articles, and the staff list. */
function refreshArticlePages(articleId?: string) {
  revalidatePath('/');
  revalidatePath('/articles');
  revalidatePath('/articles/[slug]', 'page');
  revalidatePath(articlePaths.list);

  if (articleId) {
    revalidatePath(articlePaths.edit(articleId));
  }
}

export async function createArticleAction(values: z.infer<typeof fields>): Promise<ActionResult<StaffArticle>> {
  const input = fields.parse(values);
  const result = await runAction(() => createArticle({ ...input, slug: input.slug?.trim() || undefined }));

  if (result.ok) refreshArticlePages();
  return result;
}

const edit = z.object({ id, version, changes: fields.partial().extend({ remove_cover: z.boolean().optional() }) });

export async function updateArticleAction(values: z.infer<typeof edit>): Promise<ActionResult<StaffArticle>> {
  const input = edit.parse(values);
  const result = await runAction(() => updateArticle(input.id, input.changes, input.version));

  if (result.ok) refreshArticlePages(input.id);
  return result;
}

const step = z.object({ id, version });

export async function publishArticleAction(values: z.infer<typeof step>): Promise<ActionResult<StaffArticle>> {
  const input = step.parse(values);
  const result = await runAction(() => publishArticle(input.id, input.version));

  if (result.ok) refreshArticlePages(input.id);
  return result;
}

export async function unpublishArticleAction(values: z.infer<typeof step>): Promise<ActionResult<StaffArticle>> {
  const input = step.parse(values);
  const result = await runAction(() => unpublishArticle(input.id, input.version));

  if (result.ok) refreshArticlePages(input.id);
  return result;
}

export async function deleteArticleAction(values: z.infer<typeof step>): Promise<ActionResult<undefined>> {
  const input = step.parse(values);
  const result = await runAction(async () => {
    await deleteArticle(input.id, input.version);
    return undefined;
  });

  if (result.ok) refreshArticlePages();
  return result;
}

const cover = z.object({
  id,
  version,
  contentType: z.enum(['image/jpeg', 'image/png', 'image/webp']),
  sizeBytes: z.number().int().positive().max(5 * 1024 * 1024),
});

/** Asks for the signed URL; the browser then sends the file there itself. */
export async function requestCoverAction(values: z.infer<typeof cover>): Promise<ActionResult<ArticleCoverUpload>> {
  const input = cover.parse(values);

  return runAction(() =>
    requestArticleCover(input.id, { content_type: input.contentType, size_bytes: input.sizeBytes }, input.version),
  );
}

/**
 * Reads the article again after its cover file arrived. The arrival changed the article's version,
 * and the public pages show the new picture.
 */
export async function reloadArticleAction(values: { id: string }): Promise<ActionResult<StaffArticle>> {
  const articleId = id.parse(values.id);
  const result = await runAction(() => getStaffArticle(articleId));

  if (result.ok) refreshArticlePages(articleId);
  return result;
}
