import { getLoyalty } from '../api/getLoyalty';

/**
 * A customer's points, for the hint beside "แลกแต้ม" on a bill. Only a hint: any failure leaves it
 * out and the backend still refuses a redemption larger than the balance.
 */
export async function loadPointsBalance(customerId: string): Promise<number | undefined> {
  try {
    return (await getLoyalty(customerId)).points_balance;
  } catch {
    return undefined;
  }
}
