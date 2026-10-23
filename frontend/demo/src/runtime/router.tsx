import {
  Component,
  startTransition,
  Suspense,
  useEffect,
  useLayoutEffect,
  useState,
  type ComponentType,
  type ErrorInfo,
  type ReactNode,
} from 'react';
import { prune, GenerationContext } from './async-component';
import { ParamsContext, PlaceContext } from './contexts';
import { getCookie } from './cookies';
import {
  currentGeneration,
  currentPlace,
  isNavigationError,
  navigate,
  NotFoundError,
  RedirectError,
  subscribe,
  type Place,
} from './navigation';

/**
 * The product's `src/app` folder, routed in the browser the way Next routes it: route groups are
 * left out of the address, `[id]` is a parameter, and each folder's layout, loading and error files
 * wrap everything below them. Modules load on demand, so each page arrives as its own chunk.
 */

type Loader = () => Promise<Record<string, unknown>>;

const pageFiles = import.meta.glob('../../../src/app/**/page.tsx');
const layoutFiles = import.meta.glob('../../../src/app/**/layout.tsx');
const loadingFiles = import.meta.glob('../../../src/app/**/loading.tsx');
const errorFiles = import.meta.glob('../../../src/app/**/error.tsx');

const PREFIX = '../../../src/app';

/** `../../../src/app/(staff)/customers/[id]/page.tsx` → `(staff)/customers/[id]` */
function dirOf(file: string): string {
  return file.slice(PREFIX.length + 1).split('/').slice(0, -1).join('/');
}

function byDir(files: Record<string, Loader>): Map<string, Loader> {
  return new Map(Object.entries(files).map(([file, load]) => [dirOf(file), load]));
}

const layouts = byDir(layoutFiles as Record<string, Loader>);
const loadings = byDir(loadingFiles as Record<string, Loader>);
const errors = byDir(errorFiles as Record<string, Loader>);

interface Route {
  dir: string;
  segments: string[];
  load: Loader;
}

const routes: Route[] = Object.entries(pageFiles as Record<string, Loader>)
  .map(([file, load]) => {
    const dir = dirOf(file);
    return { dir, load, segments: dir.split('/').filter((part) => part && !/^\(.*\)$/.test(part)) };
  })
  // Static segments win over parameters, as in Next.
  .sort((a, b) => {
    const score = (route: Route) => route.segments.map((part) => (part.startsWith('[') ? '1' : '0')).join('');
    return score(a).localeCompare(score(b)) || b.segments.length - a.segments.length;
  });

function match(pathname: string): { route: Route; params: Record<string, string> } | null {
  const parts = pathname.split('/').filter(Boolean).map((part) => decodeURIComponent(part));
  for (const route of routes) {
    if (route.segments.length !== parts.length) continue;
    const params: Record<string, string> = {};
    const ok = route.segments.every((segment, index) => {
      const part = parts[index]!;
      if (segment.startsWith('[') && segment.endsWith(']')) {
        params[segment.slice(1, -1)] = part;
        return true;
      }
      return segment === part;
    });
    if (ok) return { route, params };
  }
  return null;
}

/** The middleware's rule: these need a session, and someone without one goes to sign in first. */
const PROTECTED = [
  '/queue', '/appointments', '/customers', '/pets', '/consultations', '/settings', '/schedules', '/account',
  '/billing', '/lab', '/dashboard', '/reports', '/inventory', '/notifications', '/messages', '/crm',
  '/prescriptions', '/ai', '/portal',
];

function needsSession(pathname: string): boolean {
  return PROTECTED.some((prefix) => pathname === prefix || pathname.startsWith(`${prefix}/`));
}

// ---- One resolved page -----------------------------------------------------------------------------

interface Frame {
  dir: string;
  Layout?: ComponentType<{ children: ReactNode; params: Promise<Record<string, string>> }>;
  Loading?: ComponentType;
  ErrorView?: ComponentType<{ error: Error & { digest?: string }; reset: () => void }>;
}

interface Resolved {
  key: string;
  place: Place;
  generation: number;
  notFound: boolean;
  Page?: ComponentType<{ params: Promise<Record<string, string>>; searchParams: Promise<Record<string, string | string[]>> }>;
  frames: Frame[];
  params: Record<string, string>;
  paramsPromise: Promise<Record<string, string>>;
  searchPromise: Promise<Record<string, string | string[]>>;
}

