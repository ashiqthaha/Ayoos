"use client";

import Link from "next/link";
import { useParams } from "next/navigation";
import { useEffect, useMemo, useState } from "react";

import { AyoosMark } from "@/components/ayoos-mark";
import { useAuth } from "@/components/auth-provider";
import { UserMenu } from "@/components/user-menu";
import {
  cancelBookingByPatient,
  cancelBookingByProvider,
  completeBooking,
  confirmBooking,
  getMyPatientRecord,
  listBookings,
  listPatients,
  listProviders,
  markBookingNoShow,
  type Booking,
  type BookingStatus,
  type PagedList,
  type Patient,
  type Provider,
} from "@/lib/api";

const statusLabels: Record<BookingStatus, string> = {
  0: "Pending",
  1: "Confirmed",
  2: "Cancelled by patient",
  3: "Cancelled by provider",
  4: "Completed",
  5: "No-show",
};

const statusClasses: Record<BookingStatus, string> = {
  0: "bg-amber-50 text-amber-700",
  1: "bg-sky-50 text-sky-700",
  2: "bg-slate-100 text-slate-600",
  3: "bg-slate-100 text-slate-600",
  4: "bg-emerald-50 text-emerald-700",
  5: "bg-rose-50 text-rose-700",
};

const pageSize = 15;

function formatAppointment(iso: string) {
  return new Intl.DateTimeFormat(undefined, {
    weekday: "short",
    month: "short",
    day: "numeric",
    hour: "numeric",
    minute: "2-digit",
    timeZone: "UTC",
  }).format(new Date(iso));
}

