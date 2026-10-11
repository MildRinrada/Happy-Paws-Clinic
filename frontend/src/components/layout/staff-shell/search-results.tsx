import { staffShellText } from '@/features/staff-shell/content/staff-shell-text';
import type { CustomerHit, PetHit } from '@/features/staff-shell/types/staff-shell.types';
import type { SearchStatus } from './use-console-search';
import { SearchOption } from './search-option';
import { StaffIcon } from './staff-icon';

/** A result with the id the box points at and the page it opens. */
export interface Choice<T> {
  id: string;
  href: string;
  hit: T;
}

interface SearchResultsProps {
  listId: string;
  status: SearchStatus;
  customers: Choice<CustomerHit>[];
  pets: Choice<PetHit>[];
  /** "ดูผลทั้งหมด", once there is something to search for. */
  seeAll?: Choice<string>;
  activeId?: string;
  onPick: (href: string) => void;
  onHover: (id: string) => void;
}

const heading = 'px-3 pb-1 pt-2 text-xs font-semibold text-ink-500';

/**
 * The list under the search box: customers, then patients, then the way to the full results on
 * the customers page. While the first answer is on its way, or when nothing matched, a line says so.
 */
export function SearchResults({ listId, status, customers, pets, seeAll, activeId, onPick, onHover }: SearchResultsProps) {
  const text = staffShellText.search;
  // A patient found by microchip shows the chip number, so the match is plain to see.
  const query = seeAll?.hit;
  const nothing = status === 'done' && customers.length === 0 && pets.length === 0;
  const note =
    status === 'short' ? text.tooShort : status === 'loading' ? text.searching : status === 'failed' ? text.failed : nothing ? text.empty : null;

  return (
    <div className="absolute inset-x-0 top-full z-40 mt-1 max-h-[70vh] overflow-y-auto rounded-xl border border-ink-200 bg-white p-1.5 shadow-lg md:min-w-[24rem]">
      {note && <p className="px-3 py-2 text-sm text-ink-500">{note}</p>}

      <ul id={listId} role="listbox" aria-label={text.label}>
        {customers.length > 0 && (
          <li role="presentation">
            <p id={`${listId}-customers`} className={heading}>
              {text.customers}
            </p>
            <ul role="group" aria-labelledby={`${listId}-customers`}>
              {customers.map(({ id, href, hit }) => (
                <SearchOption key={id} id={id} active={id === activeId} onPick={() => onPick(href)} onHover={() => onHover(id)}>
                  <StaffIcon name="account" className="size-4 shrink-0 text-ink-400" />
                  <span className="min-w-0 flex-1 truncate font-medium">{hit.name}</span>
                  <span className="shrink-0 tabular-nums text-ink-500">{hit.phone}</span>
                </SearchOption>
              ))}
            </ul>
          </li>
        )}

        {pets.length > 0 && (
          <li role="presentation">
            <p id={`${listId}-pets`} className={heading}>
              {text.pets}
            </p>
            <ul role="group" aria-labelledby={`${listId}-pets`}>
              {pets.map(({ id, href, hit }) => (
                <SearchOption key={id} id={id} active={id === activeId} onPick={() => onPick(href)} onHover={() => onHover(id)}>
                  <StaffIcon name="customers" className="size-4 shrink-0 text-ink-400" />
                  <span className="min-w-0 flex-1 truncate">
                    <span className="font-medium">{hit.name}</span> <span className="text-ink-500">{hit.species}</span>
                    {hit.deceased && <span className="ml-2 text-xs text-ink-500">({text.deceased})</span>}
                  </span>
                  <span className="shrink-0 font-mono text-xs text-ink-500">
                    {query && hit.microchip?.includes(query) ? `${text.microchip} ${hit.microchip}` : hit.hospitalNumber}
                  </span>
                </SearchOption>
              ))}
            </ul>
          </li>
        )}

        {seeAll && (
          <SearchOption id={seeAll.id} active={seeAll.id === activeId} onPick={() => onPick(seeAll.href)} onHover={() => onHover(seeAll.id)}>
            <StaffIcon name="search" className="size-4 shrink-0 text-ink-400" />
            <span className="min-w-0 truncate text-brand-700">{text.seeAll(seeAll.hit)}</span>
          </SearchOption>
        )}
      </ul>
    </div>
  );
}