function searchObject(search: string): Record<string, string | string[]> {
  const out: Record<string, string | string[]> = {};
  for (const [key, value] of new URLSearchParams(search)) {
    const existing = out[key];
    out[key] = existing === undefined ? value : Array.isArray(existing) ? [...existing, value] : [existing, value];
  }
  return out;
}

async function titleOf(module: Record<string, unknown>, props: object): Promise<string | undefined> {
  let metadata = module.metadata as { title?: unknown } | undefined;
  if (typeof module.generateMetadata === 'function') {
    try {
      metadata = await (module.generateMetadata as (p: object) => Promise<{ title?: unknown }>)(props);
    } catch {
      metadata = undefined;
    }
  }
  const title = metadata?.title;
  if (typeof title === 'string') return title;
  if (title && typeof title === 'object') {
    const shaped = title as { absolute?: string; default?: string };
    return shaped.absolute ?? shaped.default;
  }
  return undefined;
}

async function resolve(place: Place, generation: number): Promise<Resolved> {
  const found = match(place.pathname);
  const params = found?.params ?? {};
  const paramsPromise = Promise.resolve(params);
  const searchPromise = Promise.resolve(searchObject(place.search));
  const base = { key: place.pathname, place, generation, params, paramsPromise, searchPromise };

  if (!found) return { ...base, notFound: true, frames: [] };

  const parts = found.route.dir.split('/').filter(Boolean);
  const dirs = parts.map((_, index) => parts.slice(0, index + 1).join('/'));
  const [pageModule, ...frames] = await Promise.all([
    found.route.load(),
    ...dirs.map(async (dir): Promise<Frame> => {
      const [layout, loading, error] = await Promise.all([layouts.get(dir)?.(), loadings.get(dir)?.(), errors.get(dir)?.()]);
      return {
        dir,
        Layout: layout?.default as Frame['Layout'],
        Loading: loading?.default as Frame['Loading'],
        ErrorView: error?.default as Frame['ErrorView'],
      };
    }),
  ]);

  const title = await titleOf(pageModule, { params: paramsPromise, searchParams: searchPromise });
  document.title = title ? (title.includes('Happy Paws') ? title : `${title} · Happy Paws Clinic`) : 'Happy Paws Clinic';

  return { ...base, notFound: false, Page: pageModule.default as Resolved['Page'], frames };
}

// ---- Error boundaries ------------------------------------------------------------------------------

class SegmentBoundary extends Component<
  { generation: number; View: NonNullable<Frame['ErrorView']>; children: ReactNode },
  { error: (Error & { digest?: string }) | null; generation: number }
> {
  state = { error: null as (Error & { digest?: string }) | null, generation: this.props.generation };

  static getDerivedStateFromError(error: Error) {
    return { error };
  }

  static getDerivedStateFromProps(props: { generation: number }, state: { generation: number }) {
    // A new page or a refresh gives the segment another try.
    return props.generation !== state.generation ? { error: null, generation: props.generation } : null;
  }

  render() {
    const { error } = this.state;
    if (error) {
      // redirect() and notFound() belong to the router, not to the segment's own error screen.
      if (isNavigationError(error)) throw error;
      const View = this.props.View;
      return <View error={error} reset={() => this.setState({ error: null })} />;
    }
    return this.props.children;
  }
}

class RootBoundary extends Component<{ generation: number; children: ReactNode }, { error: Error | null; generation: number }> {
  state = { error: null as Error | null, generation: this.props.generation };

  static getDerivedStateFromError(error: Error) {
    return { error };
  }

  static getDerivedStateFromProps(props: { generation: number }, state: { generation: number }) {
    return props.generation !== state.generation ? { error: null, generation: props.generation } : null;
  }

  componentDidCatch(error: Error, info: ErrorInfo) {
    if (error instanceof RedirectError) {
      navigate(error.url, { replace: true });
      return;
    }
    if (!(error instanceof NotFoundError)) {
      console.error('[demo] page failed', error, info.componentStack);
    }
  }

  render() {
    const { error } = this.state;
    if (error instanceof RedirectError) return null;
    if (error instanceof NotFoundError) return <NotFoundPage />;
    if (error) return <BrokenPage error={error} />;
    return this.props.children;
  }
}

