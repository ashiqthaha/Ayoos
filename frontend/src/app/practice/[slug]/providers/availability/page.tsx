"use client";

import Link from "next/link";
import { useParams, useSearchParams } from "next/navigation";
import { useEffect, useMemo, useState } from "react";

import { AyoosMark } from "@/components/ayoos-mark";
import { ProviderAvailabilityEditor } from "@/components/provider-availability-editor";
import { UserMenu } from "@/components/user-menu";
import { listProviders, type Provider } from "@/lib/api";

export default function ProviderAvailabilityPage() {
  const { slug } = useParams<{ slug: string }>();
  const searchParams = useSearchParams();
  const requestedProviderId = searchParams.get("providerId");
  const [providers, setProviders] = useState<Provider[]>([]);
  const [selectedId, setSelectedId] = useState(requestedProviderId ?? "");
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const controller = new AbortController();

    async function load() {
      setIsLoading(true);
      setError(null);

      try {
        const loaded = await listProviders(slug, controller.signal);
        setProviders(loaded);
        setSelectedId((current) => {
          if (loaded.some((provider) => provider.id === current)) return current;
          if (requestedProviderId && loaded.some(
            (provider) => provider.id === requestedProviderId,
          )) {
            return requestedProviderId;
          }
          return loaded.find((provider) => provider.isActive)?.id ?? loaded[0]?.id ?? "";
        });
      } catch (loadError) {
        if (!controller.signal.aborted) {
          setError(
            loadError instanceof Error
              ? loadError.message
              : "We couldn’t load the provider roster.",
          );
        }
      } finally {
        if (!controller.signal.aborted) setIsLoading(false);
      }
    }

    void load();
    return () => controller.abort();
  }, [requestedProviderId, slug]);

  const selectedProvider = useMemo(
    () => providers.find((provider) => provider.id === selectedId) ?? null,
    [providers, selectedId],
  );

  return (
    <main className="min-h-screen bg-[#f3f8f7] text-slate-900">
      <div className="mx-auto max-w-6xl px-5 py-6 sm:px-8 sm:py-8">
        <header className="flex items-center justify-between gap-4">
          <AyoosMark />
          <div className="flex items-center gap-2">
            <Link
              href={`/practice/${encodeURIComponent(slug)}/providers`}
              className="hidden rounded-xl px-3 py-2 text-sm font-semibold text-slate-600 transition hover:bg-white hover:text-teal-700 md:inline-flex"
            >
              All providers
            </Link>
            <UserMenu />
          </div>
        </header>

        <section className="mt-10 sm:mt-14">
          <p className="text-sm font-semibold text-teal-700">Scheduling</p>
          <div className="mt-2 flex flex-col gap-5 sm:flex-row sm:items-end sm:justify-between">
            <div>
              <h1 className="text-3xl font-semibold tracking-[-0.045em] text-slate-950 sm:text-5xl">
                Provider availability
              </h1>
              <p className="mt-3 max-w-2xl leading-7 text-slate-600">
                Set weekly hours, manage date-specific changes, and preview
                concrete bookable slots.
              </p>
            </div>

            {providers.length > 0 && (
              <label className="min-w-72 text-sm font-semibold text-slate-700">
                Provider
                <select
                  value={selectedId}
                  onChange={(event) => setSelectedId(event.target.value)}
                  className="mt-2 w-full rounded-xl border border-slate-200 bg-white px-4 py-3 font-medium outline-none focus:border-teal-500 focus:ring-4 focus:ring-teal-500/10"
                >
                  {providers.map((provider) => (
                    <option key={provider.id} value={provider.id}>
                      {provider.firstName} {provider.lastName}, {provider.credentials}
                      {provider.isActive ? "" : " (inactive)"}
                    </option>
                  ))}
                </select>
              </label>
            )}
          </div>

          <div className="mt-8">
            {isLoading ? (
              <div
                className="h-96 animate-pulse rounded-3xl bg-white shadow-sm"
                role="status"
              >
                <span className="sr-only">Loading provider availability…</span>
              </div>
            ) : error ? (
              <div className="rounded-3xl border border-rose-100 bg-white p-8 text-center">
                <p className="font-semibold text-rose-700">Unable to load availability</p>
                <p className="mt-2 text-sm text-slate-600">{error}</p>
              </div>
            ) : selectedProvider ? (
              <ProviderAvailabilityEditor
                key={selectedProvider.id}
                slug={slug}
                providerId={selectedProvider.id}
                isActive={selectedProvider.isActive}
              />
            ) : (
              <div className="rounded-3xl border border-dashed border-teal-200 bg-white/70 px-6 py-14 text-center">
                <h2 className="text-xl font-semibold text-slate-950">
                  Add a provider first
                </h2>
                <p className="mt-2 text-slate-600">
                  Availability can be configured after a provider profile exists.
                </p>
                <Link
                  href={`/practice/${encodeURIComponent(slug)}/providers`}
                  className="mt-6 inline-flex rounded-xl bg-teal-700 px-5 py-3 text-sm font-semibold text-white"
                >
                  Go to providers
                </Link>
              </div>
            )}
          </div>
        </section>
      </div>
    </main>
  );
}
