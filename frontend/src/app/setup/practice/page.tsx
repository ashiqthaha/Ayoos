import Link from "next/link";

import { AyoosMark } from "@/components/ayoos-mark";

export default function PracticeSetupRequiresInvitationPage() {
  return (
    <main className="grid min-h-screen place-items-center bg-[#f3f8f7] px-6 py-12 text-slate-900">
      <section className="w-full max-w-lg rounded-[2rem] border border-white bg-white p-8 text-center shadow-[0_28px_80px_rgba(15,118,110,0.12)] sm:p-10">
        <div className="flex justify-center">
          <AyoosMark />
        </div>
        <p className="mt-8 text-sm font-semibold text-teal-700">Invitation required</p>
        <h1 className="mt-2 text-3xl font-semibold tracking-[-0.04em] text-slate-950">
          Practice setup starts from your invitation link
        </h1>
        <p className="mt-4 leading-7 text-slate-600">
          Only an Ayoos super-admin can onboard a practice. Open the unique setup
          link sent for your account to continue.
        </p>
        <Link
          href="/"
          className="mt-7 inline-flex rounded-xl bg-teal-700 px-5 py-3 text-sm font-semibold text-white shadow-lg shadow-teal-700/15 transition hover:bg-teal-800 focus:outline-none focus:ring-4 focus:ring-teal-500/20"
        >
          Return to Ayoos
        </Link>
      </section>
    </main>
  );
}
