"use client";

import Link from "next/link";
import { useParams, useRouter } from "next/navigation";
import { FormEvent, useEffect, useState } from "react";

import { AyoosMark } from "@/components/ayoos-mark";
import { useAuth } from "@/components/auth-provider";
import { RequireRole } from "@/components/require-role";
import { UserMenu } from "@/components/user-menu";
import {
  PracticeAddressFields,
  PracticeContactFields,
  PracticeIdentityFields,
} from "@/components/practice-form-fields";
import {
  ApiError,
  getPractice,
  updatePractice,
  type Practice,
  type PracticeInput,
} from "@/lib/api";
import { getRealmRoles } from "@/lib/auth-client";
import {
  normalizePractice,
  toKebabCase,
  validatePractice,
  type PracticeField,
  type PracticeFieldErrors,
} from "@/lib/practice";

function formatAddress(practice: Practice) {
  return [
    practice.address.line1,
    practice.address.line2,
    [practice.address.city, practice.address.state, practice.address.postalCode]
      .filter(Boolean)
      .join(", "),
    practice.address.country,
  ].filter(Boolean);
}

function practiceToInput(practice: Practice): PracticeInput {
  return {
    name: practice.name,
    slug: practice.slug,
    timeZone: practice.timeZone,
    address: { ...practice.address, line2: practice.address.line2 ?? "" },
    contactEmail: practice.contactEmail,
    contactPhone: practice.contactPhone,
  };
}

