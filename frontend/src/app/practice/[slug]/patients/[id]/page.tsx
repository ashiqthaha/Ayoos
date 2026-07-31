"use client";

import Link from "next/link";
import { useParams } from "next/navigation";
import { FormEvent, useEffect, useState } from "react";

import { AyoosMark } from "@/components/ayoos-mark";
import { PatientFormFields } from "@/components/patient-form-fields";
import { UserMenu } from "@/components/user-menu";
import { ApiError, deactivatePatient, getPatient, updatePatient, type Patient, type PatientInput } from "@/lib/api";
import { emptyEmergencyContact, formatDate, formatPatientName, normalizePatient, patientToInput, type PatientField, type PatientFieldErrors, updatePatientField, validatePatient } from "@/lib/patients";

const sexLabels = ["Female", "Male", "Other", "Unknown"];

export default function PatientDetailPage() {
  const { slug, id } = useParams<{ slug: string; id: string }>();
  const [patient, setPatient] = useState<Patient | null>(null);
  const [form, setForm] = useState<PatientInput | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [isNotFound, setIsNotFound] = useState(false);
  const [loadError, setLoadError] = useState<string | null>(null);
  const [isEditing, setIsEditing] = useState(false);
  const [errors, setErrors] = useState<PatientFieldErrors>({});
  const [isSaving, setIsSaving] = useState(false);
  const [showDeactivateConfirm, setShowDeactivateConfirm] = useState(false);
  const [isDeactivating, setIsDeactivating] = useState(false);
  const [message, setMessage] = useState<{ tone: "success" | "error"; text: string } | null>(null);

  useEffect(() => {
    const controller = new AbortController();
    async function load() {
      setIsLoading(true);
      try {
        const loaded = await getPatient(slug, id, controller.signal);
        setPatient(loaded);
        setForm(patientToInput(loaded));
      } catch (error) {
        if (controller.signal.aborted) return;
        if (error instanceof ApiError && error.status === 404) setIsNotFound(true);
        else setLoadError(error instanceof Error ? error.message : "We couldn’t load this patient record.");
      } finally {
        if (!controller.signal.aborted) setIsLoading(false);
      }
    }
    void load();
    return () => controller.abort();
  }, [id, slug]);

  function updateField(field: PatientField, value: string) {
    setForm((current) => current ? updatePatientField(current, field, value) : current);
    setErrors((current) => ({ ...current, [field]: undefined, contact: undefined }));
    setMessage(null);
  }

  function setEmergencyContact(enabled: boolean) {
    setForm((current) => current ? { ...current, emergencyContact: enabled ? { ...emptyEmergencyContact } : null } : current);
  }

  async function saveDetails(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!form) return;
    const validationErrors = validatePatient(form);
    if (Object.keys(validationErrors).length > 0) {
      setErrors(validationErrors);
      return;
    }
    setIsSaving(true);
    setMessage(null);
    try {
      const updated = await updatePatient(slug, id, normalizePatient(form));
      setPatient(updated);
      setForm(patientToInput(updated));
      setIsEditing(false);
      setMessage({ tone: "success", text: "Patient record saved." });
    } catch (error) {
      setMessage({ tone: "error", text: error instanceof Error ? error.message : "We couldn’t save this patient record." });
    } finally {
      setIsSaving(false);
    }
  }

  async function confirmDeactivate() {
    setIsDeactivating(true);
    setMessage(null);
    try {
      const updated = await deactivatePatient(slug, id);
      setPatient(updated);
      setShowDeactivateConfirm(false);
      setMessage({ tone: "success", text: "Patient deactivated. Their record has been retained." });
    } catch (error) {
      setMessage({ tone: "error", text: error instanceof Error ? error.message : "We couldn’t deactivate this patient." });
    } finally {
      setIsDeactivating(false);
    }
  }

  if (isLoading) {
    return <main className="min-h-screen bg-[#f3f8f7] px-5 py-6"><div className="mx-auto max-w-6xl"><AyoosMark /><div className="mt-14 animate-pulse"><div className="h-11 w-80 rounded-xl bg-slate-200" /><div className="mt-8 h-96 rounded-3xl bg-white" /></div><p className="sr-only" role="status">Loading patient…</p></div></main>;
  }

  if (isNotFound) {
    return <main className="grid min-h-screen place-items-center bg-[#f3f8f7] px-5"><section className="max-w-lg rounded-3xl bg-white p-9 text-center shadow-xl shadow-teal-900/5"><p className="text-sm font-semibold text-teal-700">Patient not found</p><h1 className="mt-2 text-3xl font-semibold text-slate-950">This record isn’t available</h1><Link href={`/practice/${encodeURIComponent(slug)}/patients`} className="mt-7 inline-flex rounded-xl bg-teal-700 px-5 py-3 text-sm font-semibold text-white">Back to patients</Link></section></main>;
  }

  if (loadError || !patient || !form) {
    return <main className="grid min-h-screen place-items-center bg-[#f3f8f7] px-5"><section className="max-w-lg rounded-3xl border border-rose-100 bg-white p-9 text-center"><p className="font-semibold text-rose-700">Unable to load patient</p><p className="mt-3 text-slate-600">{loadError}</p></section></main>;
  }

  const addressLines = [patient.address.line1, patient.address.line2, `${patient.address.city}, ${patient.address.state} ${patient.address.postalCode}`, patient.address.country].filter(Boolean);

  return (
    <main className="min-h-screen bg-[#f3f8f7] text-slate-900">
      <div className="mx-auto max-w-6xl px-5 py-6 sm:px-8 sm:py-8">
        <header className="flex items-center justify-between gap-4"><AyoosMark /><div className="flex items-center gap-2"><Link href={`/practice/${encodeURIComponent(slug)}/patients`} className="hidden rounded-xl px-3 py-2 text-sm font-semibold text-slate-600 hover:bg-white hover:text-teal-700 md:inline-flex">All patients</Link><UserMenu /></div></header>
        <section className="mt-10 sm:mt-14">
          <div className="flex flex-col gap-5 sm:flex-row sm:items-end sm:justify-between">
            <div><div className="flex flex-wrap items-center gap-3"><p className="text-sm font-semibold text-teal-700">Patient record</p><span className={`rounded-full px-2.5 py-1 text-xs font-semibold ${patient.isActive ? "bg-emerald-50 text-emerald-700" : "bg-slate-200 text-slate-600"}`}>{patient.isActive ? "Active" : "Inactive"}</span></div><h1 className="mt-2 text-3xl font-semibold tracking-[-0.045em] text-slate-950 sm:text-5xl">{formatPatientName(patient)}</h1><p className="mt-3 text-slate-500">Born {formatDate(patient.dateOfBirth)} · {sexLabels[patient.sex]}</p></div>
            {!isEditing && <button type="button" onClick={() => { setForm(patientToInput(patient)); setErrors({}); setMessage(null); setIsEditing(true); }} className="w-fit rounded-xl bg-teal-700 px-5 py-3 text-sm font-semibold text-white shadow-lg shadow-teal-700/15">Edit record</button>}
          </div>

          {message && <div role="status" className={`mt-7 rounded-2xl border px-4 py-3 text-sm font-medium ${message.tone === "success" ? "border-emerald-200 bg-emerald-50 text-emerald-800" : "border-rose-200 bg-rose-50 text-rose-800"}`}>{message.text}</div>}

          {isEditing ? (
            <form onSubmit={saveDetails} noValidate className="mt-8 overflow-hidden rounded-3xl border border-white bg-white shadow-[0_24px_70px_rgba(15,118,110,0.09)]"><div className="px-6 py-8 sm:px-9 sm:py-9"><PatientFormFields value={form} errors={errors} disabled={isSaving} onChange={updateField} onEmergencyContactChange={setEmergencyContact} /></div><div className="flex flex-col-reverse gap-3 border-t border-slate-100 bg-slate-50/70 px-6 py-5 sm:flex-row sm:justify-end sm:px-9"><button type="button" disabled={isSaving} onClick={() => { setForm(patientToInput(patient)); setErrors({}); setIsEditing(false); }} className="rounded-xl px-5 py-3 text-sm font-semibold text-slate-600 hover:bg-white">Cancel</button><button type="submit" disabled={isSaving} className="rounded-xl bg-teal-700 px-5 py-3 text-sm font-semibold text-white disabled:opacity-50">{isSaving ? "Saving…" : "Save changes"}</button></div></form>
          ) : (
            <div className="mt-8 grid gap-5 lg:grid-cols-3">
              <section className="overflow-hidden rounded-3xl border border-white bg-white shadow-[0_18px_50px_rgba(15,118,110,0.08)] lg:col-span-2"><div className="border-b border-slate-100 px-6 py-5 sm:px-8"><h2 className="text-xl font-semibold text-slate-950">Contact and demographics</h2></div><dl className="grid sm:grid-cols-2"><Detail label="Email" value={patient.email || "Not provided"} /><Detail label="Phone" value={patient.phone || "Not provided"} /><Detail label="Preferred language" value={patient.preferredLanguage || "Not provided"} /><Detail label="Login link" value={patient.keycloakUserId ? "Linked" : "Not linked"} /><div className="border-t border-slate-100 px-6 py-5 sm:col-span-2 sm:px-8"><dt className="text-xs font-semibold uppercase tracking-[0.12em] text-slate-400">Address</dt><dd className="mt-2 leading-7 text-slate-700">{addressLines.map((line) => <span key={line} className="block">{line}</span>)}</dd></div></dl></section>
              <section className="rounded-3xl border border-white bg-white p-6 shadow-[0_18px_50px_rgba(15,118,110,0.08)] sm:p-8"><h2 className="text-xl font-semibold text-slate-950">Emergency contact</h2>{patient.emergencyContact ? <div className="mt-5"><p className="font-semibold text-slate-900">{patient.emergencyContact.name}</p><p className="mt-1 text-sm text-slate-500">{patient.emergencyContact.relationship}</p><a href={`tel:${patient.emergencyContact.phone}`} className="mt-4 inline-block text-sm font-semibold text-teal-700">{patient.emergencyContact.phone}</a></div> : <p className="mt-4 text-sm leading-6 text-slate-500">No emergency contact is on file.</p>}</section>
            </div>
          )}

          {!isEditing && patient.isActive && (
            <section className="mt-5 rounded-3xl border border-rose-100 bg-white px-6 py-6 sm:px-9"><div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between"><div><h2 className="font-semibold text-slate-900">Deactivate patient</h2><p className="mt-1 text-sm leading-6 text-slate-500">Keeps the record while removing the patient from active workflows.</p></div><button type="button" onClick={() => setShowDeactivateConfirm(true)} className="w-fit rounded-xl border border-rose-200 px-4 py-2.5 text-sm font-semibold text-rose-700 hover:bg-rose-50">Deactivate</button></div>{showDeactivateConfirm && <div className="mt-5 rounded-2xl border border-rose-200 bg-rose-50 p-5"><p className="font-semibold text-rose-900">Deactivate {patient.firstName} {patient.lastName}?</p><p className="mt-1 text-sm text-rose-700">This action retains the full patient record.</p><div className="mt-4 flex flex-wrap gap-3"><button type="button" disabled={isDeactivating} onClick={() => setShowDeactivateConfirm(false)} className="rounded-xl bg-white px-4 py-2.5 text-sm font-semibold text-slate-600">Cancel</button><button type="button" disabled={isDeactivating} onClick={() => void confirmDeactivate()} className="rounded-xl bg-rose-700 px-4 py-2.5 text-sm font-semibold text-white disabled:opacity-50">{isDeactivating ? "Deactivating…" : "Yes, deactivate"}</button></div></div>}</section>
          )}
        </section>
      </div>
    </main>
  );
}

function Detail({ label, value }: { label: string; value: string }) {
  return <div className="border-t border-slate-100 px-6 py-5 first:border-t-0 sm:px-8"><dt className="text-xs font-semibold uppercase tracking-[0.12em] text-slate-400">{label}</dt><dd className="mt-2 break-words font-medium text-slate-700">{value}</dd></div>;
}
