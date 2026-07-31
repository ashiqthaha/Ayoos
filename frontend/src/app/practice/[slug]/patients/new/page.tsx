"use client";

import Link from "next/link";
import { useParams, useRouter } from "next/navigation";
import { FormEvent, useState } from "react";

import { AyoosMark } from "@/components/ayoos-mark";
import { PatientFormFields } from "@/components/patient-form-fields";
import { UserMenu } from "@/components/user-menu";
import { ApiError, registerPatient, type PatientDuplicateMatch, type PatientInput } from "@/lib/api";
import { emptyEmergencyContact, formatDate, normalizePatient, patientCopy, type PatientField, type PatientFieldErrors, updatePatientField, validatePatient } from "@/lib/patients";

export default function RegisterPatientPage() {
  const { slug } = useParams<{ slug: string }>();
  const router = useRouter();
  const [form, setForm] = useState<PatientInput>(patientCopy);
  const [errors, setErrors] = useState<PatientFieldErrors>({});
  const [isSaving, setIsSaving] = useState(false);
  const [saveError, setSaveError] = useState<string | null>(null);
  const [possibleMatches, setPossibleMatches] = useState<PatientDuplicateMatch[]>([]);
  const [pendingInput, setPendingInput] = useState<PatientInput | null>(null);

  function updateField(field: PatientField, value: string) {
    setForm((current) => updatePatientField(current, field, value));
    setErrors((current) => ({ ...current, [field]: undefined, contact: undefined }));
    setPossibleMatches([]);
    setPendingInput(null);
    setSaveError(null);
  }

  function setEmergencyContact(enabled: boolean) {
    setForm((current) => ({ ...current, emergencyContact: enabled ? { ...emptyEmergencyContact } : null }));
    setPossibleMatches([]);
    setPendingInput(null);
  }

  async function save(input: PatientInput, confirmDuplicate: boolean) {
    setIsSaving(true);
    setSaveError(null);
    try {
      const result = await registerPatient(slug, input, confirmDuplicate);
      if (result.patient) {
        router.push(`/practice/${encodeURIComponent(slug)}/patients/${encodeURIComponent(result.patient.id)}`);
        return;
      }
      setPossibleMatches(result.possibleMatches);
      setPendingInput(input);
    } catch (error) {
      setSaveError(error instanceof ApiError ? error.message : "We couldn’t register this patient. Please try again.");
    } finally {
      setIsSaving(false);
    }
  }

  function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const validationErrors = validatePatient(form);
    if (Object.keys(validationErrors).length > 0) {
      setErrors(validationErrors);
      return;
    }
    const input = normalizePatient(form);
    void save(input, false);
  }

  return (
    <main className="min-h-screen bg-[#f3f8f7] text-slate-900">
      <div className="mx-auto max-w-5xl px-5 py-6 sm:px-8 sm:py-8">
        <header className="flex items-center justify-between gap-4"><AyoosMark /><div className="flex items-center gap-2"><Link href={`/practice/${encodeURIComponent(slug)}/patients`} className="hidden rounded-xl px-3 py-2 text-sm font-semibold text-slate-600 hover:bg-white hover:text-teal-700 md:inline-flex">All patients</Link><UserMenu /></div></header>
        <section className="mt-10 sm:mt-14">
          <p className="text-sm font-semibold text-teal-700">Patient registration</p>
          <h1 className="mt-2 text-3xl font-semibold tracking-[-0.045em] text-slate-950 sm:text-5xl">Register a patient</h1>
          <p className="mt-3 text-slate-500">Create a complete, practice-scoped patient record.</p>

          {possibleMatches.length > 0 && pendingInput && (
            <section className="mt-8 rounded-3xl border border-amber-200 bg-amber-50 p-6 sm:p-8" role="alert">
              <p className="text-sm font-semibold uppercase tracking-[0.12em] text-amber-700">Possible duplicate</p>
              <h2 className="mt-2 text-2xl font-semibold text-slate-950">Review the existing match before saving</h2>
              <p className="mt-2 text-sm leading-6 text-slate-600">An active patient has the same last name and date of birth. Confirm only if this is a different person.</p>
              <div className="mt-5 grid gap-3">
                {possibleMatches.map((match) => <div key={match.id} className="rounded-2xl border border-amber-100 bg-white px-5 py-4"><p className="font-semibold text-slate-900">{match.firstName} {match.lastName}</p><p className="mt-1 text-sm text-slate-500">Born {formatDate(match.dateOfBirth)} · {match.email || match.phone || "No contact shown"}</p></div>)}
              </div>
              <div className="mt-6 flex flex-wrap gap-3"><button type="button" onClick={() => { setPossibleMatches([]); setPendingInput(null); }} disabled={isSaving} className="rounded-xl border border-amber-300 bg-white px-5 py-3 text-sm font-semibold text-amber-800">Go back and review</button><button type="button" onClick={() => void save(pendingInput, true)} disabled={isSaving} className="rounded-xl bg-amber-700 px-5 py-3 text-sm font-semibold text-white disabled:opacity-60">{isSaving ? "Registering…" : "Different person — register anyway"}</button></div>
            </section>
          )}

          <form onSubmit={handleSubmit} noValidate className="mt-8 overflow-hidden rounded-3xl border border-white bg-white shadow-[0_24px_70px_rgba(15,118,110,0.09)]">
            <div className="px-6 py-8 sm:px-9 sm:py-9"><PatientFormFields value={form} errors={errors} disabled={isSaving} onChange={updateField} onEmergencyContactChange={setEmergencyContact} /></div>
            {saveError && <div className="mx-6 mb-6 rounded-2xl border border-rose-200 bg-rose-50 px-4 py-3 text-sm text-rose-700 sm:mx-9">{saveError}</div>}
            <div className="flex flex-col-reverse gap-3 border-t border-slate-100 bg-slate-50/70 px-6 py-5 sm:flex-row sm:justify-end sm:px-9"><Link href={`/practice/${encodeURIComponent(slug)}/patients`} className="rounded-xl px-5 py-3 text-center text-sm font-semibold text-slate-600 hover:bg-white">Cancel</Link><button type="submit" disabled={isSaving || possibleMatches.length > 0} className="rounded-xl bg-teal-700 px-5 py-3 text-sm font-semibold text-white shadow-lg shadow-teal-700/15 transition hover:bg-teal-800 disabled:opacity-50">{isSaving ? "Checking…" : "Register patient"}</button></div>
          </form>
        </section>
      </div>
    </main>
  );
}
