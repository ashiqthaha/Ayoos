"use client";

import Link from "next/link";
import { useParams } from "next/navigation";
import { useEffect, useMemo, useState } from "react";

import { AyoosMark } from "@/components/ayoos-mark";
import { useAuth } from "@/components/auth-provider";
import { UserMenu } from "@/components/user-menu";
import {
  getProviderBookingSchedule,
  listPatients,
  listProviders,
  type Booking,
  type BookingStatus,
  type Patient,
  type Provider,
} from "@/lib/api";

const statusLabels: Record<BookingStatus, string> = {
  0: "Requested",
  1: "Confirmed",
  2: "Cancelled",
  3: "Completed",
  4: "No-show",
};

function dateKey(date: Date) {
  const year = date.getFullYear();
  const month = String(date.getMonth() + 1).padStart(2, "0");
  const day = String(date.getDate()).padStart(2, "0");
  return `${year}-${month}-${day}`;
}

function addDays(date: Date, days: number) {
  const next = new Date(date);
  next.setDate(next.getDate() + days);
  return next;
}

function startOfWeek(date = new Date()) {
  const start = new Date(date.getFullYear(), date.getMonth(), date.getDate());
  const offset = start.getDay() === 0 ? -6 : 1 - start.getDay();
  start.setDate(start.getDate() + offset);
  return start;
}

function displayTime(iso: string) {
  return new Intl.DateTimeFormat(undefined, {
    hour: "numeric",
    minute: "2-digit",
    timeZone: "UTC",
  }).format(new Date(iso));
}

