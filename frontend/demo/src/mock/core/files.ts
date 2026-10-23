import { MOCK_API } from '../../../mock-api';
import { nowIso, table, uuid, type Row } from './db';

/**
 * Files people add in the demo: logos, pet photos, documents. Each is kept as a data URL, so a
 * picture can be shown with a plain `<img>` and survives a reload. Big files are accepted and
 * their bytes dropped, with a placeholder in their place: browser storage is small.
 */

const MAX_KEPT_BYTES = 1_500_000;
const STORAGE = `${new URL(MOCK_API).origin}/storage/`;

export interface StoredFile extends Row {
  name: string;
  content_type: string;
  size: number;
  data_url: string | null;
  created_at: string;
}

const files = () => table<StoredFile>('demo_files');

export function blobToDataUrl(blob: Blob): Promise<string> {
  return new Promise((resolve, reject) => {
    const reader = new FileReader();
    reader.onload = () => resolve(String(reader.result));
    reader.onerror = () => reject(reader.error);
    reader.readAsDataURL(blob);
  });
}

/** Keeps an uploaded file and answers its id. */
export async function keepFile(blob: Blob, name = 'file'): Promise<StoredFile> {
  const data_url = blob.size <= MAX_KEPT_BYTES ? await blobToDataUrl(blob) : null;
  return files().insert({ name, content_type: blob.type || 'application/octet-stream', size: blob.size, data_url, created_at: nowIso() });
}

/** A place to PUT a file to, as `POST /…/documents` hands out in the product. */
export function signedUpload(meta: { name?: string; content_type?: string; size?: number } = {}) {
  const id = uuid();
  files().insert({ id, name: meta.name ?? 'file', content_type: meta.content_type ?? 'application/octet-stream', size: meta.size ?? 0, data_url: null, created_at: nowIso() });
  return { id, url: `${STORAGE}${id}`, headers: { 'Content-Type': meta.content_type ?? 'application/octet-stream' } };
}

export async function putUpload(id: string, blob: Blob): Promise<void> {
  const data_url = blob.size <= MAX_KEPT_BYTES ? await blobToDataUrl(blob) : null;
  if (files().get(id)) files().update(id, { data_url, size: blob.size, content_type: blob.type || files().get(id)!.content_type });
  else files().insert({ id, name: 'file', content_type: blob.type, size: blob.size, data_url, created_at: nowIso() });
}

export async function readUpload(id: string): Promise<Blob | undefined> {
  const stored = files().get(id);
  if (!stored?.data_url) return undefined;
  return (await fetch(stored.data_url)).blob();
}

export function getFile(id: string | null | undefined): StoredFile | undefined {
  return files().get(id);
}

/** What to put in an `src` or `href` for a kept file: its data URL, or a placeholder when its bytes were dropped. */
export function fileUrl(id: string | null | undefined): string | null {
  const stored = files().get(id);
  if (!stored) return null;
  if (stored.data_url) return stored.data_url;
  return `data:text/plain;charset=utf-8,${encodeURIComponent(`ไฟล์ ${stored.name} (เดโมไม่ได้เก็บไฟล์ขนาดใหญ่ไว้)`)}`;
}
