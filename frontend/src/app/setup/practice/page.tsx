"use client";

import { useRouter } from "next/navigation";
import { FormEvent, useEffect, useState } from "react";

import { AyoosMark } from "@/components/ayoos-mark";
import {
  PracticeAddressFields,
  PracticeContactFields,
  PracticeIdentityFields,
} from "@/components/practice-form-fields";
import { ApiError, createPractice, type PracticeInput } from "@/lib/api";
import {
  emptyPractice,
  getBrowserTimeZone,
  normalizePractice,
  stepFields,
  toKebabCase,
  validatePractice,
  type PracticeField,
  type PracticeFieldErrors,
} from "@/lib/practice";

const steps = [
  { number: 1, label: "Practice" },
  { number: 2, label: "Address" },
  { number: 3, label: "Contact & review" },
];

function copyEmptyPractice(): PracticeInput {
  return { ...emptyPractice, address: { ...emptyPractice.address } };
}

export default function PracticeSetupPage() {
  const router = useRouter();
  const [step, setStep] = useState(1);
  const [form, setForm] = useState<PracticeInput>(copyEmptyPractice);
  const [errors, setErrors] = useState<PracticeFieldErrors>({});
  const [slugWasEdited, setSlugWasEdited] = useState(false);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [submitError, setSubmitError] = useState<string | null>(null);

  useEffect(() => {
    setForm((current) =>
      current.timeZone ? current : { ...current, timeZone: getBrowserTimeZone() },
    );
  }, []);

  function updateField(field: PracticeField, rawValue: string) {
    const value = field === "slug" ? toKebabCase(rawValue) : rawValue;

    if (field === "slug") {
      setSlugWasEdited(true);
    }

    setForm((current) => {
      if (field.startsWith("address.")) {
        const addressField = field.slice("address.".length) as keyof PracticeInput["address"];
        return {
          ...current,
          address: { ...current.address, [addressField]: value },
        };
      }

      if (field === "name") {
        return {
          ...current,
          name: value,
          slug: slugWasEdited ? current.slug : toKebabCase(value),
        };
      }

      return { ...current, [field]: value };
    });

    setErrors((current) => ({ ...current, [field]: undefined }));
    setSubmitError(null);
  }

  function moveToNextStep() {
    const allErrors = validatePractice(form);
    const currentErrors = Object.fromEntries(
      stepFields[step]
        .filter((field) => allErrors[field])
        .map((field) => [field, allErrors[field]]),
    ) as PracticeFieldErrors;

    if (Object.keys(currentErrors).length > 0) {
      setErrors((existing) => ({ ...existing, ...currentErrors }));
      return;
    }

    setErrors({});
    setStep((current) => Math.min(current + 1, 3));
    window.scrollTo({ top: 0, behavior: "smooth" });
  }

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    if (step < 3) {
      moveToNextStep();
      return;
    }

    const validationErrors = validatePractice(form);
    if (Object.keys(validationErrors).length > 0) {
      setErrors(validationErrors);
      return;
    }

    setIsSubmitting(true);
    setSubmitError(null);

    try {
      const created = await createPractice(normalizePractice(form));
      router.push(`/practice/${encodeURIComponent(created.slug)}`);
    } catch (error) {
      if (error instanceof ApiError && error.status === 409) {
        setSubmitError(
          "That practice URL is already in use. Choose a different slug and try again.",
        );
      } else {
        setSubmitError(
          error instanceof Error
            ? error.message
            : "We couldn’t create your practice. Please try again.",
        );
      }
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <main className="min-h-screen bg-[#f3f8f7] text-slate-900">
      <div className="mx-auto max-w-6xl px-5 py-6 sm:px-8 sm:py-8">
        <header className="flex items-center justify-between">
          <AyoosMark />
          <span className="rounded-full border border-teal-100 bg-white px-3 py-1.5 text-xs font-semibold uppercase tracking-[0.14em] text-teal-700 shadow-sm">
            Practice setup
          </span>
        </header>

        <div className="mx-auto mt-10 max-w-3xl sm:mt-14">
          <div className="mb-8">
            <p className="text-sm font-semibold text-teal-700">Let’s get you started</p>
            <h1 className="mt-2 text-3xl font-semibold tracking-[-0.04em] text-slate-950 sm:text-4xl">
              Set up your practice
            </h1>
            <p className="mt-3 max-w-2xl leading-7 text-slate-600">
              Add the essentials now. You can update these details from your practice
              dashboard at any time.
            </p>
          </div>

          <nav aria-label="Setup progress" className="mb-6">
            <ol className="grid grid-cols-3 gap-2 sm:gap-4">
              {steps.map((item) => {
                const isCurrent = item.number === step;
                const isComplete = item.number < step;

                return (
                  <li key={item.number} aria-current={isCurrent ? "step" : undefined}>
                    <div
                      className={`h-1 rounded-full ${item.number <= step ? "bg-teal-600" : "bg-slate-200"}`}
                    />
                    <div className="mt-3 flex items-center gap-2">
                      <span
                        className={`grid h-6 w-6 shrink-0 place-items-center rounded-full text-xs font-semibold ${
                          isCurrent
                            ? "bg-teal-700 text-white"
                            : isComplete
                              ? "bg-teal-100 text-teal-800"
                              : "bg-slate-100 text-slate-500"
                        }`}
                      >
                        {isComplete ? "✓" : item.number}
                      </span>
                      <span
                        className={`hidden text-sm sm:block ${isCurrent ? "font-semibold text-slate-900" : "text-slate-500"}`}
                      >
                        {item.label}
                      </span>
                    </div>
                  </li>
                );
              })}
            </ol>
          </nav>

          <form
            onSubmit={handleSubmit}
            noValidate
            className="overflow-hidden rounded-3xl border border-white bg-white shadow-[0_24px_70px_rgba(15,118,110,0.10)]"
          >
            <div className="border-b border-slate-100 px-6 py-6 sm:px-9 sm:py-8">
              <p className="text-xs font-semibold uppercase tracking-[0.16em] text-teal-700">
                Step {step} of 3
              </p>
              <h2 className="mt-2 text-2xl font-semibold tracking-[-0.03em] text-slate-950">
                {step === 1 && "Tell us about your practice"}
                {step === 2 && "Where can patients find you?"}
                {step === 3 && "How should people reach you?"}
              </h2>
              <p className="mt-2 text-sm leading-6 text-slate-500">
                {step === 1 && "These details identify your practice across Ayoos."}
                {step === 2 && "Enter the primary location for your practice."}
                {step === 3 && "Confirm your contact information, then review everything below."}
              </p>
            </div>

            <div className="px-6 py-7 sm:px-9 sm:py-9">
              {submitError && (
                <div
                  role="alert"
                  className="mb-6 rounded-2xl border border-rose-200 bg-rose-50 px-4 py-3 text-sm leading-6 text-rose-800"
                >
                  {submitError}
                </div>
              )}

              {step === 1 && (
                <PracticeIdentityFields value={form} errors={errors} onChange={updateField} />
              )}
              {step === 2 && (
                <PracticeAddressFields value={form} errors={errors} onChange={updateField} />
              )}
              {step === 3 && (
                <div className="grid gap-8">
                  <PracticeContactFields value={form} errors={errors} onChange={updateField} />

                  <section className="rounded-2xl border border-teal-100 bg-teal-50/60 p-5 sm:p-6">
                    <div className="flex items-center justify-between gap-4">
                      <div>
                        <p className="text-xs font-semibold uppercase tracking-[0.16em] text-teal-700">
                          Review
                        </p>
                        <h3 className="mt-1 text-lg font-semibold text-slate-900">
                          {form.name || "Your practice"}
                        </h3>
                      </div>
                      <button
                        type="button"
                        onClick={() => setStep(1)}
                        className="rounded-lg px-3 py-2 text-sm font-semibold text-teal-700 transition hover:bg-teal-100 focus:outline-none focus:ring-4 focus:ring-teal-500/15"
                      >
                        Edit details
                      </button>
                    </div>
                    <dl className="mt-5 grid gap-5 border-t border-teal-100 pt-5 text-sm sm:grid-cols-2">
                      <div>
                        <dt className="text-slate-500">Practice URL</dt>
                        <dd className="mt-1 break-all font-medium text-slate-800">
                          /practice/{form.slug || "your-practice"}
                        </dd>
                      </div>
                      <div>
                        <dt className="text-slate-500">Time zone</dt>
                        <dd className="mt-1 font-medium text-slate-800">
                          {form.timeZone.replaceAll("_", " ") || "Not selected"}
                        </dd>
                      </div>
                      <div>
                        <dt className="text-slate-500">Address</dt>
                        <dd className="mt-1 font-medium leading-6 text-slate-800">
                          {form.address.line1}
                          {form.address.line2 && <>, {form.address.line2}</>}
                          <br />
                          {[form.address.city, form.address.state, form.address.postalCode]
                            .filter(Boolean)
                            .join(", ")}
                          <br />
                          {form.address.country}
                        </dd>
                      </div>
                      <div>
                        <dt className="text-slate-500">Contact</dt>
                        <dd className="mt-1 break-words font-medium leading-6 text-slate-800">
                          {form.contactEmail || "No email entered"}
                          <br />
                          {form.contactPhone || "No phone entered"}
                        </dd>
                      </div>
                    </dl>
                  </section>
                </div>
              )}
            </div>

            <div className="flex items-center justify-between gap-4 border-t border-slate-100 bg-slate-50/70 px-6 py-5 sm:px-9">
              {step > 1 ? (
                <button
                  type="button"
                  onClick={() => {
                    setErrors({});
                    setStep((current) => current - 1);
                  }}
                  disabled={isSubmitting}
                  className="rounded-xl px-4 py-3 text-sm font-semibold text-slate-600 transition hover:bg-white hover:text-slate-900 focus:outline-none focus:ring-4 focus:ring-slate-300/40 disabled:opacity-50"
                >
                  Back
                </button>
              ) : (
                <span />
              )}
              <button
                type="submit"
                disabled={isSubmitting}
                className="inline-flex min-w-32 items-center justify-center rounded-xl bg-teal-700 px-5 py-3 text-sm font-semibold text-white shadow-lg shadow-teal-700/15 transition hover:bg-teal-800 focus:outline-none focus:ring-4 focus:ring-teal-500/20 disabled:cursor-not-allowed disabled:opacity-60"
              >
                {isSubmitting ? "Creating…" : step === 3 ? "Create practice" : "Continue"}
              </button>
            </div>
          </form>

          <p className="mt-6 text-center text-sm text-slate-500">
            Your practice information is stored securely in your Ayoos workspace.
          </p>
        </div>
      </div>
    </main>
  );
}
