"use client";

import { usePathname, useRouter } from "next/navigation";
import { useEffect, type ReactNode } from "react";

import { useAuth } from "@/components/auth-provider";

export function GuardLoadingState({ message }: { message: string }) {
  return (
    <main className="grid min-h-screen place-items-center bg-[#f3f8f7] px-6 text-slate-700">
      <div className="text-center" role="status">
        <span className="mx-auto block h-9 w-9 animate-spin rounded-full border-4 border-teal-100 border-t-teal-700" />
        <p className="mt-4 text-sm font-medium">{message}</p>
      </div>
    </main>
  );
}

export function AuthGuard({ children }: { children: ReactNode }) {
  const pathname = usePathname();
  const router = useRouter();
  const { isAuthenticated, isLoading } = useAuth();

  useEffect(() => {
    if (!isLoading && !isAuthenticated) {
      router.replace(`/login?returnUrl=${encodeURIComponent(pathname)}`);
    }
  }, [isAuthenticated, isLoading, pathname, router]);

  if (isLoading || !isAuthenticated) {
    return (
      <GuardLoadingState
        message={isLoading ? "Restoring your secure session…" : "Taking you to sign in…"}
      />
    );
  }

  return children;
}
