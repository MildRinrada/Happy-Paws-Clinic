import { at, nowIso, table, type Row } from '../../core/db';
import { fileUrl, getFile, signedUpload } from '../../core/files';
import { checkVersion, created, fail, invalid, matches, notFound, page, requireStaff, route, stateConflict, type Ctx } from '../../core/http';
import { SAMPLE_ARTICLES, type SampleArticle } from './articles-data';

/**
 * Articles: the public list and page, and the staff editor (docs/contract-extensions.md § 9, § 26).
 * A row keeps the editor's shape plus what the API works out on each read: the status comes from
 * `published_at` against now, a cover from a seeded photo, a drawn cover, or a file staff uploaded.
 */

export interface ArticleRow extends Row {
  slug: string;
  title: string;
  summary: string;
  body: string;
  author_name: string;
  reading_minutes: number;
  tags: string[];
  published_at: string | null;
  first_published_at: string | null;
  cover_image_path?: string;
  /** A drawn cover for a seeded article without a photo. */
  cover_tone?: SampleArticle['cover_tone'];
  /** The uploaded cover in the demo's file store. */
  cover_file_id?: string;
  /** An upload handed out whose bytes have not arrived yet. */
  pending_cover_file_id?: string;
  created_at: string;
  updated_at?: string;
  deleted_at?: string | null;
  version: number;
}

export const articles = () => table<ArticleRow>('articles');

const SCOPE = 'content:manage';
const COVER_TYPES = ['image/jpeg', 'image/png', 'image/webp'];
const COVER_MAX_BYTES = 5 * 1024 * 1024;

// ---- Seed ----------------------------------------------------------------------------------------

/** Fixed ids, so the guide and a check can open a seeded article's editor directly. */
export const seededArticleId = (index: number) => `a1c1e000-0000-4000-8000-${String(index + 1).padStart(12, '0')}`;

export function seedArticles(): void {
  SAMPLE_ARTICLES.forEach((sample, index) => {
    const written = at(-Math.max(sample.days_ago ?? 0, 0) - 2, '10:00');
    const published = sample.days_ago === null ? null : at(-sample.days_ago, sample.days_ago < 0 ? '08:00' : '09:30');
    articles().insert({
      id: seededArticleId(index),
      slug: sample.slug,
      title: sample.title,
      summary: sample.summary,
      body: sample.body,
      author_name: sample.author_name,
      reading_minutes: sample.reading_minutes,
      tags: sample.tags,
      published_at: published,
      first_published_at: published,
      cover_image_path: sample.cover_image_path,
      cover_tone: sample.cover_tone,
      created_at: written,
      updated_at: published ?? written,
      deleted_at: null,
      version: 1,
    });
  });
}

// ---- Covers --------------------------------------------------------------------------------------

const TONES: Record<NonNullable<SampleArticle['cover_tone']>, [string, string, string]> = {
  teal: ['#d5f2ec', '#7dcfc3', '#217a71'],
  sun: ['#fff4d6', '#ffd57a', '#c77d00'],
  violet: ['#ecebfb', '#b9b2f0', '#6c5ce0'],
  orange: ['#ffeedd', '#ffb766', '#d46a00'],
  forest: ['#e3ece8', '#8fb3a6', '#13322b'],
};

function paw(x: number, y: number, size: number, rotate: number, fill: string, opacity: number): string {
  const s = size;
  return (
    `<g transform="translate(${x} ${y}) rotate(${rotate}) scale(${s / 100})" fill="${fill}" opacity="${opacity}">` +
    '<ellipse cx="0" cy="22" rx="30" ry="25"/>' +
    '<ellipse cx="-34" cy="-14" rx="12" ry="16" transform="rotate(-20 -34 -14)"/>' +
    '<ellipse cx="-12" cy="-34" rx="12" ry="17"/>' +
    '<ellipse cx="14" cy="-34" rx="12" ry="17"/>' +
    '<ellipse cx="35" cy="-12" rx="12" ry="16" transform="rotate(20 35 -12)"/>' +
    '</g>'
  );
}

/** A soft illustrated cover for a seeded article that has no photo: the clinic's colours and paw prints. */
function drawnCover(tone: NonNullable<SampleArticle['cover_tone']>): string {
  const [light, mid, dark] = TONES[tone];
  const svg =
    `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 1200 675" preserveAspectRatio="xMidYMid slice">` +
    `<defs><linearGradient id="g" x1="0" y1="0" x2="1" y2="1"><stop offset="0" stop-color="${light}"/><stop offset="1" stop-color="${mid}"/></linearGradient></defs>` +
    `<rect width="1200" height="675" fill="url(#g)"/>` +
    `<circle cx="980" cy="120" r="260" fill="#ffffff" opacity="0.28"/>` +
    `<circle cx="160" cy="620" r="220" fill="${dark}" opacity="0.08"/>` +
    paw(820, 380, 300, 18, dark, 0.85) +
    paw(330, 210, 120, -14, dark, 0.22) +
    paw(520, 520, 90, 8, dark, 0.16) +
    paw(1080, 560, 110, 30, '#ffffff', 0.5) +
    `</svg>`;
  return `data:image/svg+xml;charset=utf-8,${encodeURIComponent(svg)}`;
}

