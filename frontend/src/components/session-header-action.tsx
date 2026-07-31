"use client";

import Link from "next/link";

import { useAuth } from "@/components/auth-provider";
import { UserMenu } from "@/components/user-menu";

export function SessionHeaderAction() {
  const { isAuthenticated, isLoading } = useAuth();

  if (isLoading) {
    return (
      <span className="h-10 w-24 animate-pulse rounded-xl bg-white/70" />
    );
  }

  if (isAuthenticated) {
    return <UserMenu />;
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
