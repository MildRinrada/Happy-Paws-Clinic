import type { LineBenefit as LineBenefitData } from '../../types/home.types';

/** One numbered thing LINE will do for the owner. */
export function LineBenefit({ number, benefit }: { number: number; benefit: LineBenefitData }) {
  return (
    <li className="flex min-h-[75px] items-center gap-[18px] rounded-2xl bg-white/30 px-[19px] py-3">
      <span
        aria-hidden="true"
        className="flex size-8 shrink-0 items-center justify-center rounded-lg bg-white text-lg font-bold text-line-green"
      >
        {number}
      </span>
      <p className="text-[15px] font-semibold leading-snug">{benefit.text}</p>
    </li>
  );
}
