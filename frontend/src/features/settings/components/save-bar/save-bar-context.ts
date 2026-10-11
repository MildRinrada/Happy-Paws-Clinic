'use client';

import { createContext } from 'react';

/** What one form tells the bar about itself. */
export interface SaveBarEntry {
  /** The `id` of the `<form>` the bar's save button submits. */
  formId: string;
  /** The part of the page, as the bar names it: the card's heading. */
  section: string;
  dirty: boolean;
  pending: boolean;
  /** The last save was refused; the form says why next to its fields. */
  failed: boolean;
  /** False for a form that keeps its own save button and only asks before the page is left. */
  inBar: boolean;
}

export interface SaveBarActions {
  /** False outside the settings area, where a form keeps its own save button. */
  available: boolean;
  register: (entry: SaveBarEntry, reset: () => void) => void;
  unregister: (formId: string) => void;
  /** True when the page may be left: nothing is unsaved, or the person said so. */
  confirmLeave: () => boolean;
}

export const SaveBarActionsContext = createContext<SaveBarActions>({
  available: false,
  register: () => {},
  unregister: () => {},
  confirmLeave: () => true,
});

/** Every form on the page that has told the bar about itself. */
export const SaveBarEntriesContext = createContext<SaveBarEntry[]>([]);

/** Puts each named form back as it was last saved. */
export const SaveBarResetContext = createContext<(formIds: string[]) => void>(() => {});
