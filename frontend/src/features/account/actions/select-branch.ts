'use server';

import { cookies } from 'next/headers';
import { revalidatePath } from 'next/cache';
import { BRANCH_COOKIE } from '../lib/staff-context';

/**
 * Remembers which branch the console is showing.
 *
 * A preference, not a permission. The value is only ever matched against the branches this
 * session may reach (`loadStaffContext`), and the backend re-checks the branch on every call — so
 * a hand-edited cookie can at most show the branch the session would have seen anyway.
 */
export async function selectBranch(branchId: string): Promise<void> {
  (await cookies()).set(BRANCH_COOKIE, branchId, {
    httpOnly: true,
    sameSite: 'lax',
    secure: process.env.NODE_ENV === 'production',
    path: '/',
    maxAge: 60 * 60 * 24 * 365,
  });

  revalidatePath('/', 'layout');
}
