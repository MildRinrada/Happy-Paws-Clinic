export type PackageStatus = 'active' | 'used_up' | 'expired';

/** One package and what is left of it — an entry of `GET /portal/packages`. */
export interface PackageBalance {
  id: string;
  name: string;
  /** The pet it was bought for; absent when any pet on the account may use it. */
  pet_id?: string | null;
  service_ids: string[];
  total_uses: number;
  used_count: number;
  remaining: number;
  purchased_at: string;
  /** A clinic day, `YYYY-MM-DD`; absent when it does not expire. */
  expires_on?: string | null;
  status: PackageStatus;
}
