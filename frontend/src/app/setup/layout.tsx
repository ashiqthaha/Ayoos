import type { ReactNode } from "react";

import { AuthGuard } from "@/components/auth-guard";

export default function SetupLayout({ children }: { children: ReactNode }) {
  return <AuthGuard>{children}</AuthGuard>;
}