/** Moves an upload whose bytes arrived into place. The arrival is a change, so the version moves. */
function settleCover(row: ArticleRow): ArticleRow {
  if (!row.pending_cover_file_id) return row;
  const stored = getFile(row.pending_cover_file_id);
  if (!stored || (!stored.data_url && !stored.size)) return row;
  return (
    articles().update(row.id, {
      cover_file_id: row.pending_cover_file_id,
      pending_cover_file_id: undefined,
      cover_image_path: undefined,
      cover_tone: undefined,
    }) ?? row
  );
}

function coverUrl(row: ArticleRow): string | undefined {
  if (row.cover_file_id) return fileUrl(row.cover_file_id) ?? undefined;
  if (!row.cover_image_path && row.cover_tone) return drawnCover(row.cover_tone);
  return undefined;
}

// ---- Shapes --------------------------------------------------------------------------------------

type Status = 'draft' | 'scheduled' | 'published';

function statusOf(row: ArticleRow, now = nowIso()): Status {
  if (!row.published_at) return 'draft';
  return row.published_at > now ? 'scheduled' : 'published';
}

const isPublic = (row: ArticleRow, now = nowIso()) => !row.deleted_at && statusOf(row, now) === 'published';

function publicSummary(row: ArticleRow) {
  return {
    id: row.id,
    slug: row.slug,
    title: row.title,
    summary: row.summary,
    author_name: row.author_name,
    reading_minutes: row.reading_minutes,
    ...(row.cover_image_path ? { cover_image_path: row.cover_image_path } : {}),
    ...(coverUrl(row) ? { cover_image_url: coverUrl(row) } : {}),
    published_at: row.published_at!,
    tags: row.tags ?? [],
  };
}

function staffSummary(row: ArticleRow) {
  return {
    id: row.id,
    slug: row.slug,
    title: row.title,
    summary: row.summary,
    author_name: row.author_name,
    reading_minutes: row.reading_minutes,
    status: statusOf(row),
    ...(row.published_at ? { published_at: row.published_at } : {}),
    ...(row.first_published_at ? { first_published_at: row.first_published_at } : {}),
    slug_locked: Boolean(row.first_published_at),
    has_cover: Boolean(row.cover_file_id || row.cover_image_path || row.cover_tone),
    created_at: row.created_at,
    ...(row.updated_at ? { updated_at: row.updated_at } : {}),
    version: row.version,
  };
}

function staffArticle(row: ArticleRow) {
  const { has_cover: _hasCover, ...summary } = staffSummary(row);
  return {
    ...summary,
    body: row.body,
    ...(row.cover_image_path ? { cover_image_path: row.cover_image_path } : {}),
    ...(coverUrl(row) ? { cover_image_url: coverUrl(row) } : {}),
    cover_pending: Boolean(row.pending_cover_file_id),
  };
}

// ---- Rules (the backend's ArticleSlug, ArticleText and ArticleFieldRules) ------------------------

const SLUG_INVALID = 'slug ใช้ได้แค่ตัวอักษรภาษาอังกฤษพิมพ์เล็ก ตัวเลข และขีดกลาง ยาวไม่เกิน 80 ตัวอักษร';
const SLUG_NEEDED = 'ชื่อเรื่องไม่มีตัวอักษรภาษาอังกฤษให้สร้าง slug ได้ กรุณาตั้ง slug เอง เช่น cat-vaccine-schedule';

const cleanLine = (value: unknown) => String(value ?? '').replace(/[\u0000-\u001f‪-‮⁦-⁩﻿]/g, ' ').replace(/\s+/g, ' ').trim();

function cleanBody(value: unknown): string {
  return String(value ?? '')
    .replace(/\r\n?/g, '\n')
    .replace(/[\u0000-\u0009\u000b-\u001f‪-‮⁦-⁩﻿]/g, '')
    .replace(/[ \t]+\n/g, '\n')
    .replace(/\n{3,}/g, '\n\n')
    .trim();
}

function normaliseSlug(typed: string): string {
  return typed.trim().toLowerCase().replace(/[\s_]+/g, '-').replace(/-{2,}/g, '-').replace(/^-+|-+$/g, '');
}

