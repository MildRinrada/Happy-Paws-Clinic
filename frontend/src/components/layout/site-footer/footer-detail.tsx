import type { FooterDetail as FooterDetailData } from '@/config/site-footer';

/** A small label with its value in bold underneath — a phone number, a licence number. */
export function FooterDetail({ detail }: { detail: FooterDetailData }) {
  const value = detail.lines.map((line) => (
    <span key={line} className="block">
      {line}
    </span>
  ));

  return (
    <div>
      <dt className="text-[13px]">{detail.label}</dt>
      <dd className="mt-0.5 text-sm font-bold leading-[19px]">
        {detail.href ? (
          <a href={detail.href} className="hover:underline">
            {value}
          </a>
        ) : (
          value
        )}
      </dd>
    </div>
  );
}

/** A column of label–value pairs. */
export function FooterDetailList({ details }: { details: FooterDetailData[] }) {
  return (
    <dl className="space-y-3.5">
      {details.map((detail) => (
        <FooterDetail key={detail.label} detail={detail} />
      ))}
    </dl>
  );
}
