"use client";

import Link from "next/link";
import { useParams, useRouter } from "next/navigation";
import { FormEvent, useEffect, useMemo, useState } from "react";

import { AyoosMark } from "@/components/ayoos-mark";
import { useAuth } from "@/components/auth-provider";
import { UserMenu } from "@/components/user-menu";
import {
  createBooking,
  getMyPatientRecord,
  getProviderSlots,
  listPatients,
  listProviders,
  type AvailabilitySlot,
  type Patient,
  type Provider,
} from "@/lib/api";

function dateKey(date: Date) {
  const year = date.getFullYear();
  const month = String(date.getMonth() + 1).padStart(2, "0");
  const day = String(date.getDate()).padStart(2, "0");
  return `${year}-${month}-${day}`;
}

function defaultDate() {
  const value = new Date();
  value.setDate(value.getDate() + 1);
  return dateKey(value);
}

function displayTime(time: string) {
  const [hours, minutes] = time.split(":").map(Number);
  return new Intl.DateTimeFormat(undefined, {
    hour: "numeric",
    minute: "2-digit",
    timeZone: "UTC",
  }).format(new Date(Date.UTC(2026, 0, 1, hours, minutes)));
}

function slotInstant(date: string, time: string) {
  return new Date(`${date}T${time.slice(0, 8)}Z`).toISOString();
}

