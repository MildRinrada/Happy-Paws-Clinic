'use client';

import { createContext, useContext } from 'react';
import type { NavCounts } from '@/features/staff-shell/types/staff-shell.types';

/** The menu's badges, shared by the sidebar and the phone drawer so both show the same numbers. */
export const NavCountsContext = createContext<NavCounts>({});

export function useNavCounts(): NavCounts {
  return useContext(NavCountsContext);
}
