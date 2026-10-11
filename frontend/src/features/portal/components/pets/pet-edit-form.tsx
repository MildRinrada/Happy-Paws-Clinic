'use client';

import Link from 'next/link';
import { useRouter } from 'next/navigation';
import { useState, type FormEvent } from 'react';
import { Alert } from '@/components/ui/alert';
import { startPhotoUploadAction, updatePetAction } from '../../actions/pet-actions';
import { portalPaths } from '../../content/portal-navigation';
import { petEdit } from '../../content/pet-edit';
import type { PhotoType } from '../../lib/photo-rules';
import { uploadToSignedUrl } from '@/lib/api/signed-upload';
import type { MyPet } from '../../types/portal.types';
import { TextAreaField } from '../shared/text-area-field';
import { TextField } from '../shared/text-field';
import { PhotoField } from './photo-field';

type Phase = 'idle' | 'uploading' | 'saving';

/**
 * The four things an owner may change about their pet. Saving uploads a new photo first, if one
 * was chosen, then saves everything in one request.
 */
export function PetEditForm({ pet }: { pet: MyPet }) {
  const router = useRouter();
  const [nickname, setNickname] = useState(pet.nickname ?? '');
  const [diet, setDiet] = useState(pet.diet ?? '');
  const [behaviour, setBehaviour] = useState(pet.behaviour ?? '');
  const [photo, setPhoto] = useState<File>();
  const [phase, setPhase] = useState<Phase>('idle');
  const [error, setError] = useState<string>();

  async function uploadPhoto(file: File): Promise<string | undefined> {
    setPhase('uploading');

    const started = await startPhotoUploadAction({
      petId: pet.id,
      fileName: file.name,
      // Already checked against the allowed types when the file was picked (`PhotoField`).
      contentType: file.type.toLowerCase() as PhotoType,
      sizeBytes: file.size,
    });

    if (!started.ok) {
      setError(started.error.message);
      return undefined;
    }

    if (!(await uploadToSignedUrl(started.data.upload, file))) {
      setError(petEdit.photoUploadFailed);
      return undefined;
    }

    return started.data.document.id;
  }

  async function save(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setError(undefined);

    try {
      const photoDocumentId = photo ? await uploadPhoto(photo) : undefined;
      if (photo && !photoDocumentId) return;

      setPhase('saving');
      const result = await updatePetAction({ petId: pet.id, version: pet.version, nickname, diet, behaviour, photoDocumentId });

      if (result.ok) {
        router.push(portalPaths.pet(pet.id));
        return;
      }

      setError(result.error.code === 'version_conflict' ? petEdit.conflict : result.error.message);
    } catch {
      setError(petEdit.unreachable);
    } finally {
      setPhase('idle');
    }
  }

  const busy = phase !== 'idle';

  return (
    <form onSubmit={save} className="space-y-5">
      {error && <Alert>{error}</Alert>}

      <PhotoField currentUrl={pet.photo_url} name={pet.name} onChange={setPhoto} />

      <TextField
        id="pet-nickname"
        label={petEdit.nickname}
        hint={petEdit.nicknameHint}
        value={nickname}
        onChange={(event) => setNickname(event.target.value)}
        maxLength={60}
      />
      <TextAreaField
        id="pet-diet"
        label={petEdit.diet}
        hint={petEdit.dietHint}
        value={diet}
        onChange={(event) => setDiet(event.target.value)}
        maxLength={500}
        rows={3}
      />
      <TextAreaField
        id="pet-behaviour"
        label={petEdit.behaviour}
        hint={petEdit.behaviourHint}
        value={behaviour}
        onChange={(event) => setBehaviour(event.target.value)}
        maxLength={1000}
        rows={4}
      />
      <p className="text-xs text-site-muted">{petEdit.clearHint}</p>

      <div className="flex flex-col gap-3 sm:flex-row">
        <button
          type="submit"
          disabled={busy}
          className="h-12 flex-1 rounded-xl bg-site-orange text-sm font-bold text-white transition hover:brightness-95 disabled:cursor-not-allowed disabled:opacity-50"
        >
          {phase === 'uploading' ? petEdit.uploading : phase === 'saving' ? petEdit.saving : petEdit.save}
        </button>
        <Link
          href={portalPaths.pet(pet.id)}
          className="inline-flex h-12 items-center justify-center rounded-xl border border-ink-200 px-5 text-sm font-semibold text-ink-700 hover:bg-ink-50"
        >
          {petEdit.cancel}
        </Link>
      </div>
    </form>
  );
}
