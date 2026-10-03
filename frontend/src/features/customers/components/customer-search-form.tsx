import Link from 'next/link';
import { Button, buttonClass } from '@/components/ui/button';
import { Input } from '@/components/ui/field';
import { customerList } from '../content/customer-list';
import { customerPaths } from '../lib/customer-paths';

/**
 * One box for a name, a phone, a pet's name or an HN, as the backend searches all four. A plain GET
 * form: the query lives in the address, so a search can be reloaded, bookmarked or sent back to.
 */
export function CustomerSearchForm({ query }: { query: string }) {
  const text = customerList.search;

  return (
    <form action={customerPaths.list} method="get" role="search" className="flex flex-wrap gap-2">
      <label htmlFor="customer-search" className="sr-only">
        {text.label}
      </label>
      <Input
        id="customer-search"
        name="q"
        type="search"
        defaultValue={query}
        placeholder={text.placeholder}
        autoComplete="off"
        autoFocus
        className="min-w-0 flex-1 sm:max-w-md"
      />
      <Button type="submit">{text.submit}</Button>
      {query && (
        <Link href={customerPaths.list} className={buttonClass('ghost')}>
          {text.clear}
        </Link>
      )}
    </form>
  );
}
