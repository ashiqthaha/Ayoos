"use client";

import { useParams, useRouter } from "next/navigation";
import { useEffect, type ReactNode } from "react";

import { GuardLoadingState } from "@/components/auth-guard";
import { useAuth } from "@/components/auth-provider";
import { getPracticeSlug, getRealmRoles } from "@/lib/auth-client";

type RequireRoleProps = {
  children: ReactNode;
  requiredRoles: string | readonly string[];
};

export function RequireRole({ children, requiredRoles }: RequireRoleProps) {
  const params = useParams<{ slug?: string | string[] }>();
  const router = useRouter();
  const { isAuthenticated, isLoading, user } = useAuth();
  const roles = user ? getRealmRoles(user) : [];
  const required = typeof requiredRoles === "string" ? [requiredRoles] : requiredRoles;
  const isAllowed = required.some((role) => roles.includes(role));
  const routeSlug = Array.isArray(params.slug) ? params.slug[0] : params.slug;
  const practiceSlug = routeSlug || (user ? getPracticeSlug(user) : undefined);

  useEffect(() => {
    if (isLoading || !isAuthenticated || isAllowed) {
      return;
    }

    router.replace(
      practiceSlug
        ? `/practice/${encodeURIComponent(practiceSlug)}/dashboard`
        : "/",
    );
  }, [isAllowed, isAuthenticated, isLoading, practiceSlug, router]);

  if (isLoading || !isAuthenticated || !isAllowed) {
    return (
      <GuardLoadingState
        message={isLoading ? "Checking your access…" : "Taking you to your practice dashboard…"}
      />
    );
  }

  return children;
}
