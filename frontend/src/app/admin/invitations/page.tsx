"use client";

import { FormEvent, useCallback, useEffect, useState } from "react";

import { AyoosMark } from "@/components/ayoos-mark";
import { AuthGuard } from "@/components/auth-guard";
import { RequireRole } from "@/components/require-role";
import { UserMenu } from "@/components/user-menu";
import {
  ApiError,
  createPracticeInvitation,
  listPracticeInvitations,
  revokePracticeInvitation,
  type PracticeInvitationStatus,
  type PracticeInvitationSummary,
} from "@/lib/api";

const statusClasses: Record<PracticeInvitationStatus, string> = {
  Pending: "border-sky-200 bg-sky-50 text-sky-700",
  Consumed: "border-emerald-200 bg-emerald-50 text-emerald-700",
  Expired: "border-slate-200 bg-slate-100 text-slate-600",
  Revoked: "border-rose-200 bg-rose-50 text-rose-700",
};

function formatDate(value: string) {
  return new Intl.DateTimeFormat(undefined, {
    dateStyle: "medium",
    timeStyle: "short",
  }).format(new Date(value));
}

function InvitationsContent() {
  const [email, setEmail] = useState("");
  const [expiryDays, setExpiryDays] = useState(7);
  const [invitations, setInvitations] = useState<PracticeInvitationSummary[]>([]);
  const [setupUrl, setSetupUrl] = useState<string | null>(null);
  const [isCopied, setIsCopied] = useState(false);
  const [isLoading, setIsLoading] = useState(true);
  const [isCreating, setIsCreating] = useState(false);
  const [revokingId, setRevokingId] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  const loadInvitations = useCallback(async (signal?: AbortSignal) => {
    setIsLoading(true);
    try {
      const result = await listPracticeInvitations(1, 100, signal);
      setInvitations(result.items);
      setError(null);
    } catch (loadError) {
      if (signal?.aborted) return;
      setError(
        loadError instanceof Error
          ? loadError.message
          : "We couldn’t load the invitation list.",
      );
    } finally {
      if (!signal?.aborted) setIsLoading(false);
    }
  }, []);

  useEffect(() => {
    const controller = new AbortController();
    void loadInvitations(controller.signal);
    return () => controller.abort();
  }, [loadInvitations]);

  async function handleCreate(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setIsCreating(true);
    setSetupUrl(null);
    setIsCopied(false);
    setError(null);

    try {
      const result = await createPracticeInvitation(email.trim(), expiryDays);
      setSetupUrl(result.setupUrl);
      setEmail("");
      await loadInvitations();
    } catch (createError) {
      setError(
        createError instanceof ApiError && createError.status === 409
          ? "A Keycloak account already exists for that email."
          : createError instanceof Error
            ? createError.message
            : "We couldn’t create the invitation.",
      );
    } finally {
      setIsCreating(false);
    }
  }

  async function handleCopy() {
    if (!setupUrl) return;
    await navigator.clipboard.writeText(setupUrl);
    setIsCopied(true);
  }

  async function handleRevoke(invitationId: string) {
    setRevokingId(invitationId);
    setError(null);
    try {
      await revokePracticeInvitation(invitationId);
      await loadInvitations();
    } catch (revokeError) {
      setError(
        revokeError instanceof Error
          ? revokeError.message
          : "We couldn’t revoke the invitation.",
      );
    } finally {
      setRevokingId(null);
    }
  }

  return (
    <main className="min-h-screen bg-[#f3f8f7] text-slate-900">
      <div className="mx-auto max-w-6xl px-5 py-6 sm:px-8 sm:py-8">
        <header className="flex items-center justify-between gap-5">
          <AyoosMark />
          <UserMenu />
        </header>

        <div className="mt-12">
          <p className="text-sm font-semibold uppercase tracking-[0.16em] text-teal-700">
            Ayoos administration
          </p>
          <h1 className="mt-2 text-4xl font-semibold tracking-[-0.045em] text-slate-950 sm:text-5xl">
            Practice invitations
          </h1>
          <p className="mt-4 max-w-2xl leading-7 text-slate-600">
            Create the practice-admin identity and its single-use setup link. Raw
            invitation tokens are never stored by Ayoos.
          </p>
        </div>

        {error && (
          <div role="alert" className="mt-7 rounded-2xl border border-rose-200 bg-rose-50 px-4 py-3 text-sm text-rose-800">
            {error}
          </div>
        )}

        <section className="mt-8 rounded-3xl border border-white bg-white p-6 shadow-[0_24px_70px_rgba(15,118,110,0.09)] sm:p-8">
          <h2 className="text-2xl font-semibold tracking-[-0.03em]">Invite a practice admin</h2>
          <form onSubmit={handleCreate} className="mt-6 grid gap-4 md:grid-cols-[minmax(0,1fr)_10rem_auto] md:items-end">
            <label className="grid gap-2 text-sm font-semibold text-slate-700">
              Email address
              <input
                type="email"
                required
                maxLength={320}
                value={email}
                onChange={(event) => setEmail(event.target.value)}
                className="rounded-xl border border-slate-200 bg-white px-4 py-3 font-normal outline-none transition focus:border-teal-500 focus:ring-4 focus:ring-teal-500/10"
                placeholder="admin@practice.com"
              />
            </label>
            <label className="grid gap-2 text-sm font-semibold text-slate-700">
              Expires in days
              <input
                type="number"
                required
                min={1}
                max={30}
                value={expiryDays}
                onChange={(event) => setExpiryDays(Number(event.target.value))}
                className="rounded-xl border border-slate-200 bg-white px-4 py-3 font-normal outline-none transition focus:border-teal-500 focus:ring-4 focus:ring-teal-500/10"
              />
            </label>
            <button
              type="submit"
              disabled={isCreating}
              className="rounded-xl bg-teal-700 px-5 py-3 text-sm font-semibold text-white shadow-lg shadow-teal-700/15 transition hover:bg-teal-800 disabled:cursor-wait disabled:opacity-60"
            >
              {isCreating ? "Creating…" : "Create invitation"}
            </button>
          </form>

          {setupUrl && (
            <div className="mt-6 rounded-2xl border border-amber-200 bg-amber-50 p-5">
              <p className="font-semibold text-amber-900">Copy this setup URL now</p>
              <p className="mt-1 text-sm leading-6 text-amber-800">
                This is the only time the raw link will be shown. It cannot be
                recovered from the invitation list.
              </p>
              <div className="mt-4 flex flex-col gap-3 sm:flex-row">
                <input
                  readOnly
                  value={setupUrl}
                  aria-label="Generated setup URL"
                  className="min-w-0 flex-1 rounded-xl border border-amber-200 bg-white px-4 py-3 text-sm text-slate-700"
                />
                <button
                  type="button"
                  onClick={handleCopy}
                  className="rounded-xl bg-amber-900 px-5 py-3 text-sm font-semibold text-white"
                >
                  {isCopied ? "Copied" : "Copy link"}
                </button>
              </div>
            </div>
          )}
        </section>

        <section className="mt-8 overflow-hidden rounded-3xl border border-white bg-white shadow-[0_24px_70px_rgba(15,118,110,0.09)]">
          <div className="border-b border-slate-100 px-6 py-5 sm:px-8">
            <h2 className="text-xl font-semibold">Invitation history</h2>
          </div>
          {isLoading ? (
            <p className="px-6 py-10 text-center text-sm text-slate-500">Loading invitations…</p>
          ) : invitations.length === 0 ? (
            <p className="px-6 py-10 text-center text-sm text-slate-500">No invitations have been created.</p>
          ) : (
            <div className="overflow-x-auto">
              <table className="w-full min-w-[48rem] text-left text-sm">
                <thead className="bg-slate-50 text-xs uppercase tracking-[0.12em] text-slate-500">
                  <tr>
                    <th className="px-6 py-4 font-semibold">Email</th>
                    <th className="px-6 py-4 font-semibold">Status</th>
                    <th className="px-6 py-4 font-semibold">Created</th>
                    <th className="px-6 py-4 font-semibold">Expires</th>
                    <th className="px-6 py-4 text-right font-semibold">Action</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-100">
                  {invitations.map((invitation) => (
                    <tr key={invitation.id}>
                      <td className="px-6 py-4 font-medium text-slate-800">{invitation.email}</td>
                      <td className="px-6 py-4">
                        <span className={`inline-flex rounded-full border px-2.5 py-1 text-xs font-semibold ${statusClasses[invitation.status]}`}>
                          {invitation.status}
                        </span>
                      </td>
                      <td className="px-6 py-4 text-slate-600">{formatDate(invitation.createdAt)}</td>
                      <td className="px-6 py-4 text-slate-600">{formatDate(invitation.expiresAt)}</td>
                      <td className="px-6 py-4 text-right">
                        {invitation.status === "Pending" && (
                          <button
                            type="button"
                            disabled={revokingId === invitation.id}
                            onClick={() => void handleRevoke(invitation.id)}
                            className="rounded-lg px-3 py-2 font-semibold text-rose-700 transition hover:bg-rose-50 disabled:opacity-50"
                          >
                            {revokingId === invitation.id ? "Revoking…" : "Revoke"}
                          </button>
                        )}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </section>
      </div>
    </main>
  );
}

export default function PracticeInvitationsPage() {
  return (
    <AuthGuard>
      <RequireRole requiredRoles="ayoos-superadmin">
        <InvitationsContent />
      </RequireRole>
    </AuthGuard>
  );
}
