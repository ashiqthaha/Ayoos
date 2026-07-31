"use client";

import Link from "next/link";
import { useParams, useRouter } from "next/navigation";
import { FormEvent, useEffect, useState } from "react";

import { AyoosMark } from "@/components/ayoos-mark";
import { UserMenu } from "@/components/user-menu";
import {
  ProviderFormFields,
  type ProviderField,
} from "@/components/provider-form-fields";
import {
  ApiError,
  createProvider,
  listProviders,
  type Provider,
  type ProviderInput,
} from "@/lib/api";
import {
  emptyProvider,
  normalizeProvider,
  validateProvider,
} from "@/lib/providers";

function providerCopy(): ProviderInput {
  return { ...emptyProvider };
}

export default function ProvidersPage() {
  const { slug } = useParams<{ slug: string }>();
  const router = useRouter();
  const [providers, setProviders] = useState<Provider[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [loadError, setLoadError] = useState<string | null>(null);
  const [showForm, setShowForm] = useState(false);
  const [form, setForm] = useState<ProviderInput>(providerCopy);
  const [errors, setErrors] = useState<
    Partial<Record<ProviderField, string>>
  >({});
  const [isSaving, setIsSaving] = useState(false);
  const [saveError, setSaveError] = useState<string | null>(null);

  useEffect(() => {
    const controller = new AbortController();

    async function load() {
      setIsLoading(true);
      setLoadError(null);

      try {
        setProviders(await listProviders(slug, controller.signal));
      } catch (error) {
        if (!controller.signal.aborted) {
          setLoadError(
            error instanceof Error
              ? error.message
              : "We couldn’t load the provider roster.",
          );
        }
      } finally {
        if (!controller.signal.aborted) setIsLoading(false);
      }
    }

    void load();
    return () => controller.abort();
  }, [slug]);

  function updateField(field: ProviderField, value: string) {
    setForm((current) => ({ ...current, [field]: value }));
    setErrors((current) => ({ ...current, [field]: undefined }));
    setSaveError(null);
  }

  async function handleCreate(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const validationErrors = validateProvider(form);

    if (Object.keys(validationErrors).length > 0) {
      setErrors(validationErrors);
      return;
    }

    setIsSaving(true);
    setSaveError(null);

    try {
      const provider = await createProvider(slug, normalizeProvider(form));
      router.push(
        `/practice/${encodeURIComponent(slug)}/providers/${encodeURIComponent(provider.id)}`,
      );
    } catch (error) {
      setSaveError(
        error instanceof ApiError
          ? error.message
          : "We couldn’t add this provider. Please try again.",
      );
      setIsSaving(false);
    }
  }

  return (
    <main className="min-h-screen bg-[#f3f8f7] text-slate-900">
      <div className="mx-auto max-w-6xl px-5 py-6 sm:px-8 sm:py-8">
        <header className="flex items-center justify-between gap-4">
          <AyoosMark />
          <div className="flex items-center gap-2">
            <Link
              href={`/practice/${encodeURIComponent(slug)}`}
              className="hidden rounded-xl px-3 py-2 text-sm font-semibold text-slate-600 transition hover:bg-white hover:text-teal-700 md:inline-flex"
            >
              Practice dashboard
            </Link>
            <UserMenu />
          </div>
        </header>

        <section className="mt-10 sm:mt-14">
          <div className="flex flex-col gap-5 sm:flex-row sm:items-end sm:justify-between">
            <div>
              <p className="text-sm font-semibold text-teal-700">Care team</p>
              <h1 className="mt-2 text-3xl font-semibold tracking-[-0.045em] text-slate-950 sm:text-5xl">
                Providers
              </h1>
              <p className="mt-3 max-w-2xl leading-7 text-slate-600">
                Manage clinician profiles, weekly hours, and one-off schedule
                changes.
              </p>
            </div>
            <button
              type="button"
              onClick={() => {
                setShowForm(true);
                setForm(providerCopy());
                setErrors({});
                setSaveError(null);
              }}
              className="inline-flex w-fit items-center justify-center rounded-xl bg-teal-700 px-5 py-3 text-sm font-semibold text-white shadow-lg shadow-teal-700/15 transition hover:bg-teal-800 focus:outline-none focus:ring-4 focus:ring-teal-500/20"
            >
              Add provider
            </button>
          </div>

          {showForm && (
            <form
              onSubmit={handleCreate}
              noValidate
              className="mt-8 overflow-hidden rounded-3xl border border-teal-100 bg-white shadow-[0_24px_70px_rgba(15,118,110,0.10)]"
            >
              <div className="border-b border-slate-100 px-6 py-6 sm:px-9">
                <p className="text-xs font-semibold uppercase tracking-[0.15em] text-teal-700">
                  New team member
                </p>
                <h2 className="mt-2 text-2xl font-semibold tracking-[-0.03em] text-slate-950">
                  Add a provider
                </h2>
              </div>
              <div className="px-6 py-7 sm:px-9">
                {saveError && (
                  <div
                    role="alert"
                    className="mb-6 rounded-2xl border border-rose-200 bg-rose-50 px-4 py-3 text-sm text-rose-800"
                  >
                    {saveError}
                  </div>
                )}
                <ProviderFormFields
                  value={form}
                  errors={errors}
                  disabled={isSaving}
                  onChange={updateField}
                />
              </div>
              <div className="flex flex-col-reverse gap-3 border-t border-slate-100 bg-slate-50/70 px-6 py-5 sm:flex-row sm:justify-end sm:px-9">
                <button
                  type="button"
                  onClick={() => setShowForm(false)}
                  disabled={isSaving}
                  className="rounded-xl px-5 py-3 text-sm font-semibold text-slate-600 transition hover:bg-white disabled:opacity-50"
                >
                  Cancel
                </button>
                <button
                  type="submit"
                  disabled={isSaving}
                  className="rounded-xl bg-teal-700 px-5 py-3 text-sm font-semibold text-white transition hover:bg-teal-800 disabled:opacity-60"
                >
                  {isSaving ? "Adding…" : "Add provider"}
                </button>
              </div>
            </form>
          )}

          <div className="mt-8">
            {isLoading ? (
              <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3" role="status">
                {[0, 1, 2].map((item) => (
                  <div
                    key={item}
                    className="h-56 animate-pulse rounded-3xl bg-white shadow-sm"
                  />
                ))}
                <span className="sr-only">Loading providers…</span>
              </div>
            ) : loadError ? (
              <div className="rounded-3xl border border-rose-100 bg-white p-8 text-center">
                <p className="font-semibold text-rose-700">Unable to load providers</p>
                <p className="mt-2 text-sm text-slate-600">{loadError}</p>
              </div>
            ) : providers.length === 0 ? (
              <div className="rounded-3xl border border-dashed border-teal-200 bg-white/70 px-6 py-14 text-center">
                <div className="mx-auto grid h-14 w-14 place-items-center rounded-2xl bg-teal-50 text-xl font-semibold text-teal-700">
                  +
                </div>
                <h2 className="mt-5 text-xl font-semibold text-slate-950">
                  Build your provider roster
                </h2>
                <p className="mx-auto mt-2 max-w-md leading-7 text-slate-600">
                  Add your first clinician, then set the hours patients can book.
                </p>
              </div>
            ) : (
              <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
                {providers.map((provider) => (
                  <Link
                    key={provider.id}
                    href={`/practice/${encodeURIComponent(slug)}/providers/${encodeURIComponent(provider.id)}`}
                    className="group rounded-3xl border border-white bg-white p-6 shadow-[0_14px_40px_rgba(15,118,110,0.07)] transition hover:-translate-y-0.5 hover:border-teal-100 hover:shadow-[0_18px_45px_rgba(15,118,110,0.12)] focus:outline-none focus:ring-4 focus:ring-teal-500/15"
                  >
                    <div className="flex items-start justify-between gap-4">
                      <span className="grid h-12 w-12 place-items-center rounded-2xl bg-teal-50 font-semibold text-teal-800">
                        {provider.firstName.charAt(0)}
                        {provider.lastName.charAt(0)}
                      </span>
                      <span
                        className={`rounded-full px-2.5 py-1 text-xs font-semibold ${
                          provider.isActive
                            ? "bg-emerald-50 text-emerald-700"
                            : "bg-slate-100 text-slate-500"
                        }`}
                      >
                        {provider.isActive ? "Active" : "Inactive"}
                      </span>
                    </div>
                    <h2 className="mt-5 text-xl font-semibold tracking-[-0.025em] text-slate-950 group-hover:text-teal-800">
                      {provider.firstName} {provider.lastName},{" "}
                      {provider.credentials}
                    </h2>
                    <p className="mt-1 text-sm font-medium text-teal-700">
                      {provider.specialty}
                    </p>
                    <div className="mt-5 border-t border-slate-100 pt-4 text-sm leading-6 text-slate-500">
                      <p className="truncate">{provider.email}</p>
                      <p>{provider.phone}</p>
                    </div>
                  </Link>
                ))}
              </div>
            )}
          </div>
        </section>
      </div>
    </main>
  );
}
