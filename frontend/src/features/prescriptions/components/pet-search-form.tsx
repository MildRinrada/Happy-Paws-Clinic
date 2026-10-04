import Link from 'next/link';
import { Button, buttonClass } from '@/components/ui/button';
import { Input } from '@/components/ui/field';
import { prescriptionsPage } from '../content/prescriptions-page';
import { prescriptionPaths } from '../lib/prescription-paths';

/** A plain GET form: the query lives in the address, so a search can be reloaded or sent back to. */
export function PetSearchForm({ query }: { query: string }) {
  const text = prescriptionsPage.search;

  return (
    <form action={prescriptionPaths.list} method="get" role="search" className="no-print flex flex-wrap gap-2">
      <label htmlFor="rx-pet-search" className="sr-only">
        {text.label}
      </label>
      <Input
        id="rx-pet-search"
        name="q"
        type="search"
        defaultValue={query}
        placeholder={text.placeholder}
        autoComplete="off"
        className="min-w-0 flex-1 sm:max-w-md"
      />
      <Button type="submit">{text.submit}</Button>
      {query && (
        <Link href={prescriptionPaths.list} className={buttonClass('ghost')}>
          {text.clear}
        </Link>
      )}
    </form>
  );
}