function slugFromTitle(title: string): string | undefined {
  const slug = title
    .normalize('NFD')
    .replace(/[̀-ͯ]/g, '')
    .toLowerCase()
    .replace(/[^a-z0-9]/g, '-')
    .replace(/-{2,}/g, '-')
    .replace(/^-+|-+$/g, '');
  if (!/[a-z]/.test(slug)) return undefined;
  if (slug.length <= 80) return slug;
  const cut = slug.slice(0, 80);
  return cut.slice(0, cut.lastIndexOf('-') > 0 ? cut.lastIndexOf('-') : 80).replace(/-+$/, '');
}

function checkedSlug(typed: string): string {
  const slug = normaliseSlug(typed);
  if (!/^[a-z0-9]+(-[a-z0-9]+)*$/.test(slug) || slug.length > 80) invalid('slug', SLUG_INVALID);
  return slug;
}

function ensureSlugFree(slug: string, articleId?: string): void {
  const holder = articles().find((row) => row.slug === slug && row.id !== articleId);
  if (!holder) return;
  fail(
    409,
    'article_slug_taken',
    holder.deleted_at
      ? 'slug นี้เคยใช้กับบทความที่ลบไปแล้ว จึงใช้ซ้ำไม่ได้ กรุณาตั้ง slug ใหม่'
      : 'มีบทความอื่นใช้ slug นี้อยู่แล้ว กรุณาตั้ง slug ใหม่',
    { field: 'slug', details: { slug, article_id: holder.id, deleted: Boolean(holder.deleted_at) }, message_en: 'Another article already has this slug.' },
  );
}

const LIMITS = { title: 200, summary: 500, body: 20_000, author_name: 120 } as const;
const MESSAGES = {
  title: `ต้องมีชื่อเรื่อง ยาวไม่เกิน ${LIMITS.title} ตัวอักษร`,
  summary: `ต้องมีคำโปรย ยาวไม่เกิน ${LIMITS.summary} ตัวอักษร`,
  body: `ต้องมีเนื้อหา ยาวไม่เกิน ${LIMITS.body.toLocaleString('en-US')} ตัวอักษร`,
  author_name: `ต้องมีชื่อผู้เขียน ยาวไม่เกิน ${LIMITS.author_name} ตัวอักษร`,
};

function field(name: keyof typeof LIMITS, value: unknown): string {
  const text = name === 'body' ? cleanBody(value) : cleanLine(value);
  if (!text || text.length > LIMITS[name]) invalid(name, MESSAGES[name]);
  return text;
}

function readingMinutes(value: unknown, body: string): number {
  if (value === undefined || value === null || value === '') return Math.min(120, Math.max(1, Math.ceil(body.length / 800)));
  const minutes = Number(value);
  if (!Number.isInteger(minutes) || minutes < 1 || minutes > 120) invalid('reading_minutes', 'เวลาอ่านต้องอยู่ระหว่าง 1 ถึง 120 นาที');
  return minutes;
}

function staffRow(ctx: Ctx): ArticleRow {
  const row = articles().get(ctx.params.id);
  if (!row || row.deleted_at) notFound('บทความ');
  return settleCover(row);
}

function guarded(row: ArticleRow, ctx: Ctx): void {
  if (ctx.ifMatch === undefined) fail(428, 'precondition_required', 'ต้องส่ง If-Match มาด้วย');
  checkVersion(ctx, row);
}

// ---- Public --------------------------------------------------------------------------------------

/** The newest published articles, for the assistant and the home page's helpers. */
export function publishedArticles(): ArticleRow[] {
  const now = nowIso();
  return articles()
    .where((row) => isPublic(row, now))
    .sort((a, b) => b.published_at!.localeCompare(a.published_at!));
}

route('GET', '/articles', ({ query }) => {
  const rows = publishedArticles().map(settleCover);
  const tag = query.get('tag');
  const q = query.get('q');
  const filtered = rows.filter((row) => (!tag || row.tags?.includes(tag)) && matches(q, row.title, row.summary));
  return page(filtered.map(publicSummary), query, 10);
});

route('GET', '/articles/:slug', ({ params }) => {
  const row = articles().find((candidate) => candidate.slug === params.slug && isPublic(candidate));
  if (!row) notFound('บทความ');
  const settled = settleCover(row);
  return { ...publicSummary(settled), body: settled.body };
});

// ---- Staff ---------------------------------------------------------------------------------------

route('GET', '/staff/articles', (ctx) => {
  requireStaff(ctx, SCOPE);
  const status = ctx.query.get('status');
  if (status && !['draft', 'scheduled', 'published'].includes(status)) invalid('status', 'status ต้องเป็น draft, scheduled หรือ published');
  const now = nowIso();
  const rows = articles()
    .where((row) => !row.deleted_at && (!status || statusOf(row, now) === status) && matches(ctx.query.get('q'), row.title, row.slug))
    .map(settleCover)
    .sort((a, b) => b.created_at.localeCompare(a.created_at));
  return page(rows.map(staffSummary), ctx.query);
});

