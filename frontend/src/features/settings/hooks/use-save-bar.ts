'use client';

import { useContext } from 'react';
import { SaveBarActionsContext } from '../components/save-bar/save-bar-context';

/** The settings area's save bar, from inside a form or the page list. */
export function useSaveBar() {
  return useContext(SaveBarActionsContext);
}
