/**
 * The demo's database: named tables of plain rows, kept in this browser's storage.
 *
 * A row is stored in the shape the API answers with (the product's own types, snake_case), so a
 * module that reads another module's table gets exactly what that module's endpoints return.
 * Every row has a string `id`. Tables are created on first use; nothing has to be declared.
 *
 * The whole database is one JSON document. It is written back a moment after a change, and
 * another tab of the demo picks the change up from the storage event.
 */

const KEY = 'hp-demo-db';

/** Bump when the seed changes shape, so every browser starts again from the new seed. */
export const SEED_VERSION = 2;

export interface Row {
  id: string;
  [field: string]: any;
}

interface Stored {
  version: number;
  /** The local date (YYYY-MM-DD) the seed was planted for. Appointments and the queue hang off it. */
  day: string;
  tables: Record<string, Row[]>;
  /** Per-name counters for running numbers: invoice numbers, queue tickets. */
  counters: Record<string, number>;
}

let state: Stored | null = null;
let writeQueued = false;

function load(): Stored | null {
  try {
    const raw = localStorage.getItem(KEY);
    return raw ? (JSON.parse(raw) as Stored) : null;
  } catch {
    return null;
  }
}

function persist() {
  if (writeQueued) return;
  writeQueued = true;
  queueMicrotask(() => {
    writeQueued = false;
    try {
      localStorage.setItem(KEY, JSON.stringify(state));
    } catch (error) {
      console.warn('[demo] could not save the demo data', error);
    }
  });
}

export function today(): string {
  const now = new Date();
  return `${now.getFullYear()}-${String(now.getMonth() + 1).padStart(2, '0')}-${String(now.getDate()).padStart(2, '0')}`;
}

/** Whether the stored data is usable as is, or the seed has to run again first. */
export function needsSeed(): boolean {
  state ??= load();
  return !state || state.version !== SEED_VERSION || state.day !== today();
}

/** Empties everything, ready for the seed. */
export function reset(): void {
  state = { version: SEED_VERSION, day: today(), tables: {}, counters: {} };
  persist();
}

function db(): Stored {
  state ??= load();
  if (!state) reset();
  return state!;
}

/** Reads the stored data again, for a change another tab made. */
export function reload(): void {
  state = load();
}

if (typeof window !== 'undefined') {
  window.addEventListener('storage', (event) => {
    if (event.key === KEY) reload();
  });
}

export function uuid(): string {
  return crypto.randomUUID();
}

export function nowIso(): string {
  return new Date().toISOString();
}

/** The next value of a running number, starting at 1 (or `start`). */
export function nextNumber(name: string, start = 1): number {
  const counters = db().counters;
  counters[name] = (counters[name] ?? start - 1) + 1;
  persist();
  return counters[name]!;
}

export interface Table<T extends Row> {
  all(): T[];
  get(id: string | null | undefined): T | undefined;
  find(predicate: (row: T) => boolean): T | undefined;
  where(predicate: (row: T) => boolean): T[];
  /** Adds a row; `id` is generated when missing. Answers the stored row. */
  insert(row: Omit<T, 'id'> & { id?: string }): T;
  insertMany(rows: Array<Omit<T, 'id'> & { id?: string }>): T[];
  /** Merges fields into a row, bumping `version` when the row has one. Answers the new row. */
  update(id: string, patch: Partial<T> | ((row: T) => Partial<T>)): T | undefined;
  /** Replaces a row as a whole. */
  put(row: T): T;
  remove(id: string): boolean;
  removeWhere(predicate: (row: T) => boolean): number;
  count(): number;
}

export function table<T extends Row = Row>(name: string): Table<T> {
  const rows = (): T[] => {
    const tables = db().tables;
    tables[name] ??= [];
    return tables[name] as T[];
  };

  return {
    all: () => rows().slice(),
    get: (id) => (id ? rows().find((row) => row.id === id) : undefined),
    find: (predicate) => rows().find(predicate),
    where: (predicate) => rows().filter(predicate),
    insert(row) {
      const stored = { ...row, id: row.id ?? uuid() } as T;
      rows().push(stored);
      persist();
      return stored;
    },
    insertMany(list) {
      const stored = list.map((row) => ({ ...row, id: row.id ?? uuid() }) as T);
      rows().push(...stored);
      persist();
      return stored;
    },
    update(id, patch) {
      const list = rows();
      const index = list.findIndex((row) => row.id === id);
      if (index < 0) return undefined;
      const current = list[index]!;
      const changes = typeof patch === 'function' ? patch(current) : patch;
      const next = { ...current, ...changes } as T;
      if (typeof current.version === 'number' && !('version' in changes)) (next as Row).version = current.version + 1;
      if ('updated_at' in current && !('updated_at' in changes)) (next as Row).updated_at = nowIso();
      list[index] = next;
      persist();
      return next;
    },
    put(row) {
      const list = rows();
      const index = list.findIndex((existing) => existing.id === row.id);
      if (index < 0) list.push(row);
      else list[index] = row;
      persist();
      return row;
    },
    remove(id) {
      const list = rows();
      const index = list.findIndex((row) => row.id === id);
      if (index < 0) return false;
      list.splice(index, 1);
      persist();
      return true;
    },
    removeWhere(predicate) {
      const list = rows();
      const before = list.length;
      const kept = list.filter((row) => !predicate(row));
      db().tables[name] = kept;
      persist();
      return before - kept.length;
    },
    count: () => rows().length,
  };
}

/** A single settings document stored as the one row of its own table. */
export function singleton<T extends object>(name: string, fallback: () => T): { get(): T; set(patch: Partial<T>): T } {
  const store = table<Row & T>(name);
  return {
    get() {
      return (store.get('singleton') as T | undefined) ?? (store.insert({ ...fallback(), id: 'singleton' } as never) as T);
    },
    set(patch) {
      this.get();
      return store.update('singleton', patch as never) as T;
    },
  };
}

// ---- Dates relative to the seed day ----------------------------------------------------------------

/** A local date `days` from today, as YYYY-MM-DD. */
export function day(days = 0): string {
  const date = new Date();
  date.setDate(date.getDate() + days);
  return `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, '0')}-${String(date.getDate()).padStart(2, '0')}`;
}

/** An instant `days` from today at local `hh:mm`, as an ISO string. */
export function at(days: number, time: string): string {
  const [hours, minutes] = time.split(':').map(Number);
  const date = new Date();
  date.setDate(date.getDate() + days);
  date.setHours(hours ?? 0, minutes ?? 0, 0, 0);
  return date.toISOString();
}

/** An instant `minutes` from now (negative for the past), as an ISO string. */
export function minutesFromNow(minutes: number): string {
  return new Date(Date.now() + minutes * 60_000).toISOString();
}
