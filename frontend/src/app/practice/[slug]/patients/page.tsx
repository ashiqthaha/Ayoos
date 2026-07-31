"use client";

import Link from "next/link";
import { useParams, useRouter } from "next/navigation";
import { FormEvent, KeyboardEvent, useEffect, useState } from "react";

import { AyoosMark } from "@/components/ayoos-mark";
import { UserMenu } from "@/components/user-menu";
import { listPatients, type PagedList, type Patient } from "@/lib/api";
import { formatDate, formatPatientName } from "@/lib/patients";

const pageSize = 15;

export default function PatientsPage() {
  const { slug } = useParams<{ slug: string }>();
  const router = useRouter();
  const [result, setResult] = useState<PagedList<Patient> | null>(null);
  const [searchInput, setSearchInput] = useState("");
  const [search, setSearch] = useState("");
  const [page, setPage] = useState(1);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const controller = new AbortController();

    async function load() {
      setIsLoading(true);
      setError(null);
      try {
        setResult(
          await listPatients(slug, {
            search,
            page,
            pageSize,
            signal: controller.signal,
          }),
        );
      } catch (loadError) {
        if (!controller.signal.aborted) {
          setError(loadError instanceof Error ? loadError.message : "We couldn’t load the patient list.");
        }
      } finally {
        if (!controller.signal.aborted) setIsLoading(false);
      }
    }

    void load();
    return () => controller.abort();
  }, [page, search, slug]);

  function submitSearch(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setPage(1);
    setSearch(searchInput.trim());
  }

  function openPatient(patientId: string) {
    router.push(`/practice/${encodeURIComponent(slug)}/patients/${encodeURIComponent(patientId)}`);
  }

  function handleRowKey(event: KeyboardEvent<HTMLTableRowElement>, patientId: string) {
    if (event.key === "Enter" || event.key === " ") {
      event.preventDefault();
      openPatient(patientId);
    }
  }

  return (
    <main className="min-h-screen bg-[#f3f8f7] text-slate-900">
      <div className="mx-auto max-w-6xl px-5 py-6 sm:px-8 sm:py-8">
        <header className="flex items-center justify-between gap-4">
          <AyoosMark />
          <div className="flex items-center gap-2">
            <Link href={`/practice/${encodeURIComponent(slug)}`} className="hidden rounded-xl px-3 py-2 text-sm font-semibold text-slate-600 transition hover:bg-white hover:text-teal-700 md:inline-flex">Practice</Link>
            <UserMenu />
          </div>
        </header>

        <section className="mt-10 sm:mt-14">
          <div className="flex flex-col gap-5 sm:flex-row sm:items-end sm:justify-between">
            <div>
              <p className="text-sm font-semibold text-teal-700">Patient records</p>
              <h1 className="mt-2 text-3xl font-semibold tracking-[-0.045em] text-slate-950 sm:text-5xl">Patients</h1>
              <p className="mt-3 text-slate-500">Search demographics and manage registration for this practice.</p>
            </div>
            <Link href={`/practice/${encodeURIComponent(slug)}/patients/new`} className="inline-flex w-fit items-center justify-center rounded-xl bg-teal-700 px-5 py-3 text-sm font-semibold text-white shadow-lg shadow-teal-700/15 transition hover:bg-teal-800 focus:outline-none focus:ring-4 focus:ring-teal-500/20">Register patient</Link>
          </div>

          <section className="mt-8 overflow-hidden rounded-3xl border border-white bg-white shadow-[0_18px_50px_rgba(15,118,110,0.08)]">
            <div className="border-b border-slate-100 px-5 py-5 sm:px-7">
              <form onSubmit={submitSearch} className="flex flex-col gap-3 sm:flex-row">
                <label className="sr-only" htmlFor="patient-search">Search patients</label>
                <input id="patient-search" type="search" value={searchInput} onChange={(event) => setSearchInput(event.target.value)} placeholder="Search by name, email, or phone" className="min-w-0 flex-1 rounded-xl border border-slate-200 px-4 py-3 text-sm outline-none transition focus:border-teal-500 focus:ring-4 focus:ring-teal-500/10" />
                <button type="submit" className="rounded-xl border border-teal-200 px-5 py-3 text-sm font-semibold text-teal-800 transition hover:bg-teal-50">Search</button>
                {search && <button type="button" onClick={() => { setSearchInput(""); setSearch(""); setPage(1); }} className="rounded-xl px-4 py-3 text-sm font-semibold text-slate-500 hover:bg-slate-50">Clear</button>}
              </form>
            </div>

            {error ? (
              <div className="px-6 py-14 text-center"><p className="font-semibold text-rose-700">Unable to load patients</p><p className="mt-2 text-sm text-slate-500">{error}</p></div>
            ) : isLoading ? (
              <div className="grid gap-3 px-6 py-8" role="status"><div className="h-14 animate-pulse rounded-xl bg-slate-100" /><div className="h-14 animate-pulse rounded-xl bg-slate-100" /><div className="h-14 animate-pulse rounded-xl bg-slate-100" /><span className="sr-only">Loading patients…</span></div>
            ) : !result || result.items.length === 0 ? (
              <div className="px-6 py-16 text-center"><p className="text-lg font-semibold text-slate-900">{search ? "No matching patients" : "No patients registered yet"}</p><p className="mt-2 text-sm text-slate-500">{search ? "Try another name, email, or phone number." : "Register the first patient to begin their record."}</p></div>
            ) : (
              <div className="overflow-x-auto">
                <table className="w-full text-left">
                  <thead className="bg-slate-50/80 text-xs font-semibold uppercase tracking-[0.12em] text-slate-400"><tr><th className="px-6 py-4">Patient</th><th className="px-6 py-4">Date of birth</th><th className="px-6 py-4">Contact</th><th className="px-6 py-4">Status</th></tr></thead>
                  <tbody className="divide-y divide-slate-100">
                    {result.items.map((patient) => (
                      <tr key={patient.id} tabIndex={0} onClick={() => openPatient(patient.id)} onKeyDown={(event) => handleRowKey(event, patient.id)} className="cursor-pointer outline-none transition hover:bg-teal-50/50 focus:bg-teal-50 focus:ring-2 focus:ring-inset focus:ring-teal-500/30">
                        <td className="px-6 py-5"><p className="font-semibold text-slate-900">{formatPatientName(patient)}</p><p className="mt-1 text-xs text-slate-400">{patient.id}</p></td>
                        <td className="whitespace-nowrap px-6 py-5 text-sm text-slate-600">{formatDate(patient.dateOfBirth)}</td>
                        <td className="px-6 py-5 text-sm text-slate-600"><span className="block">{patient.email || "No email"}</span><span className="mt-1 block text-slate-400">{patient.phone || "No phone"}</span></td>
                        <td className="px-6 py-5"><span className={`rounded-full px-2.5 py-1 text-xs font-semibold ${patient.isActive ? "bg-emerald-50 text-emerald-700" : "bg-slate-200 text-slate-600"}`}>{patient.isActive ? "Active" : "Inactive"}</span></td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}

            {result && result.totalCount > 0 && (
              <div className="flex flex-col gap-3 border-t border-slate-100 bg-slate-50/60 px-6 py-4 text-sm sm:flex-row sm:items-center sm:justify-between">
                <p className="text-slate-500">{result.totalCount} patient{result.totalCount === 1 ? "" : "s"} · Page {result.page} of {Math.max(result.totalPages, 1)}</p>
                <div className="flex gap-2"><button type="button" disabled={page <= 1 || isLoading} onClick={() => setPage((value) => Math.max(1, value - 1))} className="rounded-xl border border-slate-200 bg-white px-4 py-2 font-semibold text-slate-600 disabled:opacity-40">Previous</button><button type="button" disabled={page >= result.totalPages || isLoading} onClick={() => setPage((value) => value + 1)} className="rounded-xl border border-slate-200 bg-white px-4 py-2 font-semibold text-slate-600 disabled:opacity-40">Next</button></div>
              </div>
            )}
          </section>
        </section>
      </div>
    </main>
  );
}
