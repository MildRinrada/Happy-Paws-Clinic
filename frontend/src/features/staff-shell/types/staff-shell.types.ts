import type { QuickActionKey } from '@/features/analytics/lib/quick-actions';

/** The console's line icons, drawn in `components/layout/staff-shell/staff-icon.tsx`. */
export type StaffIconName =
  | 'dashboard'
  | 'queue'
  | 'calendar'
  | 'messages'
  | 'customers'
  | 'lab'
  | 'prescriptions'
  | 'billing'
  | 'inventory'
  | 'crm'
  | 'notifications'
  | 'schedules'
  | 'reports'
  | 'ai'
  | 'settings'
  | 'menu'
  | 'close'
  | 'search'
  | 'plus'
  | 'chevronDown'
  | 'collapse'
  | 'expand'
  | 'account'
  | 'signOut';

/** The menu entries that can carry a count of work waiting behind them. */
export type NavBadgeKey = 'queue' | 'messages' | 'stock' | 'lab';

/** One count beside a menu entry. `more` when the read hit its page size, so `count` is a floor. */
export interface NavCount {
  count: number;
  more: boolean;
}

/**
 * The counts that could be read and are not zero. A key is absent both when nothing is waiting and
 * when the read failed or is not allowed: the menu then shows no badge, never a zero it cannot vouch for.
 */
export type NavCounts = Partial<Record<NavBadgeKey, NavCount>>;

/** A menu entry this session may see, ready for the browser. */
export interface VisibleNavItem {
  /** Where the link goes (settings opens its first usable tab). */
  href: string;
  /** The section the entry stands for, which decides when it is the current page. */
  section: string;
  label: string;
  icon: StaffIconName;
  badge?: NavBadgeKey;
}

export interface VisibleNavGroup {
  key: string;
  label: string;
  items: VisibleNavItem[];
}

/** One entry of the "+ สร้าง" menu. */
export interface CreateAction {
  key: QuickActionKey;
  href: string;
  label: string;
}

/** A customer as the top bar's search lists them. */
export interface CustomerHit {
  id: string;
  name: string;
  phone: string;
}

/** A patient as the top bar's search lists them. */
export interface PetHit {
  id: string;
  name: string;
  hospitalNumber: string;
  species: string;
  microchip?: string;
  deceased: boolean;
}

export interface ConsoleSearchResults {
  customers: CustomerHit[];
  pets: PetHit[];
}
