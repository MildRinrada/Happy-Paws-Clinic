import { portalReferral } from '../../content/portal-referral';
import { referralPaths } from '../../lib/referral-paths';

/** Opens LINE's share sheet with a short message and the invite link. */
export function LineShareLink({ link }: { link: string }) {
  return (
    <a
      href={referralPaths.lineShare(portalReferral.lineMessage(link))}
      target="_blank"
      rel="noopener noreferrer"
      className="inline-flex h-11 items-center rounded-xl bg-[#06C755] px-5 text-sm font-bold text-white transition hover:brightness-95"
    >
      {portalReferral.line}
    </a>
  );
}
