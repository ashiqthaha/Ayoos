"use client";

import { useEffect } from "react";

import { useAuth } from "@/components/auth-provider";

export default function SilentAuthenticationCallbackPage() {
  const { completeSilentSignIn } = useAuth();

  useEffect(() => {
    void completeSilentSignIn();
  }, [completeSilentSignIn]);

  return null;
}
