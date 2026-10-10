/**
 * `GET /staff/inventory/forecast` (docs/contract-extensions.md § 33). Absent keys are null: an item
 * nobody uses has no `days_until_out` and no `runs_out_on`.
 */

export type ForecastConfidence = 'low' | 'medium' | 'high';

export interface InventoryForecastItem {
  item_id: string;
  name: string;
  kind: 'medicine' | 'supply';
  unit: string;
  reorder_level: number;
  /** Usable units now: lots not expired. */
  available: number;
  on_order: number;
  used_30d: number;
  used_90d: number;
  avg_daily_30d: number;
  avg_daily_90d: number;
  /** The higher of the two averages: the rate the forecast uses. */
  daily_rate: number;
  days_until_out?: number;
  /** A clinic calendar day, `YYYY-MM-DD`. */
  runs_out_on?: string;
  suggested_order_qty: number;
  history_days: number;
  confidence: ForecastConfidence;
}

export interface InventoryForecast {
  branch_id: string;
  as_of: string;
  cover_days: number;
  lead_time_days: number;
  data: InventoryForecastItem[];
}
