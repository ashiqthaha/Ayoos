"use client";

import Link from "next/link";
import { useParams } from "next/navigation";
import { FormEvent, useEffect, useState } from "react";

import { AyoosMark } from "@/components/ayoos-mark";
import { UserMenu } from "@/components/user-menu";
import { ProviderAvailabilityEditor } from "@/components/provider-availability-editor";
import {
  ProviderFormFields,
  type ProviderField,
} from "@/components/provider-form-fields";
import {
  ApiError,
  deactivateProvider,
  getProvider,
  updateProvider,
  type Provider,
  type ProviderInput,
} from "@/lib/api";
import { normalizeProvider, validateProvider } from "@/lib/providers";

function providerToInput(provider: Provider): ProviderInput {
  return {
    firstName: provider.firstName,
    lastName: provider.lastName,
    credentials: provider.credentials,
    specialty: provider.specialty,
    email: provider.email,
    phone: provider.phone,
  };
}

export default function ProviderDetailPage() {
  const { slug, id } = useParams<{ slug: string; id: string }>();
  const [provider, setProvider] = useState<Provider | null>(null);
  const [form, setForm] = useState<ProviderInput | null>(null);
  const [tab, setTab] = useState<"details" | "availability">("details");
  const [isLoading, setIsLoading] = useState(true);
  const [isNotFound, setIsNotFound] = useState(false);
  const [loadError, setLoadError] = useState<string | null>(null);
  const [errors, setErrors] = useState<
    Partial<Record<ProviderField, string>>
  >({});
  const [isSaving, setIsSaving] = useState(false);
  const [isDeactivating, setIsDeactivating] = useState(false);
  const [message, setMessage] = useState<{
    tone: "success" | "error";
    text: string;
  } | null>(null);

  useEffect(() => {
    const controller = new AbortController();

    async function load() {
      setIsLoading(true);
      setLoadError(null);
      setIsNotFound(false);

      try {
        const loaded = await getProvider(slug, id, controller.signal);
        setProvider(loaded);
        setForm(providerToInput(loaded));
      } catch (error) {
        if (controller.signal.aborted) return;
        if (error instanceof ApiError && error.status === 404) {
          setIsNotFound(true);
        } else {
          setLoadError(
            error instanceof Error
              ? error.message
              : "We couldn’t load this provider.",
          );
        }
      } finally {
        if (!controller.signal.aborted) setIsLoading(false);
      }
    }

    void load();
    return () => controller.abort();
  }, [id, slug]);

  function updateField(field: ProviderField, value: string) {
    setForm((current) => (current ? { ...current, [field]: value } : current));
    setErrors((current) => ({ ...current, [field]: undefined }));
    setMessage(null);
  }

  async function saveDetails(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!form) return;

    const validationErrors = validateProvider(form);
    if (Object.keys(validationErrors).length > 0) {
      setErrors(validationErrors);
      return;
    }

    setIsSaving(true);
    setMessage(null);

    try {
      const updated = await updateProvider(
        slug,
        id,
        normalizeProvider(form),
      );
      setProvider(updated);
      setForm(providerToInput(updated));
      setMessage({ tone: "success", text: "Provider details saved." });
    } catch (error) {
      setMessage({
        tone: "error",
        text:
          error instanceof Error
            ? error.message
            : "We couldn’t save this provider.",
      });
    } finally {
      setIsSaving(false);
    }
  }

  async function handleDeactivate() {
    setIsDeactivating(true);
    setMessage(null);

    try {
      const updated = await deactivateProvider(slug, id);
      setProvider(updated);
      setMessage({
        tone: "success",
        text: "Provider deactivated. Their saved schedule is retained.",
      });
    } catch (error) {
      setMessage({
        tone: "error",
        text:
          error instanceof Error
            ? error.message
            : "We couldn’t deactivate this provider.",
      });
    } finally {
      setIsDeactivating(false);
    }
  }

  if (isLoading) {
    return (
      <main className="min-h-screen bg-[#f3f8f7] px-5 py-6 text-slate-900 sm:px-8">
        <div className="mx-auto max-w-6xl">
          <AyoosMark />
          <div className="mt-14 animate-pulse">
            <div className="h-10 w-72 rounded-xl bg-slate-200" />
            <div className="mt-9 h-96 rounded-3xl bg-white" />
          </div>
          <p className="sr-only" role="status">Loading provider…</p>
        </div>
      </main>
    );
  }

  if (isNotFound) {
    return (
      <main className="grid min-h-screen place-items-center bg-[#f3f8f7] px-5 py-12">
        <section className="max-w-lg rounded-3xl bg-white p-9 text-center shadow-xl shadow-teal-900/5">
          <p className="text-sm font-semibold text-teal-700">Provider not found</p>
          <h1 className="mt-2 text-3xl font-semibold tracking-[-0.04em] text-slate-950">
            This team member isn’t available
          </h1>
          <Link
            href={`/practice/${encodeURIComponent(slug)}/providers`}
            className="mt-7 inline-flex rounded-xl bg-teal-700 px-5 py-3 text-sm font-semibold text-white"
          >
            Back to providers
          </Link>
        </section>
      </main>
    );
  }

  if (loadError || !provider || !form) {
    return (
      <main className="grid min-h-screen place-items-center bg-[#f3f8f7] px-5 py-12">
        <section className="max-w-lg rounded-3xl border border-rose-100 bg-white p-9 text-center">
          <p className="font-semibold text-rose-700">Unable to load provider</p>
          <p className="mt-3 text-slate-600">{loadError}</p>
        </section>
      </main>
    );
  }

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
          <div className="flex flex-col gap-5 sm:flex-row sm:items-end sm:justify-between">
            <div>
              <div className="flex flex-wrap items-center gap-3">
                <p className="text-sm font-semibold text-teal-700">Provider profile</p>
                <span
                  className={`rounded-full px-2.5 py-1 text-xs font-semibold ${
                    provider.isActive
                      ? "bg-emerald-50 text-emerald-700"
                      : "bg-slate-200 text-slate-600"
                  }`}
                >
                  {provider.isActive ? "Active" : "Inactive"}
                </span>
              </div>
              <h1 className="mt-2 text-3xl font-semibold tracking-[-0.045em] text-slate-950 sm:text-5xl">
                {provider.firstName} {provider.lastName},{" "}
                {provider.credentials}
              </h1>
              <p className="mt-3 text-lg text-slate-500">{provider.specialty}</p>
            </div>
          </div>

          <div className="mt-8 flex gap-1 rounded-2xl border border-white bg-white/80 p-1.5 shadow-sm sm:w-fit">
            {(["details", "availability"] as const).map((value) => (
              <button
                key={value}
                type="button"
                onClick={() => {
                  setTab(value);
                  setMessage(null);
                }}
                className={`flex-1 rounded-xl px-5 py-2.5 text-sm font-semibold capitalize transition sm:flex-none ${
                  tab === value
                    ? "bg-teal-700 text-white shadow-sm"
                    : "text-slate-500 hover:bg-teal-50 hover:text-teal-800"
                }`}
              >
                {value}
              </button>
            ))}
          </div>

          <div className="mt-6">
            {tab === "details" ? (
              <div className="grid gap-5">
                {message && (
                  <div
                    role="status"
                    className={`rounded-2xl border px-4 py-3 text-sm font-medium ${
                      message.tone === "success"
                        ? "border-emerald-200 bg-emerald-50 text-emerald-800"
                        : "border-rose-200 bg-rose-50 text-rose-800"
                    }`}
                  >
                    {message.text}
                  </div>
                )}

                <form
                  onSubmit={saveDetails}
                  noValidate
                  className="overflow-hidden rounded-3xl border border-white bg-white shadow-[0_18px_50px_rgba(15,118,110,0.08)]"
                >
                  <div className="border-b border-slate-100 px-6 py-6 sm:px-9">
                    <h2 className="text-xl font-semibold tracking-[-0.025em] text-slate-950">
                      Provider details
                    </h2>
                    <p className="mt-1 text-sm text-slate-500">
                      Keep patient-facing contact and specialty information current.
                    </p>
                  </div>
                  <div className="px-6 py-7 sm:px-9 sm:py-8">
                    <ProviderFormFields
                      value={form}
                      errors={errors}
                      disabled={isSaving}
                      onChange={updateField}
                    />
                  </div>
                  <div className="flex justify-end border-t border-slate-100 bg-slate-50/70 px-6 py-5 sm:px-9">
                    <button
                      type="submit"
                      disabled={isSaving}
                      className="rounded-xl bg-teal-700 px-5 py-3 text-sm font-semibold text-white transition hover:bg-teal-800 disabled:opacity-60"
                    >
                      {isSaving ? "Saving…" : "Save details"}
                    </button>
                  </div>
                </form>

                {provider.isActive && (
                  <section className="flex flex-col gap-4 rounded-3xl border border-rose-100 bg-white px-6 py-6 sm:flex-row sm:items-center sm:justify-between sm:px-9">
                    <div>
                      <h2 className="font-semibold text-slate-900">
                        Deactivate provider
                      </h2>
                      <p className="mt-1 text-sm leading-6 text-slate-500">
                        Stops new slots from appearing while retaining profile and
                        schedule data.
                      </p>
                    </div>
                    <button
                      type="button"
                      onClick={() => void handleDeactivate()}
                      disabled={isDeactivating}
                      className="w-fit rounded-xl border border-rose-200 px-4 py-2.5 text-sm font-semibold text-rose-700 transition hover:bg-rose-50 disabled:opacity-50"
                    >
                      {isDeactivating ? "Deactivating…" : "Deactivate"}
                    </button>
                  </section>
                )}
              </div>
            ) : (
              <ProviderAvailabilityEditor
                slug={slug}
                providerId={id}
                isActive={provider.isActive}
              />
            )}
          </div>
        </section>
      </div>
    </main>
  );
}