function NotFoundPage() {
  return (
    <main className="mx-auto flex min-h-[60vh] max-w-lg flex-col items-center justify-center gap-4 px-4 text-center" data-demo-state="not-found">
      <h1 className="text-2xl font-semibold text-ink-900">ไม่พบหน้านี้</h1>
      <p className="text-ink-600">ลิงก์อาจผิด หรือหน้านี้ถูกย้ายไปแล้ว</p>
      <a href="/" className="rounded-lg bg-brand-600 px-4 py-2 font-medium text-white">
        กลับหน้าแรก
      </a>
    </main>
  );
}

function BrokenPage({ error }: { error: Error }) {
  return (
    <main className="mx-auto flex min-h-[60vh] max-w-lg flex-col items-center justify-center gap-4 px-4 text-center" data-demo-state="error">
      <h1 className="text-2xl font-semibold text-ink-900">หน้านี้แสดงไม่ได้ในเดโม</h1>
      <p className="text-ink-600">{error.message}</p>
      <a href="/" className="rounded-lg bg-brand-600 px-4 py-2 font-medium text-white">
        กลับหน้าแรก
      </a>
    </main>
  );
}

// ---- The tree --------------------------------------------------------------------------------------

function tree(resolved: Resolved): ReactNode {
  if (resolved.notFound || !resolved.Page) return <NotFoundPage />;

  const { Page } = resolved;
  let node: ReactNode = <Page key={resolved.key} params={resolved.paramsPromise} searchParams={resolved.searchPromise} />;

  for (let index = resolved.frames.length - 1; index >= 0; index -= 1) {
    const frame = resolved.frames[index]!;
    if (frame.Loading) {
      const Loading = frame.Loading;
      node = <Suspense fallback={<Loading />}>{node}</Suspense>;
    }
    if (frame.ErrorView) {
      node = (
        <SegmentBoundary generation={resolved.generation} View={frame.ErrorView}>
          {node}
        </SegmentBoundary>
      );
    }
    if (frame.Layout) {
      const Layout = frame.Layout;
      node = <Layout params={resolved.paramsPromise}>{node}</Layout>;
    }
  }
  return node;
}

export function Router({ Root }: { Root: ComponentType<{ children: ReactNode }> }) {
  const [resolved, setResolved] = useState<Resolved | null>(null);
  const [scrollTo, setScrollTo] = useState<{ top: boolean; hash: string } | null>(null);

  useEffect(() => {
    let latest = 0;

    async function go(place: Place, generation: number, scroll: boolean) {
      const ticket = ++latest;
      if (needsSession(place.pathname) && !getCookie('hp_at') && !getCookie('hp_rt')) {
        navigate(`/login?next=${encodeURIComponent(place.pathname + place.search)}`, { replace: true });
        return;
      }
      const next = await resolve(place, generation);
      if (ticket !== latest) return;
      startTransition(() => {
        setResolved(next);
        if (scroll) setScrollTo({ top: true, hash: place.hash });
      });
    }

    const off = subscribe(({ place, generation, scroll }) => void go(place, generation, scroll));
    void go(currentPlace(), currentGeneration(), true);
    return off;
  }, []);

  useEffect(() => {
    if (resolved) prune(resolved.generation - 1);
  }, [resolved]);

  useLayoutEffect(() => {
    if (!scrollTo) return;
    const target = scrollTo.hash ? document.getElementById(decodeURIComponent(scrollTo.hash.slice(1))) : null;
    if (target) target.scrollIntoView();
    else if (scrollTo.top) window.scrollTo(0, 0);
  }, [scrollTo]);

  if (!resolved) return <BootScreen />;

  return (
    <GenerationContext value={resolved.generation}>
      <PlaceContext value={resolved.place}>
        <ParamsContext value={resolved.params}>
          <RootBoundary generation={resolved.generation}>
            <Suspense fallback={<BootScreen />}>
              <Root>{tree(resolved)}</Root>
            </Suspense>
          </RootBoundary>
        </ParamsContext>
      </PlaceContext>
    </GenerationContext>
  );
}

function BootScreen() {
  return (
    <div className="flex min-h-[50vh] items-center justify-center" aria-busy="true" aria-live="polite">
      <span className="text-sm text-ink-500">กำลังโหลด…</span>
    </div>
  );
}
