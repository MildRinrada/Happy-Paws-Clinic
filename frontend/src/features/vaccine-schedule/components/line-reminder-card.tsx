/* eslint-disable @next/next/no-img-element -- the QR code is a data URL drawn on the server; next/image has nothing to optimise. */
import { LineIcon } from '@/components/layout/site-footer/icons/line-icon';
import { vaccinePage } from '../content/vaccine-schedule-page';
import type { LineLink } from '../types/vaccine-schedule.types';

/**
 * Add the clinic on LINE to be reminded. On a phone, a button that opens LINE; on a computer, the
 * same link as a QR code to scan with the phone, beside the button.
 */
export function LineReminderCard({ line }: { line: LineLink }) {
  const text = vaccinePage.actions.line;

  return (
    <li className="flex flex-col rounded-2xl bg-line-green p-5 text-white sm:p-6 lg:col-span-2 lg:flex-row lg:items-center lg:gap-8">
      <div className="flex-1">
        <h3 className="text-xl font-bold leading-tight">{text.title}</h3>
        <p className="mt-2 text-[15px] leading-relaxed text-white/95">{text.detail}</p>
        <a
          href={line.href}
          target="_blank"
          rel="noreferrer"
          className="mt-4 inline-flex h-[52px] items-center justify-center gap-2.5 rounded-[10px] bg-white px-5 text-base font-bold text-line-green transition hover:bg-white/90"
        >
          <LineIcon className="size-7" />
          {text.button}
        </a>
        <p className="mt-2 text-sm font-semibold">{line.handle}</p>
      </div>

      {line.qrDataUrl && (
        <figure className="hidden shrink-0 text-center lg:block">
          <img
            src={line.qrDataUrl}
            alt={text.qrAlt}
            width={160}
            height={160}
            className="size-40 rounded-xl bg-white p-2"
          />
          <figcaption className="mt-2 text-[13px]">{text.qrCaption}</figcaption>
        </figure>
      )}
    </li>
  );
}
