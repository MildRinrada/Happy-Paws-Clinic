import { createContext, use, useContext, type Context, type ReactNode } from 'react';

/**
 * Runs an `async function` component in the browser. React only awaits those on a server, so the
 * build (plugins/next-compat.ts) wraps each one with this: the function is called once per set of
 * props in each generation, and its answer is read with `use()`, which suspends until ready.
 *
 * On a server a component runs once per request, at one instant. In the browser the components
 * above it render again whenever anything changes, and a prop computed on the spot
 * (`now={new Date().toISOString()}` in the staff shell) would be new each time and start a fetch on
 * every render, forever. So a value that is simply "the current time" counts as the same prop.
 *
 * `children` is not part of the answer either: a layout is called with a slot in their place and
 * the real children arrive through context.
 */

export const GenerationContext = createContext(1);

interface Entry {
  promise: Promise<ReactNode>;
  generation: number;
}

const entries = new Map<string, Entry>();

/**
 * The context that carries a component's real children to its slot: one per component, never per
 * answer. A provider of a new context is a new element type to React, which would remount the whole
 * page below a layout on every refresh and lose what its client components hold.
 */
const slotContexts = new Map<string, Context<ReactNode>>();

function slotContextOf(name: string): Context<ReactNode> {
  let context = slotContexts.get(name);
  if (!context) {
    context = createContext<ReactNode>(null);
    slotContexts.set(name, context);
  }
  return context;
}

const objectIds = new WeakMap<object, number>();
let nextObjectId = 1;

function idOf(value: object): number {
  let id = objectIds.get(value);
  if (id === undefined) {
    id = nextObjectId++;
    objectIds.set(value, id);
  }
  return id;
}

function isPlain(value: object): boolean {
  const proto = Object.getPrototypeOf(value);
  return (proto === Object.prototype || proto === null) && !('$$typeof' in value);
}

/** Within this long of the clock, a time is taken to be "now" as the caller computed it. */
const NOW_WINDOW_MS = 120_000;
const ISO_INSTANT = /^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}(\.\d+)?(Z|[+-]\d{2}:\d{2})$/;

function isNow(time: number): boolean {
  return Math.abs(time - Date.now()) < NOW_WINDOW_MS;
}

/** A key that is equal for equal data, and identity-based for anything else (elements, functions, promises). */
function keyOf(value: unknown, depth = 0): string {
  if (value === null || value === undefined) return String(value);
  const type = typeof value;
  if (type === 'string') {
    const text = value as string;
    return ISO_INSTANT.test(text) && isNow(Date.parse(text)) ? 'NOW' : JSON.stringify(text);
  }
  if (type === 'number') return value as number > 1e12 && isNow(value as number) ? 'NOW' : String(value);
  if (type === 'boolean' || type === 'bigint') return String(value);
  if (type === 'function' || type === 'symbol') return `#${idOf(value as object)}`;
  if (value instanceof Date) return isNow(value.getTime()) ? 'NOW' : `D${value.getTime()}`;
  if (depth > 6) return `#${idOf(value as object)}`;
  if (Array.isArray(value)) return `[${value.map((item) => keyOf(item, depth + 1)).join(',')}]`;
  if (isPlain(value as object)) {
    const object = value as Record<string, unknown>;
    return `{${Object.keys(object)
      .sort()
      .map((key) => `${key}:${keyOf(object[key], depth + 1)}`)
      .join(',')}}`;
  }
  return `#${idOf(value as object)}`;
}

function Slot({ from }: { from: Context<ReactNode> }) {
  return <>{useContext(from)}</>;
}

/** Drops answers from earlier generations once a newer one is on screen. */
export function prune(keepFrom: number): void {
  for (const [key, entry] of entries) {
    if (entry.generation < keepFrom) entries.delete(key);
  }
}

export function asyncComponent<P extends object>(
  render: (props: P) => Promise<ReactNode>,
  name: string,
) {
  function AsyncComponent(props: P) {
    const generation = useContext(GenerationContext);
    const { children, ...rest } = props as P & { children?: ReactNode };
    const key = `${name}|${generation}|${keyOf(rest)}`;

    const slotContext = slotContextOf(name);
    let entry = entries.get(key);
    if (!entry) {
      const callProps = (children === undefined ? rest : { ...rest, children: <Slot from={slotContext} /> }) as P;
      // Called on the next tick, so a throw inside it becomes a rejected promise like any await.
      const promise = Promise.resolve().then(() => render(callProps));
      // Read by React later; without a handler a rejection would also be reported as unhandled.
      promise.catch(() => {});
      entry = { promise, generation };
      entries.set(key, entry);
    }

    const SlotProvider = slotContext;
    return <SlotProvider value={children}>{use(entry.promise)}</SlotProvider>;
  }

  AsyncComponent.displayName = name.split('#').pop() === 'default' ? name : name.split('#').pop();
  return AsyncComponent;
}
