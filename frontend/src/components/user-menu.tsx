"use client";

import { useRouter } from "next/navigation";

import { useAuth } from "@/components/auth-provider";

export function UserMenu() {
  const router = useRouter();
  const { identity } = useAuth();

  if (!identity) return null;

  return (
    <div className="flex items-center gap-3 rounded-2xl border border-slate-200/80 bg-white/90 px-3 py-2 shadow-sm">
      <div className="hidden min-w-0 text-right sm:block">
        <p className="max-w-48 truncate text-sm font-semibold text-slate-800">
          {identity.name}
        </p>
        <p className="text-xs text-teal-700">{identity.roleLabel}</p>
      </div>
      <button
        type="button"
        onClick={() => router.push("/logout")}
        className="whitespace-nowrap rounded-lg px-2.5 py-1.5 text-xs font-semibold text-slate-600 transition hover:bg-slate-100 hover:text-teal-800 focus:outline-none focus:ring-4 focus:ring-teal-500/15"
      >
        Sign out
      </button>
    </div>
  );
}
