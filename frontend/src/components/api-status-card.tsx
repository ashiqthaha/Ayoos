"use client";

import { useEffect, useState } from "react";

import { getApiUrl, getHealthUrl } from "@/lib/api";

type ApiStatus = "checking" | "connected" | "disconnected";

const statusDetails: Record<
  ApiStatus,
  { label: string; detail: string; indicator: string }
> = {
  checking: {
    label: "Checking connection",
    detail: "Contacting the Ayoos API…",
    indicator: "bg-amber-400",
  },
  connected: {
    label: "Connected",
    detail: "The Ayoos API is healthy and ready.",
    indicator: "bg-emerald-500",
  },
  disconnected: {
    label: "Disconnected",
    detail: "Start the backend, then refresh this page.",
    indicator: "bg-rose-500",
  },
};

export function ApiStatusCard() {
  const [status, setStatus] = useState<ApiStatus>("checking");

  useEffect(() => {
    const controller = new AbortController();

    async function checkApi() {
      try {
        const response = await fetch(getHealthUrl(), {
          cache: "no-store",
          signal: controller.signal,
        });

        if (!response.ok) {
          throw new Error(`Health check returned ${response.status}`);
        }

        setStatus("connected");
      } catch (error) {
        if (error instanceof DOMException && error.name === "AbortError") {
          return;
        }

        setStatus("disconnected");
      }
    }

    void checkApi();

    return () => controller.abort();
  }, []);

  const details = statusDetails[status];

  return (
    <aside className="relative overflow-hidden rounded-[2rem] border border-white/80 bg-white/75 p-7 shadow-[0_28px_80px_rgba(33,80,61,0.14)] backdrop-blur-xl sm:p-9">
      <div className="absolute inset-x-0 top-0 h-1 bg-gradient-to-r from-[#176b4d] via-[#58b987] to-[#c7e2b8]" />

      <p className="text-xs font-semibold uppercase tracking-[0.2em] text-[#668176]">
        System status
      </p>
      <div className="mt-8 flex items-start gap-4" aria-live="polite">
        <span className="relative mt-1.5 flex h-3 w-3">
          {status === "checking" && (
            <span className="absolute inline-flex h-full w-full animate-ping rounded-full bg-amber-400 opacity-60" />
          )}
          <span
            className={`relative inline-flex h-3 w-3 rounded-full ${details.indicator}`}
          />
        </span>
        <div>
          <h2 className="text-2xl font-semibold tracking-[-0.03em] text-[#17382d]">
            {details.label}
          </h2>
          <p className="mt-2 leading-6 text-[#597168]">{details.detail}</p>
        </div>
      </div>

      <div className="mt-8 rounded-2xl bg-[#eff6ef] p-4">
        <p className="text-xs font-medium uppercase tracking-[0.15em] text-[#789085]">
          API endpoint
        </p>
        <p className="mt-2 break-all font-mono text-sm text-[#315a49]">
          {getApiUrl()}
        </p>
      </div>
    </aside>
  );
}
