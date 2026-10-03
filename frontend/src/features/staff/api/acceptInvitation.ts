import { publicApi } from '@/lib/api/server-client';
import type { AcceptInvitationResponse } from '../types/invitation.types';

export interface AcceptInvitationInput {
  invitationId: string;
  token: string;
  /** Both left out on the first call, which only asks for the authenticator secret. */
  password?: string;
  code?: string;
}

/**
 * `POST /staff/members/invitations/{id}/accept`. Anonymous: the invitee has no account yet, and
 * the token from the link is the only credential. An unknown, wrong, expired or used link is `404`.
 */
export function acceptInvitation({ invitationId, token, password, code }: AcceptInvitationInput) {
  return publicApi().post<AcceptInvitationResponse>(
    `/staff/members/invitations/${encodeURIComponent(invitationId)}/accept`,
    { body: { token, password, code } },
  );
}
