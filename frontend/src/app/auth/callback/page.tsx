"use client";

import { useRouter } from "next/navigation";
import { useEffect, useState } from "react";

import { useAuth } from "@/components/auth-provider";

function callbackReturnUrl(state: unknown): string {
  if (
    typeof state === "object"
    && state !== null
    && "returnUrl" in state
    && typeof state.returnUrl === "string"
    && state.returnUrl.startsWith("/")
    && !state.returnUrl.startsWith("//")
  ) {
    return state.returnUrl;
  }

  return "/setup/practice";
}

export default function AuthenticationCallbackPage() {
  const router = useRouter();
  const { completeSignIn } = useAuth();
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let active = true;

    async function finishSignIn() {
      try {
        const user = await completeSignIn();
        if (active) router.replace(callbackReturnUrl(user.state));
      } catch {
        if (active) {
          setError("We couldn’t complete sign in. Please return to the login page and try again.");
        }
      }
    }

    void finishSignIn();
    return () => {
      active = false;
    };
  }, [completeSignIn, router]);

  return (
    <main className="grid min-h-screen place-items-center bg-[#f3f8f7] px-6 text-slate-700">
      <section className="max-w-md text-center" role="status">
        {!error && (
          <span className="mx-auto block h-10 w-10 animate-spin rounded-full border-4 border-teal-100 border-t-teal-700" />
        )}
        <h1 className="mt-5 text-2xl font-semibold text-slate-950">
          {error ? "Sign in wasn’t completed" : "Completing sign in"}
        </h1>
        <p className="mt-3 leading-7 text-slate-600">
          {error ?? "Your secure Ayoos session will be ready in a moment."}
        </p>
        {error && (
          <button
            type="button"
            onClick={() => router.replace("/login")}
            className="mt-7 rounded-xl bg-teal-700 px-5 py-3 text-sm font-semibold text-white"
          >
            Return to sign in
          </button>
        )}
      </section>
    </main>
  );
}
