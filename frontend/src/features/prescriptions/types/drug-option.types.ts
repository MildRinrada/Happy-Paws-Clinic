/** Stock of one medicine at one branch, judged on usable units like the stock list. */
export type DrugStockStatus = 'ok' | 'low' | 'out';

/**
 * One medicine a prescriber can pick: `GET /staff/prescriptions/drug-options`, a contract extension
 * (docs/contract-extensions.md § 20). No cost, lots, suppliers or other branches, on purpose.
 */
export interface DrugOption {
  /** Sent back as `inventory_item_id` when the prescription is written. */
  item_id: string;
  name: string;
  /** What the prescription's quantity counts: เม็ด, ขวด. */
  unit: string;
  /** Selling price of one unit, charged when dispensed. */
  unit_price_satang: number;
  /** Usable units at the branch. */
  available: number;
  status: DrugStockStatus;
}

/** The picker's answer: medicines by name, with the stock of `branch_id`. */
export interface DrugOptions {
  branch_id: string;
  data: DrugOption[];
}
