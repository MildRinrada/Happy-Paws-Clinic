import { branchDotClass } from '@/features/staff-shell/lib/branch-colour';

/** The small round mark in a branch's own colour. Decoration only: the name is beside it. */
export function BranchDot({ branchId }: { branchId: string }) {
  return <span aria-hidden className={`size-2.5 shrink-0 rounded-full ${branchDotClass(branchId)}`} />;
}
