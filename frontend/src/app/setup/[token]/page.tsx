"use client";

import { useParams } from "next/navigation";
import { useEffect, useRef, useState } from "react";

import { AyoosMark } from "@/components/ayoos-mark";
import { useAuth } from "@/components/auth-provider";
import { GuardLoadingState } from "@/components/auth-guard";
import { PracticeSetupWizard } from "@/components/practice-setup-wizard";
import {
  ApiError,
  getPracticeInvitation,
  type PracticeInvitationSetup,
} from "@/lib/api";
import { getRealmRoles } from "@/lib/auth-client";

function InvalidInvitationPage() {
  return (
    <main className="grid min-h-screen place-items-center bg-[#f3f8f7] px-6 py-12 text-slate-900">
      <section className="w-full max-w-lg rounded-[2rem] border border-white bg-white p-8 text-center shadow-[0_28px_80px_rgba(15,118,110,0.12)] sm:p-10">
        <div className="flex justify-center">
          <AyoosMark />
        </div>
        <p className="mt-8 text-sm font-semibold text-rose-600">Invitation unavailable</p>
        <h1 className="mt-2 text-3xl font-semibold tracking-[-0.04em] text-slate-950">
          This invitation link is no longer valid
        </h1>
        <p className="mt-4 leading-7 text-slate-600">
          It may have expired, been revoked, or already been used. Ask your Ayoos
          super-admin for help if you still need to set up a practice.
        </p>
      </section>
    </main>
  );
}

export default function InvitedPracticeSetupPage() {
  const params = useParams<{ token: string }>();
  const rawToken = Array.isArray(params.token) ? params.token[0] : params.token;
  const {
    isAuthenticated,
    isLoading: isAuthLoading,
    signIn,
    signOut,
    user,
  } = useAuth();
  const signInStarted = useRef(false);
  const [invitation, setInvitation] = useState<PracticeInvitationSetup | null>(null);
  const [isChecking, setIsChecking] = useState(true);
  const [isInvalid, setIsInvalid] = useState(false);
  const [checkError, setCheckError] = useState<string | null>(null);
  const [createdPracticeSlug, setCreatedPracticeSlug] = useState<string | null>(null);

  useEffect(() => {
    if (!rawToken) {
      setIsInvalid(true);
      setIsChecking(false);
      return;
    }

    const controller = new AbortController();

    async function checkInvitation() {
      try {
        const result = await getPracticeInvitation(rawToken, controller.signal);
        setInvitation(result);
      } catch (error) {
        if (controller.signal.aborted) return;

        if (error instanceof ApiError && (error.status === 404 || error.status === 410)) {
          setIsInvalid(true);
        } else {
          setCheckError(
            error instanceof Error
              ? error.message
              : "We couldn’t check this invitation. Please try again.",
          );
        }
      } finally {
        if (!controller.signal.aborted) setIsChecking(false);
      }
    }

    void checkInvitation();
    return () => controller.abort();
  }, [rawToken]);

  useEffect(() => {
    if (
      isChecking
      || isInvalid
      || checkError
      || !invitation
      || isAuthLoading
      || isAuthenticated
      || signInStarted.current
    ) {
      return;
    }

    signInStarted.current = true;
    void signIn(`/setup/${encodeURIComponent(rawToken)}`).catch(() => {
      signInStarted.current = false;
      setCheckError("We couldn’t start secure sign in. Please try again.");
    });
  }, [
    checkError,
    invitation,
    isAuthLoading,
    isAuthenticated,
    isChecking,
    isInvalid,
    rawToken,
    signIn,
  ]);

  useEffect(() => {
    if (!createdPracticeSlug) return;

    const timeout = window.setTimeout(() => {
      window.location.assign("/logout");
    }, 2200);

    return () => window.clearTimeout(timeout);
  }, [createdPracticeSlug]);

  if (isChecking || (invitation && isAuthLoading)) {
    return <GuardLoadingState message="Checking your invitation…" />;
  }

  if (isInvalid) {
    return <InvalidInvitationPage />;
  }

  if (checkError || !invitation) {
    return (
      <main className="grid min-h-screen place-items-center bg-[#f3f8f7] px-6 text-slate-900">
        <section className="max-w-lg rounded-3xl border border-rose-100 bg-white p-8 text-center shadow-xl shadow-slate-900/5">
          <h1 className="text-2xl font-semibold">Unable to check this invitation</h1>
          <p className="mt-3 leading-7 text-slate-600">{checkError}</p>
          <button
            type="button"
            onClick={() => window.location.reload()}
            className="mt-6 rounded-xl bg-teal-700 px-5 py-3 text-sm font-semibold text-white"
          >
            Try again
          </button>
        </section>
      </main>
    );
  }

  if (!isAuthenticated || !user) {
    return <GuardLoadingState message="Taking you to secure sign in…" />;
  }

  if (!getRealmRoles(user).includes("practice-admin")) {
    return (
      <main className="grid min-h-screen place-items-center bg-[#f3f8f7] px-6 text-slate-900">
        <section className="max-w-lg rounded-3xl border border-amber-100 bg-white p-8 text-center shadow-xl shadow-slate-900/5">
          <h1 className="text-2xl font-semibold">Use the invited admin account</h1>
          <p className="mt-3 leading-7 text-slate-600">
            This link is for {invitation.email}. Sign out and continue with that
            practice-admin account.
          </p>
          <button
            type="button"
            onClick={() => void signOut()}
            className="mt-6 rounded-xl bg-teal-700 px-5 py-3 text-sm font-semibold text-white"
          >
            Sign out
          </button>
        </section>
      </main>
    );
  }

  if (createdPracticeSlug) {
    return (
      <main className="grid min-h-screen place-items-center bg-[#f3f8f7] px-6 text-slate-900">
        <section className="w-full max-w-lg rounded-[2rem] border border-emerald-100 bg-white p-8 text-center shadow-[0_28px_80px_rgba(15,118,110,0.12)] sm:p-10">
          <div className="mx-auto grid h-14 w-14 place-items-center rounded-full bg-emerald-100 text-2xl text-emerald-700">
            ✓
          </div>
          <h1 className="mt-6 text-3xl font-semibold tracking-[-0.04em]">
            Your practice is ready
          </h1>
          <p className="mt-4 leading-7 text-slate-600">
            Setup is complete. This invitation link has been permanently consumed.
            We’re signing you out and returning you to the main login.
          </p>
        </section>
      </main>
    );
  }

  return (
    <PracticeSetupWizard
      rawToken={rawToken}
      invitationEmail={invitation.email}
      onSuccess={setCreatedPracticeSlug}
    />
  );
}