route('GET', '/staff/articles/:id', (ctx) => {
  requireStaff(ctx, SCOPE);
  return staffArticle(staffRow(ctx));
});

route('POST', '/staff/articles', (ctx) => {
  requireStaff(ctx, SCOPE);
  const body = ctx.body ?? {};
  const title = field('title', body.title);
  const summary = field('summary', body.summary);
  const text = field('body', body.body);
  const author = field('author_name', body.author_name);
  const typed = typeof body.slug === 'string' && body.slug.trim() ? body.slug : undefined;
  const slug = typed ? checkedSlug(typed) : (slugFromTitle(title) ?? invalid('slug', SLUG_NEEDED));
  ensureSlugFree(slug);
  const now = nowIso();
  const row = articles().insert({
    slug,
    title,
    summary,
    body: text,
    author_name: author,
    reading_minutes: readingMinutes(body.reading_minutes, text),
    tags: [],
    published_at: null,
    first_published_at: null,
    created_at: now,
    updated_at: now,
    deleted_at: null,
    version: 1,
  });
  return created(staffArticle(row));
});

route('PATCH', '/staff/articles/:id', (ctx) => {
  requireStaff(ctx, SCOPE);
  const row = staffRow(ctx);
  guarded(row, ctx);
  const body = ctx.body ?? {};
  const changes: Partial<ArticleRow> = {};

  if (body.title !== undefined) changes.title = field('title', body.title);
  if (body.summary !== undefined) changes.summary = field('summary', body.summary);
  if (body.body !== undefined) changes.body = field('body', body.body);
  if (body.author_name !== undefined) changes.author_name = field('author_name', body.author_name);
  if (body.reading_minutes !== undefined) changes.reading_minutes = readingMinutes(body.reading_minutes, changes.body ?? row.body);
  if (typeof body.slug === 'string' && body.slug.trim()) {
    const slug = checkedSlug(body.slug);
    if (slug !== row.slug) {
      if (row.first_published_at) stateConflict('บทความนี้เคยเผยแพร่แล้ว จึงเปลี่ยน slug ไม่ได้ ลิงก์เดิมจะเสีย');
      ensureSlugFree(slug, row.id);
      changes.slug = slug;
    }
  }
  if (body.remove_cover === true) {
    Object.assign(changes, { cover_file_id: undefined, pending_cover_file_id: undefined, cover_image_path: undefined, cover_tone: undefined });
  }

  return staffArticle(articles().update(row.id, changes)!);
});

route('POST', '/staff/articles/:id/publish', (ctx) => {
  requireStaff(ctx, SCOPE);
  const row = staffRow(ctx);
  guarded(row, ctx);
  const now = nowIso();
  if (statusOf(row, now) === 'published') stateConflict('บทความนี้เผยแพร่อยู่แล้ว');
  const when = row.first_published_at && row.first_published_at <= now ? row.first_published_at : now;
  return staffArticle(articles().update(row.id, { published_at: when, first_published_at: when })!);
});

route('POST', '/staff/articles/:id/unpublish', (ctx) => {
  requireStaff(ctx, SCOPE);
  const row = staffRow(ctx);
  guarded(row, ctx);
  if (!row.published_at) stateConflict('บทความนี้เป็นฉบับร่างอยู่แล้ว');
  return staffArticle(articles().update(row.id, { published_at: null })!);
});

route('DELETE', '/staff/articles/:id', (ctx) => {
  requireStaff(ctx, SCOPE);
  const row = staffRow(ctx);
  guarded(row, ctx);
  // Kept, not removed: a deleted article keeps its slug, so an old link never opens another article.
  articles().update(row.id, { deleted_at: nowIso(), published_at: null });
  return undefined;
});

route('POST', '/staff/articles/:id/cover', (ctx) => {
  requireStaff(ctx, SCOPE);
  const row = staffRow(ctx);
  guarded(row, ctx);
  const type = String(ctx.body?.content_type ?? '');
  const size = Number(ctx.body?.size_bytes);
  if (!COVER_TYPES.includes(type)) invalid('content_type', 'รูปปกต้องเป็นไฟล์ JPEG, PNG หรือ WebP');
  if (!Number.isFinite(size) || size <= 0 || size > COVER_MAX_BYTES) invalid('size_bytes', 'รูปปกต้องมีขนาดไม่เกิน 5 MB');

  const upload = signedUpload({ name: `cover-${row.slug}`, content_type: type, size });
  const updated = articles().update(row.id, { pending_cover_file_id: upload.id })!;
  return {
    article: staffArticle(updated),
    upload: { url: upload.url, method: 'PUT', headers: upload.headers, expires_at: new Date(Date.now() + 15 * 60_000).toISOString() },
  };
});