export default function PracticeDashboardPage() {
  const params = useParams<{ slug: string }>();
  const router = useRouter();
  const { user } = useAuth();
  const slug = params.slug;
  const canManagePractice = user
    ? getRealmRoles(user).includes("practice-admin")
    : false;
  const [practice, setPractice] = useState<Practice | null>(null);
  const [form, setForm] = useState<PracticeInput | null>(null);
  const [errors, setErrors] = useState<PracticeFieldErrors>({});
  const [isLoading, setIsLoading] = useState(true);
  const [isNotFound, setIsNotFound] = useState(false);
  const [loadError, setLoadError] = useState<string | null>(null);
  const [isEditing, setIsEditing] = useState(false);
  const [isSaving, setIsSaving] = useState(false);
  const [banner, setBanner] = useState<{ tone: "success" | "error"; text: string } | null>(
    null,
  );
  const [reloadKey, setReloadKey] = useState(0);

  useEffect(() => {
    const controller = new AbortController();

    async function loadPractice() {
      setIsLoading(true);
      setIsNotFound(false);
      setLoadError(null);

      try {
        const loaded = await getPractice(slug, controller.signal);
        setPractice(loaded);
        setForm(practiceToInput(loaded));
      } catch (error) {
        if (controller.signal.aborted) {
          return;
        }

        if (error instanceof ApiError && error.status === 404) {
          setIsNotFound(true);
        } else {
          setLoadError(
            error instanceof Error
              ? error.message
              : "We couldn’t load this practice. Please try again.",
          );
        }
      } finally {
        if (!controller.signal.aborted) {
          setIsLoading(false);
        }
      }
    }

    void loadPractice();
    return () => controller.abort();
  }, [slug, reloadKey]);

  function updateField(field: PracticeField, rawValue: string) {
    const value = field === "slug" ? toKebabCase(rawValue) : rawValue;

    setForm((current) => {
      if (!current) return current;

      if (field.startsWith("address.")) {
        const addressField = field.slice("address.".length) as keyof PracticeInput["address"];
        return {
          ...current,
          address: { ...current.address, [addressField]: value },
        };
      }

      return { ...current, [field]: value };
    });

    setErrors((current) => ({ ...current, [field]: undefined }));
    setBanner(null);
  }

  async function handleSave(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!form || !practice) return;

    const validationErrors = validatePractice(form);
    if (Object.keys(validationErrors).length > 0) {
      setErrors(validationErrors);
      setBanner({ tone: "error", text: "Review the highlighted fields before saving." });
      return;
    }

    setIsSaving(true);
    setBanner(null);

    try {
      const updated = await updatePractice(
        practice.slug,
        normalizePractice(form),
        practice.isActive,
      );
      setPractice(updated);
      setForm(practiceToInput(updated));
      setErrors({});
      setIsEditing(false);
      setBanner({ tone: "success", text: "Practice details saved successfully." });

      if (updated.slug !== slug) {
        router.replace(`/practice/${encodeURIComponent(updated.slug)}`);
      }
    } catch (error) {
      const message =
        error instanceof ApiError && error.status === 409
          ? "That practice URL is already in use. Choose a different slug."
          : error instanceof Error
            ? error.message
            : "We couldn’t save your changes. Please try again.";
      setBanner({ tone: "error", text: message });
    } finally {
      setIsSaving(false);
    }
  }

  if (isLoading) {
    return (
      <main className="min-h-screen bg-[#f3f8f7] px-5 py-6 text-slate-900 sm:px-8 sm:py-8">
        <div className="mx-auto max-w-6xl">
          <AyoosMark />
          <div className="mt-14 animate-pulse">
            <div className="h-4 w-36 rounded bg-teal-100" />
            <div className="mt-4 h-11 w-2/3 max-w-lg rounded-xl bg-slate-200" />
            <div className="mt-10 h-72 rounded-3xl bg-white shadow-sm" />
          </div>
          <p className="sr-only" role="status">Loading practice…</p>
        </div>
      </main>
    );
  }

  if (isNotFound) {
    return (
      <main className="grid min-h-screen place-items-center bg-[#f3f8f7] px-5 py-12 text-slate-900">
        <section className="w-full max-w-lg rounded-3xl border border-white bg-white p-8 text-center shadow-[0_24px_70px_rgba(15,118,110,0.10)] sm:p-10">
          <div className="mx-auto grid h-14 w-14 place-items-center rounded-2xl bg-teal-50 text-2xl font-semibold text-teal-700">
            A
          </div>
          <p className="mt-6 text-sm font-semibold text-teal-700">Practice not found</p>
          <h1 className="mt-2 text-3xl font-semibold tracking-[-0.04em] text-slate-950">
            We couldn’t find “{slug}”
          </h1>
          <p className="mt-4 leading-7 text-slate-600">
            The link may be outdated, or this practice has not been set up yet.
          </p>
          <a
            href="/"
            className="mt-7 inline-flex rounded-xl bg-teal-700 px-5 py-3 text-sm font-semibold text-white shadow-lg shadow-teal-700/15 transition hover:bg-teal-800 focus:outline-none focus:ring-4 focus:ring-teal-500/20"
          >
            Return to Ayoos
          </a>
        </section>
      </main>
    );
  }

  if (loadError || !practice || !form) {
    return (
      <main className="grid min-h-screen place-items-center bg-[#f3f8f7] px-5 py-12 text-slate-900">
        <section className="w-full max-w-lg rounded-3xl border border-rose-100 bg-white p-8 text-center shadow-xl shadow-slate-900/5 sm:p-10">
          <p className="text-sm font-semibold text-rose-600">Unable to load practice</p>
          <h1 className="mt-2 text-2xl font-semibold tracking-[-0.03em] text-slate-950">
            Something interrupted the connection
          </h1>
          <p className="mt-4 leading-7 text-slate-600">{loadError}</p>
          <button
            type="button"
            onClick={() => setReloadKey((value) => value + 1)}
            className="mt-7 rounded-xl bg-teal-700 px-5 py-3 text-sm font-semibold text-white transition hover:bg-teal-800 focus:outline-none focus:ring-4 focus:ring-teal-500/20"
          >
            Try again
          </button>
        </section>
      </main>
    );
  }

  return (
    <main className="min-h-screen bg-[#f3f8f7] text-slate-900">
      <div className="mx-auto max-w-6xl px-5 py-6 sm:px-8 sm:py-8">
        <header className="flex items-center justify-between gap-5">
          <AyoosMark />
          <div className="flex items-center gap-3">
            <div className="hidden items-center gap-2 rounded-full border border-emerald-100 bg-white px-3 py-1.5 text-xs font-semibold text-emerald-700 shadow-sm md:flex">
              <span className="h-2 w-2 rounded-full bg-emerald-500" />
              {practice.isActive ? "Active practice" : "Inactive practice"}
            </div>
            <UserMenu />
          </div>
        </header>

        <div className="mt-10 sm:mt-14">
          <div className="flex flex-col gap-6 sm:flex-row sm:items-end sm:justify-between">
            <div>
              <p className="text-sm font-semibold text-teal-700">Practice dashboard</p>
              <h1 className="mt-2 text-3xl font-semibold tracking-[-0.045em] text-slate-950 sm:text-5xl">
                {practice.name}
              </h1>
              <p className="mt-3 text-slate-500">/{practice.slug}</p>
            </div>
            {!isEditing && (
              <div className="flex flex-wrap gap-3">
                <Link
                  href={`/practice/${encodeURIComponent(practice.slug)}/patients`}
                  className="inline-flex w-fit items-center justify-center rounded-xl border border-teal-200 bg-white px-5 py-3 text-sm font-semibold text-teal-800 transition hover:bg-teal-50 focus:outline-none focus:ring-4 focus:ring-teal-500/15"
                >
                  Manage patients
                </Link>
                <Link
                  href={`/practice/${encodeURIComponent(practice.slug)}/providers`}
                  className="inline-flex w-fit items-center justify-center rounded-xl border border-teal-200 bg-white px-5 py-3 text-sm font-semibold text-teal-800 transition hover:bg-teal-50 focus:outline-none focus:ring-4 focus:ring-teal-500/15"
                >
                  Manage providers
                </Link>
                <Link
                  href={`/practice/${encodeURIComponent(practice.slug)}/bookings`}
                  className="inline-flex w-fit items-center justify-center rounded-xl border border-teal-200 bg-white px-5 py-3 text-sm font-semibold text-teal-800 transition hover:bg-teal-50 focus:outline-none focus:ring-4 focus:ring-teal-500/15"
                >
                  Manage bookings
                </Link>
                {canManagePractice && (
                  <button
                    type="button"
                    onClick={() => {
                      setForm(practiceToInput(practice));
                      setErrors({});
                      setBanner(null);
                      setIsEditing(true);
                    }}
                    className="inline-flex w-fit items-center justify-center rounded-xl bg-teal-700 px-5 py-3 text-sm font-semibold text-white shadow-lg shadow-teal-700/15 transition hover:bg-teal-800 focus:outline-none focus:ring-4 focus:ring-teal-500/20"
                  >
                    Edit practice
                  </button>
                )}
              </div>
            )}
          </div>

          {banner && (
            <div
              role="status"
              className={`mt-7 rounded-2xl border px-4 py-3 text-sm font-medium ${
                banner.tone === "success"
                  ? "border-emerald-200 bg-emerald-50 text-emerald-800"
                  : "border-rose-200 bg-rose-50 text-rose-800"
              }`}
            >
              {banner.text}
            </div>
          )}

          {isEditing ? (
            <RequireRole requiredRoles="practice-admin">
              <form
                onSubmit={handleSave}
                noValidate
                className="mt-8 overflow-hidden rounded-3xl border border-white bg-white shadow-[0_24px_70px_rgba(15,118,110,0.09)]"
              >
              <div className="border-b border-slate-100 px-6 py-6 sm:px-9 sm:py-8">
                <h2 className="text-2xl font-semibold tracking-[-0.03em] text-slate-950">
                  Edit practice details
                </h2>
                <p className="mt-2 text-sm leading-6 text-slate-500">
                  Changes are visible across your Ayoos workspace after saving.
                </p>
              </div>
              <div className="grid gap-10 px-6 py-8 sm:px-9 sm:py-9">
                <section>
                  <h3 className="mb-5 text-sm font-semibold uppercase tracking-[0.14em] text-teal-700">
                    Practice
                  </h3>
                  <PracticeIdentityFields value={form} errors={errors} onChange={updateField} />
                </section>
                <section className="border-t border-slate-100 pt-9">
                  <h3 className="mb-5 text-sm font-semibold uppercase tracking-[0.14em] text-teal-700">
                    Address
                  </h3>
                  <PracticeAddressFields value={form} errors={errors} onChange={updateField} />
                </section>
                <section className="border-t border-slate-100 pt-9">
                  <h3 className="mb-5 text-sm font-semibold uppercase tracking-[0.14em] text-teal-700">
                    Contact
                  </h3>
                  <PracticeContactFields value={form} errors={errors} onChange={updateField} />
                </section>
              </div>
              <div className="flex flex-col-reverse gap-3 border-t border-slate-100 bg-slate-50/70 px-6 py-5 sm:flex-row sm:justify-end sm:px-9">
                <button
                  type="button"
                  onClick={() => {
                    setForm(practiceToInput(practice));
                    setErrors({});
                    setBanner(null);
                    setIsEditing(false);
                  }}
                  disabled={isSaving}
                  className="rounded-xl px-5 py-3 text-sm font-semibold text-slate-600 transition hover:bg-white hover:text-slate-900 focus:outline-none focus:ring-4 focus:ring-slate-300/40 disabled:opacity-50"
                >
                  Cancel
                </button>
                <button
                  type="submit"
                  disabled={isSaving}
                  className="rounded-xl bg-teal-700 px-5 py-3 text-sm font-semibold text-white shadow-lg shadow-teal-700/15 transition hover:bg-teal-800 focus:outline-none focus:ring-4 focus:ring-teal-500/20 disabled:cursor-not-allowed disabled:opacity-60"
                >
                  {isSaving ? "Saving…" : "Save changes"}
                </button>
              </div>
              </form>
            </RequireRole>
          ) : (
            <section className="mt-8 overflow-hidden rounded-3xl border border-white bg-white shadow-[0_24px_70px_rgba(15,118,110,0.09)]">
              <div className="border-b border-slate-100 px-6 py-6 sm:px-9">
                <h2 className="text-xl font-semibold tracking-[-0.025em] text-slate-950">
                  Practice details
                </h2>
                <p className="mt-1 text-sm text-slate-500">Primary profile and contact information</p>
              </div>
              <dl className="grid sm:grid-cols-2 lg:grid-cols-3">
                <div className="border-b border-slate-100 px-6 py-6 sm:border-r sm:px-9 lg:col-span-2">
                  <dt className="text-xs font-semibold uppercase tracking-[0.14em] text-slate-400">
                    Address
                  </dt>
                  <dd className="mt-3 leading-7 text-slate-800">
                    {formatAddress(practice).map((line) => (
                      <span key={line} className="block">{line}</span>
                    ))}
                  </dd>
                </div>
                <div className="border-b border-slate-100 px-6 py-6 sm:px-9">
                  <dt className="text-xs font-semibold uppercase tracking-[0.14em] text-slate-400">
                    Time zone
                  </dt>
                  <dd className="mt-3 font-medium text-slate-800">
                    {practice.timeZone.replaceAll("_", " ")}
                  </dd>
                </div>
                <div className="border-b border-slate-100 px-6 py-6 sm:border-r sm:px-9 lg:border-b-0">
                  <dt className="text-xs font-semibold uppercase tracking-[0.14em] text-slate-400">
                    Contact email
                  </dt>
                  <dd className="mt-3 break-words font-medium text-slate-800">
                    <a className="transition hover:text-teal-700" href={`mailto:${practice.contactEmail}`}>
                      {practice.contactEmail}
                    </a>
                  </dd>
                </div>
                <div className="border-b border-slate-100 px-6 py-6 sm:px-9 lg:border-b-0 lg:border-r">
                  <dt className="text-xs font-semibold uppercase tracking-[0.14em] text-slate-400">
                    Contact phone
                  </dt>
                  <dd className="mt-3 font-medium text-slate-800">
                    <a className="transition hover:text-teal-700" href={`tel:${practice.contactPhone}`}>
                      {practice.contactPhone}
                    </a>
                  </dd>
                </div>
                <div className="px-6 py-6 sm:px-9">
                  <dt className="text-xs font-semibold uppercase tracking-[0.14em] text-slate-400">
                    Practice URL
                  </dt>
                  <dd className="mt-3 break-all font-medium text-slate-800">/practice/{practice.slug}</dd>
                </div>
              </dl>
            </section>
          )}
        </div>
      </div>
    </main>
  );
}