export default function NewBookingPage() {
  const { slug } = useParams<{ slug: string }>();
  const router = useRouter();
  const { identity } = useAuth();
  const [providers, setProviders] = useState<Provider[]>([]);
  const [patients, setPatients] = useState<Patient[]>([]);
  const [providerId, setProviderId] = useState("");
  const [patientId, setPatientId] = useState("");
  const [date, setDate] = useState(defaultDate);
  const [slots, setSlots] = useState<AvailabilitySlot[]>([]);
  const [selectedSlot, setSelectedSlot] = useState<AvailabilitySlot | null>(null);
  const [reason, setReason] = useState("");
  const [isLoading, setIsLoading] = useState(true);
  const [isLoadingSlots, setIsLoadingSlots] = useState(false);
  const [isSaving, setIsSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const role = identity?.role;
  const isPatient = role === "patient";
  const canCreate = isPatient || role === "staff" || role === "practice-admin";

  useEffect(() => {
    if (!identity || !canCreate) return;
    const controller = new AbortController();

    async function loadChoices() {
      setIsLoading(true);
      setError(null);
      try {
        const [loadedProviders, loadedPatients] = await Promise.all([
          listProviders(slug, controller.signal),
          isPatient
            ? getMyPatientRecord(slug, controller.signal).then((patient) => [patient])
            : listPatients(slug, {
                page: 1,
                pageSize: 100,
                signal: controller.signal,
              }).then((page) => page.items),
        ]);
        const activeProviders = loadedProviders.filter((provider) => provider.isActive);
        const activePatients = loadedPatients.filter((patient) => patient.isActive);
        setProviders(activeProviders);
        setPatients(activePatients);
        setProviderId((current) => current || activeProviders[0]?.id || "");
        setPatientId((current) => current || activePatients[0]?.id || "");
      } catch (loadError) {
        if (!controller.signal.aborted) {
          setError(loadError instanceof Error
            ? loadError.message
            : "We couldn't load the booking choices.");
        }
      } finally {
        if (!controller.signal.aborted) setIsLoading(false);
      }
    }

    void loadChoices();
    return () => controller.abort();
  }, [canCreate, identity, isPatient, slug]);

  useEffect(() => {
    setSelectedSlot(null);
    if (!providerId || !date) {
      setSlots([]);
      return;
    }

    const controller = new AbortController();
    async function loadSlots() {
      setIsLoadingSlots(true);
      setError(null);
      try {
        setSlots(await getProviderSlots(
          slug,
          providerId,
          date,
          date,
          controller.signal,
        ));
      } catch (loadError) {
        if (!controller.signal.aborted) {
          setSlots([]);
          setError(loadError instanceof Error
            ? loadError.message
            : "We couldn't load available slots.");
        }
      } finally {
        if (!controller.signal.aborted) setIsLoadingSlots(false);
      }
    }

    void loadSlots();
    return () => controller.abort();
  }, [date, providerId, slug]);

  const selectedProvider = useMemo(
    () => providers.find((provider) => provider.id === providerId) ?? null,
    [providerId, providers],
  );
  const selectedPatient = useMemo(
    () => patients.find((patient) => patient.id === patientId) ?? null,
    [patientId, patients],
  );

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!selectedSlot || !providerId || !patientId) {
      setError("Choose a patient, provider, date, and available slot first.");
      return;
    }

    setIsSaving(true);
    setError(null);
    try {
      await createBooking(slug, {
        patientId,
        providerId,
        availabilityScheduleId: selectedSlot.availabilityScheduleId,
        startTime: slotInstant(selectedSlot.date, selectedSlot.startTime),
        endTime: slotInstant(selectedSlot.date, selectedSlot.endTime),
        reason: reason.trim() || null,
      });
      router.push(`/practice/${encodeURIComponent(slug)}/bookings`);
    } catch (saveError) {
      setError(saveError instanceof Error
        ? saveError.message
        : "We couldn't request this booking.");
    } finally {
      setIsSaving(false);
    }
  }

  if (identity && !canCreate) {
    return (
      <main className="grid min-h-screen place-items-center bg-[#f3f8f7] px-5 text-slate-900">
        <section className="max-w-lg rounded-3xl bg-white p-9 text-center shadow-xl shadow-teal-900/5">
          <h1 className="text-2xl font-semibold">Booking requests are patient and staff actions</h1>
          <p className="mt-3 text-slate-600">Use the schedule view to manage existing provider bookings.</p>
          <Link href={`/practice/${encodeURIComponent(slug)}/bookings`} className="mt-6 inline-flex rounded-xl bg-teal-700 px-5 py-3 text-sm font-semibold text-white">Back to bookings</Link>
        </section>
      </main>
    );
  }

  return (
    <main className="min-h-screen bg-[#f3f8f7] text-slate-900">
      <div className="mx-auto max-w-5xl px-5 py-6 sm:px-8 sm:py-8">
        <header className="flex items-center justify-between gap-4">
          <AyoosMark />
          <div className="flex items-center gap-2">
            <Link href={`/practice/${encodeURIComponent(slug)}/bookings`} className="hidden rounded-xl px-3 py-2 text-sm font-semibold text-slate-600 hover:bg-white hover:text-teal-700 md:inline-flex">Bookings</Link>
            <UserMenu />
          </div>
        </header>

        <section className="mt-10 sm:mt-14">
          <p className="text-sm font-semibold text-teal-700">New appointment</p>
          <h1 className="mt-2 text-3xl font-semibold tracking-[-0.045em] text-slate-950 sm:text-5xl">Request a booking</h1>
          <p className="mt-3 max-w-2xl leading-7 text-slate-600">Choose a provider, select a date, and reserve one of their live available slots.</p>

          {error && <div role="alert" className="mt-7 rounded-2xl border border-rose-200 bg-rose-50 px-4 py-3 text-sm font-medium text-rose-800">{error}</div>}

          <form onSubmit={submit} className="mt-8 overflow-hidden rounded-3xl border border-white bg-white shadow-[0_20px_60px_rgba(15,118,110,0.09)]">
            <div className="grid gap-8 px-6 py-8 sm:px-9 sm:py-9">
              {!isPatient && (
                <section>
                  <div className="flex items-center gap-3"><span className="grid h-8 w-8 place-items-center rounded-full bg-teal-700 text-sm font-semibold text-white">1</span><h2 className="text-lg font-semibold">Patient</h2></div>
                  <select value={patientId} onChange={(event) => setPatientId(event.target.value)} disabled={isLoading} className="mt-4 w-full rounded-xl border border-slate-200 bg-white px-4 py-3 outline-none focus:border-teal-500 focus:ring-4 focus:ring-teal-500/10">
                    {patients.length === 0 && <option value="">No active patients</option>}
                    {patients.map((patient) => <option key={patient.id} value={patient.id}>{patient.preferredName || patient.firstName} {patient.lastName}</option>)}
                  </select>
                </section>
              )}

              <section className={!isPatient ? "border-t border-slate-100 pt-8" : ""}>
                <div className="flex items-center gap-3"><span className="grid h-8 w-8 place-items-center rounded-full bg-teal-700 text-sm font-semibold text-white">{isPatient ? 1 : 2}</span><h2 className="text-lg font-semibold">Provider</h2></div>
                <select value={providerId} onChange={(event) => setProviderId(event.target.value)} disabled={isLoading} className="mt-4 w-full rounded-xl border border-slate-200 bg-white px-4 py-3 outline-none focus:border-teal-500 focus:ring-4 focus:ring-teal-500/10">
                  {providers.length === 0 && <option value="">No active providers</option>}
                  {providers.map((provider) => <option key={provider.id} value={provider.id}>{provider.firstName} {provider.lastName}, {provider.credentials} · {provider.specialty}</option>)}
                </select>
              </section>

              <section className="border-t border-slate-100 pt-8">
                <div className="flex items-center gap-3"><span className="grid h-8 w-8 place-items-center rounded-full bg-teal-700 text-sm font-semibold text-white">{isPatient ? 2 : 3}</span><h2 className="text-lg font-semibold">Date</h2></div>
                <input type="date" min={dateKey(new Date())} value={date} onChange={(event) => setDate(event.target.value)} className="mt-4 w-full rounded-xl border border-slate-200 px-4 py-3 outline-none focus:border-teal-500 focus:ring-4 focus:ring-teal-500/10 sm:max-w-xs" />
              </section>

              <section className="border-t border-slate-100 pt-8">
                <div className="flex items-center gap-3"><span className="grid h-8 w-8 place-items-center rounded-full bg-teal-700 text-sm font-semibold text-white">{isPatient ? 3 : 4}</span><h2 className="text-lg font-semibold">Available time</h2></div>
                {isLoadingSlots ? (
                  <div className="mt-4 h-20 animate-pulse rounded-2xl bg-slate-100" role="status"><span className="sr-only">Loading slots...</span></div>
                ) : slots.length === 0 ? (
                  <p className="mt-4 rounded-2xl bg-slate-50 px-4 py-5 text-sm text-slate-500">No open slots on this date. Choose another day.</p>
                ) : (
                  <div className="mt-4 flex flex-wrap gap-2">
                    {slots.map((slot) => {
                      const selected = selectedSlot?.startTime === slot.startTime && selectedSlot?.date === slot.date;
                      return <button key={`${slot.date}-${slot.startTime}`} type="button" onClick={() => setSelectedSlot(slot)} className={`rounded-xl border px-4 py-2.5 text-sm font-semibold transition ${selected ? "border-teal-700 bg-teal-700 text-white" : "border-teal-200 bg-white text-teal-800 hover:bg-teal-50"}`}>{displayTime(slot.startTime)}</button>;
                    })}
                  </div>
                )}
              </section>

              <section className="border-t border-slate-100 pt-8">
                <label className="text-sm font-semibold text-slate-700">Reason for visit <span className="font-normal text-slate-400">(optional)</span>
                  <textarea value={reason} onChange={(event) => setReason(event.target.value)} maxLength={1000} rows={3} className="mt-2 block w-full resize-y rounded-xl border border-slate-200 px-4 py-3 font-normal outline-none focus:border-teal-500 focus:ring-4 focus:ring-teal-500/10" placeholder="Share a short reason for the appointment" />
                </label>
              </section>

              {selectedSlot && selectedProvider && selectedPatient && (
                <section className="rounded-2xl border border-teal-100 bg-teal-50/60 p-5">
                  <p className="text-xs font-semibold uppercase tracking-[0.14em] text-teal-700">Ready to request</p>
                  <p className="mt-2 font-semibold text-slate-950">{selectedPatient.preferredName || selectedPatient.firstName} with {selectedProvider.firstName} {selectedProvider.lastName}</p>
                  <p className="mt-1 text-sm text-slate-600">{date} · {displayTime(selectedSlot.startTime)}–{displayTime(selectedSlot.endTime)}</p>
                </section>
              )}
            </div>

            <div className="flex flex-col-reverse gap-3 border-t border-slate-100 bg-slate-50/70 px-6 py-5 sm:flex-row sm:justify-end sm:px-9">
              <Link href={`/practice/${encodeURIComponent(slug)}/bookings`} className="rounded-xl px-5 py-3 text-center text-sm font-semibold text-slate-600 hover:bg-white">Cancel</Link>
              <button type="submit" disabled={!selectedSlot || !patientId || !providerId || isSaving} className="rounded-xl bg-teal-700 px-5 py-3 text-sm font-semibold text-white shadow-lg shadow-teal-700/15 disabled:cursor-not-allowed disabled:opacity-50">{isSaving ? "Requesting..." : "Confirm request"}</button>
            </div>
          </form>
        </section>
      </div>
    </main>
  );
}
