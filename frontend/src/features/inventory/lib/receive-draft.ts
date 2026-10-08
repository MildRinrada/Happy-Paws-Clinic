/** What only a receipt carries, as typed: the cost in baht, the expiry as `YYYY-MM-DD`. */
export interface ReceiveDraft {
  lotNumber: string;
  expiresOn: string;
  unitCost: string;
  orderId: string;
}

export const emptyReceive: ReceiveDraft = { lotNumber: '', expiresOn: '', unitCost: '', orderId: '' };
