export interface PriceTier {
  species?: string;
  min_weight_kg?: number;
  max_weight_kg?: number;
  price_satang: number;
}

/** A service on the public menu — `GET /services`, "เฉพาะบริการที่เปิดให้จองออนไลน์". */
export interface PublicService {
  id: string;
  code: string;
  name: string;
  category_code: string;
  duration_minutes: number;
  price_satang: number;
  price_tiers: PriceTier[];
  branch_ids: string[];
  /**
   * A line or two on what the service includes. Not in the API yet (a proposed extension that
   * awaits approval); the card shows it when it arrives and simply leaves it out until then.
   */
  description?: string;
}