export default function ProviderBookingSchedulePage() {
  const { slug } = useParams<{ slug: string }>();
  const { identity } = useAuth();
  const [providers, setProviders] = useState<Provider[]>([]);
  const [patients, setPatients] = useState<Patient[]>([]);
  const [providerId, setProviderId] = useState("");
  const [weekStart, setWeekStart] = useState(() => startOfWeek());
  const [bookings, setBookings] = useState<Booking[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const role = identity?.role;
  const canView = role === "provider" || role === "practice-admin";
  const week = useMemo(
    () => Array.from({ length: 7 }, (_, index) => addDays(weekStart, index)),
    [weekStart],
  );

  useEffect(() => {
    if (!identity || !canView) return;
    const controller = new AbortController();
    async function loadChoices() {
      try {
        const [loadedProviders, loadedPatients] = await Promise.all([
          listProviders(slug, controller.signal),
          role === "practice-admin"
            ? listPatients(slug, {
                page: 1,
                pageSize: 100,
                signal: controller.signal,
              }).then((page) => page.items)
            : Promise.resolve([]),
        ]);
        setProviders(loadedProviders);
        setPatients(loadedPatients);
        setProviderId((current) => current || loadedProviders.find((provider) => provider.isActive)?.id || loadedProviders[0]?.id || "");
      } catch (loadError) {
        if (!controller.signal.aborted) {
          setError(loadError instanceof Error ? loadError.message : "We couldn't load providers.");
        }
      }
    }
    void loadChoices();
    return () => controller.abort();
  }, [canView, identity, role, slug]);

  useEffect(() => {
    if (!providerId || !canView) {
      setIsLoading(false);
      return;
    }
    const controller = new AbortController();
    async function loadSchedule() {
      setIsLoading(true);
      setError(null);
      try {
        setBookings(await getProviderBookingSchedule(
          slug,
          providerId,
          dateKey(weekStart),
          dateKey(addDays(weekStart, 6)),
          controller.signal,
        ));
      } catch (loadError) {
        if (!controller.signal.aborted) {
          setError(loadError instanceof Error ? loadError.message : "We couldn't load this schedule.");
        }
      } finally {
        if (!controller.signal.aborted) setIsLoading(false);
      }
    }
    void loadSchedule();
    return () => controller.abort();
  }, [canView, providerId, slug, weekStart]);

  const patientNames = useMemo(
    () => new Map(patients.map((patient) => [patient.id, `${patient.preferredName || patient.firstName} ${patient.lastName}`])),
    [patients],
  );
  const selectedProvider = providers.find((provider) => provider.id === providerId);

  if (identity && !canView) {
    return (
      <main className="grid min-h-screen place-items-center bg-[#f3f8f7] px-5 text-slate-900">
        <section className="max-w-lg rounded-3xl bg-white p-9 text-center shadow-xl shadow-teal-900/5">
          <h1 className="text-2xl font-semibold">Provider schedules are restricted</h1>
          <p className="mt-3 text-slate-600">This view is available to providers and practice administrators.</p>
          <Link href={`/practice/${encodeURIComponent(slug)}/bookings`} className="mt-6 inline-flex rounded-xl bg-teal-700 px-5 py-3 text-sm font-semibold text-white">Back to bookings</Link>
        </section>
      </main>
    );
  }

  return (
    <main className="min-h-screen bg-[#f3f8f7] text-slate-900">
      <div className="mx-auto max-w-7xl px-5 py-6 sm:px-8 sm:py-8">
        <header className="flex items-center justify-between gap-4">
          <AyoosMark />
          <div className="flex items-center gap-2">
            <Link href={`/practice/${encodeURIComponent(slug)}/bookings`} className="hidden rounded-xl px-3 py-2 text-sm font-semibold text-slate-600 hover:bg-white hover:text-teal-700 md:inline-flex">Bookings</Link>
            <UserMenu />
          </div>
        </header>

        <section className="mt-10 sm:mt-14">
          <div className="flex flex-col gap-5 lg:flex-row lg:items-end lg:justify-between">
            <div>
              <p className="text-sm font-semibold text-teal-700">Provider schedule</p>
              <h1 className="mt-2 text-3xl font-semibold tracking-[-0.045em] text-slate-950 sm:text-5xl">Week at a glance</h1>
              <p className="mt-3 text-slate-500">{selectedProvider ? `${selectedProvider.firstName} ${selectedProvider.lastName}, ${selectedProvider.credentials}` : "Select a provider"}</p>
            </div>
            <label className="min-w-72 text-sm font-semibold text-slate-700">Provider
              <select value={providerId} onChange={(event) => setProviderId(event.target.value)} className="mt-2 block w-full rounded-xl border border-slate-200 bg-white px-4 py-3 outline-none focus:border-teal-500">
                {providers.map((provider) => <option key={provider.id} value={provider.id}>{provider.firstName} {provider.lastName}, {provider.credentials}</option>)}
              </select>
            </label>
          </div>

          {error && <div role="alert" className="mt-7 rounded-2xl border border-rose-200 bg-rose-50 px-4 py-3 text-sm font-medium text-rose-800">{error}</div>}

          <section className="mt-8 overflow-hidden rounded-3xl border border-white bg-white shadow-[0_18px_50px_rgba(15,118,110,0.08)]">
            <div className="flex flex-col gap-3 border-b border-slate-100 px-5 py-5 sm:flex-row sm:items-center sm:justify-between sm:px-7">
              <p className="font-semibold text-slate-950">{weekStart.toLocaleDateString(undefined, { month: "long", day: "numeric" })} – {week[6].toLocaleDateString(undefined, { month: "long", day: "numeric", year: "numeric" })}</p>
              <div className="flex gap-2">
                <button type="button" onClick={() => setWeekStart((current) => addDays(current, -7))} className="rounded-xl border border-slate-200 px-4 py-2 text-sm font-semibold text-slate-600">Previous</button>
                <button type="button" onClick={() => setWeekStart(startOfWeek())} className="rounded-xl border border-teal-200 px-4 py-2 text-sm font-semibold text-teal-700">Today</button>
                <button type="button" onClick={() => setWeekStart((current) => addDays(current, 7))} className="rounded-xl border border-slate-200 px-4 py-2 text-sm font-semibold text-slate-600">Next</button>
              </div>
            </div>

            {isLoading ? (
              <div className="h-96 animate-pulse bg-slate-50" role="status"><span className="sr-only">Loading provider schedule...</span></div>
            ) : (
              <div className="overflow-x-auto">
                <div className="grid min-w-[70rem] grid-cols-7 divide-x divide-slate-100">
                  {week.map((day) => {
                    const key = dateKey(day);
                    const daily = bookings.filter((booking) => booking.startTime.slice(0, 10) === key);
                    return (
                      <section key={key} className="min-h-[28rem] bg-white">
                        <header className={`border-b border-slate-100 px-3 py-4 text-center ${key === dateKey(new Date()) ? "bg-teal-50" : "bg-slate-50/60"}`}>
                          <p className="text-xs font-semibold uppercase tracking-[0.12em] text-slate-400">{day.toLocaleDateString(undefined, { weekday: "short" })}</p>
                          <p className="mt-1 text-lg font-semibold text-slate-950">{day.getDate()}</p>
                        </header>
                        <div className="grid gap-2 p-2.5">
                          {daily.length === 0 ? <p className="py-8 text-center text-xs text-slate-400">No bookings</p> : daily.map((booking) => (
                            <article key={booking.id} className={`rounded-xl border p-3 text-xs ${booking.status === 2 ? "border-slate-200 bg-slate-50 opacity-60" : "border-teal-100 bg-teal-50/60"}`}>
                              <p className="font-semibold text-slate-950">{displayTime(booking.startTime)}–{displayTime(booking.endTime)}</p>
                              <p className="mt-1 truncate text-slate-600">{patientNames.get(booking.patientId) || `Patient ${booking.patientId.slice(0, 8)}`}</p>
                              <p className="mt-2 font-semibold text-teal-700">{statusLabels[booking.status]}</p>
                            </article>
                          ))}
                        </div>
                      </section>
                    );
                  })}
                </div>
              </div>
            )}
          </section>
        </section>
      </div>
    </main>
  );
}
