"use client";

import { useRouter } from "next/navigation";
import { useEffect } from "react";

import { useAuth } from "@/components/auth-provider";

export default function LogoutPage() {
  const router = useRouter();
  const {
    clearSession,
    isAuthenticated,
    isLoading,
    signOut,
  } = useAuth();

  useEffect(() => {
    if (isLoading) return;

    async function finishSignOut() {
      if (!isAuthenticated) {
        await clearSession();
        router.replace("/login");
        return;
      }

      try {
        await signOut();
      } catch {
        await clearSession();
        router.replace("/login");
      }
    }

    void finishSignOut();
  }, [clearSession, isAuthenticated, isLoading, router, signOut]);

  return (
    <main className="grid min-h-screen place-items-center bg-[#f3f8f7] px-6 text-slate-700">
      <div className="text-center" role="status">
        <span className="mx-auto block h-9 w-9 animate-spin rounded-full border-4 border-teal-100 border-t-teal-700" />
        <p className="mt-4 text-sm font-medium">Signing you out…</p>
      </div>
    </main>
  );
}
