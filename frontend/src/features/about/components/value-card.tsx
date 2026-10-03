import type { CarePromise } from '@/features/home/types/home.types';

/** One numbered promise on a light card. */
export function ValueCard({ number, value }: { number: number; value: CarePromise }) {
  return (
    <li className="rounded-2xl bg-white p-5 shadow-[0_1px_0_rgba(0,0,0,0.04)]">
      <span aria-hidden="true" className="text-2xl font-bold text-site-orange">
        {String(number).padStart(2, '0')}
      </span>
      <h3 className="mt-2 text-lg font-bold text-ink-900">{value.title}</h3>
      <p className="mt-1 text-[15px] text-site-muted">{value.detail}</p>
    </li>
  );
}
