import { formatBaht } from '@/lib/format/money';
import { prescriptionsPage } from '../content/prescriptions-page';
import type { DispenseResult } from '../types/prescription.types';

/**
 * What the dispense did besides the label: whether stock was taken (or why not), and where the
 * medicine's charge went. Not printed.
 */
export function DispenseOutcome({ result }: { result: DispenseResult }) {
  const stockText = prescriptionsPage.stock;
  const chargeText = prescriptionsPage.charge;
  const { stock, charge } = result;
  const taken = (stock.lots ?? []).reduce((sum, lot) => sum + lot.quantity, 0);

  const stockLine =
    stock.status === 'deducted'
      ? stockText.deducted(taken, stock.unit ?? '', stock.item_name ?? '')
      : stock.status === 'insufficient'
        ? stockText.insufficient(stock.available ?? 0, stock.unit ?? '')
        : stockText.notMatched;

  const amount = charge ? formatBaht(charge.amount_satang) : '';
  const chargeLine = !charge
    ? chargeText.none
    : charge.placement === 'on_close'
      ? chargeText.onClose(amount)
      : charge.placement === 'separate_invoice'
        ? chargeText.separate(amount, charge.invoice_number ?? '')
        : chargeText.invoiced(amount, charge.invoice_number ?? '');

  return (
    <div className="space-y-0.5 text-xs">
      <p className={stock.status === 'deducted' ? 'text-ink-600' : 'text-amber-800'}>{stockLine}</p>
      <p className="text-ink-600">{chargeLine}</p>
    </div>
  );
}
