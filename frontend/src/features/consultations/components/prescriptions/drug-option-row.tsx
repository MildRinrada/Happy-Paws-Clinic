import type { DrugOption } from '@/features/prescriptions/types/drug-option.types';
import { drugPicker } from '../../content/drug-picker';
import { StockStatusBadge } from './stock-status-badge';

/** One suggestion: the name, then the stock at the case's branch and the price per unit. */
export function DrugOptionRow({ option }: { option: DrugOption }) {
  return (
    <span className="flex flex-col gap-0.5">
      <span className="font-medium text-ink-900">{option.name}</span>
      <span className="flex flex-wrap items-center gap-2 text-xs text-ink-600">
        <StockStatusBadge status={option.status} />
        <span>{drugPicker.available(option.available, option.unit)}</span>
        <span>{drugPicker.price(option.unit_price_satang, option.unit)}</span>
      </span>
    </span>
  );
}
