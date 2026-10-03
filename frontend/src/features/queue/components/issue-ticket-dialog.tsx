'use client';

import { useEffect, useRef, useState, useTransition } from 'react';
import { Alert } from '@/components/ui/alert';
import { Button } from '@/components/ui/button';
import { Field, Input, Select } from '@/components/ui/field';
import type { PetSummary } from '@/features/pets/types/pet.types';
import type { PublicService } from '@/features/service-catalog/types/service.types';
import { issueWalkInTicketAction, searchPetsAction } from '../actions/queue-actions';
import type { TicketPriority } from '../types/queue.types';

interface Props {
  branchId: string;
  services: PublicService[];
}

/**
 * The counter's "walk-in" form: find the patient, pick the service, print a number.
 *
 * One idempotency key is minted when the form opens and sent with every attempt from it. That is
 * what makes a double-click, or a retry after a dropped response, return the ticket already
 * issued instead of printing a second one — the contract requires the header on every POST that
 * creates a queue ticket, for exactly this desk.
 */
export function IssueTicketDialog({ branchId, services }: Props) {
  const dialog = useRef<HTMLDialogElement>(null);
  const [idempotencyKey, setIdempotencyKey] = useState('');

  const [query, setQuery] = useState('');
  const [matches, setMatches] = useState<PetSummary[]>([]);
  const [pet, setPet] = useState<PetSummary | null>(null);
  const [serviceId, setServiceId] = useState(services[0]?.id ?? '');
  const [priority, setPriority] = useState<TicketPriority>('normal');
  const [reason, setReason] = useState('');

  const [error, setError] = useState<string | null>(null);
  const [issued, setIssued] = useState<string | null>(null);
  const [searching, startSearch] = useTransition();
  const [submitting, startSubmit] = useTransition();

  // Search as the counter types, but not on every keystroke: a short pause means they have stopped.
  useEffect(() => {
    if (pet || query.trim().length < 2) {
      setMatches([]);
      return;
    }

    const timer = setTimeout(() => {
      startSearch(async () => {
        const result = await searchPetsAction(query);

        setMatches(result.ok ? result.data : []);
        setError(result.ok ? null : result.error.message);
      });
    }, 250);

    return () => clearTimeout(timer);
  }, [query, pet]);

  function open() {
    setIdempotencyKey(crypto.randomUUID());
    setQuery('');
    setPet(null);
    setPriority('normal');
    setReason('');
    setError(null);
    setIssued(null);
    dialog.current?.showModal();
  }

  function submit() {
    if (!pet || !serviceId) {
      return;
    }

    setError(null);
    startSubmit(async () => {
      const result = await issueWalkInTicketAction({
        petId: pet.id,
        serviceId,
        branchId,
        priority,
        priorityReason: priority === 'normal' ? undefined : reason,
        idempotencyKey,
      });

      if (result.ok) {
        setIssued(result.data.number);
      } else {
        setError(result.error.message);
      }
    });
  }

  const needsReason = priority !== 'normal';
  const ready = pet !== null && serviceId !== '' && (!needsReason || reason.trim().length > 0);

  return (
    <>
      <Button onClick={open}>+ ออกบัตรคิว walk-in</Button>

      <dialog
        ref={dialog}
        aria-labelledby="issue-ticket-title"
        // m-auto: Tailwind's reset zeroes the margin a modal <dialog> centres itself with.
        className="m-auto w-full max-w-lg rounded-2xl border border-ink-200 p-0 backdrop:bg-ink-900/40"
      >
        <div className="space-y-4 p-6">
          <h2 id="issue-ticket-title" className="font-display text-lg font-semibold text-ink-900">
            ออกบัตรคิว walk-in
          </h2>

          {issued ? (
            <div className="space-y-4 text-center">
              <p className="text-sm text-ink-600">ออกบัตรคิวแล้ว</p>
              <p className="font-display text-6xl font-semibold tracking-tight text-brand-700">{issued}</p>
              <Button onClick={() => dialog.current?.close()}>ปิด</Button>
            </div>
          ) : (
            <>
              {error && <Alert>{error}</Alert>}

              {pet ? (
                <div className="flex items-center justify-between rounded-lg border border-brand-200 bg-brand-50 px-3.5 py-2.5">
                  <div>
                    <p className="font-medium text-ink-900">{pet.name}</p>
                    <p className="text-sm text-ink-600">
                      {[pet.species, pet.breed, pet.hospital_number].filter(Boolean).join(' · ')}
                    </p>
                  </div>
                  <Button size="sm" variant="ghost" onClick={() => setPet(null)}>
                    เปลี่ยน
                  </Button>
                </div>
              ) : (
                <Field label="ค้นหาสัตว์เลี้ยง" htmlFor="walkin-pet" hint="พิมพ์ชื่อ หรือเลข HN อย่างน้อย 2 ตัวอักษร">
                  <Input
                    id="walkin-pet"
                    value={query}
                    autoComplete="off"
                    onChange={(event) => setQuery(event.target.value)}
                  />
                  {searching && <p className="text-sm text-ink-500">กำลังค้นหา…</p>}
                  {matches.length > 0 && (
                    <ul className="max-h-48 divide-y divide-ink-100 overflow-auto rounded-lg border border-ink-200">
                      {matches.map((match) => (
                        <li key={match.id}>
                          <button
                            type="button"
                            className="w-full px-3.5 py-2 text-left hover:bg-ink-50"
                            onClick={() => setPet(match)}
                          >
                            <span className="font-medium text-ink-900">{match.name}</span>{' '}
                            <span className="text-sm text-ink-500">
                              {match.species} · {match.hospital_number}
                            </span>
                          </button>
                        </li>
                      ))}
                    </ul>
                  )}
                </Field>
              )}

              <Field label="บริการ" htmlFor="walkin-service">
                <Select id="walkin-service" value={serviceId} onChange={(event) => setServiceId(event.target.value)}>
                  {services.length === 0 && <option value="">ยังไม่มีบริการที่เปิดให้เลือก</option>}
                  {services.map((service) => (
                    <option key={service.id} value={service.id}>
                      {service.name} · {service.duration_minutes} นาที
                    </option>
                  ))}
                </Select>
              </Field>

              <Field label="ความเร่งด่วน" htmlFor="walkin-priority">
                <Select
                  id="walkin-priority"
                  value={priority}
                  onChange={(event) => setPriority(event.target.value as TicketPriority)}
                >
                  <option value="normal">ตามลำดับ</option>
                  <option value="urgent">เร่งด่วน</option>
                  <option value="emergency">ฉุกเฉิน</option>
                </Select>
              </Field>

              {needsReason && (
                <Field
                  label="เหตุผลที่ต้องแซงคิว"
                  htmlFor="walkin-reason"
                  hint="บันทึกไว้ใน audit log — การแซงคิวต้องอธิบายได้ภายหลัง"
                >
                  <Input id="walkin-reason" value={reason} onChange={(event) => setReason(event.target.value)} />
                </Field>
              )}

              <div className="flex justify-end gap-2 pt-2">
                <Button variant="ghost" onClick={() => dialog.current?.close()}>
                  ยกเลิก
                </Button>
                <Button disabled={!ready || submitting} onClick={submit}>
                  {submitting ? 'กำลังออกบัตร…' : 'ออกบัตรคิว'}
                </Button>
              </div>
            </>
          )}
        </div>
      </dialog>
    </>
  );
}