export default function BookingsPage() {
  const { slug } = useParams<{ slug: string }>();
  const { identity } = useAuth();
  const [result, setResult] = useState<PagedList<Booking> | null>(null);
  const [providers, setProviders] = useState<Provider[]>([]);
  const [patients, setPatients] = useState<Patient[]>([]);
  const [status, setStatus] = useState<"all" | BookingStatus>("all");
  const [providerFilter, setProviderFilter] = useState("");
  const [patientFilter, setPatientFilter] = useState("");
  const [fromDate, setFromDate] = useState("");
  const [toDate, setToDate] = useState("");
  const [page, setPage] = useState(1);
  const [isLoading, setIsLoading] = useState(true);
  const [pendingId, setPendingId] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [notice, setNotice] = useState<string | null>(null);

  const role = identity?.role;
  const canManageClinicalStatus = role === "provider" || role === "practice-admin";
  const canCreate = role === "patient" || role === "practice-admin";
  const canCancel = role === "patient" || canManageClinicalStatus;

  useEffect(() => {
    if (!identity) return;
    const controller = new AbortController();

    async function load() {
      setIsLoading(true);
      setError(null);
      try {
        const bookingPromise = listBookings(slug, {
          status: status === "all" ? undefined : status,
          providerId: providerFilter || undefined,
          patientId: role === "patient" ? undefined : patientFilter || undefined,
          fromDate: fromDate || undefined,
          toDate: toDate || undefined,
          page,
          pageSize,
          signal: controller.signal,
        });
        const providerPromise = listProviders(slug, controller.signal);
        const patientPromise = role === "patient"
          ? getMyPatientRecord(slug, controller.signal).then((patient) => [patient])
          : role === "practice-admin"
            ? listPatients(slug, {
                page: 1,
                pageSize: 100,
                signal: controller.signal,
              }).then((patientsPage) => patientsPage.items)
            : Promise.resolve([]);
        const [bookings, loadedProviders, loadedPatients] = await Promise.all([
          bookingPromise,
          providerPromise,
          patientPromise,
        ]);
        setResult(bookings);
        setProviders(loadedProviders);
        setPatients(loadedPatients);
      } catch (loadError) {
        if (!controller.signal.aborted) {
          setError(loadError instanceof Error
            ? loadError.message
            : "We couldn't load the booking list.");
        }
      } finally {
        if (!controller.signal.aborted) setIsLoading(false);
      }
    }

    void load();
    return () => controller.abort();
  }, [fromDate, identity, page, patientFilter, providerFilter, role, slug, status, toDate]);

  const providerNames = useMemo(
    () => new Map(providers.map((provider) => [
      provider.id,
      `${provider.firstName} ${provider.lastName}, ${provider.credentials}`,
    ])),
    [providers],
  );
  const patientNames = useMemo(
    () => new Map(patients.map((patient) => [
      patient.id,
      `${patient.preferredName || patient.firstName} ${patient.lastName}`,
    ])),
    [patients],
  );

  async function runAction(
    booking: Booking,
    action: "confirm" | "cancel" | "complete" | "no-show",
  ) {
    setPendingId(booking.id);
    setError(null);
    setNotice(null);
    try {
      const updated = action === "confirm"
        ? await confirmBooking(slug, booking.id)
        : action === "cancel"
          ? role === "patient"
            ? await cancelBookingByPatient(slug, booking.id)
            : await cancelBookingByProvider(slug, booking.id)
          : action === "complete"
            ? await completeBooking(slug, booking.id)
            : await markBookingNoShow(slug, booking.id);
      setResult((current) => current && ({
        ...current,
        items: current.items.map((item) => item.id === updated.id ? updated : item),
      }));
      setNotice(`Booking marked ${statusLabels[updated.status].toLowerCase()}.`);
    } catch (actionError) {
      setError(actionError instanceof Error
        ? actionError.message
        : "We couldn't update this booking.");
    } finally {
      setPendingId(null);
    }
  }

  return (
    <main className="min-h-screen bg-[#f3f8f7] text-slate-900">
      <div className="mx-auto max-w-6xl px-5 py-6 sm:px-8 sm:py-8">
        <header className="flex items-center justify-between gap-4">
          <AyoosMark />
          <div className="flex items-center gap-2">
            <Link href={`/practice/${encodeURIComponent(slug)}`} className="hidden rounded-xl px-3 py-2 text-sm font-semibold text-slate-600 hover:bg-white hover:text-teal-700 md:inline-flex">
              Practice
            </Link>
            <UserMenu />
          </div>
        </header>

        <section className="mt-10 sm:mt-14">
          <div className="flex flex-col gap-5 sm:flex-row sm:items-end sm:justify-between">
            <div>
              <p className="text-sm font-semibold text-teal-700">Scheduling</p>
              <h1 className="mt-2 text-3xl font-semibold tracking-[-0.045em] text-slate-950 sm:text-5xl">Bookings</h1>
              <p className="mt-3 text-slate-500">Review upcoming care and move each visit through its workflow.</p>
            </div>
            <div className="flex flex-wrap gap-3">
              {canManageClinicalStatus && (
                <Link href={`/practice/${encodeURIComponent(slug)}/bookings/schedule`} className="inline-flex items-center rounded-xl border border-teal-200 bg-white px-5 py-3 text-sm font-semibold text-teal-800 hover:bg-teal-50">
                  Weekly schedule
                </Link>
              )}
              {canCreate && (
                <Link href={`/practice/${encodeURIComponent(slug)}/bookings/new`} className="inline-flex items-center rounded-xl bg-teal-700 px-5 py-3 text-sm font-semibold text-white shadow-lg shadow-teal-700/15 hover:bg-teal-800">
                  Request booking
                </Link>
              )}
            </div>
          </div>

          {(error || notice) && (
            <div role="status" className={`mt-7 rounded-2xl border px-4 py-3 text-sm font-medium ${error ? "border-rose-200 bg-rose-50 text-rose-800" : "border-emerald-200 bg-emerald-50 text-emerald-800"}`}>
              {error || notice}
            </div>
          )}

          <section className="mt-8 overflow-hidden rounded-3xl border border-white bg-white shadow-[0_18px_50px_rgba(15,118,110,0.08)]">
            <div className="border-b border-slate-100 px-5 py-5 sm:px-7">
              <h2 className="text-lg font-semibold text-slate-950">Appointments</h2>
              <div className="mt-4 grid gap-3 sm:grid-cols-2 lg:grid-cols-5">
                <label className="text-xs font-semibold uppercase tracking-wide text-slate-500">Status
                <select
                  value={status}
                  onChange={(event) => {
                    setStatus(event.target.value === "all" ? "all" : Number(event.target.value) as BookingStatus);
                    setPage(1);
                  }}
                  className="mt-1 block w-full rounded-xl border border-slate-200 bg-white px-3 py-2.5 text-sm font-medium normal-case tracking-normal text-slate-700 outline-none focus:border-teal-500"
                >
                  <option value="all">All statuses</option>
                  {Object.entries(statusLabels).map(([value, label]) => (
                    <option key={value} value={value}>{label}</option>
                  ))}
                </select>
                </label>
                {role !== "patient" && (
                  <label className="text-xs font-semibold uppercase tracking-wide text-slate-500">Provider
                    <select value={providerFilter} onChange={(event) => { setProviderFilter(event.target.value); setPage(1); }} className="mt-1 block w-full rounded-xl border border-slate-200 bg-white px-3 py-2.5 text-sm font-medium normal-case tracking-normal text-slate-700 outline-none focus:border-teal-500">
                      <option value="">All providers</option>
                      {providers.map((provider) => <option key={provider.id} value={provider.id}>{provider.firstName} {provider.lastName}</option>)}
                    </select>
                  </label>
                )}
                {role === "practice-admin" && (
                  <label className="text-xs font-semibold uppercase tracking-wide text-slate-500">Patient
                    <select value={patientFilter} onChange={(event) => { setPatientFilter(event.target.value); setPage(1); }} className="mt-1 block w-full rounded-xl border border-slate-200 bg-white px-3 py-2.5 text-sm font-medium normal-case tracking-normal text-slate-700 outline-none focus:border-teal-500">
                      <option value="">All patients</option>
                      {patients.map((patient) => <option key={patient.id} value={patient.id}>{patient.preferredName || patient.firstName} {patient.lastName}</option>)}
                    </select>
                  </label>
                )}
                <label className="text-xs font-semibold uppercase tracking-wide text-slate-500">From
                  <input type="date" value={fromDate} onChange={(event) => { setFromDate(event.target.value); setPage(1); }} className="mt-1 block w-full rounded-xl border border-slate-200 bg-white px-3 py-2 text-sm font-medium normal-case tracking-normal text-slate-700 outline-none focus:border-teal-500" />
                </label>
                <label className="text-xs font-semibold uppercase tracking-wide text-slate-500">To
                  <input type="date" value={toDate} onChange={(event) => { setToDate(event.target.value); setPage(1); }} className="mt-1 block w-full rounded-xl border border-slate-200 bg-white px-3 py-2 text-sm font-medium normal-case tracking-normal text-slate-700 outline-none focus:border-teal-500" />
                </label>
              </div>
            </div>

            {isLoading ? (
              <div className="grid gap-3 px-6 py-8" role="status">
                <div className="h-24 animate-pulse rounded-2xl bg-slate-100" />
                <div className="h-24 animate-pulse rounded-2xl bg-slate-100" />
                <span className="sr-only">Loading bookings...</span>
              </div>
            ) : !result || result.items.length === 0 ? (
              <div className="px-6 py-16 text-center">
                <p className="text-lg font-semibold text-slate-900">No bookings found</p>
                <p className="mt-2 text-sm text-slate-500">Try another status or request a new appointment.</p>
              </div>
            ) : (
              <ul className="divide-y divide-slate-100">
                {result.items.map((booking) => (
                  <li key={booking.id} className="grid gap-4 px-5 py-5 sm:grid-cols-[minmax(0,1fr)_auto] sm:items-center sm:px-7">
                    <div>
                      <div className="flex flex-wrap items-center gap-2">
                        <p className="font-semibold text-slate-950">{formatAppointment(booking.scheduledStart)}</p>
                        <span className={`rounded-full px-2.5 py-1 text-xs font-semibold ${statusClasses[booking.status]}`}>{statusLabels[booking.status]}</span>
                      </div>
                      <p className="mt-2 text-sm text-slate-600">{providerNames.get(booking.providerId) || `Provider ${booking.providerId.slice(0, 8)}`}</p>
                      <p className="mt-1 text-sm text-slate-400">{patientNames.get(booking.patientId) || `Patient ${booking.patientId.slice(0, 8)}`}{booking.reason ? ` · ${booking.reason}` : ""}</p>
                    </div>
                    <div className="flex flex-wrap gap-2 sm:justify-end">
                      {canManageClinicalStatus && booking.status === 0 && (
                        <button type="button" disabled={pendingId === booking.id} onClick={() => void runAction(booking, "confirm")} className="rounded-xl bg-teal-700 px-3.5 py-2 text-xs font-semibold text-white disabled:opacity-50">Confirm</button>
                      )}
                      {canManageClinicalStatus && booking.status === 1 && (
                        <>
                          <button type="button" disabled={pendingId === booking.id} onClick={() => void runAction(booking, "complete")} className="rounded-xl bg-emerald-700 px-3.5 py-2 text-xs font-semibold text-white disabled:opacity-50">Complete</button>
                          <button type="button" disabled={pendingId === booking.id} onClick={() => void runAction(booking, "no-show")} className="rounded-xl border border-rose-200 px-3.5 py-2 text-xs font-semibold text-rose-700 disabled:opacity-50">No-show</button>
                        </>
                      )}
                      {canCancel && (booking.status === 0 || booking.status === 1) && (
                        <button type="button" disabled={pendingId === booking.id} onClick={() => void runAction(booking, "cancel")} className="rounded-xl border border-slate-200 px-3.5 py-2 text-xs font-semibold text-slate-600 disabled:opacity-50">Cancel</button>
                      )}
                    </div>
                  </li>
                ))}
              </ul>
            )}

            {result && result.totalCount > 0 && (
              <div className="flex items-center justify-between border-t border-slate-100 bg-slate-50/60 px-6 py-4 text-sm">
                <p className="text-slate-500">{result.totalCount} booking{result.totalCount === 1 ? "" : "s"} · Page {result.page} of {Math.max(result.totalPages, 1)}</p>
                <div className="flex gap-2">
                  <button type="button" disabled={page <= 1 || isLoading} onClick={() => setPage((value) => Math.max(1, value - 1))} className="rounded-xl border border-slate-200 bg-white px-4 py-2 font-semibold disabled:opacity-40">Previous</button>
                  <button type="button" disabled={page >= result.totalPages || isLoading} onClick={() => setPage((value) => value + 1)} className="rounded-xl border border-slate-200 bg-white px-4 py-2 font-semibold disabled:opacity-40">Next</button>
                </div>
              </div>
            )}
          </section>
        </section>
      </div>
    </main>
  );
}
