import Link from 'next/link';
import { labPage } from '../content/lab-page';
import { labHref, type LabFilters } from '../lib/lab-filters';

/** Everyone's requests, or only the ones the signed-in vet sent: "งานค้างของหมอ". */
export function LabOwnerToggle({ filters }: { filters: LabFilters }) {
  const text = labPage.owner;
  const options = [
    { mine: false, label: text.everyone },
    { mine: true, label: text.mine },
  ];

  return (
    <nav aria-label={text.label} className="flex items-center gap-1 text-sm">
      <span className="mr-1 text-ink-500">{text.label}</span>
      {options.map((option) => {
        const current = option.mine === filters.mine;

        return (
          <Link
            key={option.label}
            href={labHref({ ...filters, mine: option.mine, cursor: undefined })}
            aria-current={current ? 'page' : undefined}
            className={`rounded-md px-2 py-1 ${current ? 'bg-ink-100 font-medium text-ink-900' : 'text-ink-600 hover:bg-ink-50'}`}
          >
            {option.label}
          </Link>
        );
      })}
    </nav>
  );
}
