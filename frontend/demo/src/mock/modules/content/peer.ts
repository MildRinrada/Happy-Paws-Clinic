import { find, Reply, type Auth, type Ctx } from '../../core/http';
import { table } from '../../core/db';

/**
 * Reads another module's answers through its own routes, so the assistant and the AI quote the
 * clinic's hours, services and numbers exactly as the site shows them, in the shapes the product's
 * types describe. Anything that fails or is not there yet answers `undefined`, never an error:
 * a chat reply must not break because another part of the demo is still empty.
 */
export async function callRoute<T>(
  method: string,
  path: string,
  options: { query?: Record<string, string | undefined>; auth?: Auth | null; body?: unknown } = {},
): Promise<T | undefined> {
  const found = find(method, path);
  if (!found) return undefined;

  const query = new URLSearchParams();
  for (const [key, value] of Object.entries(options.query ?? {})) {
    if (value !== undefined && value !== '') query.set(key, value);
  }

  const ctx: Ctx = {
    method,
    path,
    params: found.params,
    query,
    body: options.body,
    headers: new Headers(),
    auth: options.auth ?? null,
    cookies: {},
  };

  try {
    const result = (await found.handler(ctx)) as unknown;
    if (result instanceof Reply) return result.status < 300 ? (result.body as T) : undefined;
    return result as T;
  } catch {
    return undefined;
  }
}

/** A list answer's rows, whether the route answers `{ data }` or a bare array. */
export function rowsOf<T>(answer: unknown): T[] {
  if (Array.isArray(answer)) return answer as T[];
  if (answer && typeof answer === 'object' && Array.isArray((answer as { data?: unknown }).data)) {
    return (answer as { data: T[] }).data;
  }
  return [];
}

// ---- The clinic as the public site shows it --------------------------------------------------------

export interface DayHours {
  day: string;
  opens?: string;
  closes?: string;
  closed: boolean;
}

export interface ClinicFacts {
  name: string;
  days: DayHours[];
  holidays: Array<{ date: string; name: string }>;
  contacts: Array<{ kind: string; label: string; value: string; url?: string }>;
  branches: Array<{ id: string; name: string; address?: string }>;
  services: Array<{ id: string; name: string; category_code: string; duration_minutes: number; price_satang: number }>;
}

export async function clinicFacts(): Promise<ClinicFacts> {
  const [profile, hours, contacts, branches, services] = await Promise.all([
    callRoute<{ name?: string }>('GET', '/clinic'),
    callRoute<{ days?: DayHours[]; holidays?: Array<{ date: string; name: string }> }>('GET', '/clinic/hours'),
    callRoute('GET', '/clinic/contacts'),
    callRoute('GET', '/clinic/branches'),
    callRoute('GET', '/services', { query: { limit: '100' } }),
  ]);

  return {
    name: profile?.name || 'Happy Paws Clinic',
    days: hours?.days ?? [],
    holidays: hours?.holidays ?? [],
    contacts: rowsOf(contacts),
    branches: rowsOf(branches),
    services: rowsOf(services),
  };
}

const WEEKDAY_TH: Record<string, string> = {
  monday: 'จันทร์',
  tuesday: 'อังคาร',
  wednesday: 'พุธ',
  thursday: 'พฤหัสบดี',
  friday: 'ศุกร์',
  saturday: 'เสาร์',
  sunday: 'อาทิตย์',
};

export const weekdayTh = (day: string) => WEEKDAY_TH[day] ?? day;

/** "จันทร์–ศุกร์ 09:00–20:00 น. · เสาร์–อาทิตย์ 09:00–18:00 น.", runs of days with the same hours folded. */
export function hoursLine(days: DayHours[]): string | undefined {
  if (!days.length) return undefined;
  const groups: Array<{ first: string; last: string; text: string }> = [];
  for (const day of days) {
    const text = day.closed || !day.opens ? 'ปิด' : `${day.opens}–${day.closes} น.`;
    const current = groups[groups.length - 1];
    if (current && current.text === text) current.last = day.day;
    else groups.push({ first: day.day, last: day.day, text });
  }
  return groups
    .map((group) =>
      group.first === group.last
        ? `${weekdayTh(group.first)} ${group.text}`
        : `${weekdayTh(group.first)}–${weekdayTh(group.last)} ${group.text}`,
    )
    .join(' · ');
}

export function baht(satang: number): string {
  return `${Math.round(satang / 100).toLocaleString('th-TH')} บาท`;
}

/** Rows of another module's table, or none while that module has not planted any. */
export function rowsOfTable<T = any>(name: string): T[] {
  try {
    return table(name).all() as unknown as T[];
  } catch {
    return [];
  }
}
