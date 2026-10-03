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
}
