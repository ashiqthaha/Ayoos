"use client";

import { useRouter, useSearchParams } from "next/navigation";
import { Suspense, useEffect, useState } from "react";

import { AyoosMark } from "@/components/ayoos-mark";
import { useAuth } from "@/components/auth-provider";

function safeReturnUrl(value: string | null): string {
  return value?.startsWith("/") && !value.startsWith("//")
    ? value
    : "/setup/practice";
}

function LoginContent() {
  const router = useRouter();
  const searchParams = useSearchParams();
  const { isAuthenticated, isLoading, signIn } = useAuth();
  const [isRedirecting, setIsRedirecting] = useState(false);
  const returnUrl = safeReturnUrl(searchParams.get("returnUrl"));

  useEffect(() => {
    if (!isLoading && isAuthenticated) {
      router.replace(returnUrl);
    }
  }, [isAuthenticated, isLoading, returnUrl, router]);

  async function handleSignIn() {
    setIsRedirecting(true);
    try {
      await signIn(returnUrl);
    } catch {
      setIsRedirecting(false);
    }
  }

  return (
    <main className="relative grid min-h-screen place-items-center overflow-hidden bg-[#f3f8f7] px-6 py-12 text-slate-900">
      <div className="pointer-events-none absolute -right-32 -top-32 h-96 w-96 rounded-full bg-emerald-200/40 blur-3xl" />
      <div className="pointer-events-none absolute -bottom-40 -left-24 h-[28rem] w-[28rem] rounded-full bg-teal-100/60 blur-3xl" />

      <section className="relative w-full max-w-md rounded-[2rem] border border-white bg-white/90 p-7 shadow-[0_28px_80px_rgba(15,118,110,0.12)] backdrop-blur sm:p-10">
        <AyoosMark />
        <p className="mt-10 text-sm font-semibold uppercase tracking-[0.16em] text-teal-700">
          Secure workspace
        </p>
        <h1 className="mt-3 text-4xl font-semibold tracking-[-0.045em] text-slate-950">
          Sign in to Ayoos
        </h1>
        <p className="mt-4 leading-7 text-slate-600">
          Continue through the Ayoos identity service to manage your practice.
        </p>

        <button
          type="button"
          onClick={handleSignIn}
          disabled={isLoading || isRedirecting || isAuthenticated}
          className="mt-8 w-full rounded-xl bg-teal-700 px-5 py-3.5 text-sm font-semibold text-white shadow-lg shadow-teal-700/15 transition hover:bg-teal-800 focus:outline-none focus:ring-4 focus:ring-teal-500/20 disabled:cursor-wait disabled:opacity-60"
        >
          {isLoading
            ? "Checking your session…"
            : isRedirecting || isAuthenticated
              ? "Continuing…"
              : "Sign in"}
        </button>

        <p className="mt-6 text-center text-xs leading-5 text-slate-400">
          Authentication is handled by Keycloak using the authorization code
          flow with PKCE.
        </p>
      </section>
    </main>
  );
}

export default function LoginPage() {
  return (
    <Suspense>
      <LoginContent />
    </Suspense>
  );
}
