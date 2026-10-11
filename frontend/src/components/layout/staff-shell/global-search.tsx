'use client';

import { useRouter } from 'next/navigation';
import { useCallback, useId, useRef, useState, type FocusEvent, type KeyboardEvent } from 'react';
import { customerPaths } from '@/features/customers/lib/customer-paths';
import { petPaths } from '@/features/pets/lib/pet-paths';
import { staffShellText } from '@/features/staff-shell/content/staff-shell-text';
import type { CustomerHit, PetHit } from '@/features/staff-shell/types/staff-shell.types';
import { SearchResults, type Choice } from './search-results';
import { StaffIcon } from './staff-icon';
import { useConsoleSearch } from './use-console-search';
import { useSearchShortcut } from './use-search-shortcut';

/**
 * The top bar's one box for finding a customer or a patient, as the WAI-ARIA combobox pattern
 * describes it: focus stays in the box, the arrows move through the results, Enter opens the one
 * highlighted (or, with none, the full results on the customers page) and Escape closes the list,
 * then empties the box. `/` or Ctrl+K reaches it from anywhere.
 *
 * On a phone the box is a magnifier button that opens it in a strip under the top bar.
 */
export function GlobalSearch({ withPets }: { withPets: boolean }) {
  const router = useRouter();
  const listId = useId();
  const input = useRef<HTMLInputElement>(null);
  const [query, setQuery] = useState('');
  const [listOpen, setListOpen] = useState(false);
  const [expanded, setExpanded] = useState(false);
  const [activeId, setActiveId] = useState<string | undefined>();
  const { results, status } = useConsoleSearch(query, withPets);
  const text = staffShellText.search;
  const q = query.trim();

  const customers: Choice<CustomerHit>[] = (results?.customers ?? []).map((hit) => ({
    id: `${listId}-customer-${hit.id}`,
    href: customerPaths.detail(hit.id),
    hit,
  }));
  const pets: Choice<PetHit>[] = (results?.pets ?? []).map((hit) => ({
    id: `${listId}-pet-${hit.id}`,
    href: petPaths.detail(hit.id),
    hit,
  }));
  const seeAll: Choice<string> | undefined = q.length >= 2 ? { id: `${listId}-all`, href: customerPaths.search(q), hit: q } : undefined;
  const choices = [...customers, ...pets, ...(seeAll ? [seeAll] : [])];
  // A highlight left over from older results points at nothing; it is simply not shown.
  const active = choices.find((choice) => choice.id === activeId);
  const showList = listOpen && q.length > 0;

  const openBox = useCallback(() => {
    setExpanded(true);
    setListOpen(true);
    // On a wide screen the box is already showing, so the very next key lands in it. The phone's
    // strip only appears with this render, so it is focused once it is on the page; a box that
    // already has focus is left alone, or letters typed meanwhile would be selected and replaced.
    const focus = () => {
      input.current?.focus();
      input.current?.select();
    };
    focus();
    requestAnimationFrame(() => {
      if (document.activeElement !== input.current) focus();
    });
  }, []);

  useSearchShortcut(openBox);

  const reset = () => {
    setQuery('');
    setListOpen(false);
    setExpanded(false);
    setActiveId(undefined);
  };

  const go = (href: string) => {
    reset();
    input.current?.blur();
    router.push(href);
  };

  const move = (step: 1 | -1) => {
    if (choices.length === 0) return;

    const index = active ? choices.indexOf(active) : step === 1 ? -1 : 0;
    setActiveId(choices[(index + step + choices.length) % choices.length]?.id);
  };

  const onKeyDown = (event: KeyboardEvent<HTMLInputElement>) => {
    switch (event.key) {
      case 'ArrowDown':
      case 'ArrowUp':
        event.preventDefault();
        setListOpen(true);
        move(event.key === 'ArrowDown' ? 1 : -1);
        break;
      case 'Enter': {
        const target = active ?? seeAll;
        if (target) {
          event.preventDefault();
          go(target.href);
        }
        break;
      }
      case 'Escape':
        event.preventDefault();
        if (showList) {
          setListOpen(false);
          setActiveId(undefined);
        } else {
          reset();
          input.current?.blur();
        }
        break;
    }
  };

  // Leaving the box and its list for anywhere else closes both, and the phone's strip with them.
  const onBlur = (event: FocusEvent<HTMLDivElement>) => {
    if (!event.currentTarget.contains(event.relatedTarget as Node | null)) {
      setListOpen(false);
      setExpanded(false);
    }
  };

  const found = customers.length + pets.length;
  const announcement =
    status === 'loading' ? text.searching : status === 'failed' ? text.failed : status === 'done' ? (found > 0 ? text.found(found) : text.empty) : '';

  return (
    <div className="min-w-0 md:max-w-md md:flex-1">
      <button
        type="button"
        onClick={openBox}
        aria-label={text.open}
        className="flex size-10 items-center justify-center rounded-lg text-ink-700 hover:bg-ink-100 md:hidden"
      >
        <StaffIcon name="search" className="size-5" />
      </button>

      <div
        onBlur={onBlur}
        className={`${expanded ? 'absolute inset-x-0 top-full z-40 block border-b border-ink-200 bg-white p-3 shadow-lg' : 'hidden'} md:relative md:inset-auto md:block md:border-0 md:bg-transparent md:p-0 md:shadow-none`}
      >
        <div className="relative">
          <StaffIcon name="search" className="pointer-events-none absolute left-3 top-1/2 size-4 -translate-y-1/2 text-ink-400" />
          <input
            ref={input}
            type="search"
            role="combobox"
            aria-label={text.label}
            aria-expanded={showList}
            aria-controls={listId}
            aria-autocomplete="list"
            aria-activedescendant={showList ? active?.id : undefined}
            autoComplete="off"
            spellCheck={false}
            placeholder={text.placeholder}
            value={query}
            onChange={(event) => {
              setQuery(event.target.value);
              setListOpen(true);
              setActiveId(undefined);
            }}
            onFocus={() => setListOpen(true)}
            onKeyDown={onKeyDown}
            className="h-10 w-full rounded-lg border border-ink-200 bg-ink-50 pl-9 pr-9 text-sm text-ink-900 placeholder:text-ink-400 focus:border-brand-500 focus:bg-white md:h-9"
          />
          {!query && (
            <kbd
              aria-hidden
              className="pointer-events-none absolute right-2 top-1/2 hidden -translate-y-1/2 rounded border border-ink-200 bg-white px-1.5 font-sans text-xs text-ink-500 md:block"
            >
              {text.shortcut}
            </kbd>
          )}
        </div>

        {showList && (
          <SearchResults
            listId={listId}
            status={status}
            customers={customers}
            pets={pets}
            seeAll={seeAll}
            activeId={active?.id}
            onPick={go}
            onHover={setActiveId}
          />
        )}

        <p className="sr-only" aria-live="polite">
          {showList ? announcement : ''}
        </p>
      </div>
    </div>
  );
}
