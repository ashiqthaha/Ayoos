"use client";

import Link from "next/link";

import { useAuth } from "@/components/auth-provider";
import { UserMenu } from "@/components/user-menu";
import { getRealmRoles } from "@/lib/auth-client";

export function SessionHeaderAction() {
  const { isAuthenticated, isLoading, user } = useAuth();

  if (isLoading) {
    return (
      <span className="h-10 w-24 animate-pulse rounded-xl bg-white/70" />
    );
  }

  if (isAuthenticated) {
    return (
      <div className="flex items-center gap-3">
        {user && getRealmRoles(user).includes("ayoos-superadmin") && (
          <Link
            href="/admin/invitations"
            className="rounded-xl border border-[#176b4d]/20 bg-white/70 px-4 py-2.5 text-sm font-semibold text-[#176b4d] transition hover:bg-white"
          >
            Invitations
          </Link>
        )}
        <UserMenu />
      </div>
    );
  }

  return (
    <Link
      href="/login"
      className="rounded-xl bg-[#176b4d] px-4 py-2.5 text-sm font-semibold text-white shadow-lg shadow-[#176b4d]/15 transition hover:bg-[#12583f] focus:outline-none focus:ring-4 focus:ring-[#176b4d]/20"
    >
      Sign in
    </Link>
  );
}
